using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.TemplateWizard;

namespace UnoSolutionTemplate.Wizard
{
	public class UnoSolutionWizard : IWizard
	{
		private string _targetPath;

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
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			_targetPath = replacementsDictionary["$destinationdirectory$"];
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}
	}
}
