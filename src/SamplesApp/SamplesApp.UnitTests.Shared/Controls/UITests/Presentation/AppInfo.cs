#nullable enable

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.UI.Xaml;

namespace SampleControl.Presentation;

/// <summary>Build and environment facts shown on Home and in Settings › About.</summary>
public sealed class AppInfo
{
	public string Platform => SampleChooserViewModel.TargetPlatform;

	public string TargetFramework { get; } = GetTargetFramework();

	public string Configuration => SampleChooserViewModel.IsDebug ? "Debug" : "Release";

	public string Runtime => RuntimeInformation.FrameworkDescription;

#if HAS_UNO
	public string UIFramework => "Uno.UI";
#else
	public string UIFramework => "WinUI";
#endif

	public string UIFrameworkVersion { get; } =
		typeof(UIElement).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
		?? typeof(UIElement).Assembly.GetName().Version?.ToString()
		?? "unknown";

	/// <summary>The version with its commit cut to 7 characters and free to wrap before it, for display (diagnostics keep the full one).</summary>
	public string UIFrameworkVersionShort => ShortenVersion(UIFrameworkVersion).Replace("+", "+\u200B");

	public string OperatingSystem => RuntimeInformation.OSDescription;

	public string Repository => SampleChooserViewModel.RepositoryPath;

	/// <summary>The repository path with break opportunities after its separators, so it wraps between folders.</summary>
	public string RepositoryDisplay => AddPathBreaks(Repository);

	public override string ToString()
	{
		StringBuilder builder = new();
		builder.AppendLine($"Platform: {Platform}");
		builder.AppendLine($"Target framework: {TargetFramework}");
		builder.AppendLine($"Configuration: {Configuration}");
		builder.AppendLine($"Runtime: {Runtime}");
		builder.AppendLine($"{UIFramework}: {UIFrameworkVersion}");
		builder.AppendLine($"OS: {OperatingSystem}");
		builder.Append($"Repository: {Repository}");
		return builder.ToString();
	}

	// "1.2.3+0123456789abcdef..." -> "1.2.3+0123456"
	internal static string ShortenVersion(string version)
	{
		var plus = version.IndexOf('+');
		return plus >= 0 && version.Length - plus - 1 > 7 ? version.Substring(0, plus + 8) : version;
	}

	internal static string AddPathBreaks(string path)
		=> path.Replace("\\", "\\\u200B").Replace("/", "/\u200B");

	private static string GetTargetFramework()
		=> FormatTargetFramework(
			typeof(AppInfo).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName,
			typeof(AppInfo).Assembly.GetCustomAttribute<TargetPlatformAttribute>()?.PlatformName);

	// (".NETCoreApp,Version=v10.0", "Android36.0") -> "net10.0-android36.0"; SDK-defined platforms such as "Desktop1.0" drop their placeholder version.
	internal static string FormatTargetFramework(string? frameworkName, string? platformName)
	{
		if (frameworkName is null)
		{
			return "unknown";
		}

		const string versionMarker = "Version=v";
		var index = frameworkName.IndexOf(versionMarker, StringComparison.Ordinal);
		var framework = index >= 0 ? $"net{frameworkName.Substring(index + versionMarker.Length)}" : frameworkName;

		if (string.IsNullOrEmpty(platformName))
		{
			return framework;
		}

		var versionStart = platformName.IndexOfAny("0123456789".ToCharArray());
		var platform = versionStart > 0 && platformName.Substring(versionStart) == "1.0"
			? platformName.Substring(0, versionStart)
			: platformName;

		return $"{framework}-{platform.ToLowerInvariant()}";
	}
}
