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

	public string OperatingSystem => RuntimeInformation.OSDescription;

	public string Repository => SampleChooserViewModel.RepositoryPath;

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

	// ".NETCoreApp,Version=v10.0" -> "net10.0"
	private static string GetTargetFramework()
	{
		var frameworkName = typeof(AppInfo).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
		if (frameworkName is null)
		{
			return "unknown";
		}

		const string versionMarker = "Version=v";
		var index = frameworkName.IndexOf(versionMarker, StringComparison.Ordinal);
		return index >= 0 ? $"net{frameworkName.Substring(index + versionMarker.Length)}" : frameworkName;
	}
}
