using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using StreamJsonRpc;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;
using Task = System.Threading.Tasks.Task;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

public class EntryPoint : IDisposable
{
	private const string UnoPlatformOutputPane = "Uno Platform";
	private const string FolderKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
	private const string RemoteControlServerPortProperty = "UnoRemoteControlPort";
	private readonly DTE _dte;
	private readonly DTE2 _dte2;
	private readonly string _toolsPath;
	private readonly AsyncPackage _asyncPackage;
	private Action<string>? _debugAction;
	private Action<string>? _infoAction;
	private Action<string>? _verboseAction;
	private Action<string>? _warningAction;
	private Action<string>? _errorAction;
	private System.Diagnostics.Process? _process;

	private int RemoteControlServerPort;
	private bool _closing;
	private bool _isDisposed;
	private IdeChannelClient? _ideChannelClient;
	private readonly _dispSolutionEvents_BeforeClosingEventHandler _closeHandler;
	private readonly _dispBuildEvents_OnBuildDoneEventHandler _onBuildDoneHandler;
	private readonly _dispBuildEvents_OnBuildProjConfigBeginEventHandler _onBuildProjConfigBeginHandler;

	public EntryPoint(DTE2 dte2, string toolsPath, AsyncPackage asyncPackage, Action<Func<Task<Dictionary<string, string>>>> globalPropertiesProvider)
	{
		_dte = dte2 as DTE;
		_dte2 = dte2;
		_toolsPath = toolsPath;
		_asyncPackage = asyncPackage;
		globalPropertiesProvider(OnProvideGlobalPropertiesAsync);

		SetupOutputWindow();

		_closeHandler = () => SolutionEvents_BeforeClosing();
		_dte.Events.SolutionEvents.BeforeClosing += _closeHandler;

		_onBuildDoneHandler = (s, a) => BuildEvents_OnBuildDone(s, a);
		_dte.Events.BuildEvents.OnBuildDone += _onBuildDoneHandler;

		_onBuildProjConfigBeginHandler = (string project, string projectConfig, string platform, string solutionConfig) => _ = BuildEvents_OnBuildProjConfigBeginAsync(project, projectConfig, platform, solutionConfig);
		_dte.Events.BuildEvents.OnBuildProjConfigBegin += _onBuildProjConfigBeginHandler;

		// Start the RC server early, as iOS and Android projects capture the globals early
		// and don't recreate it unless out-of-process msbuild.exe instances are terminated.
		//
		// This will can possibly be removed when all projects are migrated to the sdk project system.
		_ = UpdateProjectsAsync();
	}

