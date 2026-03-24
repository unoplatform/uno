using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

		AddRow(table, "requestedWorkingDirectory", info.RequestedWorkingDirectory);
		AddRow(table, "workingDirectory", info.WorkingDirectory);
		AddRow(table, "effectiveWorkspaceDirectory", info.EffectiveWorkspaceDirectory);
		AddRow(table, "selectedSolutionPath", info.SelectedSolutionPath);
		AddRow(table, "selectedGlobalJsonPath", info.SelectedGlobalJsonPath);
		AddRow(table, "resolutionKind", info.ResolutionKind?.ToString());

		AddSection(table, "Uno SDK");
		AddRow(table, "sdkSource", info.UnoSdkSource);
		AddRow(table, "sdkSourcePath", info.UnoSdkSourcePath);
		AddRow(table, "globalJsonPath", info.GlobalJsonPath);
		AddRow(table, "sdkPackage", info.UnoSdkPackage);
		AddRow(table, "sdkVersion", info.UnoSdkVersion);
		AddRow(table, "sdkPath", info.UnoSdkPath);
		AddRow(table, "packagesJsonPath", info.PackagesJsonPath);

		AddSection(table, "DevServer");
		AddRow(table, "devServerPackageVersion", info.DevServerPackageVersion);
		AddRow(table, "devServerPackagePath", info.DevServerPackagePath);
		AddRow(table, "devServerHostPath", info.HostPath);

		AddSection(table, "Settings");
		AddRow(table, "settingsPackageVersion", info.SettingsPackageVersion);
		AddRow(table, "settingsPackagePath", info.SettingsPackagePath);
		AddRow(table, "settingsPath", info.SettingsPath);

		AddSection(table, ".NET");
		AddRow(table, "dotNetVersion", info.DotNetVersion);
		AddRow(table, "dotNetTfm", info.DotNetTfm);

		AddSection(table, "Add-Ins");
		AddRow(table, "discoveryMethod", info.AddInsDiscoveryMethod);
		AddRow(table, "addInsDiscoveryDurationMs", info.AddInsDiscoveryDurationMs.ToString(CultureInfo.InvariantCulture));
		AddRow(table, "totalDiscoveryDurationMs", info.DiscoveryDurationMs.ToString(CultureInfo.InvariantCulture));
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

		AddSection(table, "Active Servers");
		if (info.ActiveServers.Count > 0)
		{
			foreach (var server in info.ActiveServers)
			{
				var localTag = server.IsInWorkspace ? " [green][[workspace]][/]" : " [grey][[other]][/]";
				table.AddRow(
					new Markup($"[white]processId[/]"),
					new Markup($"[grey]{server.ProcessId}[/]{localTag}"));
				AddRow(table, "port", server.Port.ToString(CultureInfo.InvariantCulture));
				AddRow(table, "mcpEndpoint", server.McpEndpoint);
				AddRow(table, "solution", server.SolutionPath);
				AddRow(table, "parentProcessId", server.ParentProcessId.ToString(CultureInfo.InvariantCulture));
				AddRow(table, "startTime", server.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture));
				AddRow(table, "ideChannelId", server.IdeChannelId);
				AddRow(table, "processChain", FormatProcessChain(server.ProcessChain));
				if (server != info.ActiveServers[^1])
				{
					table.AddEmptyRow();
				}
			}
		}
		else
		{
			AddRow(table, "status", "not running");
		}

		AnsiConsole.Write(table);

		WriteList("warnings", info.Warnings, "yellow");
		WriteList("errors", info.Errors, "red");
		WriteList("candidateSolutions", info.CandidateSolutions, "grey");
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

	private static string? FormatProcessChain(IReadOnlyList<ProcessChainEntry> processChain)
	{
		if (processChain.Count == 0)
		{
			return null;
		}

		// Use the shared formatter, then apply Spectre Console bold markup to names.
		return string.Join(
			" → ",
			processChain.Reverse().Select(entry =>
			{
				var name = ProcessChainEntry.ShortenProcessName(entry.ProcessName);
				var pid = entry.ProcessId.ToString(CultureInfo.InvariantCulture);
				return string.IsNullOrWhiteSpace(name)
					? pid
					: $"[bold]{Escape(name)}[/] ({pid})";
			}));
	}

	private static string? GetMissingHint(string key)
	{
		return key switch
		{
			"requestedWorkingDirectory" => "directory requested by the caller",
			"sdkSource" => "global.json or project source",
			"sdkSourcePath" => "global.json or project file path",
			"globalJsonPath" => "global.json in working directory or parents",
			"effectiveWorkspaceDirectory" => "resolved Uno workspace directory",
			"selectedSolutionPath" => "solution selected for the workspace",
			"selectedGlobalJsonPath" => "global.json selected for the workspace",
			"resolutionKind" => "how the workspace was selected",
			"sdkPackage" => "msbuild-sdks entry in global.json",
			"sdkVersion" => "msbuild-sdks entry in global.json",
			"sdkPath" => "restored Uno.Sdk package in NuGet cache",
			"packagesJsonPath" => "Uno.Sdk targets/netstandard2.0/packages.json",
			"devServerPackageVersion" => "Uno.WinUI.DevServer entry in packages.json",
			"devServerPackagePath" => "Uno.WinUI.DevServer package in NuGet cache",
			"settingsPackageVersion" => "uno.settings.devserver entry in packages.json",
			"settingsPackagePath" => "uno.settings.devserver package in NuGet cache",
			"dotNetVersion" => "dotnet --version output",
			"dotNetTfm" => "parsed dotnet --version",
			"devServerHostPath" => "Uno.WinUI.DevServer host for current dotnet TFM",
			"settingsPath" => "uno.settings.devserver tools/manager/Uno.Settings.dll",
			"discoveryMethod" => "convention-based targets parsing",
			"addIns" => "resolved add-in DLLs from .targets files",
			_ => null
		};
	}

}
