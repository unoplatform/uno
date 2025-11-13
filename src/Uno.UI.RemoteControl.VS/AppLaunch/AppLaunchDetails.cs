using System;

namespace Uno.UI.RemoteControl.VS.AppLaunch;

/// <summary>
/// Correlation details for a single application launch cycle.
/// For now we key on StartupProjectPath which is stable across the Play/Build sequence.
/// </summary>
/// <param name="StartupProjectPath">Path to the startup project.</param>
/// <param name="IsDebug">Whether the application was launched in debug mode (true) or without debugger (false).</param>
internal readonly record struct AppLaunchDetails(string? StartupProjectPath, bool? IsDebug = null)
{
	/// <summary>
	/// Determines equality based only on StartupProjectPath, allowing IsDebug to be updated during the lifecycle.
	/// </summary>
	public bool Equals(AppLaunchDetails other) => string.Equals(StartupProjectPath, other.StartupProjectPath, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Gets hash code based only on StartupProjectPath for consistent correlation.
	/// </summary>
	public override int GetHashCode() => StartupProjectPath?.ToUpperInvariant().GetHashCode() ?? 0;
}
