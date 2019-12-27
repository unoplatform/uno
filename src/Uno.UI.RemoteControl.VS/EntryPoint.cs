using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Uno.UI.RemoteControl.VS
{
	public class EntryPoint
	{
		private const string UnoPlatformOutputPane = "Uno Platform";
		private const string FolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
		private const string RemoteControlServerPortProperty = "UnoRemoteControlPort";
		private readonly DTE _dte;
		private readonly DTE2 _dte2;
		private readonly string _toolsPath;
		private readonly AsyncPackage _asyncPackage;
		private Action<string> _debugAction;
		private Action<string> _infoAction;
		private Action<string> _verboseAction;
		private Action<string> _warningAction;
		private Action<string> _errorAction;
		private System.Diagnostics.Process _process;

		private int RemoteControlServerPort;

		public EntryPoint(DTE2 dte2, string toolsPath, AsyncPackage asyncPackage, Action<Func<Task<Dictionary<string, string>>>> globalPropertiesProvider)
		{
			_dte = dte2 as DTE;
			_dte2 = dte2;
			_toolsPath = toolsPath;
			_asyncPackage = asyncPackage;
			globalPropertiesProvider(OnProvideGlobalPropertiesAsync);

			SetupOutputWindow();

			_dte.Events.SolutionEvents.BeforeClosing += () => SolutionEvents_BeforeClosingAsync();

			_dte.Events.BuildEvents.OnBuildDone +=
				(s, a) => BuildEvents_OnBuildDoneAsync(s, a);

			_dte.Events.BuildEvents.OnBuildProjConfigBegin += 
				(string project, string projectConfig, string platform, string solutionConfig) => BuildEvents_OnBuildProjConfigBeginAsync(project, projectConfig, platform, solutionConfig);

			// Start the RC server early, as iOS and Android projects capture the globals early
			// and don't recreate it unless out-of-process msbuild.exe instances are terminated.
			//
			// This will can possibly be removed when all projects are migrated to the sdk project system.
			UpdateProjectsAsync();
		}

		private async Task<Dictionary<string, string>> OnProvideGlobalPropertiesAsync()
		{
			if (RemoteControlServerPort == 0)
			{
				_warningAction(
					$"The Remote Control server is not yet started, providing [0] as the server port. " +
					$"Rebuilding the application may fix the issue.");
			}

			return new Dictionary<string, string> {
				{ RemoteControlServerPortProperty, RemoteControlServerPort.ToString(CultureInfo.InvariantCulture) }
			};
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

			_debugAction = s => owPane.OutputString("[DEBUG] " + s + "\r\n");
			_infoAction = s => owPane.OutputString("[INFO] " + s + "\r\n");
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

		private async Task BuildEvents_OnBuildProjConfigBeginAsync(string project, string projectConfig, string platform, string solutionConfig)
		{
			await UpdateProjectsAsync();
		}

		private async Task UpdateProjectsAsync()
		{
			try
			{
				await StartServerAsync();

				foreach (var p in await GetProjectsAsync())
				{
					if (GetMsbuildProject(p.FileName) is Microsoft.Build.Evaluation.Project msbProject && IsApplication(msbProject))
					{
						var portString = RemoteControlServerPort.ToString(CultureInfo.InvariantCulture);
						SetGlobalProperty(p.FileName, RemoteControlServerPortProperty, portString);
					}
				}
			}
			catch (Exception e)
			{
				_debugAction($"UpdateProjectsAsync failed: {e}");
			}
		}

		private async Task BuildEvents_OnBuildDoneAsync(vsBuildScope Scope, vsBuildAction Action)
		{
			await StartServerAsync();
		}

		private async Task SolutionEvents_BeforeClosingAsync()
		{
			if (_process != null)
			{
				try
				{
					_debugAction($"Terminating Remote Control server (pid: {_process.Id})");
					_process.Kill();
					_debugAction($"Terminated Remote Control server (pid: {_process.Id})");
				}
				catch (Exception e)
				{
					_debugAction($"Failed to terminate Remote Control server (pid: {_process.Id}): {e}");
				}
				finally
				{
					_process = null;
				}
			}
		}

		private async Task StartServerAsync()
		{
			if (_process?.HasExited ?? true)
			{
				RemoteControlServerPort = GetTcpPort();

				var sb = new StringBuilder();

				var hostBinPath = Path.Combine(_toolsPath, "host", "Uno.UI.RemoteControl.Host.dll");
				string arguments = $"{hostBinPath} --httpPort {RemoteControlServerPort}";
				var pi = new ProcessStartInfo("dotnet", arguments)
				{
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = Path.Combine(_toolsPath, "host"),
				};

				// redirect the output
				pi.RedirectStandardOutput = true;
				pi.RedirectStandardError = true;

				_process = new System.Diagnostics.Process();

				// hookup the eventhandlers to capture the data that is received
				_process.OutputDataReceived += (sender, args) => _debugAction(args.Data);
				_process.ErrorDataReceived += (sender, args) => _errorAction(args.Data);

				_process.StartInfo = pi;
				_process.Start();

				// start our event pumps
				_process.BeginOutputReadLine();
				_process.BeginErrorReadLine();
			}
		}

		private static int GetTcpPort()
		{
			var l = new TcpListener(IPAddress.Loopback, 0);
			l.Start();
			var port = ((IPEndPoint)l.LocalEndpoint).Port;
			l.Stop();
			return port;
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

		private IEnumerable<EnvDTE.Project> EnumSubProjects(EnvDTE.Project folder)
		{
			if (folder.ProjectItems != null)
			{
				var subProjects = folder.ProjectItems
					.OfType<EnvDTE.ProjectItem>()
					.Select(p => p.Object)
					.Where(p => p != null)
					.Cast<EnvDTE.Project>();

				foreach (var project in subProjects)
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
			var msbuildProject = GetMsbuildProject(projectFullName);
			if (msbuildProject == null)
			{
				_debugAction($"Failed to find project {projectFullName}, cannot provide listen port to the app.");
			}
			else
			{
				SetGlobalProperty(msbuildProject, propertyName, propertyValue);
			}
		}

		private static Microsoft.Build.Evaluation.Project GetMsbuildProject(string projectFullName)
			=> ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFullName).FirstOrDefault();

		public void SetGlobalProperties(string projectFullName, IDictionary<string, string> properties)
		{
			var msbuildProject = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(projectFullName).FirstOrDefault();
			if (msbuildProject == null)
			{
				_debugAction($"Failed to find project {projectFullName}, cannot provide listen port to the app.");
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

		private bool IsApplication(Microsoft.Build.Evaluation.Project project)
		{
			var isAndroidApp = project.GetPropertyValue("AndroidApplication")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
			var isiOSApp = project.GetPropertyValue("ProjectTypeGuids")?.Equals("{FEACFBD2-3405-455C-9665-78FE426C6842};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var ismacOSApp = project.GetPropertyValue("ProjectTypeGuids")?.Equals("{A3F8F2AB-B479-4A4A-A458-A89E7DC349F1};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", StringComparison.OrdinalIgnoreCase) ?? false;
			var isExe = project.GetPropertyValue("OutputType")?.Equals("Exe", StringComparison.OrdinalIgnoreCase) ?? false;
			var isWasm = project.GetPropertyValue("WasmHead")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

			return isAndroidApp
				|| (isiOSApp && isExe)
				|| (ismacOSApp && isExe)
				|| isWasm;
		}
	}
}
