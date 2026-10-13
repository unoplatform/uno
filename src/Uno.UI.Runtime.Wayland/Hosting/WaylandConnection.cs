#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

// Threading contract:
// - Protocol events fire on the display's dispatch thread while it holds SyncRoot. Handlers only copy
//   event data, acknowledge protocol state (ping, configure) and post work elsewhere; they never wait.
// - Requests may be issued from any thread; each proxy request takes SyncRoot. Proxies whose listeners
//   must not miss events are created and subscribed inside one SyncRoot section.
// - UI state (XamlRoot, window wrapper, input sinks' public events) is updated on the Uno UI thread
//   through PostToUI. Rendering state belongs to each window's render thread.
internal sealed class WaylandConnection : IDisposable
{
	internal Input.WaylandKeyboardInjectionPolicy KeyboardInjectionPolicy { get; } = new();
	private readonly HashSet<WaylandWindowWrapper> _windows = new();
	// Dispatch thread or SyncRoot.
	private readonly Dictionary<uint, WaylandOutput> _outputs = new();
	private volatile WaylandOutput? _defaultOutput;
	// UI thread: render-thread teardown of closed windows that has not finished yet.
	// Failed teardowns are kept so shutdown observes them.
	private readonly List<Task> _retiringWindows = new();
	private volatile bool _allWindowsClosed;
	private volatile bool _uiCallbacksClosed;
	// The launcher's startup token, consumed by the first activation (xdg-activation-v1 startup notification).
	private string? _startupActivationToken = TakeStartupActivationToken();

