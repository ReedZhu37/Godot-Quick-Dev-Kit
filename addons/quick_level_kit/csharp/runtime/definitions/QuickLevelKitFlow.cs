using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitFlow : Resource
{
    public static readonly StringName ErrorStartStageMissing = "start_stage_missing";
    public static readonly StringName ErrorStageIdEmpty = "stage_id_empty";
    public static readonly StringName ErrorStageIdDuplicate = "stage_id_duplicate";
    public static readonly StringName ErrorRouteTargetUnknown = "route_target_unknown";
    public static readonly StringName ErrorRouteTargetEmpty = "route_target_empty";
    [Export] public StringName StartStageId { get; set; } = "prepare";
    [Export] public Array<QuickLevelKitStage> Stages { get; set; } = new();

    public QuickLevelKitStage? GetStage(StringName stageId)
    {
        foreach (var stage in Stages)
            if (stage != null && stage.StageId == stageId) return stage;
        return null;
    }
    public bool HasStage(StringName stageId) => GetStage(stageId) != null;
    public Array<Dictionary> GetValidationErrors()
    {
        var errors = new Array<Dictionary>();
        if (!HasStage(StartStageId)) errors.Add(new() { ["code"] = ErrorStartStageMissing, ["stage_id"] = StartStageId });
        var seen = new System.Collections.Generic.HashSet<StringName>();
        for (int index = 0; index < Stages.Count; index++)
        {
            var stage = Stages[index];
            if (stage == null) continue;
            if (stage.StageId.IsEmpty) errors.Add(new() { ["code"] = ErrorStageIdEmpty, ["index"] = index });
            else if (!seen.Add(stage.StageId)) errors.Add(new() { ["code"] = ErrorStageIdDuplicate, ["stage_id"] = stage.StageId });
            foreach (var route in stage.Routes)
            {
                if (route == null) continue;
                if (route.TargetStageId.IsEmpty) errors.Add(new() { ["code"] = ErrorRouteTargetEmpty, ["stage_id"] = stage.StageId });
                else if (!HasStage(route.TargetStageId)) errors.Add(new() { ["code"] = ErrorRouteTargetUnknown, ["stage_id"] = stage.StageId, ["target_stage_id"] = route.TargetStageId });
            }
        }
        return errors;
    }
    public bool Validate() => GetValidationErrors().Count == 0;
}
