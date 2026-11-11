using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitStage : Resource
{
    [Export] public StringName StageId { get; set; } = "";
    [Export] public Array<QuickLevelKitRoute> Routes { get; set; } = new();

    public QuickLevelKitRoute? ResolveRoute(Dictionary context)
    {
        foreach (var route in Routes)
            if (route != null && route.Matches(context)) return route;
        return null;
    }
    public virtual void OnEnter(QuickLevelKitStageRuntime runtime) { }
    public virtual void OnExit(QuickLevelKitStageRuntime runtime) { }
    public virtual void OnPreparationCompleted(QuickLevelKitStageRuntime runtime) { }
}
