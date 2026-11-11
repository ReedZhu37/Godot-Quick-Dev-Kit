using Godot;
using Godot.Collections;
namespace QuickDev.Level.Tests;
public partial class QuickLevelTestProbe : QuickLevelKitLevel
{
    public readonly System.Collections.Generic.List<string> Events = new();
    [Export] public bool AutoPrepare { get; set; } = true;
    [Export] public bool AutoComplete { get; set; }
    protected override void PrepareLevel() { Events.Add("prepare"); if (AutoPrepare) CompletePreparation(); }
    protected override void StartLevel() { Events.Add("start"); if (AutoComplete) Complete(); }
    protected override void FinishLevel(Dictionary data) { Events.Add("finish"); }
    protected override void TeardownLevel() { Events.Add("teardown"); }
}
