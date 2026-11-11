using Godot;
using Godot.Collections;
namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitCatalog : Resource
{
    public enum NextStatus { Available, Final, Unknown }
    public static readonly StringName ErrorEntryMissing = "entry_missing";
    public static readonly StringName ErrorLevelIdEmpty = "level_id_empty";
    public static readonly StringName ErrorLevelIdDuplicate = "level_id_duplicate";
    public static readonly StringName ErrorSceneMissing = "scene_missing";
    public static readonly StringName ErrorSceneRootNotLevel = "scene_root_not_level";
    [Export] public Array<QuickLevelKitDefinition> Levels { get; set; } = new();
    [Export] public QuickLevelKitFlow? DefaultFlow { get; set; }

    public int GetLevelCount() => Levels.Count;
    public QuickLevelKitDefinition? GetDefinitionAt(int index) => index >= 0 && index < Levels.Count ? Levels[index] : null;
    public int FindIndex(StringName levelId)
    {
        for (int i = 0; i < Levels.Count; i++) if (Levels[i] != null && Levels[i].LevelId == levelId) return i;
        return -1;
    }
    public QuickLevelKitDefinition? GetDefinition(StringName levelId) => GetDefinitionAt(FindIndex(levelId));
    public StringName GetFirstLevelId() => GetDefinitionAt(0)?.LevelId ?? new StringName("");
    public StringName GetLastLevelId() => GetDefinitionAt(Levels.Count - 1)?.LevelId ?? new StringName("");
    public Dictionary ResolveNext(StringName levelId)
    {
        int index = FindIndex(levelId);
        if (index < 0) return Resolution(NextStatus.Unknown);
        if (index >= Levels.Count - 1) return Resolution(NextStatus.Final);
        var next = Levels[index + 1];
        return next == null ? Resolution(NextStatus.Unknown) : Resolution(NextStatus.Available, next);
    }
    public QuickLevelKitFlow? GetFlowFor(StringName levelId) => GetDefinition(levelId)?.Flow ?? DefaultFlow;
    public Array<Dictionary> GetValidationErrors()
    {
        var errors = new Array<Dictionary>();
        var seen = new System.Collections.Generic.HashSet<StringName>();
        for (int index = 0; index < Levels.Count; index++)
        {
            var definition = Levels[index];
            if (definition == null) { errors.Add(ValidationError(ErrorEntryMissing, index)); continue; }
            if (definition.LevelId.IsEmpty) errors.Add(ValidationError(ErrorLevelIdEmpty, index));
            else if (!seen.Add(definition.LevelId)) errors.Add(ValidationError(ErrorLevelIdDuplicate, index, definition.LevelId));
            if (definition.Scene == null) { errors.Add(ValidationError(ErrorSceneMissing, index, definition.LevelId)); continue; }
            var root = definition.Scene.Instantiate();
            if (root is not QuickLevelKitLevel) errors.Add(ValidationError(ErrorSceneRootNotLevel, index, definition.LevelId));
            root?.Free();
        }
        return errors;
    }
    public bool Validate() => GetValidationErrors().Count == 0;
    private static Dictionary Resolution(NextStatus status, QuickLevelKitDefinition? definition = null) => new()
    {
        ["status"] = (int)status, ["level_id"] = definition?.LevelId ?? new StringName(""), ["definition"] = definition is null ? default(Variant) : Variant.From(definition)
    };
    private static Dictionary ValidationError(StringName code, int index, StringName? levelId = null) => new()
    {
        ["code"] = code, ["index"] = index, ["level_id"] = levelId ?? new StringName("")
    };
}
