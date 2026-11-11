using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitAdvanceStage : QuickLevelKitStage
{
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    {
        var manager = runtime.Session.Context.Manager;
        if (manager is QuickLevelKitManager typed && IsInstanceValid(typed)) { if (typed.AdvanceLevel()) return; }
        else if (IsInstanceValid(manager) && manager!.HasMethod("AdvanceLevel") && manager.Call("AdvanceLevel").AsBool()) return;
        runtime.Session.RequestTransition(new Dictionary { ["type"] = new StringName("finished"), ["data"] = new Dictionary() });
    }
}
