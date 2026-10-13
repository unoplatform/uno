#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Rendering;
using Uno.UI.Runtime.Wayland.Native;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

internal sealed unsafe class WaylandDisplay : IDisposable
{
	// Bounds the startup roundtrip so a frozen compositor cannot hang host selection.
	private const int StartupTimeoutMilliseconds = 5000;
	private const int EINTR = 4;
	private const int EAGAIN = 11;
	private const int EPROTO = 71;

	private readonly Dictionary<nint, WaylandProxy> _proxies = new();
	private readonly Dictionary<uint, WaylandGlobal> _globals = new();
	private readonly ConcurrentQueue<(Action Command, Action? Rejected)> _commands = new();
	private readonly WaylandPresentationCoordinator _presentation;
	private nint _display;
	private Thread? _thread;
	private int _wakeRead = -1;
	private int _wakeWrite = -1;
	private bool _stopping;
	private bool _disposing;
	private Exception? _callbackError;
	private volatile Exception? _dispatchError;
	public object SyncRoot { get; } = new();
	public WlRegistry Registry { get; private set; } = null!;
	public int FileDescriptor { get; }
	public bool IsDisconnected => _display == 0;
	// Native wl_display pointer for libwayland-egl and libwayland-cursor consumers.
	internal nint NativeHandle => _display;
	public Exception? DispatchError => _dispatchError;
	public bool IsDispatchThread => _thread == Thread.CurrentThread;
	public IReadOnlyList<WaylandGlobal> Globals
	{
		get
		{
			lock (SyncRoot)
			{
				return _globals.Values.OrderBy(global => global.Name).ToArray();
			}
		}
	}

	private WaylandDisplay(nint display)
	{
		_display = display;
		_presentation = new(SyncRoot, (command, rejected) => TryPost(command, rejected), () =>
		{
			Monitor.PulseAll(SyncRoot);
			WakeDispatch();
		});
		FileDescriptor = WaylandNative.wl_display_get_fd(display);
	}

	public static WaylandDisplay Connect(string? name = null)
	{
		var handle = WaylandNative.wl_display_connect(name);
		if (handle == 0)
		{
			throw new IOException("Unable to connect to the Wayland display. Check WAYLAND_DISPLAY and XDG_RUNTIME_DIR.");
		}
		return Initialize(handle);
	}

