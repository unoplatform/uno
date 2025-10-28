using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Uno.UI.Helpers;
using VSLangProj;
using VSLangProj110;

namespace Uno.UI.RemoteControl.VS.Helpers;

// Please keep this file in sync with the Uno.UI.RC.VS DTEHelper file

[Flags]
internal enum ProjectAttribute
{
	/// <summary>
	/// Output type is exe or winexe
	/// </summary>
	Application = 1 << 0,

	/// <summary>
	/// Output type is library
	/// </summary>
	Library = 1 << 1,

	/// <summary>
	/// Project is a startup project
	/// </summary>
	Startup = 1 << 2,
}

internal static class DTEHelper
{
	internal const string FolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

	public static int GetMSBuildOutputVerbosity(this DTE2 dte)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var properties = dte?.Properties["Environment", "ProjectsAndSolution"];
		var logOutput = properties?.Item("MSBuildOutputVerbosity").Value is int log ? log : 0;
		return logOutput;
	}

	public static bool IsApplication(this EnvDTE.Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		return project.Properties?.Item("OutputType")?.Value is prjOutputTypeEx.prjOutputTypeEx_Exe or prjOutputTypeEx.prjOutputTypeEx_WinExe or prjOutputTypeEx.prjOutputTypeEx_AppContainerExe;
	}

	public static bool IsLibrary(this EnvDTE.Project project)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		return project.Properties?.Item("OutputType")?.Value is prjOutputTypeEx.prjOutputTypeEx_Library or prjOutputTypeEx.prjOutputTypeEx_WinMDObj;
	}

	public static async ValueTask<IEnumerable<Project>> GetProjectsAsync(this DTE dte, ProjectAttribute? attributes = null)
	{
		var projects = attributes?.HasFlag(ProjectAttribute.Startup) ?? false
			? await dte.GetStartupProjectsAsync()
			: await dte.GetProjectsRecursiveAsync();

		projects ??= [];

		if (attributes?.HasFlag(ProjectAttribute.Application) ?? false)
		{
			projects = projects.Where(IsApplication);
		}
		if (attributes?.HasFlag(ProjectAttribute.Library) ?? false)
		{
			projects = projects.Where(IsLibrary);
		}

		return projects;
	}

	public static async Task<IEnumerable<string?>> GetProjectUserSettingsAsync(this DTE dte, AsyncPackage asyncPackage, string name, ProjectAttribute? attributes = null)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		return await asyncPackage.GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution
			? []
			: (await dte.GetProjectsAsync(attributes))
			.Select(GetHierarchy)
			.OfType<IVsBuildPropertyStorage>()
			.Select(propertyStorage => GetUserProperty(propertyStorage, name))
			.ToArray();

#pragma warning disable VSTHRD010 // False positive rule: we are on the main thread here (including the materialization of the list using the ToArray())
		IVsHierarchy GetHierarchy(Project project)
		{
			_ = solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);
			return hierarchy;
		}
