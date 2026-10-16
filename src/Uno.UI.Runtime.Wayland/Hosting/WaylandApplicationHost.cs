#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Linux;
using Uno.UI.Runtime.Wayland.Protocols;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland;

// Created through WaylandHostBuilder; nothing outside the assembly constructs or consumes it.
internal sealed class WaylandApplicationHost : UnoPlatformHost, IApplicationHost, IDisposable
{
	private const int ShutdownTimeoutMilliseconds = 5000;

	// Services are registered once per process, so the connection is process-wide too: like X11's shared state,
	// a secondary host reuses it and only the host that connected tears it down.
	private static readonly object _connectionGate = new();
	private static readonly WaylandCoreApplicationExtension _coreApplication = new(() => _connection);
	private static volatile WaylandConnection? _connection;
	private static bool _connectionClosed;

	// The connection that passed probing, handed to the selected host to avoid a probe/create race.
	private static WaylandDisplay? _probedDisplay;

	[ThreadStatic] private static bool _isDispatcherThread;

	private readonly Func<Application> _appBuilder;
	private readonly WaylandDispatcher? _eventLoop;
	private bool _ownsConnection;
	private Task? _shutdown;

	internal WaylandApplicationHost(Func<Application> appBuilder, bool useSystemHarfBuzz)
	{
		_appBuilder = appBuilder ?? throw new ArgumentNullException(nameof(appBuilder));
		LinuxHarfBuzz.Isolate(useSystemHarfBuzz);

		if (CoreDispatcher.DispatchOverride is null)
		{
			_eventLoop = new WaylandDispatcher();
			_eventLoop.Schedule(() =>
			{
				Thread.CurrentThread.Name = "Uno Event Loop";
				_isDispatcherThread = true;
			});
			CoreDispatcher.DispatchOverride = (action, _) => _eventLoop.Schedule(action);
			CoreDispatcher.HasThreadAccessOverride = () => _isDispatcherThread;
		}
		else
		{
			// Another host already owns the UI thread (secondary app); reuse its dispatcher.
			CoreDispatcher.DispatchOverride(() => _isDispatcherThread = true, NativeDispatcherPriority.Normal);
		}
	}

	// Probes for a compositor that serves everything the host needs. Never throws: an unusable Wayland session
	// leaves the next registered host (typically X11) to be selected.
	internal static bool IsSupported()
	{
		if (!OperatingSystem.IsLinux() || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
		{
			return false;
		}
		lock (_connectionGate)
		{
			if (_connection is not null || _probedDisplay is { IsDisconnected: false, DispatchError: null })
			{
				return true;
			}
		}
		try
		{
			// Connect performs the initial registry roundtrip.
			var display = WaylandDisplay.Connect();
			if (HasRequiredGlobals(display))
			{
				lock (_connectionGate)
				{
					var previous = _probedDisplay;
					_probedDisplay = display;
					previous?.Dispose();
				}
				return true;
			}
			display.Dispose();
			if (typeof(WaylandApplicationHost).Log().IsEnabled(LogLevel.Information))
			{
				typeof(WaylandApplicationHost).Log().Info("The Wayland compositor does not advertise wl_compositor, xdg_wm_base and wl_shm; the Wayland host is unavailable.");
			}
		}
		catch (Exception error)
		{
			if (typeof(WaylandApplicationHost).Log().IsEnabled(LogLevel.Information))
			{
				typeof(WaylandApplicationHost).Log().Info($"Unable to connect to the Wayland compositor; the Wayland host is unavailable ({error.Message}).");
			}
		}
		return false;
	}

	private static bool HasRequiredGlobals(WaylandDisplay display)
		=> display.GetGlobal(WlCompositor.ProtocolName) is not null
			&& display.GetGlobal(XdgWmBase.ProtocolName) is not null
			&& display.GetGlobal(WlShm.ProtocolName) is not null;

	protected override void Initialize()
	{
		lock (_connectionGate)
		{
			if (_connection is not null)
			{
				return;
			}
			if (_connectionClosed)
			{
				// Registered services still reference the closed connection.
				throw new InvalidOperationException("The Wayland host cannot run again after its connection was closed.");
			}
			var display = _probedDisplay ?? WaylandDisplay.Connect();
			_probedDisplay = null;
			try
			{
				if (!HasRequiredGlobals(display))
				{
					throw new PlatformNotSupportedException("Required Wayland globals are unavailable.");
				}
				var connection = new WaylandConnection(display);
				display.StartDispatch();
				WaylandRegistrations.Register(connection, _coreApplication);
				_connection = connection;
				_ownsConnection = true;
			}
			catch
			{
				display.Dispose();
				throw;
			}
		}
		WaylandDiagnostics.Trace("host: connected, dispatch thread started");
	}

	protected override Task RunLoop()
	{
		Thread.CurrentThread.Name = "Main Thread (keep-alive)";
		try
		{
			if (_eventLoop is not null)
			{
				_eventLoop.Schedule(StartApp);
			}
			else
			{
				CoreDispatcher.DispatchOverride(StartApp, NativeDispatcherPriority.Normal);
			}

			while (!ShouldExit())
			{
				Thread.Sleep(100);
			}

			if (_connection?.Display.DispatchError is { } error)
			{
				// The compositor went away or ended the connection: nothing can be shown anymore, so the app ends
				// with a failure exit code instead of an unhandled exception.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("The Wayland connection failed; exiting.", error);
				}
				Environment.ExitCode = 1;
			}
			else
			{
				WaylandDiagnostics.Trace("host: exiting");
			}
			return Task.CompletedTask;
		}
		finally
		{
			Dispose();
		}
	}

