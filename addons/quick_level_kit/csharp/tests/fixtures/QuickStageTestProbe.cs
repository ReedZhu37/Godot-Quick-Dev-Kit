using System;
using Godot.Collections;
namespace QuickDev.Level.Tests;
public partial class QuickStageTestProbe : QuickLevelKitStage
{
    public int Entered, Exited;
    public QuickLevelKitStageRuntime LastRuntime = null!;
    public Action<QuickLevelKitStageRuntime>? EnterAction;
    public Action<QuickLevelKitStageRuntime>? ExitAction;
    public override void OnEnter(QuickLevelKitStageRuntime runtime)
    { Entered++; LastRuntime = runtime; EnterAction?.Invoke(runtime); }
    public override void OnExit(QuickLevelKitStageRuntime runtime)
    { Exited++; ExitAction?.Invoke(runtime); }
}
