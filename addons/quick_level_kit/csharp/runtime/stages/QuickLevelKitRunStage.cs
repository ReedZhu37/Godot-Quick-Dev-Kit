using Godot;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitRunStage : QuickLevelKitStage
{
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    {
        var level = runtime.Session.Context.Level;
        if (level is QuickLevelKitLevel typed && IsInstanceValid(typed)) typed.BeginRunning();
        else if (IsInstanceValid(level) && level!.HasMethod("BeginRunning")) level.Call("BeginRunning");
    }
}
