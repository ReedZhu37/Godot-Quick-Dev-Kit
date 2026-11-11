namespace QuickDev.Level;

/// <summary>A minimal level that completes preparation synchronously and stays running.</summary>
public partial class DemoLevel : QuickLevelKitLevel
{
    protected override void PrepareLevel() => CompletePreparation();
}
