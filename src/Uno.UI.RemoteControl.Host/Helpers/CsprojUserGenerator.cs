using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;
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