	/// <summary>Connects using a connected Unix socket, transferring ownership of the descriptor to libwayland.</summary>
	public static WaylandDisplay ConnectToFileDescriptor(int fd)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(fd);
		var handle = WaylandNative.wl_display_connect_to_fd(fd);
		if (handle == 0)
		{
			throw new IOException("Unable to initialize the Wayland connection.");
		}
		return Initialize(handle);
	}

	private static WaylandDisplay Initialize(nint handle)
	{
		var display = new WaylandDisplay(handle);
		try
		{
			using var args = new RequestArguments(1);
			var registry = WaylandNative.wl_proxy_marshal_array_flags(handle, 1, ProtocolMetadata.Get(WlRegistry.ProtocolName), 1, 0, args.Pointer);
			display.Registry = new WlRegistry(display, registry);
			display.Registry.Global += (_, evt) => display._globals[evt.Name] = new(evt.Name, evt.Interface, evt.Version);
			display.Registry.GlobalRemove += (_, evt) => display._globals.Remove(evt.Name);
			display.Roundtrip(StartupTimeoutMilliseconds);
			return display;
		}
		catch
		{
			display.Dispose();
			throw;
		}
	}

	public WaylandGlobal? GetGlobal(string interfaceName)
	{
		lock (SyncRoot)
		{
			return _globals.Values.FirstOrDefault(global => global.Interface == interfaceName);
		}
	}

	public WaylandProxy Bind(string interfaceName, uint maximumVersion)
	{
		lock (SyncRoot)
		{
			var global = GetGlobal(interfaceName) ?? throw new NotSupportedException($"The compositor does not advertise {interfaceName}.");
			var supported = (uint)ProtocolMetadata.Get(interfaceName)->Version;
			return Registry.Bind(global.Name, interfaceName, Math.Min(Math.Min(maximumVersion, supported), global.Version));
		}
	}

	public WlCallback Sync()
	{
		lock (SyncRoot)
		{
			ThrowIfUnavailable();
			using var args = new RequestArguments(1);
			var handle = WaylandNative.wl_proxy_marshal_array_flags(_display, 0, ProtocolMetadata.Get(WlCallback.ProtocolName), 1, 0, args.Pointer);
			var callback = new WlCallback(this, handle);
			WakeDispatch();
			return callback;
		}
	}

	public void Roundtrip()
	{
		lock (SyncRoot)
		{
			ThrowIfUnavailable();
			if (_thread is not null)
			{
				throw new InvalidOperationException("Roundtrip is only allowed before starting the dispatch thread.");
			}
			Check(WaylandNative.wl_display_roundtrip(_display));
			ThrowCallbackError();
		}
	}

	// Startup-only roundtrip that gives up after the timeout instead of blocking like wl_display_roundtrip.
	private void Roundtrip(int timeoutMilliseconds)
	{
		lock (SyncRoot)
		{
			var done = false;
			using var callback = Sync();
			callback.Done += (_, _) => done = true;
			var deadline = Environment.TickCount64 + timeoutMilliseconds;
			while (!done)
			{
				while (WaylandNative.wl_display_prepare_read(_display) != 0)
				{
					Check(WaylandNative.wl_display_dispatch_pending(_display));
				}
				var remaining = deadline - Environment.TickCount64;
				if (done || remaining <= 0)
				{
					WaylandNative.wl_display_cancel_read(_display);
					if (done)
					{
						break;
					}
					throw new IOException($"The Wayland compositor did not answer within {timeoutMilliseconds} ms.");
				}
				var fd = new Posix.PollFd { Fd = FileDescriptor, Events = (short)(1 | (FlushCore() ? 4 : 0)) };
				var result = Posix.poll(&fd, 1, (int)remaining);
				if (result > 0 && (fd.Revents & 1) != 0)
				{
					Check(WaylandNative.wl_display_read_events(_display));
				}
				else
				{
					WaylandNative.wl_display_cancel_read(_display);
					if ((fd.Revents & (8 | 16 | 32)) != 0)
					{
						throw new IOException("The Wayland connection closed during startup.");
					}
				}
				Check(WaylandNative.wl_display_dispatch_pending(_display));
				ThrowCallbackError();
			}
		}
	}

	public void Flush()
	{
		lock (SyncRoot)
		{
			ThrowIfUnavailable();
			FlushCore();
			WakeDispatch();
		}
	}

	/// <summary>
	/// Runs <paramref name="command"/> on the dispatch thread before its next read, under <see cref="SyncRoot"/>.
	/// A failing command stops the loop like a failing event handler.
	/// </summary>
	public void Post(Action command) => _ = TryPost(command);

	internal bool TryPost(Action command, Action? rejected = null)
	{
		ArgumentNullException.ThrowIfNull(command);
		lock (SyncRoot)
		{
			if (!_stopping && !_disposing && _display != 0 && DispatchError is null)
			{
				_commands.Enqueue((command, rejected));
				WakeDispatch();
				return true;
			}
		}
		rejected?.Invoke();
		return false;
	}

	internal bool SubmitPresentation(Func<bool> submit)
	{
		if (IsDispatchThread)
		{
			throw new InvalidOperationException("Presentation must run outside dispatch callbacks.");
		}
		return _presentation.Submit(submit);
	}

	private void RejectCommands()
	{
		while (_commands.TryDequeue(out var command))
		{
			command.Rejected?.Invoke();
		}
	}

	private void RunCommands()
	{
		while (!_presentation.IsActive && _commands.TryDequeue(out var command))
		{
			try
			{
				command.Command();
			}
			catch (Exception error)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("A posted Wayland display command failed.", error);
				}
				CaptureCallbackError(error);
			}
			ThrowCallbackError();
		}
	}

	public void StartDispatch()
	{
		lock (SyncRoot)
		{
			ThrowIfUnavailable();
			if (_thread is not null)
			{
				throw new InvalidOperationException("The dispatch loop is already running.");
			}
			int* fds = stackalloc int[2];
			if (Posix.pipe2(fds, 0x800 | 0x80000) != 0)
			{
				throw new IOException($"pipe2 failed: {Marshal.GetLastPInvokeError()}");
			}
			_wakeRead = fds[0];
			_wakeWrite = fds[1];
			_stopping = false;
			_thread = new Thread(DispatchLoop) { IsBackground = true, Name = "Uno Wayland dispatch" };
			_thread.Start();
		}
	}

	public void StopDispatch()
	{
		Thread? thread;
		lock (SyncRoot)
		{
			thread = _thread;
			if (thread == Thread.CurrentThread)
			{
				throw new InvalidOperationException("Stop or dispose the display outside its event callbacks.");
			}
			_stopping = true;
			Monitor.PulseAll(SyncRoot);
			WakeDispatch();
		}
		thread?.Join();
		lock (SyncRoot)
		{
			if (_thread != thread)
			{
				return;
			}
			if (_wakeRead >= 0)
			{
				_ = Posix.close(_wakeRead);
				_ = Posix.close(_wakeWrite);
				_wakeRead = _wakeWrite = -1;
			}
			_thread = null;
			RejectCommands();
		}
	}

	private void DispatchLoop()
	{
		var prepared = false;
		try
		{
			Posix.PollFd* fds = stackalloc Posix.PollFd[2];
			byte* bytes = stackalloc byte[64];
			while (true)
			{
				lock (SyncRoot)
				{
					if (_stopping)
					{
						return;
					}
					if (_presentation.IsActive)
					{
						// EGL reads and dispatches its own queue; do not prepare a competing reader during submission.
						Monitor.Wait(SyncRoot);
						continue;
					}
					RunCommands();
					if (_presentation.IsActive)
					{
						continue;
					}
					while (WaylandNative.wl_display_prepare_read(_display) != 0)
					{
						Check(WaylandNative.wl_display_dispatch_pending(_display));
						ThrowCallbackError();
					}
					prepared = true;
					var writable = FlushCore();
					fds[0] = new() { Fd = FileDescriptor, Events = (short)(1 | (writable ? 4 : 0)) };
					fds[1] = new() { Fd = _wakeRead, Events = 1 };
				}
				var result = Posix.poll(fds, 2, -1);
				var error = Marshal.GetLastPInvokeError();
				lock (SyncRoot)
				{
					if (result > 0 && (fds[0].Revents & 1) != 0)
					{
						prepared = false;
						Check(WaylandNative.wl_display_read_events(_display));
					}
					else
					{
						WaylandNative.wl_display_cancel_read(_display);
						prepared = false;
					}
					if (_stopping)
					{
						return;
					}
					if (result < 0 && error != EINTR)
					{
						throw new IOException($"Wayland poll failed: {Marshal.GetPInvokeErrorMessage(error)}");
					}
					if ((fds[1].Revents & 1) != 0)
					{
						while (Posix.read(_wakeRead, bytes, 64) > 0) { }
					}
					if ((fds[0].Revents & (8 | 16 | 32)) != 0)
					{
						throw new IOException("The Wayland connection closed.");
					}
					Check(WaylandNative.wl_display_dispatch_pending(_display));
					ThrowCallbackError();
				}
			}
		}
		catch (Exception error)
		{
			lock (SyncRoot)
			{
				_dispatchError = error;
			}
		}
		finally
		{
			if (prepared)
			{
				WaylandNative.wl_display_cancel_read(_display);
			}
			lock (SyncRoot)
			{
				RejectCommands();
			}
		}
	}

	// Returns whether the socket is full (EAGAIN), so the rest goes out once it is writable.
	private bool FlushCore()
	{
		if (WaylandNative.wl_display_flush(_display) >= 0)
		{
			return false;
		}
		var errno = Marshal.GetLastPInvokeError();
		if (errno == EAGAIN)
		{
			return true;
		}
		// A failed flush does not set the display error, so report the write's own errno (e.g. EPIPE).
		throw CreateConnectionError(errno);
	}

	internal void WakeDispatch()
	{
		if (_wakeWrite >= 0)
		{
			byte value = 1;
			_ = Posix.write(_wakeWrite, &value, 1);
		}
	}
	internal void Register(WaylandProxy proxy)
	{
		lock (SyncRoot)
		{
			_proxies.Add(proxy.Handle, proxy);
		}
	}
	internal void Unregister(nint handle) => _proxies.Remove(handle);
	internal WaylandProxy? FindProxy(nint handle) => handle == 0 ? null : _proxies.GetValueOrDefault(handle);
	internal void CaptureCallbackError(Exception error) => _callbackError ??= error;
	// Called by generated event thunks; the failure is fatal for the loop, so name its source while it is known.
	internal void CaptureCallbackError(Exception error, WaylandProxy proxy, string eventName)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"The {proxy.InterfaceName} v{proxy.Version} {eventName} handler failed.", error);
		}
		CaptureCallbackError(error);
	}
	private void ThrowCallbackError()
	{
		if (_callbackError is { } error)
		{
			_callbackError = null;
			throw new InvalidOperationException("A Wayland event handler failed.", error);
		}
	}
	private void Check(int result)
	{
		if (result < 0)
		{
			throw CreateConnectionError();
		}
	}

	// A fatal display error: EPROTO carries the compositor's protocol error, anything else is an errno such as EPIPE.
	private IOException CreateConnectionError(int fallbackErrno = 0)
	{
		var error = WaylandNative.wl_display_get_error(_display);
		if (error == 0)
		{
			error = fallbackErrno;
		}
		if (error == EPROTO)
		{
			WlInterface* @interface = null;
			uint id = 0;
			var code = WaylandNative.wl_display_get_protocol_error(_display, &@interface, &id);
			var name = @interface is null ? "unknown" : Marshal.PtrToStringUTF8((nint)@interface->Name);
			return new IOException($"Wayland protocol error {code} on {name}@{id}.");
		}
		return error == 0
			? new IOException("The Wayland connection failed.")
			: new IOException($"Wayland connection error {error}: {Marshal.GetPInvokeErrorMessage(error)}.");
	}
	internal void ThrowIfUnavailable()
	{
		ObjectDisposedException.ThrowIf(IsDisconnected || _disposing, this);
		if (DispatchError is { } error)
		{
			throw new IOException("The Wayland dispatch loop failed.", error);
		}
	}
	public void Dispose()
	{
		lock (SyncRoot)
		{
			if (_thread == Thread.CurrentThread)
			{
				throw new InvalidOperationException("Dispose the display outside its event callbacks.");
			}
			_disposing = true;
		}
		StopDispatch();
		lock (SyncRoot)
		{
			if (_display != 0)
			{
				foreach (var proxy in _proxies.Values.ToArray())
				{
					proxy.DisposeLocal();
				}
				WaylandNative.wl_display_disconnect(_display);
				_display = 0;
			}
		}
	}
}

internal sealed record WaylandGlobal(uint Name, string Interface, uint Version);
