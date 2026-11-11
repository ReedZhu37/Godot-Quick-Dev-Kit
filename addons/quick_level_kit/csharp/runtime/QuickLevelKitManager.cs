using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitManager : Node
{
    [Signal] public delegate void SessionStartedEventHandler(StringName levelId);
    [Signal] public delegate void SessionStoppedEventHandler(StringName levelId);
    [Signal] public delegate void StageChangedEventHandler(StringName previousId, StringName currentId);
    [Signal] public delegate void LevelLoadedEventHandler(QuickLevelKitLevel level);
    [Signal] public delegate void TransitionRejectedEventHandler(Dictionary context);
    [Signal] public delegate void ProgressChangedEventHandler(QuickLevelKitProgress progress);
    [Signal] public delegate void FlowErrorEventHandler(StringName code, string message);
    public static readonly StringName ErrorCatalogMissing = "catalog_missing", ErrorCatalogInvalid = "catalog_invalid",
        ErrorLevelUnknown = "level_unknown", ErrorSceneInvalid = "scene_invalid", ErrorRootNotLevel = "root_not_level", ErrorNoSession = "no_session";
    [Export] public QuickLevelKitCatalog? Catalog { get; set; }
    private QuickLevelKitProgressStore? _progressStore;
    private QuickLevelKitProgress _progress = new();
    private Node? _container, _activeHost, _stagingHost;
    private QuickLevelKitSession? _session;
    private QuickLevelKitLevel? _currentLevel;
    private StringName _currentLevelId = "";
    private ulong _generation;
    private bool _exiting;

    public override void _Ready()
    {
        _exiting = false;
        _progressStore ??= new QuickLevelKitConfigFileProgressStore();
        EnsureDefaultHosts();
        _progress = _progressStore.LoadProgress();
    }
    public override void _ExitTree() { _exiting = true; FinishFlow(); }
    public bool ConfigureHosts(Node activeHost, Node stagingHost)
    {
        if (!IsInstanceValid(activeHost) || !IsInstanceValid(stagingHost) || activeHost == stagingHost) return false;
        _activeHost = activeHost;
        _stagingHost = stagingHost;
        return true;
    }
    public void SetCatalog(QuickLevelKitCatalog? value) => Catalog = value;
    public QuickLevelKitCatalog? GetCatalog() => Catalog;
    public void SetProgressStore(QuickLevelKitProgressStore? store)
    {
        if (store == null) return;
        _progressStore = store;
        _progress = store.LoadProgress();
        if (_session != null) _session.Context.Progress = _progress;
    }
    public QuickLevelKitProgressStore? GetProgressStore() => _progressStore;
    public QuickLevelKitProgress GetProgress() => _progress;
    public Error SaveProgress() => _progressStore?.SaveProgress(_progress) ?? Error.Unconfigured;
    public QuickLevelKitLevel? GetCurrentLevel() => IsInstanceValid(_currentLevel) ? _currentLevel : null;
    public StringName GetCurrentLevelId() => _currentLevelId;
    public StringName GetCurrentStageId() => _session?.GetCurrentStageId() ?? new StringName("");
    public bool IsFlowActive() => _session?.IsActive() ?? false;
    public bool IsLevelUnlocked(StringName levelId)
    {
        if (Catalog == null) return false;
        int index = Catalog.FindIndex(levelId), highest = Catalog.FindIndex(_progress.HighestUnlockedLevelId);
        return index >= 0 && highest >= 0 && index <= highest;
    }
    public bool UnlockLevel(StringName levelId)
    {
        if (Catalog == null) return false;
        int index = Catalog.FindIndex(levelId), highest = Catalog.FindIndex(_progress.HighestUnlockedLevelId);
        if (index < 0 || (highest >= 0 && index <= highest)) return false;
        _progress.HighestUnlockedLevelId = levelId;
        SaveProgress();
        EmitSignal(SignalName.ProgressChanged, _progress);
        return true;
    }
    public bool LoadFirstLevel()
    {
        if (Catalog == null || Catalog.GetLevelCount() == 0)
        {
            EmitFlowError(ErrorCatalogMissing, "A valid catalog is required.");
            return false;
        }
        return LoadLevel(Catalog.GetFirstLevelId());
    }
    public bool ContinueGame()
    {
        if (Catalog == null) { EmitFlowError(ErrorCatalogMissing, "A valid catalog is required."); return false; }
        var levelId = _progress.HighestUnlockedLevelId;
        if (levelId.IsEmpty || Catalog.FindIndex(levelId) < 0) levelId = Catalog.GetFirstLevelId();
        return LoadLevel(levelId);
    }
    public bool LoadLevel(StringName levelId)
    {
        if (_exiting) return false;
        var catalog = Catalog;
        if (catalog == null || !catalog.Validate()) { EmitFlowError(ErrorCatalogInvalid, "Catalog is missing or invalid."); return false; }
        var definition = catalog.GetDefinition(levelId);
        if (definition == null) { EmitFlowError(ErrorLevelUnknown, $"Unknown level id '{levelId}'."); return false; }
        ulong generation = ++_generation;
        TeardownCurrent();
        if (_generation != generation || _exiting) return false;
        EnsureDefaultHosts();
        var level = InstantiateLevel(definition);
        if (level == null) return false;
        _currentLevel = level;
        _currentLevelId = definition.LevelId;
        level.FlowManager = this;
        _activeHost!.AddChild(level);
        // Ready/teardown/signal callbacks can replace this request synchronously.
        if (_generation != generation || _exiting) return false;
        var flow = catalog.GetFlowFor(definition.LevelId);
        if (flow == null || !flow.Validate()) flow = QuickLevelKitDefaultFlow.Create();
        var context = new QuickLevelKitContext { LevelId = definition.LevelId, Level = level, Manager = this, Progress = _progress };
        var session = new QuickLevelKitSession(flow, context);
        _session = session;
        session.TransitionRejected += payload => { if (_session == session) EmitSignal(SignalName.TransitionRejected, payload); };
        session.SessionStopped += () => EmitSignal(SignalName.SessionStopped, context.LevelId);
        session.StageChanged += (previous, current) => { if (_session == session) EmitSignal(SignalName.StageChanged, previous, current); };
        _progress.CurrentLevelId = definition.LevelId;
        if (_progress.HighestUnlockedLevelId.IsEmpty) _progress.HighestUnlockedLevelId = definition.LevelId;
        SaveProgress();
        EmitSignal(SignalName.ProgressChanged, _progress);
        if (_generation != generation) return false;
        EmitSignal(SignalName.LevelLoaded, level);
        if (_generation != generation) return false;
        EmitSignal(SignalName.SessionStarted, definition.LevelId);
        if (_generation != generation) return false;
        session.Start();
        return true;
    }
    public bool RequestTransition(Dictionary context)
    {
        var session = _session;
        if (session == null || !session.IsActive()) { EmitFlowError(ErrorNoSession, "No active flow session to transition."); return false; }
        return session.RequestTransition(context);
    }
    public bool AdvanceLevel()
    {
        if (Catalog == null) return false;
        var resolution = Catalog.ResolveNext(_currentLevelId);
        if (resolution["status"].AsInt32() != (int)QuickLevelKitCatalog.NextStatus.Available) return false;
        var nextId = resolution["level_id"].AsStringName();
        ulong generation = _generation;
        UnlockLevel(nextId);
        if (_generation != generation) return false;
        return LoadLevel(nextId);
    }
    public bool ReloadLevel() => !_currentLevelId.IsEmpty && LoadLevel(_currentLevelId);
    public bool FinishFlow() { ++_generation; TeardownCurrent(); return true; }
    public bool ResetProgress()
    {
        if (Catalog == null) return false;
        _progress = new QuickLevelKitProgress { HighestUnlockedLevelId = Catalog.GetFirstLevelId(), CurrentLevelId = Catalog.GetFirstLevelId() };
        if (_session != null) _session.Context.Progress = _progress;
        SaveProgress();
        EmitSignal(SignalName.ProgressChanged, _progress);
        return true;
    }
    private QuickLevelKitLevel? InstantiateLevel(QuickLevelKitDefinition definition)
    {
        if (definition.Scene == null) { EmitFlowError(ErrorSceneInvalid, $"Level '{definition.LevelId}' has no scene."); return null; }
        var root = definition.Scene.Instantiate();
        if (root is not QuickLevelKitLevel level)
        {
            root?.Free();
            EmitFlowError(ErrorRootNotLevel, "Level scene root must extend QuickLevelKitLevel.");
            return null;
        }
        if (level.LevelId.IsEmpty) level.LevelId = definition.LevelId;
        return level;
    }
    private void TeardownCurrent()
    {
        var session = _session;
        var level = _currentLevel;
        // Detach state first so callbacks cannot recursively tear down the same session.
        _session = null;
        _currentLevel = null;
        _currentLevelId = "";
        session?.Stop();
        if (IsInstanceValid(level))
        {
            level!.Teardown();
            if (IsInstanceValid(level))
            {
                level.GetParent()?.RemoveChild(level);
                level.QueueFree();
            }
        }
    }
    private void EnsureDefaultHosts()
    {
        if (IsInstanceValid(_activeHost) && IsInstanceValid(_stagingHost)) return;
        _container = new Node { Name = "LevelContainer" };
        _activeHost = new Node { Name = "ActiveLevelHost" };
        _stagingHost = new Node { Name = "StagingLevelHost" };
        _container.AddChild(_activeHost);
        _container.AddChild(_stagingHost);
        AddChild(_container);
    }
    private void EmitFlowError(StringName code, string message) { EmitSignal(SignalName.FlowError, code, message); GD.PushError(message); }
}
