using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace Uno.UI.RemoteControl.VS.Helpers;

internal static class DTEHelper
{
	public static int GetMSBuildOutputVerbosity(this DTE2 dte)
	{
		ThreadHelper.ThrowIfNotOnUIThread();
		var properties = dte?.Properties["Environment", "ProjectsAndSolution"];
		var logOutput = properties?.Item("MSBuildOutputVerbosity").Value is int log ? log : 0;
		return logOutput;
	}
}
