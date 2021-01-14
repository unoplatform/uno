#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;

namespace UnoSolutionTemplate.Wizard
{
	public class UnoSolutionWizard : IWizard
	{
		private const string ProjectKinds_vsProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

		private string? _targetPath;
		private DTE2? _dte;

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
			var nugetConfigPath = Path.Combine(_targetPath, "..\\.vsconfig");

			if (!File.Exists(nugetConfigPath))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream($"{GetType().Assembly.GetName().Name}..vsconfig"));
				File.WriteAllText(nugetConfigPath, reader.ReadToEnd());
			}

			OpenWelcomePage();
			SetStartupProject();
			SetUWPAnyCPUBuildableAndDeployable();
			SetDefaultConfiguration();
		}

		private void OpenWelcomePage()
			=> _dte?.ItemOperations.Navigate("https://platform.uno/docs/articles/get-started-wizard.html", vsNavigateOptions.vsNavigateOptionsNewWindow);

		private void SetStartupProject()
		{
			if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
			{
				try
				{
					var uwpProject = GetAllProjects().FirstOrDefault(s => s.Name.EndsWith(".UWP", StringComparison.OrdinalIgnoreCase));

					if (uwpProject is { })
					{
						val.StartupProjects = uwpProject.UniqueName;
					}

					var x86Config = val.SolutionConfigurations
						.Cast<SolutionConfiguration2>()
						.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "x86");

					x86Config?.Activate();
				}
				catch (Exception)
				{
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		private void SetUWPAnyCPUBuildableAndDeployable()
		{
			if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
			{
				try
				{
					var anyCpuConfig = val.SolutionConfigurations
						.Cast<SolutionConfiguration2>()
						.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "Any CPU");

					foreach (SolutionConfiguration2 solutionConfiguration2 in val.SolutionConfigurations)
					{
						foreach (SolutionContext solutionContext in anyCpuConfig.SolutionContexts)
						{
							if (solutionContext.ProjectName.EndsWith(".UWP.csproj"))
							{
								solutionContext.ShouldBuild = true;
								solutionContext.ShouldDeploy = true;
							}
						}
					}
				}
				catch (Exception)
				{
				}
			}
		}


		private void SetDefaultConfiguration()
		{
			if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
			{
				try
				{
					var x86Config = val.SolutionConfigurations
						.Cast<SolutionConfiguration2>()
						.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "x86");

					x86Config?.Activate();
				}
				catch (Exception)
				{
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			_targetPath = replacementsDictionary["$destinationdirectory$"];

			if (runKind == WizardRunKind.AsMultiProject)
			{
				_dte = (DTE2)automationObject;
			}
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}

		public Project[] GetAllProjects()
		{
			var list = new List<Project>();
			if (_dte != null)
			{
				var projects = _dte.Solution.Projects;
				var item = projects.GetEnumerator();
				while (item.MoveNext())
				{
					var project = item.Current as Project;
					if (project == null)
					{
						continue;
					}

					if (project.Kind == ProjectKinds_vsProjectKindSolutionFolder)
					{
						list.AddRange(GetSolutionFolderProjects(project));
					}
					else
					{
						list.Add(project);
					}
				}
			}

			return list.ToArray();
		}

		private Project[] GetSolutionFolderProjects(Project solutionFolder)
		{
			var list = new List<Project>();
			for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
			{
				var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
				if (subProject == null)
				{
					continue;
				}

				// If this is another solution folder, do a recursive call, otherwise add
				if (subProject.Kind == ProjectKinds_vsProjectKindSolutionFolder)
				{
					list.AddRange(GetSolutionFolderProjects(subProject));
				}
				else
				{
					list.Add(subProject);
				}
			}

			return list.ToArray();
		}
	}
}
