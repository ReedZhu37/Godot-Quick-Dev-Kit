#if TOOLS
using Godot;

namespace QuickDev.Level;

/// <summary>Registers the C# implementation. Enable only one Quick Level Kit language plugin.</summary>
[Tool]
public partial class QuickLevelKitPlugin : EditorPlugin
{
    public const string AutoloadName = "QuickLevelKit";
    public const string AutoloadPath = "res://addons/quick_level_kit/csharp/runtime/QuickLevelKitManager.cs";
    private EditorDock? _dock;
    private QuickLevelKitDock? _catalogEditor;

    public override void _EnterTree()
    {
        _dock = new EditorDock
        {
            Name = "Quick Level Kit C#",
            Title = "Quick Level Kit C#",
            DefaultSlot = EditorDock.DockSlot.RightUl
        };
        _catalogEditor = new QuickLevelKitDock();
        _dock.AddChild(_catalogEditor);
        AddDock(_dock);
    }

    public override void _ExitTree()
    {
        if (_dock == null) return;
        RemoveDock(_dock);
        _dock.QueueFree();
        _dock = null;
        _catalogEditor = null;
        // Editor shutdown/reload must preserve the project's autoload registration.
    }

    public override bool _Handles(GodotObject @object) => @object is QuickLevelKitCatalog;

    public override void _Edit(GodotObject @object)
    {
        _catalogEditor?.SetCatalog(@object as QuickLevelKitCatalog);
    }

    public override void _MakeVisible(bool visible)
    {
        if (visible) _dock?.MakeVisible();
        else _dock?.Close();
    }

    public override void _EnablePlugin()
    {
        var setting = $"autoload/{AutoloadName}";
        if (ProjectSettings.HasSetting(setting))
        {
            if (!OwnsAutoload())
                GD.PushWarning("Quick Level Kit C#: QuickLevelKit is already registered by another implementation. Disable the other language plugin first, then re-enable this plugin. The existing autoload was preserved.");
            return;
        }
        AddAutoloadSingleton(AutoloadName, AutoloadPath);
    }

    public override void _DisablePlugin()
    {
        if (OwnsAutoload()) RemoveAutoloadSingleton(AutoloadName);
    }

    private static bool OwnsAutoload()
    {
        var setting = $"autoload/{AutoloadName}";
        if (!ProjectSettings.HasSetting(setting)) return false;
        var path = ProjectSettings.GetSetting(setting).AsString().TrimStart('*');
        // Godot 4.7 stores new autoloads as UIDs, while older projects retain paths.
        if (path.StartsWith("uid://", System.StringComparison.Ordinal))
        {
            var uid = ResourceUid.TextToId(path);
            if (!ResourceUid.HasId(uid)) return false;
            path = ResourceUid.GetIdPath(uid);
        }
        return path == AutoloadPath;
    }
}
#endif
