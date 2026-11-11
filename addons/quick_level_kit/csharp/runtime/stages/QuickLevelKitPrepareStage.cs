using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitPrepareStage : QuickLevelKitStage
{
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    {
        var level = runtime.Session.Context.Level;
        if (!IsInstanceValid(level)) { OnPreparationCompleted(runtime); return; }
        if (level is QuickLevelKitLevel typed)
        {
            // Use a native Callable so stop/exit removes the connection itself, not only
            // a managed event delegate retained by the generated C# signal bridge.
            var handler = Callable.From(() => OnPreparationCompleted(runtime));
            typed.Connect(QuickLevelKitLevel.SignalName.PreparationCompleted, handler, (uint)ConnectFlags.OneShot);
            runtime.PreparationDisconnect = () =>
            {
                if (IsInstanceValid(typed) && typed.IsConnected(QuickLevelKitLevel.SignalName.PreparationCompleted, handler))
                    typed.Disconnect(QuickLevelKitLevel.SignalName.PreparationCompleted, handler);
            };
            typed.BeginPreparation();
            return;
        }
        if (!level!.HasMethod("BeginPreparation")) { OnPreparationCompleted(runtime); return; }
        if (level.HasSignal("PreparationCompleted"))
        {
            var handler = Callable.From(() => OnPreparationCompleted(runtime));
            level.Connect("PreparationCompleted", handler, (uint)ConnectFlags.OneShot);
            runtime.PreparationDisconnect = () => { if (IsInstanceValid(level) && level.IsConnected("PreparationCompleted", handler)) level.Disconnect("PreparationCompleted", handler); };
        }
        level.Call("BeginPreparation");
    }
    public override void OnPreparationCompleted(QuickLevelKitStageRuntime runtime) =>
        runtime.Session.RequestTransition(new Dictionary { ["type"] = new StringName("ready"), ["data"] = new Dictionary() });
}
