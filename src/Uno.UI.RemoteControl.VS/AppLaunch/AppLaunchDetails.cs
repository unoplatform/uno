namespace Uno.UI.RemoteControl.VS.AppLaunch;

/// <summary>
/// Correlation details for a single application launch cycle.
/// For now we key on StartupProjectPath which is stable across the Play/Build sequence.
/// </summary>
internal readonly record struct AppLaunchDetails(string? StartupProjectPath);
