using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Dict = Godot.Collections.Dictionary;
namespace QuickDev.Level.Tests;

/// <summary>Dependency-free headless parity and regression suite. Exit code is nonzero on failure.</summary>
public partial class QuickLevelKitTests : Node
{
    private int _checks, _failures;
    private const string Root = "res://addons/quick_level_kit/csharp/";
    private const string GraphPath = "user://quick_level_kit_csharp_graph.tres";
    private const string SavePath = "user://quick_level_kit_csharp_tests.cfg";
    public override void _Ready() => Callable.From(Run).CallDeferred();
    private void Check(bool condition, string description)
    { _checks++; if (!condition) { _failures++; GD.PrintErr("FAIL: " + description); } }
    private void Case(string name, Action action)
    { try { action(); GD.Print("CASE: " + name); } catch (Exception ex) { _failures++; GD.PrintErr($"FAIL: {name}: {ex}"); } }
    private static Dict Payload(string type) => new() { ["type"] = type, ["data"] = new Dict() };
    private static QuickLevelKitRoute Route(string type, string target) => new() { ContextType = type, TargetStageId = target };
    private static QuickStageTestProbe Stage(string id, params QuickLevelKitRoute[] routes) => new() { StageId = id, Routes = new Array<QuickLevelKitRoute>(routes) };
    private static QuickLevelKitFlow Flow(params QuickLevelKitStage[] stages) => new() { StartStageId = stages[0].StageId, Stages = new Array<QuickLevelKitStage>(stages) };
    private static QuickLevelKitCatalog Catalog(bool autoComplete = false)
    {
        var result = new QuickLevelKitCatalog { DefaultFlow = QuickLevelKitDefaultFlow.Create() };
        for (int i = 1; i <= 3; i++) result.Levels.Add(new QuickLevelKitDefinition
        { LevelId = $"level_00{i}", Scene = GD.Load<PackedScene>(Root + "tests/fixtures/" + (autoComplete ? "QuickLevelAutoComplete.tscn" : "QuickLevelTestProbe.tscn")) });
        return result;
    }
    private QuickLevelKitManager Manager(bool autoComplete = false)
    {
        var manager = new QuickLevelKitManager();
        manager.SetProgressStore(new QuickLevelKitProgressStore());
        AddChild(manager); manager.SetCatalog(Catalog(autoComplete)); manager.ResetProgress(); return manager;
    }
    private void Run()
    {
        var args = OS.GetCmdlineUserArgs();
        if (args.Contains("save-write")) Case("cross-process write", WriteSave);
        else if (args.Contains("save-read")) Case("cross-process read", ReadSave);
        else
        {
            Case("catalog navigation and validation", TestCatalog);
            Case("routes and flow validation", TestFlow);
            Case("level lifecycle and signals", TestLevel);
            Case("sessions and synchronous transitions", TestSession);
            Case("reentrant session stop", TestReentrantSession);
            Case("runtime isolation and delayed preparation disconnect", TestIsolation);
            Case("progress copying and malformed save data", TestProgress);
            Case("manager load, advance, reload, final, rejection, reset", TestManager);
            Case("manager synchronous completion and callbacks", TestManagerReentrancy);
            Case("manager delayed stage changes and progress replacement", TestManagerDelayedState);
            Case("editor catalog operations", TestDock);
            Case("package and sample", TestPackage);
            Case("typed resource graph round trip", TestResourceGraph);
        }
        GD.Print($"Quick Level Kit C# tests: {_checks} checks, {_failures} failures.");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }
    private void TestCatalog()
    {
        var c = Catalog();
        Check(c.GetLevelCount() == 3 && c.GetFirstLevelId() == "level_001" && c.GetLastLevelId() == "level_003", "ordered catalog");
        Check(c.FindIndex("level_002") == 1 && c.FindIndex("missing") == -1, "lookup");
        Check(c.GetDefinitionAt(-1) == null && c.GetDefinitionAt(3) == null, "bounds");
        Check(c.ResolveNext("level_001")["status"].AsInt32() == 0 && c.ResolveNext("level_001")["level_id"].AsString() == "level_002", "next available");
        Check(c.ResolveNext("level_003")["status"].AsInt32() == 1 && c.ResolveNext("missing")["status"].AsInt32() == 2, "final and unknown");
        Check(c.Validate(), "valid catalog");
        c.Levels[0].LevelId = ""; Check(!c.Validate() && c.GetValidationErrors()[0].ContainsKey("code"), "empty id structured error");
        c = Catalog(); c.Levels[1].LevelId = "level_001"; Check(!c.Validate(), "duplicate id");
        c = Catalog(); c.Levels[0].Scene = null; Check(!c.Validate(), "missing scene");
        c = Catalog(); c.Levels.Add(null!); Check(!c.Validate(), "null definition");
        c = Catalog(); var badRoot = new Node(); var packed = new PackedScene(); packed.Pack(badRoot); badRoot.Free(); c.Levels[0].Scene = packed; Check(!c.Validate(), "wrong scene root");
        c = Catalog(); Check(c.GetFlowFor("level_001") == c.DefaultFlow, "default flow");
        var custom = new QuickLevelKitFlow(); c.Levels[0].Flow = custom; Check(c.GetFlowFor("level_001") == custom, "definition overrides flow");
    }
    private void TestFlow()
    {
        var r = Route("completed", "advance");
        Check(r.Matches(Payload("completed")) && !r.Matches(Payload("failed")), "route type");
        Check(new QuickLevelKitRoute().Matches(Payload("anything")), "wildcard");
        r.RequiredPayloadKeys = new[] { "score" }; Check(!r.Matches(Payload("completed")), "required key absent");
        var p = Payload("completed"); p["data"] = new Dict { ["score"] = 10 }; Check(r.Matches(p), "required key present");
        p["data"] = 2; Check(!r.Matches(p), "wrong payload shape");
        var s = Stage("run", Route("go", "finish"), Route("go", "other"));
        Check(s.ResolveRoute(Payload("go"))!.TargetStageId == "finish" && s.ResolveRoute(Payload("unknown")) == null, "first route wins");
        var f = Flow(Stage("start", Route("go", "finish")), Stage("finish"));
        Check(f.Validate() && f.HasStage("finish") && f.GetStage("ghost") == null, "valid flow lookup");
        f.StartStageId = "ghost"; Check(!f.Validate(), "unknown start"); f.StartStageId = "start";
        f.Stages[0].Routes[0].TargetStageId = "ghost"; Check(!f.Validate(), "dangling route");
        f.Stages[0].Routes[0].TargetStageId = ""; Check(!f.Validate(), "empty route");
        Check(!Flow(Stage("same"), Stage("same")).Validate(), "duplicate stage");
        Check(!Flow(Stage("")).Validate(), "empty stage");
        var d = QuickLevelKitDefaultFlow.Create(); Check(d.Validate() && d.StartStageId == "prepare", "default valid");
        foreach (var id in new[] { "prepare", "run", "advance", "reload", "stop" }) Check(d.HasStage(id), "default stage " + id);
        foreach (var pair in new[] { ("completed", "advance"), ("failed", "reload"), ("retry", "reload") }) Check(d.GetStage("run")!.ResolveRoute(Payload(pair.Item1))!.TargetStageId == pair.Item2, "default " + pair.Item1);
        Check(d.GetStage("prepare")!.ResolveRoute(Payload("ready"))!.TargetStageId == "run", "ready route");
        Check(d.GetStage("advance")!.ResolveRoute(Payload("finished"))!.TargetStageId == "stop", "finished route");
    }
    private void TestLevel()
    {
        var level = new QuickLevelTestProbe { AutoPrepare = false }; int prepared = 0; var emitted = new System.Collections.Generic.List<Dict>();
        level.PreparationCompleted += () => prepared++; level.TransitionRequested += p => emitted.Add(p);
        level.CompletePreparation(); Check(prepared == 0, "preparation cannot finish before beginning");
        level.BeginPreparation(); level.CompletePreparation(); level.CompletePreparation(); Check(prepared == 1, "preparation signal once");
        level.BeginRunning(); level.Complete(new Dict { ["score"] = 9 });
        Check(string.Join(",", level.Events) == "prepare,start,finish", "lifecycle hooks");
        Check(emitted[0]["type"].AsString() == "completed" && emitted[0]["data"].AsGodotDictionary()["score"].AsInt32() == 9, "unified payload");
        level.Fail(); level.Retry(); Check(emitted[1]["type"].AsString() == "failed" && emitted[2]["type"].AsString() == "retry", "fail retry payload");
        level.Teardown(); level.Teardown(); level.BeginPreparation(); level.BeginRunning(); level.Complete();
        Check(level.Events.Count(x => x == "teardown") == 1 && emitted.Count == 3, "teardown idempotent and suppresses later calls"); level.Free();
    }
    private void TestSession()
    {
        var a = Stage("a", Route("go", "b")); var b = Stage("b"); var context = new QuickLevelKitContext(); context.SetValue("score", 3);
        var session = new QuickLevelKitSession(Flow(a, b), context); int rejects = 0, stops = 0;
        session.TransitionRejected += _ => rejects++; session.SessionStopped += () => stops++;
        session.Start(); session.Start(); Check(a.Entered == 1 && session.GetCurrentStageId() == "a", "start once");
        Check(!session.RequestTransition(Payload("unknown")) && rejects == 1, "unmatched rejected");
        Check(session.RequestTransition(Payload("go")) && session.GetCurrentStageId() == "b" && a.Exited == 1, "transition exits and enters");
        Check(b.LastRuntime.Session.Context.GetValue("score").AsInt32() == 3, "persistent shared context");
        session.Stop(); session.Stop(); Check(!session.IsActive() && !session.RequestTransition(Payload("go")) && stops == 1, "stop once and reject");
        a = Stage("a", Route("go", "b")); b = Stage("b", Route("go", "c"));
        a.EnterAction = rt => rt.Session.RequestTransition(Payload("go")); b.EnterAction = a.EnterAction;
        session = new QuickLevelKitSession(Flow(a, b, Stage("c")), new QuickLevelKitContext()); session.Start();
        Check(session.GetCurrentStageId() == "c" && a.Exited == 1 && b.Exited == 1, "synchronous enter transition chain"); session.Stop();
    }
    private void TestReentrantSession()
    {
        var a = Stage("a", Route("go", "b")); var b = Stage("b");
        a.ExitAction = rt => rt.Session.Stop();
        var session = new QuickLevelKitSession(Flow(a, b), new QuickLevelKitContext()); session.Start(); session.RequestTransition(Payload("go"));
        Check(!session.IsActive() && b.Entered == 0 && a.Exited == 1, "stop inside OnExit must not recurse or enter target");
        a = Stage("a"); a.EnterAction = rt => rt.Session.Stop(); session = new QuickLevelKitSession(Flow(a), new QuickLevelKitContext()); session.Start();
        Check(!session.IsActive() && a.Exited == 1, "stop inside OnEnter");
        a = Stage("a", Route("go", "b")); b = Stage("b");
        bool restarted = false;
        a.ExitAction = rt => { if (!restarted) { restarted = true; rt.Session.Stop(); rt.Session.Start(); } };
        session = new QuickLevelKitSession(Flow(a, b), new QuickLevelKitContext()); session.Start();
        var oldRuntime = a.LastRuntime; session.RequestTransition(Payload("go"));
        Check(session.IsActive() && session.GetCurrentStageId() == "a" && a.Entered == 2 && b.Entered == 0, "OnExit stop/start preserves restarted stage");
        Check(a.LastRuntime != oldRuntime && session.GetCurrentStage() == a, "old transition cannot replace restarted runtime");
        a.ExitAction = null; session.Stop();
        a = Stage("a", Route("go", "b")); b = Stage("b");
        a.ExitAction = rt => rt.Session.Stop();
        session = new QuickLevelKitSession(Flow(a, b), new QuickLevelKitContext());
        var restartSession = session; bool restartFromSignal = true;
        session.SessionStopped += () => { if (restartFromSignal) { restartFromSignal = false; restartSession.Start(); } };
        session.Start(); session.RequestTransition(Payload("go"));
        Check(session.IsActive() && session.GetCurrentStageId() == "a" && a.Entered == 2 && b.Entered == 0, "SessionStopped restart cannot be overwritten by old transition");
        a.ExitAction = null; session.Stop();
        a = Stage("a", Route("go", "b")); b = Stage("b");
        bool restartFromExit = true;
        a.ExitAction = rt => { if (restartFromExit) { restartFromExit = false; rt.Session.Start(); } };
        a.EnterAction = rt => { if (a.Entered == 2) rt.Session.RequestTransition(Payload("go")); };
        session = new QuickLevelKitSession(Flow(a, b), new QuickLevelKitContext());
        session.Start(); session.Stop();
        Check(session.IsActive() && session.GetCurrentStageId() == "b", "external Stop drains transitions queued during OnExit restart");
        session.Stop();
    }
    private static System.Collections.Generic.HashSet<Callable> PreparationConnections(QuickLevelKitLevel level) =>
        level.GetSignalConnectionList(QuickLevelKitLevel.SignalName.PreparationCompleted)
            .Select(connection => connection["callable"].AsCallable()).ToHashSet();
    private void TestIsolation()
    {
        var stage = Stage("a"); var shared = Flow(stage);
        var one = new QuickLevelKitSession(shared, new QuickLevelKitContext()); var two = new QuickLevelKitSession(shared, new QuickLevelKitContext());
        one.Start(); var runtimeOne = stage.LastRuntime; runtimeOne.State["token"] = 4;
        two.Start(); Check(stage.LastRuntime != runtimeOne && !stage.LastRuntime.State.ContainsKey("token"), "shared stage has per-session runtime state");
        one.Context.SetValue("private", 2); Check(two.Context.GetValue("private").VariantType == Variant.Type.Nil, "context dictionaries isolated"); one.Stop(); two.Stop();
        var level = new QuickLevelTestProbe { AutoPrepare = false };
        var prepare = new QuickLevelKitPrepareStage { StageId = "prepare", Routes = new Array<QuickLevelKitRoute> { Route("ready", "run") } };
        var session = new QuickLevelKitSession(Flow(prepare, Stage("run")), new QuickLevelKitContext { Level = level });
        // Godot installs a permanent C# EventSignalMiddleman bridge. Preserve that exact baseline,
        // but require the stage's own newly connected callable to disappear on Stop.
        var baseline = PreparationConnections(level);
        session.Start(); var started = PreparationConnections(level);
        Check(started.IsSupersetOf(baseline) && started.Count == baseline.Count + 1, "exactly one preparation handler installed");
        var stageConnections = started.Except(baseline).ToArray();
        session.Stop(); var stopped = PreparationConnections(level);
        Check(stopped.SetEquals(baseline) && stageConnections.All(callable => !level.IsConnected(QuickLevelKitLevel.SignalName.PreparationCompleted, callable)), "stop disconnects delayed preparation and preserves original bridge identities");
        level.CompletePreparation(); Check(!session.IsActive(), "late completion cannot revive stopped session"); level.Free();
    }
    private void TestProgress()
    {
        var p = new QuickLevelKitProgress { CurrentLevelId = "level_002", HighestUnlockedLevelId = "level_003" };
        var state = new Dict { ["nested"] = new Dict { ["stars"] = 3 } }; p.SetLevelState("level_001", state);
        state["nested"].AsGodotDictionary()["stars"] = 99;
        Check(p.GetLevelState("level_001")["nested"].AsGodotDictionary()["stars"].AsInt32() == 3, "setter deep copy");
        var copy = p.DuplicateProgress(); var exported = copy.ToDictionary(); exported["level_states"].AsGodotDictionary().Clear();
        Check(p.GetLevelState("level_001").Count == 1 && copy.GetLevelState("level_001").Count == 1, "duplicate and serialization isolated");
        var got = p.GetLevelState("level_001"); got.Clear(); Check(p.GetLevelState("level_001").Count == 1, "getter copy");
        var restored = new QuickLevelKitProgress(); restored.FromDictionary(p.ToDictionary());
        Check(restored.CurrentLevelId == "level_002" && restored.HighestUnlockedLevelId == "level_003", "dictionary round trip");
        foreach (var malformed in new Variant[] { 7, "wrong", new Godot.Collections.Array(), default })
        {
            restored.FromDictionary(new Dict { ["level_states"] = malformed });
            Check(restored.LevelStates.Count == 0, "malformed level_states safely defaults: " + malformed.VariantType);
        }
        var store = new QuickLevelKitConfigFileProgressStore(SavePath); store.Clear();
        Check(store.LoadProgress().CurrentLevelId == "", "missing save defaults");
        Check(store.SaveProgress(p) == Error.Ok, "save succeeds");
        Check(new QuickLevelKitConfigFileProgressStore(SavePath).LoadProgress().GetLevelState("level_001").Count == 1, "new store reads saved state");
        foreach (var malformed in new Variant[] { 5, "wrong", new Godot.Collections.Array(), new Dict { ["level_states"] = 12 } })
        {
            var config = new ConfigFile(); config.SetValue("quick_level_kit", "progress", malformed); config.Save(SavePath);
            Check(store.LoadProgress().LevelStates.Count == 0, "wrong persisted shape safe");
        }
        using (var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write)) file.StoreString("not valid [config");
        Check(store.LoadProgress().CurrentLevelId == "", "corrupt config defaults");
        Check(store.Clear() == Error.Ok && store.Clear() == Error.Ok && !FileAccess.FileExists(SavePath), "clear idempotent");
    }
    private void TestManager()
    {
        var m = Manager();
        try
        {
            var events = new System.Collections.Generic.List<string>(); int rejects = 0;
            m.LevelLoaded += _ => events.Add("loaded"); m.SessionStarted += id => events.Add("started:" + id); m.TransitionRejected += _ => rejects++;
            Check(m.LoadFirstLevel() && m.GetCurrentLevelId() == "level_001" && m.GetCurrentStageId() == "run", "first load synchronous prepare");
            Check(string.Join(",", events) == "loaded,started:level_001", "load signal order");
            Check(!m.RequestTransition(Payload("unknown")) && rejects == 1, "manager rejected signal");
            m.GetCurrentLevel()!.Complete(); Check(m.GetCurrentLevelId() == "level_002" && m.IsLevelUnlocked("level_002"), "advance and unlock");
            Check(m.GetProgress().HighestUnlockedLevelId == "level_002", "advance updates progress");
            var previous = m.GetCurrentLevel(); previous!.Fail(); Check(m.GetCurrentLevel() != previous && m.GetCurrentLevelId() == "level_002", "fail reloads new instance");
            previous = m.GetCurrentLevel(); previous!.Retry(); Check(m.GetCurrentLevel() != previous, "retry reloads new instance");
            m.LoadLevel("level_003"); m.GetCurrentLevel()!.Complete(); Check(!m.IsFlowActive(), "final completion stops");
            m.GetProgress().SetLevelState("level_002", new Dict { ["done"] = true });
            Check(m.ResetProgress() && m.GetProgress().HighestUnlockedLevelId == "level_001" && m.GetProgress().LevelStates.Count == 0, "reset progress");
            m.UnlockLevel("level_003"); Check(m.ContinueGame() && m.GetCurrentLevelId() == "level_003", "continue highest unlocked");
            Check(!m.UnlockLevel("missing") && !m.UnlockLevel("level_002"), "invalid/backward unlock rejected");
            Check(!m.LoadLevel("missing") && m.GetCurrentLevelId() == "level_003", "invalid load preserves existing level");
            var host = new Node(); Check(!m.ConfigureHosts(host, host) && !m.ConfigureHosts(null!, host), "host validation"); host.Free();
        }
        finally { m.FinishFlow(); m.Free(); }
    }
    private void TestManagerReentrancy()
    {
        var m = Manager(true);
        try { Check(m.LoadFirstLevel(), "synchronous completion accepted"); Check(!m.IsFlowActive() && m.GetProgress().HighestUnlockedLevelId == "level_003", "sync complete advances all levels and stops without stale session"); }
        finally { m.FinishFlow(); m.Free(); }
        m = Manager();
        try
        {
            m.LevelLoaded += _ => m.FinishFlow(); m.LoadFirstLevel();
            Check(!m.IsFlowActive() && m.GetCurrentLevel() == null, "reentrant stop from LevelLoaded");
        }
        finally { m.FinishFlow(); m.Free(); }
        m = Manager();
        try
        {
            bool once = false; m.SessionStarted += _ => { if (!once) { once = true; m.LoadLevel("level_002"); } };
            m.LoadFirstLevel(); Check(m.GetCurrentLevelId() == "level_002" && m.GetCurrentStageId() == "run", "reentrant load from SessionStarted preserves newer session");
        }
        finally { m.FinishFlow(); m.Free(); }
    }
    private void TestManagerDelayedState()
    {
        var manager = Manager();
        try
        {
            var catalog = Catalog();
            catalog.Levels[0].Scene = GD.Load<PackedScene>(Root + "tests/fixtures/QuickLevelDelayedPrepare.tscn");
            var observed = new System.Collections.Generic.List<string>();
            manager.StageChanged += (previous, current) => observed.Add($"{previous}>{current}");
            manager.SetCatalog(catalog);
            Check(manager.LoadFirstLevel() && manager.GetCurrentStageId() == "prepare", "delayed level remains preparing");
            Check(observed.SequenceEqual(new[] { ">prepare" }), "initial stage change emitted exactly once");
            manager.GetCurrentLevel()!.CompletePreparation();
            Check(manager.GetCurrentStageId() == "run" && observed.SequenceEqual(new[] { ">prepare", "prepare>run" }), "delayed preparation forwards exactly one manager stage change");
            manager.GetCurrentLevel()!.CompletePreparation();
            Check(observed.Count == 2, "duplicate preparation cannot repeat stage notification");
            manager.FinishFlow();
            var stage = Stage("observe"); catalog.DefaultFlow = Flow(stage); catalog.Levels[0].Flow = null;
            manager.LoadFirstLevel(); var context = stage.LastRuntime.Session.Context; var previousProgress = context.Progress;
            manager.SetProgressStore(new QuickLevelKitProgressStore());
            Check(manager.GetProgress() != previousProgress && context.Progress == manager.GetProgress(), "replacing progress store updates active context reference");
            manager.GetProgress().SetLevelState("level_001", new Dict { ["replacement"] = true });
            Check(context.Progress!.GetLevelState("level_001")["replacement"].AsBool(), "active stage observes replacement progress writes");
            manager.ResetProgress();
            Check(context.Progress == manager.GetProgress() && context.Progress!.LevelStates.Count == 0, "reset keeps active context progress synchronized");
        }
        finally { manager.FinishFlow(); manager.Free(); }
    }
    private void TestDock()
    {
        // Reflection keeps runtime exports usable when the editor-only dock is excluded in release builds.
        var type = typeof(QuickLevelKitManager).Assembly.GetTypes().FirstOrDefault(t => t.Name == "QuickLevelKitDock");
        Check(type != null, "dock included in editor/debug assembly"); if (type == null) return;
        var dock = (GodotObject)Activator.CreateInstance(type)!;
        object Invoke(string name, params object[] args) => type.GetMethod(name)!.Invoke(dock, args)!;
        try
        {
            var catalog = new QuickLevelKitCatalog(); Invoke("SetCatalog", catalog);
            var scene = GD.Load<PackedScene>(Root + "tests/fixtures/QuickLevelTestProbe.tscn");
            Check((bool)Invoke("AddLevel", new StringName("first"), scene), "dock add first");
            Check((bool)Invoke("AddLevel", new StringName("second"), scene), "dock add second");
            Check((bool)Invoke("MoveLevel", 1, -1) && catalog.Levels[0].LevelId == "second", "dock reorder");
            Check(!(bool)Invoke("MoveLevel", 0, -1), "dock move bounds");
            Check((bool)Invoke("RemoveLevel", 0) && catalog.GetLevelCount() == 1, "dock remove");
            var errors = (Array<Dict>)Invoke("ValidateCatalog"); Check(errors.Count == 0, "dock validation");
            var flow = (QuickLevelKitFlow)Invoke("CreateDefaultFlowFor", 0); Check(flow != null && flow.Validate() && catalog.Levels[0].Flow == flow, "dock creates default flow");
        }
        finally { dock.Free(); }
    }
    private void TestPackage()
    {
        var cfg = new ConfigFile(); Check(cfg.Load(Root + "plugin.cfg") == Error.Ok, "plugin config exists");
        var script = cfg.GetValue("plugin", "script", "").AsString(); Check(script.EndsWith(".cs") && FileAccess.FileExists(Root + script), "C# plugin entry exists");
        Check(ResourceLoader.Load<PackedScene>(Root + "examples/BasicUsage.tscn") != null, "sample scene loads");
    }
    private QuickLevelKitCatalog WriteResourceGraph()
    {
        var catalog = Catalog();
        catalog.Levels[0].DisplayName = "First level / 第一关";
        catalog.Levels[0].Metadata = new Dict
        {
            ["difficulty"] = 2,
            ["nested"] = new Dict { ["tags"] = new Godot.Collections.Array { "tutorial", "forest" } }
        };
        var entry = new QuickLevelKitStage { StageId = "custom_start", Routes = new Array<QuickLevelKitRoute>
        {
            new() { ContextType = "scored", TargetStageId = "custom_end", RequiredPayloadKeys = new[] { "score", "stars" } },
            new() { ContextType = "", TargetStageId = "custom_end" }
        } };
        catalog.Levels[0].Flow = Flow(entry, new QuickLevelKitStage { StageId = "custom_end" });
        catalog.Levels[1].Flow = catalog.DefaultFlow; // Shared subresource identity must also survive.
        Check(ResourceSaver.Save(catalog, GraphPath) == Error.Ok, "typed catalog graph save");
        return catalog;
    }
    private QuickLevelKitCatalog ReadResourceGraph()
    {
        var resource = ResourceLoader.Load(GraphPath, cacheMode: ResourceLoader.CacheMode.Ignore);
        Check(resource is QuickLevelKitCatalog, "catalog C# script type survives without GlobalClass");
        if (resource is not QuickLevelKitCatalog catalog) throw new InvalidOperationException("Saved graph lost catalog script type");
        Check(catalog.Levels.Count == 3 && catalog.Levels.All(d => d.GetType() == typeof(QuickLevelKitDefinition)), "typed definition array survives");
        Check(catalog.Levels.Select(d => d.LevelId.ToString()).SequenceEqual(new[] { "level_001", "level_002", "level_003" }), "definition ids and ordering survive");
        var definition = catalog.Levels[0];
        Check(definition.DisplayName == "First level / 第一关", "definition display name survives");
        Check(definition.Scene != null && definition.Scene.ResourcePath == Root + "tests/fixtures/QuickLevelTestProbe.tscn", "scene reference survives");
        var level = definition.Scene!.Instantiate();
        Check(level is QuickLevelTestProbe, "saved scene reference instantiates native C# level"); level.Free();
        Check(definition.Metadata["difficulty"].AsInt32() == 2 && definition.Metadata["nested"].AsGodotDictionary()["tags"].AsGodotArray()[1].AsString() == "forest", "nested metadata survives");
        var flow = definition.Flow ?? throw new InvalidOperationException("Definition flow missing");
        Check(flow.GetType() == typeof(QuickLevelKitFlow) && flow.StartStageId == "custom_start" && flow.Stages.Count == 2, "flow type and exports survive");
        Check(flow.Stages.All(stage => stage.GetType() == typeof(QuickLevelKitStage)) && flow.Stages[1].StageId == "custom_end", "base stage script type and ids survive");
        var routes = flow.Stages[0].Routes;
        Check(routes.Count == 2 && routes.All(route => route.GetType() == typeof(QuickLevelKitRoute)), "route types and ordering survive");
        Check(routes[0].ContextType == "scored" && routes[0].TargetStageId == "custom_end" && routes[0].RequiredPayloadKeys.SequenceEqual(new[] { "score", "stars" }), "all route exports survive");
        Check(routes[1].ContextType == "" && routes[1].RequiredPayloadKeys.Length == 0, "wildcard defaults survive");
        Check(flow.Validate() && flow.Stages[0].ResolveRoute(new Dict { ["type"] = "scored", ["data"] = new Dict { ["score"] = 10, ["stars"] = 3 } }) == routes[0], "loaded custom graph remains executable");
        var defaults = catalog.DefaultFlow ?? throw new InvalidOperationException("Default flow missing");
        Check(defaults.StartStageId == "prepare" && defaults.Stages.Count == 5 && defaults.Validate(), "default flow graph survives");
        Check(defaults.GetStage("prepare") is QuickLevelKitPrepareStage && defaults.GetStage("run") is QuickLevelKitRunStage && defaults.GetStage("advance") is QuickLevelKitAdvanceStage && defaults.GetStage("reload") is QuickLevelKitReloadStage && defaults.GetStage("stop") is QuickLevelKitStopStage, "all five derived stage C# types survive");
        Check(catalog.Levels[1].Flow == defaults && catalog.Levels[2].Flow == null, "shared resource identity and null override survive");
        Check(catalog.Validate(), "loaded catalog validates");
        return catalog;
    }
    private void TestResourceGraph()
    {
        var original = WriteResourceGraph(); var loaded = ReadResourceGraph();
        Check(original != loaded && original.Levels[0] != loaded.Levels[0], "Ignore cache loads distinct resource graph");
        loaded.Levels[0].DisplayName = "Edited after load";
        Check(original.Levels[0].DisplayName != loaded.Levels[0].DisplayName, "loaded exported values remain independently editable");
        Check(DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GraphPath)) == Error.Ok, "resource graph cleanup");
    }
    private void WriteSave()
    {
        var store = new QuickLevelKitConfigFileProgressStore(SavePath); store.Clear();
        var p = new QuickLevelKitProgress { CurrentLevelId = "level_002", HighestUnlockedLevelId = "level_003" };
        p.SetLevelState("level_001", new Dict { ["deaths"] = 7, ["nested"] = new Dict { ["stars"] = 3 } });
        Check(store.SaveProgress(p) == Error.Ok, "cross-process save");
        WriteResourceGraph();
    }
    private void ReadSave()
    {
        var store = new QuickLevelKitConfigFileProgressStore(SavePath); var p = store.LoadProgress();
        Check(p.CurrentLevelId == "level_002" && p.HighestUnlockedLevelId == "level_003", "cross-process ids");
        Check(p.GetLevelState("level_001")["deaths"].AsInt32() == 7 && p.GetLevelState("level_001")["nested"].AsGodotDictionary()["stars"].AsInt32() == 3, "cross-process nested state");
        Check(store.Clear() == Error.Ok, "cross-process cleanup");
        ReadResourceGraph();
        Check(DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GraphPath)) == Error.Ok, "cross-process graph cleanup");
    }
}
