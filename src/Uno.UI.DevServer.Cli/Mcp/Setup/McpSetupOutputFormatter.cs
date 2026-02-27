using Spectre.Console;

namespace Uno.UI.DevServer.Cli.Mcp.Setup;

/// <summary>
/// Human-readable output formatter for MCP setup commands.
/// </summary>
internal static class McpSetupOutputFormatter
{
	public static void WriteStatus(StatusResponse response, string workspace)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[white]MCP Server Status[/]")
			.AddColumn(new TableColumn("[grey]Server[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]IDE[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Status[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Cfg File[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Transport[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Variant[/]").LeftAligned());

		foreach (var server in response.Servers)
		{
			var isFirst = true;
			foreach (var ide in server.Ides)
			{
				var serverCol = isFirst ? Escape(server.Name) : "";
				var statusMarkup = FormatStatus(ide.Status);
				var hasLocation = ide.Locations is { Count: > 0 };
				var cfgFile = hasLocation ? ShortenPath(ide.Locations![0].Path, workspace) : "-";
				var transport = hasLocation ? ide.Locations![0].Transport : "-";
				var variant = hasLocation ? ide.Locations![0].Variant : "-";
				var valColor = hasLocation ? "white" : "grey";

				table.AddRow(
					new Markup($"[white]{serverCol}[/]"),
					new Markup($"[white]{Escape(ide.Ide)}[/]"),
					statusMarkup,
					new Markup($"[{valColor}]{Escape(cfgFile)}[/]"),
					new Markup($"[{valColor}]{Escape(transport)}[/]"),
					new Markup($"[{valColor}]{Escape(variant)}[/]"));

				if (ide.Warnings is { Count: > 0 })
				{
					foreach (var warning in ide.Warnings)
					{
						table.AddRow(
							new Markup(""),
							new Markup(""),
							new Markup(""),
							new Markup($"[yellow]⚠ {Escape(warning)}[/]"),
							new Markup(""),
							new Markup(""));
					}
				}

				isFirst = false;
			}
		}

		AnsiConsole.Write(table);

		AnsiConsole.MarkupLine($"\n[grey]Tool version:[/] [white]{Escape(response.ToolVersion)}[/]");
		AnsiConsole.MarkupLine($"[grey]Expected variant:[/] [white]{Escape(response.ExpectedVariant)}[/]");

		if (response.DetectedIdes.Count > 0)
		{
			AnsiConsole.MarkupLine($"[grey]IDEs detected:[/] [white]{Escape(string.Join(", ", response.DetectedIdes))}[/]");
		}

		if (response.CallerIde is not null)
		{
			AnsiConsole.MarkupLine($"[grey]Caller IDE:[/] [white]{Escape(response.CallerIde)}[/]");
		}
	}

	public static void WriteInstall(OperationResponse response, string workspace)
	{
		WriteOperations("MCP Install", response, workspace);
	}

	public static void WriteUninstall(OperationResponse response, string workspace)
	{
		WriteOperations("MCP Uninstall", response, workspace);
	}

	private static void WriteOperations(string title, OperationResponse response, string workspace)
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title($"[white]{Escape(title)}[/]")
			.AddColumn(new TableColumn("[grey]Server[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Action[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Path[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Details[/]").LeftAligned());

		foreach (var op in response.Operations)
		{
			var actionMarkup = FormatAction(op.Action);
			table.AddRow(
				new Markup($"[white]{Escape(op.Server)}[/]"),
				actionMarkup,
				new Markup($"[grey]{Escape(ShortenPath(op.Path ?? "-", workspace))}[/]"),
				new Markup($"[grey]{Escape(op.Reason ?? "")}[/]"));
		}

		AnsiConsole.Write(table);
	}

	private static Markup FormatStatus(string status) => status switch
	{
		"registered" => new Markup("[green]registered[/]"),
		"outdated" => new Markup("[yellow]outdated[/]"),
		"missing" => new Markup("[red]missing[/]"),
		_ => new Markup($"[grey]{Escape(status)}[/]"),
	};

	private static Markup FormatAction(string action) => action switch
	{
		"created" => new Markup("[green]created[/]"),
		"updated" => new Markup("[green]updated[/]"),
		"skipped" => new Markup("[grey]skipped[/]"),
		"removed" => new Markup("[yellow]removed[/]"),
		"not_found" => new Markup("[grey]not_found[/]"),
		"error" => new Markup("[red]error[/]"),
		_ => new Markup($"[grey]{Escape(action)}[/]"),
	};

	private static string Escape(string text) => text.Replace("[", "[[").Replace("]", "]]");

	private static string ShortenPath(string path, string workspace)
	{
		// Normalize mixed separators (templates use '/' but OS may use '\')
		path = path.Replace('/', Path.DirectorySeparatorChar);
		workspace = workspace.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);

		// Prefer workspace-relative paths
		if (path.StartsWith(workspace, StringComparison.OrdinalIgnoreCase)
			&& path.Length > workspace.Length
			&& path[workspace.Length] == Path.DirectorySeparatorChar)
		{
			return "." + path[workspace.Length..];
		}

		// Fall back to home-relative paths
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(home) && path.StartsWith(home, StringComparison.OrdinalIgnoreCase))
		{
			return "~" + path[home.Length..];
		}

		return path;
	}
}
