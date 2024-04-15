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

namespace Uno.UI.RemoteControl.VS.Helpers;

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

	public static async Task<IEnumerable<Project>> GetProjectsAsync(this DTE dte)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var solutionProjectItems = dte.Solution.Projects;

		if (solutionProjectItems != null)
		{
			return EnumerateProjects(solutionProjectItems);
		}
		else
		{
			return Array.Empty<Project>();
		}
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

		if (dte.Solution.SolutionBuild.StartupProjects is object[] startupProjects
			&& startupProjects.Length > 0)
		{
			var projects = await dte.GetProjectsAsync();

			return startupProjects
				.OfType<string>()
				.Select(p => projects.FirstOrDefault(p2 =>
				{
					Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
					return p2.UniqueName == p;
				}))
				.Where(p => p is not null).ToArray();
		}

		return null;
	}
}
