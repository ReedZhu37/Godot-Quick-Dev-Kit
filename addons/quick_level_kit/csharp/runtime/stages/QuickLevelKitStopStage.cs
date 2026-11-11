using Godot;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitStopStage : QuickLevelKitStage
{
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    {
        var manager = runtime.Session.Context.Manager;
        if (manager is QuickLevelKitManager typed && IsInstanceValid(typed)) typed.FinishFlow();
        else if (IsInstanceValid(manager) && manager!.HasMethod("FinishFlow")) manager.Call("FinishFlow");
    }
}
