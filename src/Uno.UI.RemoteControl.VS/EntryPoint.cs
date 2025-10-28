using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using StreamJsonRpc;
using Uno.IDE;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.DebuggerHelper;
using Uno.UI.RemoteControl.VS.Helpers;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Uno.UI.RemoteControl.VS.Notifications;
using ILogger = Uno.UI.RemoteControl.VS.Helpers.ILogger;
using Task = System.Threading.Tasks.Task;
using _udeiMsg = Uno.UI.RemoteControl.Messaging.IdeChannel.DevelopmentEnvironmentStatusIdeMessage;

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
	private (System.Diagnostics.Process process, int port, CancellationTokenSource attachedServices)? _devServer;
	private SemaphoreSlim _devServerGate = new(1);
	private IServiceProvider? _visualStudioServiceProvider;
	private bool _closing;
	private bool _isDisposed;
	private IdeChannelClient? _ideChannelClient;
	private ProfilesObserver? _debuggerObserver;
	private InfoBarFactory? _infoBarFactory;
	private GlobalJsonObserver? _globalJsonObserver;
	private _dispSolutionEvents_BeforeClosingEventHandler? _closeHandler;
	private _dispBuildEvents_OnBuildBeginEventHandler? _onBuildBeginHandler;
	private _dispBuildEvents_OnBuildDoneEventHandler? _onBuildDoneHandler;
	private _dispBuildEvents_OnBuildProjConfigBeginEventHandler? _onBuildProjConfigBeginHandler;
	private UnoMenuCommand? _unoMenuCommand;
	private IUnoDevelopmentEnvironmentIndicator? _udei;
	private CompositeCommandHandler _commands;

	// Legacy API v2
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
		_commands = new(new Logger(this), ("VS.RC", CommonCommandHandlers.OpenBrowser), ("Dev Server", new DevServerCommandHandler(this)));

		_ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
		{
			globalPropertiesProvider(OnProvideGlobalPropertiesAsync);

			var services = new SimpleServiceProvider();
			await InitializeAsync(asyncPackage, services, _ct.Token);
		});
	}

	// Current API v3
	public EntryPoint(
		DTE2 dte2,
		string toolsPath,
		AsyncPackage asyncPackage,
		string vsixChannelHandle)
	{
		_dte = dte2 as DTE;
		_dte2 = dte2;
		_toolsPath = toolsPath;
		_asyncPackage = asyncPackage;
		_commands = new(new Logger(this), ("VS.RC", CommonCommandHandlers.OpenBrowser), ("Dev Server", new DevServerCommandHandler(this)));

		_ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
		{
			var services = await InitializeVsixChannelAsync(
				vsixChannelHandle,
				remoteServices: [
					typeof(Uno.IDE.IUnoDevelopmentEnvironmentIndicator)
				],
				localServices: [
					(typeof(Uno.IDE.IGlobalPropertiesProvider), new GlobalPropertiesProvider(OnProvideGlobalPropertiesAsync)),
					(typeof(Uno.IDE.ICommandHandler), _commands)
				],
				_ct.Token);

			await InitializeAsync(asyncPackage, services, _ct.Token);
		});
	}

	private async Task<IServiceProvider> InitializeVsixChannelAsync(string vsixChannelHandle, Type[] remoteServices, (Type type, object instance)[] localServices, CancellationToken ct)
	{
		var rpcStream = new NamedPipeClientStream(
			serverName: ".",
			pipeName: vsixChannelHandle,
			direction: PipeDirection.InOut,
			options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);
		await rpcStream.ConnectAsync(ct).ConfigureAwait(false);

		var rpc = new JsonRpc(rpcStream);
		ct.Register(rpc.Dispose);

		foreach (var service in localServices)
		{
			rpc.AddLocalRpcTarget(service.type, service.instance, null);
		}

		var services = new SimpleServiceProvider();
		ct.Register(services.Dispose);

		foreach (var service in remoteServices)
		{
			services.Register(service, rpc.Attach(service));
		}

		rpc.StartListening();

		return services;
	}

	private async Task InitializeAsync(AsyncPackage asyncPackage, IServiceProvider services, CancellationToken ct)
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		SetupOutputWindow();
		_udei = services.GetService<IUnoDevelopmentEnvironmentIndicator>();

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
		StopDevServer();
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

	private async Task OnStartupProjectChangedAsync()
	{
		if (_dte.Solution.SolutionBuild.StartupProjects is null)
		{
			// The user unloaded all projects, we need to reset the state
			_isFirstProfileTfmChange = true;
		}

		if (!await HasUnoTargetFrameworkInStartupProjectAsync() && _debuggerObserver is not null)
		{
			_debugAction?.Invoke($"The user setting is not yet initialized, aligning framework and profile");

			// The user settings file is not available, we have created the
			// file, but we also need to align the profile.
			string currentActiveDebugFramework = "";

			var hasTargetFramework = _debuggerObserver
				.UnconfiguredProject
				?.Services
				.ActiveConfiguredProjectProvider
				?.ActiveConfiguredProject
				?.ProjectConfiguration
				.Dimensions
				.TryGetValue("TargetFramework", out currentActiveDebugFramework) ?? false;

			if (hasTargetFramework)
			{
				await OnDebugFrameworkChangedAsync(null, currentActiveDebugFramework, true);
			}
		}

		// We make sure to trigger the `EnsureServerAsync` as we write the port in the user file of the startup project.
		await EnsureServerAsync();
	}

	private async Task EnsureServerAsync()
	{
		var devServerCt = default(CancellationTokenSource);
		if (_isDisposed || _closing)
		{
			return;
		}

		_debugAction?.Invoke($"Starting server (tid:{Environment.CurrentManagedThreadId})");

		// As Android projects are "library", we cannot filter on "Application" projects.
		// Instead, we persist the port only in the current startup projects ... and we make sure to re-write it when the startup project changes (cf. OnStartupProjectChangedAsync).
		const ProjectAttribute persistenceFilter = ProjectAttribute.Startup;

		await _devServerGate.WaitAsync();
		try
		{
			var persistedPorts = (await _dte
				.GetProjectUserSettingsAsync(_asyncPackage, RemoteControlServerPortProperty, persistenceFilter))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.Select(str => int.TryParse(str, out var p) ? p : -1)
				.ToArray();
			var persistedPort = persistedPorts.FirstOrDefault(p => p > 0);

			var port = _devServer?.port ?? persistedPort;
			// Determine if the port configuration is incorrect:
			// - Either no ports or multiple ports are persisted (`persistedPorts is { Length: 0 or > 1 }`).
			// - Or the currently used port (`port`) does not match the persisted port (`persistedPort`).
			var portMisConfigured = persistedPorts is { Length: 0 or > 1 } || port != persistedPort;

			if (_devServer is { process.HasExited: false })
			{
				if (portMisConfigured)
				{
					_debugAction?.Invoke($"Server already running on port {_devServer?.port}, but port is not configured properly on all projects. Updating it ...");

					// The dev-server is already running, but at least one project is not configured properly
					// (This can happen when a project is being added to the solution while opened - Reminder: This EnsureServerAsync is invoked each time a project is built)
					// We make sure to set the current port for **all** projects.
					await _dte.SetProjectUserSettingsAsync(_asyncPackage, RemoteControlServerPortProperty, port.ToString(CultureInfo.InvariantCulture), persistenceFilter);
				}
				else
				{
					_debugAction?.Invoke($"Server already running on port {_devServer?.port}");
				}

				return;
			}

			// Safety: Cancel previous services! (Should have already been cancelled by the exit handler);
			_devServer?.attachedServices.Cancel();

			devServerCt = CancellationTokenSource.CreateLinkedTokenSource(_ct.Token);
			if (_udei is not null)
			{
				await _udei.NotifyAsync(_udeiMsg.DevServer.Starting, devServerCt.Token);
			}

			if (EnsureTcpPort(ref port) || portMisConfigured)
			{
				// The port has changed, or all application projects does not have the same port number (or is not configured), we update port in *all* user files
				await _dte.SetProjectUserSettingsAsync(_asyncPackage, RemoteControlServerPortProperty, port.ToString(CultureInfo.InvariantCulture), persistenceFilter);
			}

			_debugAction?.Invoke($"Using available port {port}");

			var version = GetDotnetMajorVersion();
			if (version < 7)
			{
				throw new InvalidOperationException($"Unsupported dotnet version ({version}) detected");
			}

			var pipeGuid = Guid.NewGuid();

			var hostBinPath = Path.Combine(_toolsPath, "host", $"net{version}.0", "Uno.UI.RemoteControl.Host.dll");
			var arguments = $"\"{hostBinPath}\" --httpPort {port} --ppid {System.Diagnostics.Process.GetCurrentProcess().Id} --ideChannel \"{pipeGuid}\" --solution \"{_dte.Solution.FullName}\"";
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

			var devServer = new System.Diagnostics.Process { EnableRaisingEvents = true };
			_devServer = (devServer, port, devServerCt);

			// hookup the event handlers to capture the data that is received
			devServer.OutputDataReceived += (sender, args) => _debugAction?.Invoke(args.Data);
			devServer.ErrorDataReceived += (sender, args) => _errorAction?.Invoke(args.Data);

			devServer.StartInfo = pi;
			devServer.Exited += (sender, args) => _ = OnExitAsync();

			if (devServer.Start())
			{
				// start our event pumps
				devServer.BeginOutputReadLine();
				devServer.BeginErrorReadLine();

				_ideChannelClient = new IdeChannelClient(pipeGuid, new Logger(this));
				_ = TrackConnectionTimeoutAsync(_ideChannelClient);
				_ideChannelClient.OnMessageReceived += OnMessageReceivedAsync;
				_ideChannelClient.Connected += OnIdeChannelConnected;
				_ideChannelClient.ConnectToHost();

				// Use scoped DI instead of this!
				var remoteCommands = new IdeChannelCommandHandler(_ideChannelClient);
				_commands.Register("Send to Dev Server", remoteCommands);
				devServerCt.Token.Register(() =>
				{
					_commands.Unregister(remoteCommands);
					remoteCommands.Dispose();
				});
			}
			else
			{
				_devServer = null;
				devServerCt.Cancel();
				throw new InvalidOperationException("Failed to start dev-server process");
			}
		}
		catch (Exception e)
		{
			_errorAction?.Invoke($"Failed to start server: {e}");
			if (_udei is not null)
			{
				await _udei.NotifyAsync(_udeiMsg.DevServer.Failed(e), _ct.Token);
			}
			devServerCt?.Cancel();
		}
		finally
		{
			_devServerGate.Release();
		}

		async Task TrackConnectionTimeoutAsync(IdeChannelClient ideChannel)
		{
			// The dev-server is expected to connect back to the IDE as soon as possible, 10sec should be more than enough.
			await Task.Delay(10_000, devServerCt.Token);
			if (ideChannel.MessagesReceivedCount is 0 && _udei is not null && !devServerCt.IsCancellationRequested)
			{
				await _udei.NotifyAsync(_udeiMsg.DevServer.Timeout, devServerCt.Token);
			}
		}

		async Task OnExitAsync()
		{
			// Abort attached services
			devServerCt.Cancel();

			if (_closing || _ct.IsCancellationRequested)
			{
				_debugAction?.Invoke($"Remote Control server exited ({_devServer?.process.ExitCode}) and won't be restarted as solution is closing.");
				return;
			}

			// If not closing, restart!
			if (_udei is not null)
			{
				await _udei.NotifyAsync(_udeiMsg.DevServer.Restarting, _ct.Token);
			}

			_debugAction?.Invoke($"Remote Control server exited ({_devServer?.process.ExitCode}). It will restart in 5sec.");

			await Task.Delay(5000, _ct.Token);

			if (_closing || _ct.IsCancellationRequested)
			{
				_debugAction?.Invoke($"Remote Control server will not be restarted as solution is closing.");
				return;
			}

			await EnsureServerAsync();
		}
	}

	private void StopDevServer()
	{
		if (_devServer is { process: var devServer, attachedServices: var ct })
		{
			try
			{
				_debugAction?.Invoke($"Terminating Remote Control server (pid: {devServer.Id})");
				ct.Cancel();
				devServer.Kill();
				_debugAction?.Invoke($"Terminated Remote Control server (pid: {devServer.Id})");

				_ideChannelClient?.Dispose();
				_ideChannelClient = null;
			}
			catch (Exception e)
			{
				_debugAction?.Invoke($"Failed to terminate Remote Control server (pid: {devServer.Id}): {e}");
			}
			finally
			{
				_devServer = null;

				// Invoke Dispose to make sure other event handlers are detached
				Dispose();
			}
		}
	}

	public async Task RestartDevServerAsync(CancellationToken ct)
	{
		StopDevServer();

		await EnsureServerAsync();
	}

	private void OnIdeChannelConnected(object sender, EventArgs e) =>
		// As we're here, we know that the devserver has started properly
		_ = SetupMcpAsync(_ct.Token);


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
					await OnForceHotReloadRequestedAsync(fhr);
					break;
				case UpdateFileIdeMessage ufm:
					await OnUpdateFileRequestedAsync(ufm);
					break;
				case NotificationRequestIdeMessage nr:
					await OnNotificationRequestedAsync(sender, nr);
					break;
				case DevelopmentEnvironmentStatusIdeMessage when _udei is null:
					_warningAction?.Invoke("Got an UDEI message, but there is no VSIX channel available. Please update Uno's extension in Visual Studio!");
					break;
				case DevelopmentEnvironmentStatusIdeMessage desm:
					await _udei.NotifyAsync(desm, CancellationToken.None);
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

	private async Task OnForceHotReloadRequestedAsync(ForceHotReloadIdeMessage request)
	{
		try
		{
			// Programmatically trigger the "Apply Code Changes" command in Visual Studio.
			// Which will trigger the hot reload.
			_dte.ExecuteCommand("Debug.ApplyCodeChanges");

			// Send a message back to indicate that the request has been received and acted upon.
			if (_ideChannelClient is not null)
			{
				await _ideChannelClient.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Success()), _ct.Token);
			}
		}
		catch (Exception e) when (_ideChannelClient is not null)
		{
			await _ideChannelClient.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Fail(e)), _ct.Token);

			throw;
		}
	}

	private async Task OnUpdateFileRequestedAsync(UpdateFileIdeMessage request)
	{
		try
		{
			if (request.FileContent is { Length: > 0 } fileContent)
			{
				var filePath = request.FileFullName;

				// Update the file content in the IDE using the DTE API.
				var document = _dte2.Documents
					.OfType<Document>()
					.FirstOrDefault(d => AbsolutePathComparer.ComparerIgnoreCase.Equals(d.FullName, filePath));

				var textDocument = document?.Object("TextDocument") as TextDocument;

				if (textDocument is null) // The document is not open in the IDE, so we need to open it.
				{
					// Resolve the path to the document (in case it's not open in the IDE).
					// The path may contain a mix of forward and backward slashes, so we normalize it by using Path.GetFullPath.
					var adjustedPathForOpening = Path.GetFullPath(filePath);

					document = _dte2.Documents.Open(adjustedPathForOpening);
					textDocument = document?.Object("TextDocument") as TextDocument;
				}

				if (document is null || textDocument is null)
				{
					throw new InvalidOperationException($"Failed to open document {filePath}");
				}

				// Replace the content of the document with the new content.

				// Flags: 0b0000_0011 = vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers | vsEPReplaceTextOptions.vsEPReplaceTextNormalizeNewLines
				// https://learn.microsoft.com/en-us/dotnet/api/envdte.vsepreplacetextoptions?view=visualstudiosdk-2022#fields
				const int flags = 0b0000_0011;

				textDocument.StartPoint.CreateEditPoint()
					.ReplaceText(textDocument.EndPoint, fileContent, flags);

				if (request.ForceSaveOnDisk)
				{
					// Save the document.
					document.Save();
				}

				// Send a message back to indicate that the request has been received and acted upon.
				if (_ideChannelClient is not null)
				{
					await _ideChannelClient.SendToDevServerAsync(
						new IdeResultMessage(request.CorrelationId, Result.Success()), _ct.Token);
				}
			}
		}
		catch (Exception e) when (_ideChannelClient is not null)
		{
			// Send a message back to indicate that the request has failed.
			await _ideChannelClient.SendToDevServerAsync(new IdeResultMessage(request.CorrelationId, Result.Fail(e)), _ct.Token);

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
				var p = port;
				if (IPGlobalProperties
					.GetIPGlobalProperties()
					.GetActiveTcpListeners()
					.All(ep => ep.Port != p))
				{
					// As a safety, we also try to open a socket just like how kestrell does.
					// Note : We do NOT use a TCPListener here, as it will not throw an exception if the port is already in use **by Kestrell**.
					var so = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					so.Bind(new IPEndPoint(IPAddress.Any, port));
					so.Close();

					return false;
				}
			}
			catch
			{
				_debugAction?.Invoke($"Failed to reused previous port {port}, choosing a new one.");
			}
		}

		tcp = new TcpListener(IPAddress.Any, 0) { ExclusiveAddressUse = true };
		tcp.Start();
		port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();

		return true; // HasChanged
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
			_debuggerObserver?.Dispose();
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
