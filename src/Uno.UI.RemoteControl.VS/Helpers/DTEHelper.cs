using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
}
