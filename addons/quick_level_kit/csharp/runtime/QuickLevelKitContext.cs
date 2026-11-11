using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitContext : RefCounted
{
    public StringName LevelId { get; set; } = "";
    public Node? Level { get; set; }
    public GodotObject? Manager { get; set; }
    public QuickLevelKitProgress? Progress { get; set; }
    public Dictionary Data { get; set; } = new();
    public void SetValue(Variant key, Variant value) => Data[key] = value;
    public Variant GetValue(Variant key, Variant defaultValue = default) => Data.TryGetValue(key, out var value) ? value : defaultValue;
}
