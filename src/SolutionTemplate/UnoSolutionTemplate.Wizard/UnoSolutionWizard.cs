#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;

namespace UnoSolutionTemplate.Wizard
{
	public class UnoSolutionWizard : IWizard
	{
		private string? _targetPath;
		private DTE2? _dte;

		public void BeforeOpeningFile(global::EnvDTE.ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(global::EnvDTE.Project project)
		{
		}

		public void ProjectItemFinishedGenerating(global::EnvDTE.ProjectItem projectItem)
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
			SetAsStartupProject();
		}

		private void OpenWelcomePage()
			=> _dte?.ItemOperations.Navigate("https://platform.uno/docs/articles/get-started-wizard.html", vsNavigateOptions.vsNavigateOptionsNewWindow);

		private void SetAsStartupProject()
		{
			if (_dte != null && _dte.Solution.SolutionBuild is SolutionBuild2 val)
			{
				try
				{
					var uwpProject = _dte.Solution.Projects.Cast<Project>().FirstOrDefault(s => s.Name.EndsWith(".UWP", StringComparison.OrdinalIgnoreCase));

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
	}
}
