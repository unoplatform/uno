using System.Collections.Generic;
using System.Globalization;
using Spectre.Console;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class DiscoveryOutputFormatter
{
	public static void WritePlainText(DiscoveryInfo info)
	{
		var table = new Table()
			.Border(TableBorder.Minimal)
			.Title("[white]Discovery[/]")
			.AddColumn(new TableColumn("[grey]Key[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Value[/]").LeftAligned());

		AddRow(table, "workingDirectory", info.WorkingDirectory);

		AddSection(table, "Uno SDK");
		AddRow(table, "source", info.UnoSdkSource);
		AddRow(table, "sourcePath", info.UnoSdkSourcePath);
		AddRow(table, "globalJsonPath", info.GlobalJsonPath);
		AddRow(table, "package", info.UnoSdkPackage);
		AddRow(table, "version", info.UnoSdkVersion);
		AddRow(table, "sdkPath", info.UnoSdkPath);
		AddRow(table, "packagesJsonPath", info.PackagesJsonPath);

		AddSection(table, "DevServer");
		AddRow(table, "devServerPackageVersion", info.DevServerPackageVersion);
		AddRow(table, "devServerPackagePath", info.DevServerPackagePath);
		AddRow(table, "hostPath", info.HostPath);

		AddSection(table, "Settings");
		AddRow(table, "settingsPackageVersion", info.SettingsPackageVersion);
		AddRow(table, "settingsPackagePath", info.SettingsPackagePath);
		AddRow(table, "settingsPath", info.SettingsPath);

		AddSection(table, ".NET");
		AddRow(table, "dotNetVersion", info.DotNetVersion);
		AddRow(table, "dotNetTfm", info.DotNetTfm);

		AddSection(table, "Add-Ins");
		AddRow(table, "discoveryMethod", info.AddInsDiscoveryMethod);
		AddRow(table, "discoveryDurationMs", info.AddInsDiscoveryDurationMs.ToString(CultureInfo.InvariantCulture));
		if (info.AddIns.Count > 0)
		{
			foreach (var addIn in info.AddIns)
			{
				table.AddRow(
					new Markup($"  [white]{Escape(addIn.PackageName)}[/] [grey]{Escape(addIn.PackageVersion)}[/]"),
					new Markup($"[grey]{Escape(addIn.EntryPointDll)}[/] [dim]({Escape(addIn.DiscoverySource)})[/]"));
			}
		}
		else
		{
			AddRow(table, "addIns", null);
		}

		AnsiConsole.Write(table);

		WriteList("warnings", info.Warnings, "yellow");
		WriteList("errors", info.Errors, "red");
	}

	private static void AddRow(Table table, string key, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			var cell = new Markup("[yellow]<null>[/]");
			var hint = GetMissingHint(key);
			if (!string.IsNullOrWhiteSpace(hint))
			{
				table.AddRow(
					new Markup($"[white]{Escape(key)}[/]"),
					new Markup($"[yellow]<null>[/] [grey](missing {Escape(hint)})[/]"));
				return;
			}
			table.AddRow(new Markup($"[white]{Escape(key)}[/]"), cell);
			return;
		}

		table.AddRow(
			new Markup($"[white]{Escape(key)}[/]"),
			new Markup($"[grey]{Escape(value)}[/]"));
	}

	private static void AddSection(Table table, string title)
	{
		table.AddEmptyRow();
		table.AddRow(new Markup($"[aqua]{Escape(title)}[/]"), new Markup(""));
	}

	private static void WriteList(string key, IReadOnlyList<string> items, string color)
	{
		if (items.Count == 0)
		{
			return;
		}

		AnsiConsole.Write(new Markup($"[{color}]{Escape(key)}[/]"));
		AnsiConsole.WriteLine(":");

		foreach (var item in items)
		{
			AnsiConsole.Write(new Markup("- "));
			AnsiConsole.WriteLine(Escape(item));
		}
	}

	private static string Escape(string value)
		=> Markup.Escape(value);

	private static string? GetMissingHint(string key)
	{
		return key switch
		{
			"unoSdkSource" => "global.json or project source",
			"unoSdkSourcePath" => "global.json or project file path",
			"globalJsonPath" => "global.json in working directory or parents",
			"unoSdkPackage" => "msbuild-sdks entry in global.json",
			"unoSdkVersion" => "msbuild-sdks entry in global.json",
			"unoSdkPath" => "restored Uno.Sdk package in NuGet cache",
			"packagesJsonPath" => "Uno.Sdk targets/netstandard2.0/packages.json",
			"devServerPackageVersion" => "Uno.WinUI.DevServer entry in packages.json",
			"devServerPackagePath" => "Uno.WinUI.DevServer package in NuGet cache",
			"settingsPackageVersion" => "uno.settings.devserver entry in packages.json",
			"settingsPackagePath" => "uno.settings.devserver package in NuGet cache",
			"dotNetVersion" => "dotnet --version output",
			"dotNetTfm" => "parsed dotnet --version",
			"hostPath" => "Uno.WinUI.DevServer host for current dotnet TFM",
			"settingsPath" => "uno.settings.devserver tools/manager/Uno.Settings.dll",
			"discoveryMethod" => "convention-based targets parsing",
			"addIns" => "resolved add-in DLLs from .targets files",
			_ => null
		};
	}

}
