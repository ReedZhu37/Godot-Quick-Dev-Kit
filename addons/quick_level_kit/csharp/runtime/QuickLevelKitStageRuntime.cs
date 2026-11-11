using System;
using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitStageRuntime : RefCounted
{
    public QuickLevelKitStage Stage { get; set; } = null!;
    public QuickLevelKitSession Session { get; set; } = null!;
    public Dictionary State { get; set; } = new();
    // Per-entry cleanup belongs here, never on the shared Stage resource.
    internal Action? PreparationDisconnect { get; set; }
    public QuickLevelKitStageRuntime() { }
    public QuickLevelKitStageRuntime(QuickLevelKitStage stage, QuickLevelKitSession session) { Stage = stage; Session = session; }
}
