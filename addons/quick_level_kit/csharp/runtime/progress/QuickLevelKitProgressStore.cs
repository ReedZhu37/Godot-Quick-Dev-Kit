using Godot;
namespace QuickDev.Level;

public partial class QuickLevelKitProgressStore : RefCounted
{
    public virtual QuickLevelKitProgress LoadProgress() => new();
    public virtual Error SaveProgress(QuickLevelKitProgress progress) => Error.Ok;
    public virtual Error Clear() => Error.Ok;
}
