#nullable enable

using System.IO;
using System.Runtime.CompilerServices;

namespace SamplesApp.AppiumTests.Infrastructure;

internal static class SnapshotPaths
{
	public static string ResolveSnapshotsDirectory(
		AppiumTestOptions? options = null,
		[CallerFilePath] string? callerFilePath = null)
	{
		if (!string.IsNullOrWhiteSpace(options?.SnapshotsDirectoryOverride))
		{
			return Path.GetFullPath(options.SnapshotsDirectoryOverride!);
		}

		var testsDirectory = Path.GetDirectoryName(callerFilePath!)
			?? throw new InvalidDataException("Unable to resolve the SamplesApp.AppiumTests tests directory.");
		var projectDirectory = Path.GetDirectoryName(testsDirectory)
			?? throw new InvalidDataException("Unable to resolve the SamplesApp.AppiumTests project directory.");
		return Path.Combine(projectDirectory, "Snapshots");
	}

	public static string ResolveBaselinePath(
		AppiumPlatform platform,
		AccessibilitySnapshotDefinition definition,
		AppiumTestOptions? options = null,
		[CallerFilePath] string? callerFilePath = null)
		=> Path.Combine(
			ResolveSnapshotsDirectory(options, callerFilePath),
			FlavorOf(platform),
			definition.SnapshotId + ".json");

	public static string FlavorOf(AppiumPlatform platform)
		=> platform switch
		{
			AppiumPlatform.Windows => "win32",
			AppiumPlatform.Mac => "macos",
			AppiumPlatform.Wasm => "wasm",
			_ => throw new InvalidDataException($"Unsupported Appium platform '{platform}'."),
		};
}
