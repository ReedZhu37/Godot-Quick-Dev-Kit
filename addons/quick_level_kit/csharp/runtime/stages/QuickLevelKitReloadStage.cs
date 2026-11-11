using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitReloadStage : QuickLevelKitStage
{
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    {
        var manager = runtime.Session.Context.Manager;
        if (manager is QuickLevelKitManager typed && IsInstanceValid(typed)) { if (typed.ReloadLevel()) return; }
        else if (IsInstanceValid(manager) && manager!.HasMethod("ReloadLevel") && manager.Call("ReloadLevel").AsBool()) return;
        runtime.Session.RequestTransition(new Dictionary { ["type"] = new StringName("finished"), ["data"] = new Dictionary() });
    }
}
