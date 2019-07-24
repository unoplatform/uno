using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Uno.UI.HotReload.VS
{
	public class EntryPoint
	{
		private const string UnoPlatformOutputPane = "Uno Platform";
		private const string FolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
		private readonly DTE _dte;
		private readonly DTE2 _dte2;
		private readonly string _toolsPath;
		private readonly AsyncPackage _asyncPackage;
		private Action<string> _infoAction;
		private Action<string> _verboseAction;
		private Action<string> _warningAction;
		private Action<string> _errorAction;

		public EntryPoint(DTE2 dte2, string toolsPath, AsyncPackage asyncPackage)
		{
			_dte = dte2 as DTE;
			_dte2 = dte2;
			_toolsPath = toolsPath;
			_asyncPackage = asyncPackage;

			SetupOutputWindow();

			_dte.Events.BuildEvents.OnBuildBegin += (s, e) => BuildEvents_OnBuildBeginAsync(s, e);
		}

		private void SetupOutputWindow()
		{
			var ow = _dte2.ToolWindows.OutputWindow;

			// Add a new pane to the Output window.
			var owPane = ow
				.OutputWindowPanes
				.OfType<OutputWindowPane>()
				.FirstOrDefault(p => p.Name == UnoPlatformOutputPane);

			if (owPane == null)
			{
				owPane = ow
				.OutputWindowPanes
				.Add(UnoPlatformOutputPane);
			}

			_infoAction = s => owPane.OutputString("[INFO] " + s + "\r\n");
			_verboseAction = s => owPane.OutputString("[VERBOSE] " + s + "\r\n");
			_warningAction = s => owPane.OutputString("[WARNING] " + s + "\r\n");
			_errorAction = e => owPane.OutputString("[ERROR] " + e + "\r\n");

			_infoAction($"Uno Remote Control initialized ({GetAssemblyVersion()})");
		}

		private object GetAssemblyVersion()
		{
			var assembly = GetType().GetTypeInfo().Assembly;

			if (assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>() is AssemblyInformationalVersionAttribute aiva)
			{
				return aiva.InformationalVersion;
			}
			else if (assembly.GetCustomAttribute<AssemblyVersionAttribute>() is AssemblyVersionAttribute ava)
			{
				return ava.Version;
			}
			else
			{
				return "Unknown";
			}
		}

		private async Task BuildEvents_OnBuildBeginAsync(vsBuildScope Scope, vsBuildAction Action)
		{
			foreach(var project in await GetProjectsAsync())
			{
				SetGlobalProperty(project.FileName, "UnoRemoteControlPort", "12345");
			}
		}

		private async System.Threading.Tasks.Task<IEnumerable<EnvDTE.Project>> GetProjectsAsync()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var projectService = await _asyncPackage.GetServiceAsync(typeof(IProjectService)) as IProjectService;

			var solutionProjectItems = _dte.Solution.Projects;

			if (solutionProjectItems != null)
			{
				return EnumerateProjects(solutionProjectItems);				
			}
			else
			{
				return Array.Empty<EnvDTE.Project>();
			}
		}

		private IEnumerable<EnvDTE.Project> EnumerateProjects(EnvDTE.Projects vsSolution)
		{
			foreach (var project in vsSolution.OfType<EnvDTE.Project>())
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

		private IEnumerable<EnvDTE.Project> EnumSubProjects(EnvDTE.Project solutionFolder)
		{
			if (solutionFolder.ProjectItems != null)
			{
				foreach(var project in solutionFolder.ProjectItems.OfType<EnvDTE.Project>())
				{
					if(project.Kind == FolderKind)
					{
						foreach(var subProject in EnumSubProjects(project))
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

		public void SetGlobalProperty(string projectFullName, string propertyName, string propertyValue)
		{
			var msbuildProject = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFullName).FirstOrDefault();
			if (msbuildProject == null)
			{

			}
			else
			{
				SetGlobalProperty(msbuildProject, propertyName, propertyValue);
			}
		}

		public void SetGlobalProperties(string projectFullName, IDictionary<string, string> properties)
		{
			var msbuildProject = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFullName).FirstOrDefault();
			if (msbuildProject == null)
			{
			}
			else
			{
				foreach (var property in properties)
				{
					SetGlobalProperty(msbuildProject, property.Key, property.Value);
				}
			}
		}

		private void SetGlobalProperty(Microsoft.Build.Evaluation.Project msbuildProject, string propertyName, string propertyValue)
		{
			msbuildProject.SetGlobalProperty(propertyName, propertyValue);
		}
	}
}
