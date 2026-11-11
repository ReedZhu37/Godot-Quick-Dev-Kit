using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitProgress : RefCounted
{
    public StringName CurrentLevelId { get; set; } = "";
    public StringName HighestUnlockedLevelId { get; set; } = "";
    public Dictionary LevelStates { get; set; } = new();
    public Dictionary ToDictionary() => new()
    {
        ["current_level_id"] = CurrentLevelId.ToString(),
        ["highest_unlocked_level_id"] = HighestUnlockedLevelId.ToString(),
        ["level_states"] = LevelStates.Duplicate(true)
    };
    public void FromDictionary(Dictionary data)
    {
        CurrentLevelId = ReadId(data, "current_level_id");
        HighestUnlockedLevelId = ReadId(data, "highest_unlocked_level_id");
        LevelStates = data.TryGetValue("level_states", out var states) && states.VariantType == Variant.Type.Dictionary
            ? states.AsGodotDictionary().Duplicate(true) : new();
    }
    public QuickLevelKitProgress DuplicateProgress()
    {
        var copy = new QuickLevelKitProgress();
        copy.FromDictionary(ToDictionary());
        return copy;
    }
    public Dictionary GetLevelState(StringName levelId) => LevelStates.TryGetValue(levelId.ToString(), out var state) && state.VariantType == Variant.Type.Dictionary
        ? state.AsGodotDictionary().Duplicate(true) : new();
    public void SetLevelState(StringName levelId, Dictionary state) => LevelStates[levelId.ToString()] = state.Duplicate(true);
    private static StringName ReadId(Dictionary data, string key) => data.TryGetValue(key, out var value) &&
        (value.VariantType == Variant.Type.String || value.VariantType == Variant.Type.StringName) ? value.AsStringName() : new StringName("");
}
