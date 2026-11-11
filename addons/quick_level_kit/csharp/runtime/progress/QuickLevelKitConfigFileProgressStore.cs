using Godot;
using Godot.Collections;
namespace QuickDev.Level;

public partial class QuickLevelKitConfigFileProgressStore : QuickLevelKitProgressStore
{
    public const string DefaultPath = "user://quick_level_kit_progress.cfg";
    public const string Section = "quick_level_kit";
    public string SavePath { get; set; } = DefaultPath;
    public QuickLevelKitConfigFileProgressStore() { }
    public QuickLevelKitConfigFileProgressStore(string path) => SavePath = path;
    public override QuickLevelKitProgress LoadProgress()
    {
        var progress = new QuickLevelKitProgress();
        using var config = new ConfigFile();
        if (config.Load(SavePath) != Error.Ok) return progress;
        var encoded = config.GetValue(Section, "progress", new Dictionary());
        if (encoded.VariantType == Variant.Type.Dictionary) progress.FromDictionary(encoded.AsGodotDictionary());
        return progress;
    }
    public override Error SaveProgress(QuickLevelKitProgress progress)
    {
        using var config = new ConfigFile();
        config.SetValue(Section, "progress", progress.ToDictionary());
        return config.Save(SavePath);
    }
    public override Error Clear() => FileAccess.FileExists(SavePath) ? DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath)) : Error.Ok;
}