	internal WaylandConnection(WaylandDisplay display)
	{
		Display = display;
		lock (display.SyncRoot)
		{
			Compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, 6);
			WmBase = (XdgWmBase)display.Bind(XdgWmBase.ProtocolName, uint.MaxValue);
			WmBase.Ping += (_, e) => WmBase.Pong(e.Serial);
			Shm = (WlShm)display.Bind(WlShm.ProtocolName, 1);
			Viewporter = TryBind<WpViewporter>(WpViewporter.ProtocolName, 1);
			FractionalScale = Viewporter is null ? null : TryBind<WpFractionalScaleManagerV1>(WpFractionalScaleManagerV1.ProtocolName, 1);
			Decorations = TryBind<ZxdgDecorationManagerV1>(ZxdgDecorationManagerV1.ProtocolName, 1);
			Subcompositor = TryBind<WlSubcompositor>(WlSubcompositor.ProtocolName, 1);
			Activation = TryBind<XdgActivationV1>(XdgActivationV1.ProtocolName, 1);
			IconManager = TryBind<XdgToplevelIconManagerV1>(XdgToplevelIconManagerV1.ProtocolName, 1);
			// Debugging aid: forces the libwayland-cursor theme fallback used by compositors without cursor-shape-v1.
			CursorShape = Environment.GetEnvironmentVariable("UNO_WAYLAND_DISABLE_CURSOR_SHAPE") == "1"
				? null
				: TryBind<WpCursorShapeManagerV1>(WpCursorShapeManagerV1.ProtocolName, 1);
			TextInputManager = TryBind<ZwpTextInputManagerV3>(ZwpTextInputManagerV3.ProtocolName, 1);
			Exporter = TryBind<ZxdgExporterV2>(ZxdgExporterV2.ProtocolName, 1);
			// TODO: multiple seats; the first advertised one is used for now.
			Seat = TryBind<WlSeat>(WlSeat.ProtocolName, 9);
			if (Seat is not null)
			{
				Seat.Capabilities += (_, e) =>
				{
					SeatCapabilities = e.Capabilities;
					if (WaylandDiagnostics.IsEnabled)
					{
						WaylandDiagnostics.Trace(FormattableString.Invariant($"seat: capabilities pointer={(e.Capabilities & 1) != 0} keyboard={(e.Capabilities & 2) != 0} touch={(e.Capabilities & 4) != 0}"));
					}
				};
			}
			foreach (var global in display.Globals)
			{
				if (global.Interface == WlOutput.ProtocolName)
				{
					AddOutput(global);
				}
			}
			// Outputs come and go with monitor hotplug; the display's own registry handler has already updated its globals.
			display.Registry.Global += (_, e) =>
			{
				if (e.Interface == WlOutput.ProtocolName)
				{
					AddOutput(new WaylandGlobal(e.Name, e.Interface, e.Version));
				}
			};
			display.Registry.GlobalRemove += (_, e) =>
			{
				if (_outputs.Remove(e.Name, out var output))
				{
					if (WaylandDiagnostics.IsEnabled)
					{
						WaylandDiagnostics.Trace(FormattableString.Invariant($"output {output.Info.Name}: removed"));
					}
					UpdateDefaultOutput();
					OutputRemoved?.Invoke(this, output);
					output.Dispose();
				}
			};
		}
	}

	internal WaylandDisplay Display { get; }
	internal WlCompositor Compositor { get; }
	internal XdgWmBase WmBase { get; }
	internal WlShm Shm { get; }
	internal WpViewporter? Viewporter { get; }
	internal WpFractionalScaleManagerV1? FractionalScale { get; }
	internal ZxdgDecorationManagerV1? Decorations { get; }
	internal WlSubcompositor? Subcompositor { get; }
	internal XdgActivationV1? Activation { get; }
	internal XdgToplevelIconManagerV1? IconManager { get; }
	internal WpCursorShapeManagerV1? CursorShape { get; }
	internal ZwpTextInputManagerV3? TextInputManager { get; }
	internal ZxdgExporterV2? Exporter { get; }
	internal WlSeat? Seat { get; }

	// Written on the dispatch thread; read under SyncRoot or on the dispatch thread.
	internal uint SeatCapabilities { get; private set; }

	// Raised on the dispatch thread before the removed output's proxy is released.
	internal event EventHandler<WaylandOutput>? OutputRemoved;

	// Serial of the latest keyboard enter, key, button or touch event, for requests that require user interaction.
	// Dispatch thread only.
	internal uint InputSerial { get; set; }

	// Dispatch thread: the most recently activated toplevel, used as the requesting surface for activation tokens.
	internal WlSurface? ActiveSurface { get; set; }

	// The xdg_toplevel app_id, matched by compositors against the application's .desktop file (WM_CLASS on X11).
	internal static string AppId
		=> Windows.ApplicationModel.Package.Current.Id.Name is { Length: > 0 } name ? name : "uno.app";

	// Any thread: the first advertised output, for windows that have not entered one yet.
	internal WaylandOutputInfo? DefaultOutputInfo => _defaultOutput?.Info;

	// Dispatch thread or SyncRoot.
	internal WaylandOutput? FindOutput(WlOutput? proxy)
	{
		foreach (var output in _outputs.Values)
		{
			if (output.Proxy == proxy)
			{
				return output;
			}
		}
		return null;
	}

	private void AddOutput(WaylandGlobal global)
	{
		var proxy = (WlOutput)Display.Registry.Bind(global.Name, WlOutput.ProtocolName, Math.Min(global.Version, WlOutput.MaximumVersion));
		var output = new WaylandOutput(global.Name, proxy);
		output.Changed += (_, _) =>
		{
			if (WaylandDiagnostics.IsEnabled)
			{
				WaylandDiagnostics.Trace(FormattableString.Invariant($"output {output.Info.Name}: {output.Info.PixelWidth}x{output.Info.PixelHeight} scale {output.Info.Scale} \"{output.Info.Description}\""));
			}
		};
		_outputs.Add(global.Name, output);
		UpdateDefaultOutput();
	}

	private void UpdateDefaultOutput()
	{
		WaylandOutput? first = null;
		foreach (var output in _outputs.Values)
		{
			if (first is null || output.GlobalName < first.GlobalName)
			{
				first = output;
			}
		}
		_defaultOutput = first;
	}

	// Dispatch thread: allows keyboard compose to reset when the input method produces text.
	internal event EventHandler? TextInputReceived;

	internal void NotifyTextInputReceived() => TextInputReceived?.Invoke(this, EventArgs.Empty);

	internal void PostToUI(Action action)
		=> NativeDispatcher.Main.Enqueue(() =>
		{
			if (!_uiCallbacksClosed)
			{
				action();
			}
		}, NativeDispatcherPriority.High);

	// UI thread: fence queued work before disconnecting a borrowed dispatcher.
	internal void CloseUICallbacks() => _uiCallbacksClosed = true;

	private static string? TakeStartupActivationToken()
	{
		var token = Environment.GetEnvironmentVariable("XDG_ACTIVATION_TOKEN");
		// Child processes must not reuse the token.
		Environment.SetEnvironmentVariable("XDG_ACTIVATION_TOKEN", null);
		return string.IsNullOrEmpty(token) ? null : token;
	}

	// Dispatch thread. Compositors may refuse focus without a recent input serial; the request is then a hint.
	internal void Activate(WlSurface surface, Func<bool> isAlive)
	{
		if (Activation is not { } activation)
		{
			return;
		}
		if (_startupActivationToken is { } startupToken)
		{
			_startupActivationToken = null;
			activation.Activate(startupToken, surface);
			return;
		}
		var token = activation.GetActivationToken();
		token.Done += (_, e) =>
		{
			token.Dispose();
			if (isAlive())
			{
				activation.Activate(e.Token, surface);
			}
		};
		token.SetAppId(AppId);
		if (ActiveSurface is { } active)
		{
			token.SetSurface(active);
		}
		if (InputSerial != 0 && Seat is { } seat)
		{
			token.SetSerial(InputSerial, seat);
		}
		token.Commit();
	}

	private T? TryBind<T>(string name, uint version) where T : WaylandProxy
		=> Display.GetGlobal(name) is null ? null : (T)Display.Bind(name, version);

	// UI thread only.
	internal void AddWindow(WaylandWindowWrapper window)
	{
		_windows.Add(window);
		_allWindowsClosed = false;
	}

	internal void RemoveWindow(WaylandWindowWrapper window, Task completion)
	{
		_windows.Remove(window);
		TrackWindowTeardown(completion);
		_allWindowsClosed = _windows.Count == 0;
	}

	internal void TrackWindowTeardown(Task completion)
	{
		_retiringWindows.RemoveAll(task => task.IsCompletedSuccessfully);
		_retiringWindows.Add(completion);
	}

	internal bool AllWindowsClosed => _allWindowsClosed;

	internal bool IsClosing { get; private set; }

	internal WaylandWindowWrapper? FindWindow(Microsoft.UI.Xaml.Window window)
	{
		foreach (var wrapper in _windows)
		{
			if (wrapper.OwnerWindow == window)
			{
				return wrapper;
			}
		}
		return null;
	}

	// UI thread. Closes the native windows without raising Closing or Closed, like the X11 host on exit.
	// Completes once every window's renderer and protocol objects are gone.
	internal Task CloseAllWindows()
	{
		IsClosing = true;
		var completions = new List<Task>();
		foreach (var window in new List<WaylandWindowWrapper>(_windows))
		{
			try
			{
				window.Close();
			}
			catch (Exception error)
			{
				completions.Add(Task.FromException(error));
			}
		}
		completions.AddRange(_retiringWindows);
		return Task.WhenAll(completions);
	}

	public void Dispose() => Display.Dispose();
}