	private Task<Dictionary<string, string>> OnProvideGlobalPropertiesAsync()
	{
		if (RemoteControlServerPort == 0)
		{
			_warningAction?.Invoke(
				$"The Remote Control server is not yet started, providing [0] as the server port. " +
				$"Rebuilding the application may fix the issue.");
		}

		return Task.FromResult(new Dictionary<string, string> {
			{ RemoteControlServerPortProperty, RemoteControlServerPort.ToString(CultureInfo.InvariantCulture) }
		});
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

		_debugAction = s =>
		{
			if (!_closing)
			{
				owPane.OutputString("[DEBUG] " + s + "\r\n");
			}
		};
		_infoAction = s =>
		{
			if (!_closing)
			{
				owPane.OutputString("[INFO] " + s + "\r\n");
			}
		};
		_verboseAction = s =>
		{
			if (!_closing)
			{
				owPane.OutputString("[VERBOSE] " + s + "\r\n");
			}
		};
		_warningAction = s =>
		{
			if (!_closing)
			{
				owPane.OutputString("[WARNING] " + s + "\r\n");
			}
		};
		_errorAction = e =>
		{
			if (!_closing)
			{
				owPane.OutputString("[ERROR] " + e + "\r\n");
			}
		};

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
			StartServer();
			var portString = RemoteControlServerPort.ToString(CultureInfo.InvariantCulture);
			foreach (var p in await GetProjectsAsync())
			{
				var filename = string.Empty;
				try
				{
					filename = p.FileName;
				}
				catch (Exception ex)
				{
					_debugAction?.Invoke($"Exception on retrieving {p.UniqueName} details. Err: {ex}.");
					_warningAction?.Invoke($"Cannot read {p.UniqueName} project details (It may be unloaded).");
				}
				if (string.IsNullOrWhiteSpace(filename) == false
					&& GetMsbuildProject(filename) is Microsoft.Build.Evaluation.Project msbProject
					&& IsApplication(msbProject))
				{
					SetGlobalProperty(filename, RemoteControlServerPortProperty, portString);
				}
			}
		}
		catch (Exception e)
		{
			_debugAction?.Invoke($"UpdateProjectsAsync failed: {e}");
		}
	}

	private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
	{
		StartServer();
	}

	private void SolutionEvents_BeforeClosing()
	{
		// Detach event handler to avoid this being called multiple times
		_dte.Events.SolutionEvents.BeforeClosing -= _closeHandler;

		if (_process is not null)
		{
			try
			{
				_debugAction?.Invoke($"Terminating Remote Control server (pid: {_process.Id})");
				_process.Kill();
				_debugAction?.Invoke($"Terminated Remote Control server (pid: {_process.Id})");

				_ideChannelClient?.Dispose();
				_ideChannelClient = null;
			}
			catch (Exception e)
			{
				_debugAction?.Invoke($"Failed to terminate Remote Control server (pid: {_process.Id}): {e}");
			}
			finally
			{
				_closing = true;
				_process = null;

				// Invoke Dispose to make sure other event handlers are detached
				Dispose();
			}
		}
	}

	private int GetDotnetMajorVersion()
	{
		var result = ProcessHelpers.RunProcess("dotnet", "--version", Path.GetDirectoryName(_dte.Solution.FileName));

		if (result.exitCode != 0)
		{
			throw new InvalidOperationException($"Unable to detect current dotnet version (\"dotnet --version\" exited with code {result.exitCode})");
		}

		if (result.output.Contains("."))
		{
			if (int.TryParse(result.output.Substring(0, result.output.IndexOf('.')), out int majorVersion))
			{
				return majorVersion;
			}
		}

		throw new InvalidOperationException($"Unable to detect current dotnet version (\"dotnet --version\" returned \"{result.output}\")");
	}

	private void StartServer()
	{
		if (_process?.HasExited ?? true)
		{
			RemoteControlServerPort = GetTcpPort();

			var version = GetDotnetMajorVersion();
			if (version < 7)
			{
				throw new InvalidOperationException($"Unsupported dotnet version ({version}) detected");
			}
			var runtimeVersionPath = $"net{version}.0";

			var sb = new StringBuilder();

			var pipeGuid = Guid.NewGuid();

			var hostBinPath = Path.Combine(_toolsPath, "host", runtimeVersionPath, "Uno.UI.RemoteControl.Host.dll");
			string arguments = $"\"{hostBinPath}\" --httpPort {RemoteControlServerPort} --ppid {System.Diagnostics.Process.GetCurrentProcess().Id} --ideChannel \"{pipeGuid}\"";
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

			// hookup the event handlers to capture the data that is received
			_process.OutputDataReceived += (sender, args) => _debugAction?.Invoke(args.Data);
			_process.ErrorDataReceived += (sender, args) => _errorAction?.Invoke(args.Data);

			_process.StartInfo = pi;
			_process.Start();

			// start our event pumps
			_process.BeginOutputReadLine();
			_process.BeginErrorReadLine();

			_ideChannelClient = new IdeChannelClient(pipeGuid, new Logger(this));
			_ideChannelClient.ConnectToHost();
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

	public void SetGlobalProperty(string projectFullName, string propertyName, string propertyValue)
	{
		var msbuildProject = GetMsbuildProject(projectFullName);
		if (msbuildProject == null)
		{
			_debugAction?.Invoke($"Failed to find project {projectFullName}, cannot provide listen port to the app.");
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
			_debugAction?.Invoke($"Failed to find project {projectFullName}, cannot provide listen port to the app.");
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
		var outputType = project.GetPropertyValue("OutputType");
		return outputType is not null &&
   				(outputType.Equals("Exe", StringComparison.OrdinalIgnoreCase) || outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase));
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}
		_isDisposed = true;

		try
		{
			_dte.Events.BuildEvents.OnBuildDone -= _onBuildDoneHandler;
			_dte.Events.BuildEvents.OnBuildProjConfigBegin -= _onBuildProjConfigBeginHandler;
		}
		catch (Exception e)
		{
			_debugAction?.Invoke($"Failed to dispose Remote Control server: {e}");
		}
	}

	private class Logger(EntryPoint entryPoint) : ILogger
	{
		private readonly EntryPoint _entryPoint = entryPoint;

		public void Debug(string message) => _entryPoint._debugAction?.Invoke(message);
		public void Error(string message) => _entryPoint._errorAction?.Invoke(message);
		public void Info(string message) => _entryPoint._infoAction?.Invoke(message);
		public void Warn(string message) => _entryPoint._warningAction?.Invoke(message);
		public void Verbose(string message) => _entryPoint._verboseAction?.Invoke(message);
	}
}
