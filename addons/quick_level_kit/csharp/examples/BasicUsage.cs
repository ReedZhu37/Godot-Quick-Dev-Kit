using Godot;

namespace QuickDev.Level;

/// <summary>Run BasicUsage.tscn after enabling the C# plugin in a Godot .NET project.</summary>
public partial class BasicUsage : Node
{
    public override void _Ready()
    {
        var status = GetNode<Label>("Status");
        var manager = GetNodeOrNull<QuickLevelKitManager>("/root/QuickLevelKit");
        if (manager == null)
        {
            status.Text = "Enable Quick Level Kit (C#) to register the QuickLevelKit autoload.";
            return;
        }
        var definition = new QuickLevelKitDefinition
        {
            LevelId = "demo_001",
            Scene = GD.Load<PackedScene>("res://addons/quick_level_kit/csharp/examples/DemoLevel.tscn")
        };
        var catalog = new QuickLevelKitCatalog
        {
            Levels = new() { definition },
            DefaultFlow = QuickLevelKitDefaultFlow.Create()
        };
        manager.SetCatalog(catalog);
        manager.LoadFirstLevel();
        status.Text = $"Loaded: {manager.GetCurrentLevelId()} @ {manager.GetCurrentStageId()}";
    }
}