	private bool ShouldExit()
	{
		if (Volatile.Read(ref _shutdown) is not null || _coreApplication.ExitRequested || _connection?.Display.DispatchError is not null)
		{
			return true;
		}
		return Application.Current?.DispatcherShutdownMode == DispatcherShutdownMode.OnLastWindowClose
			&& _connection?.AllWindowsClosed == true;
	}

	private void StartApp()
	{
		Application.Start(_ =>
		{
			var app = _appBuilder();
			app.Host = this;
			return app;
		});
	}

	// Waits for the ordered shutdown unless called on the UI thread, which the shutdown itself needs.
	public void Dispose()
	{
		if (!_ownsConnection)
		{
			return;
		}
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var shutdown = Interlocked.CompareExchange(ref _shutdown, started.Task, null);
		if (shutdown is null)
		{
			shutdown = started.Task;
			_ = ShutdownAsync(started);
		}
		if (CoreDispatcher.HasThreadAccessOverride?.Invoke() != true)
		{
			shutdown.GetAwaiter().GetResult();
		}
	}

	// Windows close on the UI thread; each render thread then releases its EGL or wl_shm resources and destroys the
	// window's protocol objects. Only then do the UI and dispatch threads stop and the display disconnect.
	private async Task ShutdownAsync(TaskCompletionSource completion)
	{
		try
		{
			var connection = _connection!;
			WaylandDiagnostics.Trace("host: shutdown started");
			if (!await RunOnUIAsync("closing windows", connection.CloseAllWindows).ConfigureAwait(false))
			{
				return;
			}
			WaylandDiagnostics.Trace("host: windows closed");
			if (!await RunOnUIAsync("stopping the UI thread", StopDispatcher).ConfigureAwait(false))
			{
				return;
			}
			connection.Dispose();
			lock (_connectionGate)
			{
				_connection = null;
				_connectionClosed = true;
			}
			WaylandDiagnostics.Trace("host: disconnected");
		}
		catch (Exception error)
		{
			if (_connection?.Display.DispatchError is null && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("The Wayland host failed to shut down.", error);
			}
		}
		finally
		{
			completion.TrySetResult();
		}
	}

	private Task StopDispatcher()
	{
		if (_eventLoop is not null)
		{
			return _eventLoop.Stop();
		}
		_connection!.CloseUICallbacks();
		return Task.CompletedTask;
	}

	// False when the step failed or timed out: a renderer may still use the native display, so the connection is
	// left for process exit to reclaim.
	private async Task<bool> RunOnUIAsync(string name, Func<Task> step)
	{
		var result = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		void Run()
		{
			try
			{
				result.TrySetResult(step());
			}
			catch (Exception error)
			{
				result.TrySetException(error);
			}
		}
		if (CoreDispatcher.HasThreadAccessOverride?.Invoke() == true)
		{
			Run();
		}
		else
		{
			CoreDispatcher.DispatchOverride(Run, NativeDispatcherPriority.High);
		}
		var task = result.Task.Unwrap();
		if (await Task.WhenAny(task, Task.Delay(ShutdownTimeoutMilliseconds)).ConfigureAwait(false) != task)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Wayland shutdown timed out after {ShutdownTimeoutMilliseconds} ms while {name}; the connection is left open.");
			}
			return false;
		}
		try
		{
			await task.ConfigureAwait(false);
		}
		catch (Exception error)
		{
			if (_connection?.Display.DispatchError is null && this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Wayland shutdown failed while {name}; the connection is left open.", error);
			}
			return false;
		}
		return true;
	}
}
