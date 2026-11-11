using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitSession : RefCounted
{
    [Signal] public delegate void TransitionRejectedEventHandler(Dictionary context);
    [Signal] public delegate void SessionStoppedEventHandler();
    [Signal] public delegate void StageChangedEventHandler(StringName previousId, StringName currentId);
    public QuickLevelKitFlow? Flow { get; set; }
    public QuickLevelKitContext Context { get; set; } = new();
    private StringName _currentStageId = "";
    private QuickLevelKitStageRuntime? _stageRuntime;
    private bool _active;
    private readonly System.Collections.Generic.Queue<Dictionary> _pendingContexts = new();
    private bool _flushing;
    private int _callbackDepth;
    private ulong _generation;
    public QuickLevelKitSession() { }
    public QuickLevelKitSession(QuickLevelKitFlow flow, QuickLevelKitContext context) { Flow = flow; Context = context; }
    public void Start()
    {
        if (_active || Flow == null) return;
        ++_generation;
        _active = true;
        EnterStage(Flow.StartStageId);
        FlushPending();
    }
    public bool RequestTransition(Dictionary contextPayload)
    {
        if (!_active || _stageRuntime == null) return false;
        var route = _stageRuntime.Stage.ResolveRoute(contextPayload);
        bool accepted = route != null && Flow != null && Flow.HasStage(route.TargetStageId);
        _pendingContexts.Enqueue(contextPayload);
        FlushPending();
        return accepted;
    }
    public void Stop()
    {
        if (!_active) return;
        ++_generation;
        _active = false;
        _pendingContexts.Clear();
        _currentStageId = "";
        ExitCurrentStage();
        EmitSignal(SignalName.SessionStopped);
        // OnExit may have restarted this session while callbacks were guarded.
        FlushPending();
    }
    public bool IsActive() => _active;
    public StringName GetCurrentStageId() => _currentStageId;
    public QuickLevelKitStage? GetCurrentStage() => _stageRuntime?.Stage;
    private void FlushPending()
    {
        if (_flushing || _callbackDepth > 0) return;
        _flushing = true;
        try
        {
            while (_active && _pendingContexts.Count > 0) ApplyTransition(_pendingContexts.Dequeue());
        }
        finally { _flushing = false; }
    }
    private void ApplyTransition(Dictionary payload)
    {
        if (_stageRuntime == null) return;
        var route = _stageRuntime.Stage.ResolveRoute(payload);
        if (route == null || Flow == null || !Flow.HasStage(route.TargetStageId))
        {
            EmitSignal(SignalName.TransitionRejected, payload);
            return;
        }
        var target = route.TargetStageId;
        ulong generation = _generation;
        ExitCurrentStage();
        if (_active && _generation == generation) EnterStage(target);
    }
    private void EnterStage(StringName stageId)
    {
        var stage = Flow?.GetStage(stageId);
        if (stage == null) { _active = false; _currentStageId = ""; return; }
        var previous = _currentStageId;
        ulong generation = _generation;
        _currentStageId = stageId;
        var runtime = new QuickLevelKitStageRuntime(stage, this);
        _stageRuntime = runtime;
        _callbackDepth++;
        try { stage.OnEnter(runtime); }
        finally { _callbackDepth--; }
        if (_active && _generation == generation && _stageRuntime == runtime)
            EmitSignal(SignalName.StageChanged, previous, stageId);
    }
    private void ExitCurrentStage()
    {
        var runtime = _stageRuntime;
        _stageRuntime = null;
        if (runtime == null) return;
        // Detach before callbacks: Stop or a new session can be requested inside OnExit.
        runtime.PreparationDisconnect?.Invoke();
        runtime.PreparationDisconnect = null;
        _callbackDepth++;
        try { runtime.Stage.OnExit(runtime); }
        finally { _callbackDepth--; }
    }
}
