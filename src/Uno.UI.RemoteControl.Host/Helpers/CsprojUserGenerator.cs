using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.VisualStudio.SolutionPersistence;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace Uno.UI.RemoteControl.Host.Helpers;

/// <summary>
/// Provides functionality to update or generate .csproj.user files for projects within a solution, setting the Uno
/// Remote Control port for development tooling integration.
/// </summary>
public static class CsprojUserGenerator
{
	private static readonly XNamespace MsbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

	/// <summary>
	/// Sets the user port configuration for all project files referenced by the specified solution file.
	/// </summary>
	/// <remarks>If the specified solution file does not exist or does not reference any projects, no changes are
	/// made. Only solution files are supported; other file types are ignored.</remarks>
	/// <param name="solution">The full path to an existing solution file (.sln or .slnx) whose referenced projects will have their user port
	/// updated. Must not be null, empty, or point to a non-existent file.</param>
	/// <param name="port">The port number to assign to each project's user configuration. Must be a valid TCP port number.</param>
	public static async Task SetCsprojUserPort(string solution, int port)
	{
		// Only accept an existing solution file. Do not try to interpret other file types (e.g. .csproj).
		if (string.IsNullOrWhiteSpace(solution) || !File.Exists(solution))
		{
			return; // Only use the provided path; do not search or treat it as a csproj.
		}

		var csprojUserPaths = new List<string>();
		var extension = Path.GetExtension(solution);

		if (
			string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
		{
			csprojUserPaths.AddRange(await GetCsprojUserPathsFromSolutionAsync(solution));
		}
		else
		{
			// Provided path is not a solution file - nothing to do.
			return;
		}

		if (csprojUserPaths.Count == 0)
		{
			return;
		}

		var unique = new HashSet<string>(csprojUserPaths, StringComparer.OrdinalIgnoreCase);

		GenerateCsprojUserOrRetrievePortWhenAvailable(unique, port);
	}

	/// <summary>
	/// Sets the Uno Remote Control port for a single project's <c>.csproj.user</c> file, creating the file if it does
	/// not yet exist. Unlike <see cref="SetCsprojUserPort(string, int)"/>, this does not require the project to be part
	/// of a solution — which makes it safe to call for a freshly created or renamed project whose owning solution may
	/// not yet reference it (a common cause of the running app failing to connect back to the DevServer).
	/// </summary>
	/// <param name="projectPath">Full path to the project's <c>.csproj</c> (or its <c>.csproj.user</c>) file.</param>
	/// <param name="port">The port number to assign. Must be between 1 and 65535.</param>
	public static void SetCsprojUserPortForProject(string projectPath, int port)
	{
		if (string.IsNullOrWhiteSpace(projectPath))
		{
			return;
		}

		if (port is <= 0 or > ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");
		}

		var csprojUserPath = ResolveCsprojUserPath(projectPath);
		if (csprojUserPath is null)
		{
			return; // Not a project file we know how to handle (e.g. a solution was passed by mistake).
		}

		GenerateCsprojUserOrRetrievePortWhenAvailable(new[] { csprojUserPath }, port);
	}

	/// <summary>
	/// Sets the Uno Remote Control port for whatever the supplied path points at: a solution (.sln/.slnx) updates every
	/// referenced project, a project (.csproj) updates that project only, and a directory updates the solutions it
	/// contains — or, failing that, the projects it contains. Anything else is ignored.
	/// </summary>
	/// <remarks>
	/// This is the resilient entry point for tooling (e.g. the MCP <c>uno_app_start</c> flow) that only knows the
	/// project being launched, not necessarily its solution. It guarantees the launched project's embedded
	/// <c>UnoRemoteControlPort</c> can be made to match the running DevServer even across renames or multi-solution
	/// workspaces, where solution-based synchronization alone silently misses the project.
	/// </remarks>
	/// <param name="path">A solution file, a project file, or a directory containing either.</param>
	/// <param name="port">The port number to assign. Must be between 1 and 65535.</param>
	public static async Task SetCsprojUserPortForPath(string path, int port)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return;
		}

		if (port is <= 0 or > ushort.MaxValue)
		{
			throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be between 1 and 65535.");
		}

		if (Directory.Exists(path))
		{
			var solutions = Directory.EnumerateFiles(path, "*.sln")
				.Concat(Directory.EnumerateFiles(path, "*.slnx"))
				.ToList();

			if (solutions.Count > 0)
			{
				foreach (var solution in solutions)
				{
					await SetCsprojUserPort(solution, port);
				}

				return;
			}

			foreach (var project in Directory.EnumerateFiles(path, "*.csproj"))
			{
				SetCsprojUserPortForProject(project, port);
			}

			return;
		}

