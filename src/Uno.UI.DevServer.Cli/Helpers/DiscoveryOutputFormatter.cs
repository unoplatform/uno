using System.Collections.Generic;
using System.Text;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class DiscoveryOutputFormatter
{
	public static string ToPlainText(DiscoveryInfo info, bool useColor)
	{
		var sb = new StringBuilder();

		AppendValue(sb, "workingDirectory", info.WorkingDirectory);
		AppendValue(sb, "unoSdkSource", info.UnoSdkSource);
		AppendValue(sb, "unoSdkSourcePath", info.UnoSdkSourcePath);
		AppendValue(sb, "unoSdkPackage", info.UnoSdkPackage);
		AppendValue(sb, "unoSdkVersion", info.UnoSdkVersion);
		AppendValue(sb, "unoSdkPath", info.UnoSdkPath);
		AppendValue(sb, "packagesJsonPath", info.PackagesJsonPath);
		AppendValue(sb, "devServerPackageVersion", info.DevServerPackageVersion);
		AppendValue(sb, "devServerPackagePath", info.DevServerPackagePath);
		AppendValue(sb, "settingsPackageVersion", info.SettingsPackageVersion);
		AppendValue(sb, "settingsPackagePath", info.SettingsPackagePath);
		AppendValue(sb, "dotNetVersion", info.DotNetVersion);
		AppendValue(sb, "dotNetTfm", info.DotNetTfm);
		AppendValue(sb, "hostPath", info.HostPath);
		AppendValue(sb, "settingsPath", info.SettingsPath);

		AppendList(sb, "warnings", info.Warnings, useColor ? ConsoleColor.Yellow : null);
		AppendList(sb, "errors", info.Errors, useColor ? ConsoleColor.Red : null);

		return sb.ToString();
	}

	private static void AppendValue(StringBuilder sb, string key, string? value)
	{
		sb.Append(key);
		sb.Append(": ");
		sb.AppendLine(string.IsNullOrWhiteSpace(value) ? "<null>" : value);
	}

	private static void AppendList(StringBuilder sb, string key, IReadOnlyList<string> items, ConsoleColor? color)
	{
		if (items.Count == 0)
		{
			return;
		}

		AppendHeader(sb, key, color);

		foreach (var item in items)
		{
			sb.Append("- ");
			sb.AppendLine(item);
		}
	}

	private static void AppendHeader(StringBuilder sb, string key, ConsoleColor? color)
	{
		if (color is null || Console.IsOutputRedirected)
		{
			sb.Append(key);
			sb.AppendLine(":");
			return;
		}

		sb.Append(GetAnsiColor(color.Value));
		sb.Append(key);
		sb.AppendLine(":");
		sb.Append(AnsiReset);
	}

	private static string GetAnsiColor(ConsoleColor color)
	{
		return color switch
		{
			ConsoleColor.Red => "\u001b[31m",
			ConsoleColor.Yellow => "\u001b[33m",
			ConsoleColor.Cyan => "\u001b[36m",
			ConsoleColor.Green => "\u001b[32m",
			ConsoleColor.Gray => "\u001b[90m",
			_ => ""
		};
	}

	private const string AnsiReset = "\u001b[0m";
}
