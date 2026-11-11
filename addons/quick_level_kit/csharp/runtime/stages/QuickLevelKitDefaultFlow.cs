using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitDefaultFlow : RefCounted
{
    public static readonly StringName StagePrepare = "prepare", StageRun = "run", StageAdvance = "advance", StageReload = "reload", StageStop = "stop";
    public static QuickLevelKitFlow Create() => new()
    {
        StartStageId = StagePrepare,
        Stages = new Array<QuickLevelKitStage>
        {
            new QuickLevelKitPrepareStage { StageId = StagePrepare, Routes = new() { Route("ready", StageRun) } },
            new QuickLevelKitRunStage { StageId = StageRun, Routes = new() { Route("completed", StageAdvance), Route("failed", StageReload), Route("retry", StageReload) } },
            new QuickLevelKitAdvanceStage { StageId = StageAdvance, Routes = new() { Route("finished", StageStop) } },
            new QuickLevelKitReloadStage { StageId = StageReload },
            new QuickLevelKitStopStage { StageId = StageStop }
        }
    };
    private static QuickLevelKitRoute Route(StringName type, StringName target) => new() { ContextType = type, TargetStageId = target };
}