		var extension = Path.GetExtension(path);
		if (string.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase))
		{
			await SetCsprojUserPort(path, port);
		}
		else if (string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(extension, ".user", StringComparison.OrdinalIgnoreCase))
		{
			SetCsprojUserPortForProject(path, port);
		}
	}

	/// <summary>
	/// Reads the Uno Remote Control port currently configured in a project's <c>.csproj.user</c> file, if any.
	/// Intended for fail-fast validation before launch: a caller can compare the returned value against the running
	/// DevServer port and surface a clear error instead of letting the app connect to a dead port and time out.
	/// </summary>
	/// <param name="projectPath">Full path to the project's <c>.csproj</c> (or its <c>.csproj.user</c>) file.</param>
	/// <param name="port">When this method returns <see langword="true"/>, the configured port; otherwise 0.</param>
	/// <returns><see langword="true"/> if a valid port was found; otherwise <see langword="false"/>.</returns>
	public static bool TryGetConfiguredPort(string projectPath, out int port)
	{
		port = 0;

		if (string.IsNullOrWhiteSpace(projectPath))
		{
			return false;
		}

		var csprojUserPath = ResolveCsprojUserPath(projectPath);
		if (csprojUserPath is null || !File.Exists(csprojUserPath))
		{
			return false;
		}

		try
		{
			var document = XDocument.Load(csprojUserPath);
			var ns = document.Root?.GetDefaultNamespace() ?? XNamespace.None;

			var value = document.Root?
				.Elements(ns + "PropertyGroup")
				.Elements(ns + "UnoRemoteControlPort")
				.FirstOrDefault()?.Value;

			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			// The DevServer marks tooling-assigned ports with a trailing '#'; strip it before parsing.
			value = value.TrimEnd('#').Trim();

			return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out port);
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Maps a project path to its <c>.csproj.user</c> counterpart, or returns <see langword="null"/> when the path is
	/// not a project file (e.g. a solution was supplied by mistake).
	/// </summary>
	private static string? ResolveCsprojUserPath(string projectPath)
	{
		if (projectPath.EndsWith(".user", StringComparison.OrdinalIgnoreCase))
		{
			return projectPath;
		}

		if (projectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			return projectPath + ".user";
		}

		return null;
	}

	private static async Task<List<string>> GetCsprojUserPathsFromSolutionAsync(string solutionPath)
	{
		var result = new List<string>();
		try
		{
			var moniker = Path.GetExtension(solutionPath);
			var serializer = SolutionSerializers.GetSerializerByMoniker(moniker);
			if (serializer == null)
			{
				return result;
			}

			var model = await serializer.OpenAsync(solutionPath, System.Threading.CancellationToken.None).ConfigureAwait(false);
			if (model == null)
			{
				return result;
			}

			var rootDir = Path.GetDirectoryName(solutionPath)!;
			foreach (var p in model.SolutionProjects)
			{
				var filePath = p.FilePath;
				var projectPath = Path.GetFullPath(Path.IsPathRooted(filePath) ? filePath : Path.Combine(rootDir, filePath));
				if (string.Equals(Path.GetExtension(projectPath), ".csproj", StringComparison.OrdinalIgnoreCase))
				{
					result.Add(projectPath + ".user");
				}
			}
		}
		catch
		{
			// Ignore errors and do not fallback.
		}
		return result;
	}

	private static void GenerateCsprojUserOrRetrievePortWhenAvailable(IEnumerable<string> csprojUserPaths, int port)
	{
		foreach (var csprojUserPath in csprojUserPaths)
		{
			XDocument document;
			if (File.Exists(csprojUserPath))
			{
				document = XDocument.Load(csprojUserPath);
				var ns = document.Root?.GetDefaultNamespace() ?? XNamespace.None;
				var propertyUpdated = false;

				if (document.Root != null)
				{
					foreach (var propertyGroup in document.Root.Elements(ns + "PropertyGroup"))
					{
						var propertyElement = propertyGroup.Element(ns + "UnoRemoteControlPort");
						if (propertyElement != null)
						{
							propertyElement.Value = $"{port.ToString(CultureInfo.InvariantCulture)}#";
							propertyUpdated = true;
							break;
						}
					}

					if (!propertyUpdated)
					{
						document.Root.Add(GetCsprojPropertyGroupForPort(port.ToString(CultureInfo.InvariantCulture), ns));
					}
				}
			}
			else
			{
				document = new XDocument(new XElement(MsbuildNamespace + "Project",
					new XAttribute("ToolsVersion", "Current")
				));

				document.Root?.Add(GetCsprojPropertyGroupForPort(port.ToString(CultureInfo.InvariantCulture), MsbuildNamespace));
			}

			document.Save(csprojUserPath);
		}
	}

	private static XElement GetCsprojPropertyGroupForPort(string remoteControlServerPort, XNamespace ns)
		=> new(ns + "PropertyGroup",
			new XElement(ns + "UnoPlatformIDE", "cli"),
			new XElement(ns + "UnoRemoteControlPort", $"{remoteControlServerPort}#"));
}
