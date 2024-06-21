using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.DebuggerHelper;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;
using Task = System.Threading.Tasks.Task;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

public partial class EntryPoint : IDisposable
{
	private const string UnoPlatformOutputPane = "Uno Platform";
	private const string RemoteControlServerPortProperty = "UnoRemoteControlPort";
	private const string UnoVSExtensionLoadedProperty = "_UnoVSExtensionLoaded";

	private readonly CancellationTokenSource _ct = new();
	private readonly DTE _dte;
	private readonly DTE2 _dte2;
	private readonly string _toolsPath;
	private readonly AsyncPackage _asyncPackage;
	private Action<string>? _debugAction;
	private Action<string>? _infoAction;
	private Action<string>? _verboseAction;
	private Action<string>? _warningAction;
	private Action<string>? _errorAction;
	private int _msBuildLogLevel;
	private System.Diagnostics.Process? _process;

	private int RemoteControlServerPort;
	private bool _closing;
	private bool _isDisposed;
	private IdeChannelClient? _ideChannelClient;
	private ProfilesObserver _debuggerObserver;
	private readonly Func<Task> _globalPropertiesChanged;
	private readonly _dispSolutionEvents_BeforeClosingEventHandler _closeHandler;
	private readonly _dispBuildEvents_OnBuildDoneEventHandler _onBuildDoneHandler;
	private readonly _dispBuildEvents_OnBuildProjConfigBeginEventHandler _onBuildProjConfigBeginHandler;

	public EntryPoint(
		DTE2 dte2
		, string toolsPath
		, AsyncPackage asyncPackage
		, Action<Func<Task<Dictionary<string, string>>>> globalPropertiesProvider
		, Func<Task> globalPropertiesChanged)
	{
		_dte = dte2 as DTE;
		_dte2 = dte2;
		_toolsPath = toolsPath;
		_asyncPackage = asyncPackage;
		globalPropertiesProvider(OnProvideGlobalPropertiesAsync);
		_globalPropertiesChanged = globalPropertiesChanged;

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

		_debuggerObserver = new ProfilesObserver(
			asyncPackage
			, _dte
			, (previous, newFramework) => OnDebugFrameworkChangedAsync(previous, newFramework)
			, OnDebugProfileChangedAsync
			, OnStartupProjectChangedAsync
			, _debugAction);

		_ = _debuggerObserver.ObserveProfilesAsync();

		_ = _globalPropertiesChanged();

		TelemetryHelper.DataModelTelemetrySession.AddSessionChannel(new TelemetryEventListener(this));
	}

	private async Task<Dictionary<string, string>> OnProvideGlobalPropertiesAsync()
	{
		Dictionary<string, string> properties = new()
		{
			[UnoVSExtensionLoadedProperty] = "true"
		};

		if (RemoteControlServerPort != 0)
		{
			properties.Add(RemoteControlServerPortProperty, RemoteControlServerPort.ToString(CultureInfo.InvariantCulture));
		}

		await Task.Yield();

		return properties;
	}

	[MemberNotNull(
		nameof(_debugAction)
		, nameof(_infoAction)
		, nameof(_verboseAction)
		, nameof(_warningAction)
		, nameof(_errorAction))]
	private void SetupOutputWindow()
	{
		var ow = _dte2.ToolWindows.OutputWindow;

		_msBuildLogLevel = _dte2.GetMSBuildOutputVerbosity();

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
			if (!_closing && _msBuildLogLevel >= 3 /* MSBuild Log Detailed */)
			{
				owPane.OutputString("[DEBUG] " + s + "\r\n");
			}
		};
		_infoAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 2 /* MSBuild Log Normal */)
			{
				owPane.OutputString("[INFO] " + s + "\r\n");
			}
		};
		_verboseAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 4 /* MSBuild Log Diagnostic */)
			{
				owPane.OutputString("[VERBOSE] " + s + "\r\n");
			}
		};
		_warningAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 1 /* MSBuild Log Minimal */)
			{
				owPane.OutputString("[WARNING] " + s + "\r\n");
			}
		};
		_errorAction = e =>
		{
			if (!_closing && _msBuildLogLevel >= 0 /* MSBuild Log Quiet */)
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
			foreach (var p in await _dte.GetProjectsAsync())
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

			var pipeGuid = Guid.NewGuid();

			var hostBinPath = Path.Combine(_toolsPath, "host", runtimeVersionPath, "Uno.UI.RemoteControl.Host.dll");
			var arguments = $"\"{hostBinPath}\" --httpPort {RemoteControlServerPort} --ppid {System.Diagnostics.Process.GetCurrentProcess().Id} --ideChannel \"{pipeGuid}\"";
			var pi = new ProcessStartInfo("dotnet", arguments)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = Path.Combine(_toolsPath, "host"),

				// redirect the output
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

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
			_ideChannelClient.ForceHotReloadRequested += OnForceHotReloadRequestedAsync;
			_ideChannelClient.ConnectToHost();

			_ = _globalPropertiesChanged();
		}
	}

	private async Task OnForceHotReloadRequestedAsync(object? sender, ForceHotReloadIdeMessage request)
	{
		try
		{
			_dte.ExecuteCommand("Debug.ApplyCodeChanges");

			// Send a message back to indicate that the request has been received and acted upon.
			if (_ideChannelClient is not null)
			{
				await _ideChannelClient.SendToDevServerAsync(new HotReloadRequestedIdeMessage(request.CorrelationId, Result.Success()), _ct.Token);
			}
		}
		catch (Exception e) when (_ideChannelClient is not null)
		{
			await _ideChannelClient.SendToDevServerAsync(new HotReloadRequestedIdeMessage(request.CorrelationId, Result.Fail(e)), _ct.Token);

			throw;
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
			_ct.Cancel(false);
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
		public void Debug(string message) => entryPoint._debugAction?.Invoke(message);
		public void Error(string message) => entryPoint._errorAction?.Invoke(message);
		public void Info(string message) => entryPoint._infoAction?.Invoke(message);
		public void Warn(string message) => entryPoint._warningAction?.Invoke(message);
		public void Verbose(string message) => entryPoint._verboseAction?.Invoke(message);
	}
}
