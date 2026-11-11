using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitDefinition : Resource
{
    [Export] public StringName LevelId { get; set; } = "";
    [Export] public PackedScene? Scene { get; set; }
    [Export] public string DisplayName { get; set; } = "";
    [Export] public Dictionary Metadata { get; set; } = new();
    [Export] public QuickLevelKitFlow? Flow { get; set; }
}
