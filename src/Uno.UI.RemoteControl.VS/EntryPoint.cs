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
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.DebuggerHelper;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Uno.UI.RemoteControl.VS.Notifications;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;
using Task = System.Threading.Tasks.Task;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

public partial class EntryPoint : IDisposable
{
	private const string UnoPlatformOutputPane = "Uno Platform";
	private const string RemoteControlServerPortProperty = "UnoRemoteControlPort";
	private const string UnoRemoteControlConfigCookieProperty = "UnoRemoteControlConfigCookie";
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
	private SemaphoreSlim _processGate = new(1);
	private IServiceProvider? _visualStudioServiceProvider;

	private int _remoteControlServerPort;
	private string? _remoteControlConfigCookie;
	private bool _closing;
	private bool _isDisposed;
	private IdeChannelClient? _ideChannelClient;
	private ProfilesObserver? _debuggerObserver;
	private InfoBarFactory? _infoBarFactory;
	private GlobalJsonObserver? _globalJsonObserver;
	private readonly Func<Task> _globalPropertiesChanged;
	private _dispSolutionEvents_BeforeClosingEventHandler? _closeHandler;
	private _dispBuildEvents_OnBuildBeginEventHandler? _onBuildBeginHandler;
	private _dispBuildEvents_OnBuildDoneEventHandler? _onBuildDoneHandler;
	private _dispBuildEvents_OnBuildProjConfigBeginEventHandler? _onBuildProjConfigBeginHandler;
	private UnoMenuCommand? _unoMenuCommand;

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

