using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitRoute : Resource
{
    [Export] public StringName ContextType { get; set; } = "";
    [Export] public StringName TargetStageId { get; set; } = "";
    [Export] public string[] RequiredPayloadKeys { get; set; } = System.Array.Empty<string>();

    public bool Matches(Dictionary context)
    {
        if (!ContextType.IsEmpty && (!context.TryGetValue("type", out var type) ||
            (type.VariantType != Variant.Type.String && type.VariantType != Variant.Type.StringName) ||
            type.AsStringName() != ContextType)) return false;
        if (RequiredPayloadKeys.Length == 0) return true;
        if (!context.TryGetValue("data", out var payload) || payload.VariantType != Variant.Type.Dictionary) return false;
        var data = payload.AsGodotDictionary();
        foreach (string key in RequiredPayloadKeys)
            if (!data.ContainsKey(new StringName(key))) return false;
        return true;
    }
}
