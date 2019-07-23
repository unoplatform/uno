using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.UI.HotReload.VS
{
	public class EntryPoint
	{
		private const string UnoPlatformOutputPane = "Uno Platform";

		private readonly DTE _dte;
		private readonly DTE2 _dte2;
		private readonly string _toolsPath;

		private Action<string> _infoAction;
		private Action<string> _verboseAction;
		private Action<string> _warningAction;
		private Action<string> _errorAction;

		public EntryPoint(DTE2 dte2, string toolsPath)
		{
			_dte = dte2 as DTE;
			_dte2 = dte2;
			_toolsPath = toolsPath;

			SetupOutputWindow();

			_dte.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
		}

		private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
		{
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
	}
}