		_ = ThreadHelper.JoinableTaskFactory.RunAsync(() => InitializeAsync(asyncPackage));
	}

	private async Task InitializeAsync(AsyncPackage asyncPackage)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		SetupOutputWindow();

		_closeHandler = () => SolutionEvents_BeforeClosing();
		_dte.Events.SolutionEvents.BeforeClosing += _closeHandler;

		_onBuildBeginHandler = (s, a) => _ = EnsureServerAsync();
		_dte.Events.BuildEvents.OnBuildBegin += _onBuildBeginHandler;

		_onBuildDoneHandler = (s, a) => _ = EnsureServerAsync();
		_dte.Events.BuildEvents.OnBuildDone += _onBuildDoneHandler;

		_onBuildProjConfigBeginHandler = (string project, string projectConfig, string platform, string solutionConfig) => _ = UpdateProjectsAsync();
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

		if (await _asyncPackage.GetServiceAsync(typeof(SVsShell)) is IVsShell shell
			&& await _asyncPackage.GetServiceAsync(typeof(SVsInfoBarUIFactory)) is IVsInfoBarUIFactory infoBarFactory)
		{
			_infoBarFactory = new InfoBarFactory(infoBarFactory, shell);

			_globalJsonObserver = new GlobalJsonObserver(asyncPackage, _dte, _infoBarFactory, _debugAction, _infoAction, _warningAction, _errorAction);
		}

		_ = _debuggerObserver.ObserveProfilesAsync();

		TelemetryHelper.DataModelTelemetrySession.AddSessionChannel(new TelemetryEventListener(this));
	}

	private Task<Dictionary<string, string>> OnProvideGlobalPropertiesAsync()
	{
		Dictionary<string, string> properties = new()
		{
			[UnoVSExtensionLoadedProperty] = "true"
		};

		if (_remoteControlServerPort is not 0)
		{
			properties.Add(RemoteControlServerPortProperty, _remoteControlServerPort.ToString(CultureInfo.InvariantCulture));
		}

		if (_remoteControlConfigCookie is not null)
		{
			properties.Add(UnoRemoteControlConfigCookieProperty, _remoteControlConfigCookie);
		}

		return Task.FromResult(properties);
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
				owPane.OutputString($"[DEBUG][{DateTime.Now:HH:mm:ss.ff}] " + s + "\r\n");
			}
		};
		_infoAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 2 /* MSBuild Log Normal */)
			{
				owPane.OutputString($"[INFO][{DateTime.Now:HH:mm:ss.ff}] " + s + "\r\n");
			}
		};
		_verboseAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 4 /* MSBuild Log Diagnostic */)
			{
				owPane.OutputString($"[VERBOSE][{DateTime.Now:HH:mm:ss.ff}] " + s + "\r\n");
			}
		};
		_warningAction = s =>
		{
			if (!_closing && _msBuildLogLevel >= 1 /* MSBuild Log Minimal */)
			{
				owPane.OutputString($"[WARNING][{DateTime.Now:HH:mm:ss.ff}] " + s + "\r\n");
			}
		};
		_errorAction = e =>
		{
			if (!_closing && _msBuildLogLevel >= 0 /* MSBuild Log Quiet */)
			{
				owPane.OutputString($"[ERROR][{DateTime.Now:HH:mm:ss.ff}] " + e + "\r\n");
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

	private async Task UpdateProjectsAsync()
	{
		try
		{
			await EnsureServerAsync();
		}
		catch (Exception e)
		{
			_debugAction?.Invoke($"UpdateProjectsAsync failed: {e}");
		}
	}

	private void SolutionEvents_BeforeClosing()
	{
		// Detach event handler to avoid this being called multiple times
		_dte.Events.SolutionEvents.BeforeClosing -= _closeHandler;

		_closing = true;
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

	private async Task EnsureServerAsync()
	{
		_debugAction?.Invoke($"Starting server (tid:{Environment.CurrentManagedThreadId})");

		if (_process is { HasExited: false })
		{
			_debugAction?.Invoke("Server already running");
			return;
		}

		await _processGate.WaitAsync();
		try
		{

			if (EnsureTcpPort(ref _remoteControlServerPort))
			{
				// Update the cookie file, so a rebuild will be triggered
				_remoteControlConfigCookie ??= Path.GetTempFileName();
				File.WriteAllText(_remoteControlConfigCookie, _remoteControlServerPort.ToString(CultureInfo.InvariantCulture));

				// Push the new port to the project using global properties
				_ = _globalPropertiesChanged();
			}

			_debugAction?.Invoke($"Using available port {_remoteControlServerPort}");

			var version = GetDotnetMajorVersion();
			if (version < 7)
			{
				throw new InvalidOperationException($"Unsupported dotnet version ({version}) detected");
			}

			var pipeGuid = Guid.NewGuid();

			var hostBinPath = Path.Combine(_toolsPath, "host", $"net{version}.0", "Uno.UI.RemoteControl.Host.dll");
			var arguments = $"\"{hostBinPath}\" --httpPort {_remoteControlServerPort} --ppid {System.Diagnostics.Process.GetCurrentProcess().Id} --ideChannel \"{pipeGuid}\" --solution \"{_dte.Solution.FullName}\"";
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

			_process = new System.Diagnostics.Process { EnableRaisingEvents = true };

			// hookup the event handlers to capture the data that is received
			_process.OutputDataReceived += (sender, args) => _debugAction?.Invoke(args.Data);
			_process.ErrorDataReceived += (sender, args) => _errorAction?.Invoke(args.Data);

			_process.StartInfo = pi;
			_process.Exited += (sender, args) => _ = RestartAsync();

			if (_process.Start())
			{
				// start our event pumps
				_process.BeginOutputReadLine();
				_process.BeginErrorReadLine();

				_ideChannelClient = new IdeChannelClient(pipeGuid, new Logger(this));
				_ideChannelClient.OnMessageReceived += OnMessageReceivedAsync;
				_ideChannelClient.ConnectToHost();

				// Set the port to the projects
				// This LEGACY as port should be set through the global properties (cf. OnProvideGlobalPropertiesAsync)
				var portString = _remoteControlServerPort.ToString(CultureInfo.InvariantCulture);
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				var projects = (await _dte.GetProjectsAsync()).ToArray(); // EnumerateProjects must be called on the UI thread.
				await TaskScheduler.Default;
				foreach (var project in projects)
				{
					var filename = string.Empty;
					try
					{
						filename = project.FileName;
					}
					catch (Exception ex)
					{
						_debugAction?.Invoke($"Exception on retrieving {project.UniqueName} details. Err: {ex}.");
						_warningAction?.Invoke($"Cannot read {project.UniqueName} project details (It may be unloaded).");
					}
					if (string.IsNullOrWhiteSpace(filename) == false
						&& GetMsbuildProject(filename) is Microsoft.Build.Evaluation.Project msbProject
						&& IsApplication(msbProject))
					{
						SetGlobalProperty(filename, RemoteControlServerPortProperty, portString);
					}
				}
			}
			else
			{
				_process = null;
				_remoteControlServerPort = 0;
			}
		}
		catch (Exception e)
		{
			_errorAction?.Invoke($"Failed to start server: {e}");
		}
		finally
		{
			_processGate.Release();
		}

		async Task RestartAsync()
		{
			if (_closing || _ct.IsCancellationRequested)
			{
				if (_remoteControlConfigCookie is not null)
				{
					File.WriteAllText(_remoteControlConfigCookie, "--closed--"); // Make sure VS will re-build on next start
				}

				_debugAction?.Invoke($"Remote Control server exited ({_process?.ExitCode}) and won't be restarted as solution is closing.");
				return;
			}

			_debugAction?.Invoke($"Remote Control server exited ({_process?.ExitCode}). It will restart in 5sec.");

			await Task.Delay(5000, _ct.Token);

			if (_closing || _ct.IsCancellationRequested)
			{
				if (_remoteControlConfigCookie is not null)
				{
					File.WriteAllText(_remoteControlConfigCookie, "--closed--"); // Make sure VS will re-build on next start
				}

				_debugAction?.Invoke($"Remote Control server will not be restarted as solution is closing.");
				return;
			}

			await EnsureServerAsync();
		}
	}

	private async Task OnMessageReceivedAsync(object? sender, IdeMessage devServerMessage)
	{
		try
		{
			switch (devServerMessage)
			{
				case AddMenuItemRequestIdeMessage amir:
					await OnAddMenuItemRequestedAsync(sender, amir);
					break;
				case ForceHotReloadIdeMessage fhr:
					await OnForceHotReloadRequestedAsync(sender, fhr);
					break;
				case NotificationRequestIdeMessage nr:
					await OnNotificationRequestedAsync(sender, nr);
					break;
				default:
					_debugAction?.Invoke($"Unknown message type {devServerMessage?.GetType()} from DevServer");
					break;
			}
		}
		catch (Exception e)
		{
			_debugAction?.Invoke($"Failed to handle IdeMessage with message {e.Message}");
		}
	}

	private async Task OnNotificationRequestedAsync(object? sender, NotificationRequestIdeMessage message)
	{
		await _asyncPackage.JoinableTaskFactory.SwitchToMainThreadAsync();

		if (await _asyncPackage.GetServiceAsync(typeof(SVsShell)) is IVsShell shell &&
			await _asyncPackage.GetServiceAsync(typeof(SVsInfoBarUIFactory)) is IVsInfoBarUIFactory infoBarFactory)
		{
			await CreateInfoBarAsync(message, shell, infoBarFactory);
		}
	}

	private async Task OnAddMenuItemRequestedAsync(object? sender, AddMenuItemRequestIdeMessage cr)
	{
		if (_ideChannelClient == null)
		{
			return;
		}

		if (_unoMenuCommand is not null)
		{
			//ignore when duplicated
			if (!_unoMenuCommand.CommandList.Contains(cr))
			{
				_unoMenuCommand.CommandList.Add(cr);
			}
		}
		else
		{
			_unoMenuCommand = await UnoMenuCommand.InitializeAsync(_asyncPackage, _ideChannelClient, cr);
		}
	}

	private async Task CreateInfoBarAsync(NotificationRequestIdeMessage e, IVsShell shell, IVsInfoBarUIFactory infoBarFactory)
	{
		if (_ideChannelClient is null || _infoBarFactory is null)
		{
			return;
		}

		var infoBar = await _infoBarFactory.CreateAsync(
			new InfoBarModel(
				new ActionBarTextSpan[] {
					new(e.Title, Bold: true),
					new(" " + e.Message)
				},
				e.Commands.Select(Commands => new ActionBarItem(Commands.Text, Commands.Name, ActionContext: Commands.Parameter, IsButton: true)).ToArray(),
				e.Kind == NotificationKind.Information ? KnownMonikers.StatusInformation : KnownMonikers.StatusError,
				isCloseButtonVisible: true));

		if (infoBar is not null)
		{
			infoBar.ActionItemClicked += (s, e) =>
			{
				_asyncPackage.JoinableTaskFactory.Run(async () =>
				{
					if (e.ActionItem is ActionBarItem action &&
						action.Name is { } command)
					{
						var cmd = new CommandRequestIdeMessage(
							System.Diagnostics.Process.GetCurrentProcess().Id,
							command,
							action.ActionContext?.ToString());

						await _ideChannelClient.SendToDevServerAsync(cmd, _ct.Token);

						infoBar.Close();
					}
				});
			};
			await infoBar.TryShowInfoBarUIAsync();
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

	private bool EnsureTcpPort(ref int port)
	{
		TcpListener tcp;

		if (port is not 0)
		{
			// If possible we try to re-use the same port, so running apps will be able to resume connection
			// (and we prevent a rebuild of the application).
			try
			{
				tcp = new TcpListener(IPAddress.Loopback, port);
				tcp.Start();
				tcp.Stop();

				return false;
			}
			catch
			{
				_debugAction?.Invoke($"Failed to reused previous port {port}, choosing a new one.");
			}
		}

		tcp = new TcpListener(IPAddress.Loopback, 0);
		tcp.Start();
		port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();

		return true; // HasChanged
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

	private static Microsoft.Build.Evaluation.Project? GetMsbuildProject(string projectFullName)
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
			_dte.Events.BuildEvents.OnBuildBegin -= _onBuildBeginHandler;
			_dte.Events.BuildEvents.OnBuildDone -= _onBuildDoneHandler;
			_dte.Events.BuildEvents.OnBuildProjConfigBegin -= _onBuildProjConfigBeginHandler;
			_globalJsonObserver?.Dispose();
			_infoBarFactory?.Dispose();
			_unoMenuCommand?.Dispose();
		}
		catch (Exception e)
		{
			_debugAction?.Invoke($"Failed to dispose Remote Control server: {e}");
		}
	}

	protected IServiceProvider VisualStudioServiceProvider
	{
		get
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_visualStudioServiceProvider == null)
			{
				_visualStudioServiceProvider = _dte as IServiceProvider;
				if (_visualStudioServiceProvider == null)
				{
					var serviceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider?)((_dte is Microsoft.VisualStudio.OLE.Interop.IServiceProvider) ? _dte : null);
					_visualStudioServiceProvider = (IServiceProvider)new Microsoft.VisualStudio.Shell.ServiceProvider(serviceProvider);
				}
			}
			return _visualStudioServiceProvider;
		}
	}

	private Version? GetVisualStudioReleaseVersion()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (VisualStudioServiceProvider?.GetService(typeof(SVsShell)) is IVsShell service)
		{
			if (service.GetProperty(-9068, out var releaseVersion) != 0)
			{
				return null;
			}
			if (releaseVersion is string releaseVersionAsText && Version.TryParse(releaseVersionAsText.Split(' ')[0], out var result))
			{
				return result;
			}
		}

		return null;
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