#pragma warning restore VSTHRD010
	}


	public static async ValueTask<int> SetProjectUserSettingsAsync(this DTE dte, AsyncPackage asyncPackage, string name, string value, ProjectAttribute? attributes = null)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (await asyncPackage.GetServiceAsync(typeof(SVsSolution)) is not IVsSolution solution)
		{
			return -1;
		}

		var updatedProjects = 0;
		foreach (var project in await dte.GetProjectsAsync(attributes))
		{
			// Convert DTE project to IVsHierarchy
			_ = solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy);
			if (hierarchy is IVsBuildPropertyStorage propertyStorage)
			{
				propertyStorage.SetUserProperty(name, value);
				updatedProjects++;
			}
		}

		return updatedProjects;
	}

	public static void SetUserProperty(this IVsBuildPropertyStorage propertyStorage, string propertyName, string propertyValue)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		_ = propertyStorage.SetPropertyValue(
			propertyName,        // Property name
			null,                 // Configuration name, null applies to all configurations
			(uint)_PersistStorageType.PST_USER_FILE,  // Specifies that this is a user-specific property
			propertyValue             // Property value
		);
	}

	public static string? GetUserProperty(this IVsBuildPropertyStorage propertyStorage, string propertyName)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		_ = propertyStorage.GetPropertyValue(
			propertyName,        // Property name
			null,                 // Configuration name, null applies to all configurations
			(uint)_PersistStorageType.PST_USER_FILE,  // Specifies that this is a user-specific property
			out var propertyValue             // Property value
		);

		return propertyValue;
	}

	private static async Task<IEnumerable<Project>> GetProjectsRecursiveAsync(this DTE dte)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var solutionProjectItems = dte.Solution.Projects;

		return solutionProjectItems is null
			? []
			: EnumerateProjects(solutionProjectItems);
	}

	private static IEnumerable<Project> EnumerateProjects(Projects vsSolution)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		foreach (var project in vsSolution.OfType<Project>())
		{
			if (project.Kind == FolderKind /* Folder */)
			{
				foreach (var subProject in EnumSubProjects(project))
				{
					yield return subProject;
				}
			}
			else
			{
				yield return project;
			}
		}
	}

	private static IEnumerable<Project> EnumSubProjects(Project folder)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (folder.ProjectItems != null)
		{
			var subProjects = folder.ProjectItems
				.OfType<ProjectItem>()
				.Select(p =>
				{
					Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

					return p.Object;
				})
				.Where(p => p != null)
				.Cast<Project>();

			foreach (var project in subProjects)
			{
				if (project.Kind == FolderKind)
				{
					foreach (var subProject in EnumSubProjects(project))
					{
						yield return subProject;
					}
				}
				else
				{
					yield return project;
				}
			}
		}
	}

	public static async Task<Project[]?> GetStartupProjectsAsync(this DTE dte)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (dte.Solution.SolutionBuild.StartupProjects is object[] { Length: > 0 } startupProjects)
		{
			var projects = await dte.GetProjectsRecursiveAsync();

			return startupProjects
				.OfType<string>()
				.Select(startupProjectName => projects.FirstOrDefault(project =>
				{
					Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
					return project.UniqueName == startupProjectName;
				}))
				.Where(project => project is not null)
				.ToArray();
		}

		return null;
	}

	/// <summary>
	/// Retrieves the path to the .csproj file associated with the specified file by traversing upward in the directory hierarchy.
	/// </summary>
	/// <param name="filePath">The absolute path of the file for which the associated .csproj file is to be located.</param>
	/// <returns>The full path to the .csproj file.</returns>
	/// <exception cref="InvalidOperationException">
	/// Thrown if the provided <paramref name="filePath"/> is not valid, no .csproj file is found in the directory tree, 
	/// or multiple .csproj files are found in a directory, making it ambiguous to determine the correct one.
	/// </exception>
	private static string GetCsprojForFile(string filePath)
		=> Path.GetDirectoryName(filePath) is not { Length: > 0 } fileDir
			? throw new InvalidOperationException($"File path '{filePath}' is not valid (should be absolute).")
			: GetCsprojRecursive(new DirectoryInfo(fileDir));

	private static string GetCsprojRecursive(DirectoryInfo dir)
		=> dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly) switch
		{
			null or [] when dir.Parent is { } parentDir => GetCsprojRecursive(parentDir),
			null or [] => throw new InvalidOperationException($"No .csproj file found in {dir.FullName} or any of its parent directories."),
			[var singleProject] => singleProject.FullName,
			_ => throw new InvalidOperationException($"Multiple .csproj files found in {dir.FullName}, unable to determine which one to use."),
		};

	private static IEnumerable<string> GetAllCsprojRecursive(DirectoryInfo? dir)
	{
		while (dir is not null)
		{
			foreach (var csproj in dir.GetFiles("*.csproj", SearchOption.TopDirectoryOnly))
			{
				yield return csproj.FullName;
			}

			dir = dir.Parent;
		}
	}

	//private static Project FindProjectInSolution(this DTE2 dte, string projectFilePath)
	//{
	//	foreach (Project project in dte.Solution.Projects)
	//	{
	//		var p = FindProjectRecursive(project, projectFilePath);
	//		if (p != null) return p;
	//	}
	//	return null;
	//}

	public static async ValueTask<Project?> FindProjectForFileAsync(this DTE2 dte, string filePath, ProjectAttribute? attributes = null)
	{
		if (Path.GetDirectoryName(filePath) is not { Length: > 0 } fileDir)
		{
			throw new InvalidOperationException($"File path '{filePath}' is not valid (should be absolute).");
		}

		var projects = (await dte.GetProjectsAsync(attributes)).ToList();
		return GetAllCsprojRecursive(new DirectoryInfo(fileDir))
			.Select(projectFile => projects.FirstOrDefault(p => p?.FullName is { } pfn && StringComparer.OrdinalIgnoreCase.Equals(pfn, projectFile)))
			.FirstOrDefault(project => project is not null);
	}

	public static async ValueTask AddFileAsync(this Project project, string filePath, string buildAction = "Compile")
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var item = project.ProjectItems.AddFromFile(filePath);
		item.Properties.Item("ItemType").Value = buildAction;
	}
}
