using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitLevel : Node
{
    [Signal] public delegate void PreparationCompletedEventHandler();
    [Signal] public delegate void TransitionRequestedEventHandler(Dictionary context);
    [Export] public StringName LevelId { get; set; } = "";
    public GodotObject? FlowManager { get; set; }
    private bool _preparationStarted, _preparationCompleted, _finished, _tornDown;
    public void BeginPreparation()
    {
        if (_tornDown) return;
        _preparationStarted = true;
        _preparationCompleted = false;
        _finished = false;
        PrepareLevel();
    }
    public void CompletePreparation()
    {
        if (!_preparationStarted || _preparationCompleted || _tornDown) return;
        _preparationCompleted = true;
        EmitSignal(SignalName.PreparationCompleted);
    }
    public void BeginRunning()
    {
        if (_tornDown || _finished) return;
        _preparationStarted = false;
        StartLevel();
    }
    public void Complete(Dictionary? context = null) => RequestLevelTransition("completed", context ?? new());
    public void Fail(Dictionary? context = null) => RequestLevelTransition("failed", context ?? new());
    public void Retry(Dictionary? context = null) => RequestLevelTransition("retry", context ?? new());
    public void Teardown()
    {
        if (_tornDown) return;
        _tornDown = true;
        TeardownLevel();
    }
    private void RequestLevelTransition(StringName type, Dictionary data)
    {
        if (_tornDown) return;
        _finished = true;
        if (data.Count > 0) FinishLevel(data);
        var context = new Dictionary { ["type"] = type, ["data"] = data };
        EmitSignal(SignalName.TransitionRequested, context);
        // A transition signal may synchronously unload this level. Do not forward a stale event.
        if (_tornDown || !IsInstanceValid(this)) return;
        var manager = FlowManager;
        if (!IsInstanceValid(manager) && IsInsideTree()) manager = GetNodeOrNull("/root/QuickLevelKit");
        if (manager is QuickLevelKitManager typed) typed.RequestTransition(context);
        else if (IsInstanceValid(manager) && manager!.HasMethod("RequestTransition")) manager.Call("RequestTransition", context);
    }
    protected virtual void PrepareLevel() { }
    protected virtual void StartLevel() { }
    protected virtual void FinishLevel(Dictionary context) { }
    protected virtual void TeardownLevel() { }
}
