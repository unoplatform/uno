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
		private readonly bool _enableNuGetConfig;
		private readonly string _vsSuffix;
		private string? _targetPath;
		private DTE2? _dte;

		public UnoSolutionWizard(bool enableNuGetConfig, string vsSuffix)
		{
			_enableNuGetConfig = enableNuGetConfig;
			_vsSuffix = vsSuffix;
		}

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
			var vsConfigPath = Path.Combine(_targetPath, "..\\.vsconfig");

			if (!File.Exists(vsConfigPath))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(FindManifestFileName($".vsconfig.{_vsSuffix}")));
				File.WriteAllText(vsConfigPath, reader.ReadToEnd());
			}

			var globalJson = Path.Combine(_targetPath, "..\\global.json");

			if (!File.Exists(globalJson))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(FindManifestFileName($"global.json")));
				File.WriteAllText(globalJson, reader.ReadToEnd());
			}

			var nugetConfigPath = Path.Combine(_targetPath, "..\\NuGet.config");

			if (_enableNuGetConfig && !File.Exists(nugetConfigPath))
			{
				using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream(FindManifestFileName("NuGet-netcore.config")));
				File.WriteAllText(nugetConfigPath, reader.ReadToEnd());
			}

			OpenWelcomePage();
			SetStartupProject();
			SetUWPAnyCPUBuildableAndDeployable();
			SetiOSDeployable();
			SetDefaultConfiguration();
		}

		private string FindManifestFileName(string fileName)
			=> GetType().Assembly.GetManifestResourceNames().FirstOrDefault(f => f.EndsWith("." + fileName, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Unable to find [{fileName}] in the assembly");

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			_targetPath = replacementsDictionary["$destinationdirectory$"];

			if (runKind == WizardRunKind.AsMultiProject)
			{
				_dte = (DTE2)automationObject;
			}
		}

		public bool ShouldAddProjectItem(string filePath) => true;

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

		private void OpenWelcomePage()
			=> _dte?.ItemOperations.Navigate("https://platform.uno/docs/articles/get-started-wizard.html", vsNavigateOptions.vsNavigateOptionsNewWindow);

		private void SetStartupProject()
		{
			try
			{
				if (_dte?.Solution.SolutionBuild is SolutionBuild val)
				{
					var uwpProject = GetAllProjects().FirstOrDefault(s => s.Name.EndsWith(".UWP", StringComparison.OrdinalIgnoreCase));

					if (uwpProject is { })
					{
						val.StartupProjects = uwpProject.UniqueName;
					}

					GetSolutionConfiguration(val, "Debug", "x86")?.Activate();
				}
				else
				{
					// Unable to set the startup project when running from RunFinished since VS 2022 17.2 Preview 2
					// throw new InvalidOperationException();
				}
			}
			catch (Exception)
			{
			}
		}

		private static SolutionConfiguration GetSolutionConfiguration(SolutionBuild solutionBuild, string configuration, string platformName)
			=> solutionBuild
				.SolutionConfigurations
				.Cast<SolutionConfiguration>()
				.FirstOrDefault(c =>
				{
					// Use reflection to get to SolutionConfiguration2 because VS 2019 references don't match runtime types on VS 2022.
					var solutionPlatformName = c.GetType()
						.GetProperty("EnvDTE80.SolutionConfiguration2.PlatformName", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
						?.GetValue(c)
						?.ToString();

					return c.Name == configuration && platformName == solutionPlatformName;
				});

		private void SetUWPAnyCPUBuildableAndDeployable()
		{
			if (_dte?.Solution.SolutionBuild is SolutionBuild val)
			{
				try
				{
					var anyCpuConfig = GetSolutionConfiguration(val, "Debug", "Any CPU");

					foreach (SolutionConfiguration solutionConfiguration2 in val.SolutionConfigurations)
					{
						foreach (SolutionContext solutionContext in anyCpuConfig.SolutionContexts)
						{
							if (solutionContext.ProjectName.EndsWith(".UWP.csproj", StringComparison.Ordinal))
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

		private void SetiOSDeployable()
		{
			if (_dte?.Solution.SolutionBuild is SolutionBuild val)
			{
				try
				{
					var iOSConfigs = new[] {
						GetSolutionConfiguration(val, "Debug", "iPhone"),
						GetSolutionConfiguration(val, "Debug", "iPhoneSimulator")
					}.Select(c => c.SolutionContexts);

					foreach (SolutionConfiguration solutionConfiguration2 in val.SolutionConfigurations)
					{
						foreach (var iOSSolutionContext in iOSConfigs)
						{
							var iOSProject = iOSSolutionContext
								.Cast<SolutionContext>()
								.FirstOrDefault(c => c.ProjectName.EndsWith(".iOS.csproj", StringComparison.Ordinal));
							if (iOSProject != null)
							{
								iOSProject.ShouldDeploy = true;
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
			try
			{
				if (_dte?.Solution.SolutionBuild is SolutionBuild2 val)
				{
					var x86Config = val.SolutionConfigurations
						.Cast<SolutionConfiguration2>()
						.FirstOrDefault(c => c.Name == "Debug" && c.PlatformName == "x86");

					x86Config?.Activate();
				}
				else
				{
					// Unable to set the startup project when running from RunFinished since VS 2022 17.2 Preview 2
					// throw new InvalidOperationException();
				}
			}
			catch (Exception)
			{
			}
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
