#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Wayland.Native;
using Uno.UI.Runtime.Wayland.Protocols;

namespace Uno.UI.Runtime.Wayland;

// Threading follows the X11 host: requests are sent from any thread under Lock() and flushed when the outermost
// lock is released. The dispatch thread takes the lock only for dispatch_pending, and never while it is a registered
// reader (between prepare_read and read_events/cancel_read), so a thread holding the lock never waits on it.
internal sealed unsafe class WaylandDisplay : IDisposable
{
	// Bounds the startup roundtrip so a frozen compositor cannot hang host selection.
	private const int StartupTimeoutMilliseconds = 5000;
	// Bounds how long the dispatch loop takes to notice a stop request it was not woken for.
	private const int DispatchPollMilliseconds = 1000;
	private const int EINTR = 4;
	private const int EAGAIN = 11;
	private const int EPROTO = 71;
	private const short POLLIN = 1;
	private const short POLLOUT = 4;
	private const short POLLERR = 8;
	private const short POLLHUP = 16;
	private const short POLLNVAL = 32;

	private readonly Dictionary<nint, WaylandProxy> _proxies = new();
	private readonly Dictionary<uint, WaylandGlobal> _globals = new();
	private readonly object _syncRoot = new();
	// Under _syncRoot.
	private int _lockDepth;
	private bool _flushPending;
	private nint _display;
	private Thread? _thread;
	private volatile bool _stopping;
	private bool _disposing;
	private Exception? _callbackError;
	private volatile Exception? _dispatchError;
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
			using (Lock())
			{
				return _globals.Values.OrderBy(global => global.Name).ToArray();
			}
		}
	}

	private WaylandDisplay(nint display)
	{
		_display = display;
		FileDescriptor = WaylandNative.wl_display_get_fd(display);
	}

	/// <summary>
	/// Locks the display for requests and protocol state, like XLock on X11. Event handlers run under it.
	/// Requests are flushed when the outermost lock is released, waiting while the socket is full.
	/// </summary>
	public DisplayLock Lock() => new(this);

	internal readonly ref struct DisplayLock
	{
		private readonly WaylandDisplay _display;

		public DisplayLock(WaylandDisplay display)
		{
			_display = display;
			Monitor.Enter(display._syncRoot);
			display._lockDepth++;
		}

		public void Dispose()
		{
			try
			{
				if (--_display._lockDepth == 0 && _display._flushPending)
				{
					_display._flushPending = false;
					_display.FlushPending();
				}
			}
			finally
			{
				Monitor.Exit(_display._syncRoot);
			}
		}
	}

	// Under the lock: the request just marshalled goes out when the outermost lock is released.
	internal void RequestSent() => _flushPending = true;

	public static WaylandDisplay Connect(string? name = null)
	{
		var handle = WaylandNative.wl_display_connect(name);
		if (handle == 0)
		{
			throw new IOException("Unable to connect to the Wayland display. Check WAYLAND_DISPLAY and XDG_RUNTIME_DIR.");
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
		using (Lock())
		{
			return _globals.Values.FirstOrDefault(global => global.Interface == interfaceName);
		}
	}

	public WaylandProxy Bind(string interfaceName, uint maximumVersion)
	{
		using (Lock())
		{
			var global = GetGlobal(interfaceName) ?? throw new NotSupportedException($"The compositor does not advertise {interfaceName}.");
			var supported = (uint)ProtocolMetadata.Get(interfaceName)->Version;
			return Registry.Bind(global.Name, interfaceName, Math.Min(Math.Min(maximumVersion, supported), global.Version));
		}
	}

	public WlCallback Sync()
	{
		using (Lock())
		{
			ThrowIfUnavailable();
			return SyncCore();
		}
	}

	private WlCallback SyncCore()
	{
		using var args = new RequestArguments(1);
		var handle = WaylandNative.wl_proxy_marshal_array_flags(_display, 0, ProtocolMetadata.Get(WlCallback.ProtocolName), 1, 0, args.Pointer);
		RequestSent();
		return new WlCallback(this, handle);
	}

	// Startup-only roundtrip that gives up after the timeout instead of blocking like wl_display_roundtrip.
	// The lock is taken only to send, flush and dispatch, never while this thread is a registered reader.
	private void Roundtrip(int timeoutMilliseconds)
	{
		var done = false;
		WlCallback callback;
		using (Lock())
		{
			callback = Sync();
			callback.Done += (_, _) => done = true;
		}
		using var syncCallback = callback;
		var deadline = Environment.TickCount64 + timeoutMilliseconds;
		while (!done)
		{
			// Flush before becoming a reader; wait for a full socket without a read prepared.
			bool full;
			using (Lock())
			{
				full = FlushCore();
			}
			var remaining = (int)Math.Max(0, deadline - Environment.TickCount64);
			if (full && remaining > 0)
			{
				var wfd = new Posix.PollFd { Fd = FileDescriptor, Events = POLLOUT };
				Posix.poll(&wfd, 1, remaining);
				if ((wfd.Revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
				{
					throw new IOException("The Wayland connection closed during startup.");
				}
				continue;
			}

			while (WaylandNative.wl_display_prepare_read(_display) != 0)
			{
				using (Lock())
				{
					Check(WaylandNative.wl_display_dispatch_pending(_display));
					ThrowCallbackError();
				}
			}
			var prepared = true;
			try
			{
				remaining = (int)Math.Max(0, deadline - Environment.TickCount64);
				var fd = new Posix.PollFd { Fd = FileDescriptor, Events = POLLIN };
				Posix.poll(&fd, 1, remaining);
				// Non-blocking once poll returned: with nothing to read it reads nothing.
				prepared = false;
				Check(WaylandNative.wl_display_read_events(_display));
				if ((fd.Revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
				{
					throw new IOException("The Wayland connection closed during startup.");
				}
			}
			finally
			{
				if (prepared)
				{
					WaylandNative.wl_display_cancel_read(_display);
				}
			}
			using (Lock())
			{
				Check(WaylandNative.wl_display_dispatch_pending(_display));
				ThrowCallbackError();
			}
			if (!done && Environment.TickCount64 >= deadline)
			{
				throw new IOException($"The Wayland compositor did not answer within {timeoutMilliseconds} ms.");
			}
		}
	}

	/// <summary>
	/// Runs <paramref name="requests"/> under the lock on the calling thread, so its requests are flushed together.
	/// Returns false without running it once the connection is closing or failed.
	/// </summary>
	public void Send(Action requests) => _ = TrySend(requests);

	public bool TrySend(Action requests)
	{
		using (Lock())
		{
			if (_stopping || _disposing || _display == 0 || DispatchError is not null)
			{
				if (WaylandDiagnostics.IsEnabled)
				{
					WaylandDiagnostics.Trace($"display: requests dropped ({(DispatchError is null ? "stopping" : "dispatch failed")})");
				}
				return false;
			}
			requests();
			return true;
		}
	}

	/// <summary>
	/// Holds the lock across a frame commit (surface state, then the buffer or eglSwapBuffers), so requests from
	/// other threads cannot land inside it. EGL reads its own queue meanwhile, which the dispatch thread never blocks.
	/// </summary>
	internal bool SubmitPresentation(Func<bool> submit)
	{
		if (IsDispatchThread)
		{
			throw new InvalidOperationException("Presentation must run outside dispatch callbacks.");
		}
		using (Lock())
		{
			return submit();
		}
	}

	public void StartDispatch()
	{
		using (Lock())
		{
			ThrowIfUnavailable();
			if (_thread is not null)
			{
				throw new InvalidOperationException("The dispatch loop is already running.");
			}
			_stopping = false;
			_thread = new Thread(DispatchLoop) { IsBackground = true, Name = "Uno Wayland dispatch" };
			_thread.Start();
		}
	}

	public void StopDispatch()
	{
		Thread? thread;
		WlCallback? wake = null;
		using (Lock())
		{
			thread = _thread;
			if (thread == Thread.CurrentThread)
			{
				throw new InvalidOperationException("Stop or dispose the display outside its event callbacks.");
			}
			_stopping = true;
			if (thread is not null && _display != 0 && DispatchError is null)
			{
				// The reply makes the socket readable, so the loop wakes now instead of at its next poll timeout.
				wake = SyncCore();
			}
		}
		thread?.Join();
		using (Lock())
		{
			wake?.DisposeLocal();
			if (_thread == thread)
			{
				_thread = null;
			}
		}
	}

	// libwayland is thread-safe for prepare_read, read_events and cancel_read, so none of them runs under the lock.
	// prepare_read is retried after dispatching under the lock; poll then read_events (non-blocking after poll) run
	// lock-free, and cancel_read happens only when leaving the loop with a read prepared. A thread presenting under
	// the lock, with EGL reading its own queue, therefore never waits on this one.
	private void DispatchLoop()
	{
		var prepared = false;
		try
		{
			while (!_stopping)
			{
				// Requests queued while dispatching are flushed when the lock is released, before the next attempt.
				while (WaylandNative.wl_display_prepare_read(_display) != 0)
				{
					using (Lock())
					{
						Check(WaylandNative.wl_display_dispatch_pending(_display));
						ThrowCallbackError();
					}
				}
				prepared = true;
				var fd = new Posix.PollFd { Fd = FileDescriptor, Events = POLLIN };
				var result = Posix.poll(&fd, 1, DispatchPollMilliseconds);
				var error = Marshal.GetLastPInvokeError();
				// Non-blocking once poll returned: with nothing to read it reads nothing.
				prepared = false;
				Check(WaylandNative.wl_display_read_events(_display));
				if (result < 0 && error != EINTR)
				{
					throw new IOException($"Wayland poll failed: {Marshal.GetPInvokeErrorMessage(error)}");
				}
				if ((fd.Revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
				{
					throw new IOException("The Wayland connection closed.");
				}
				using (Lock())
				{
					Check(WaylandNative.wl_display_dispatch_pending(_display));
					ThrowCallbackError();
				}
			}
		}
		catch (Exception error)
		{
			_dispatchError = error;
		}
		finally
		{
			if (prepared)
			{
				WaylandNative.wl_display_cancel_read(_display);
			}
		}
	}

	// Like XFlush: waits while the socket is full. A broken connection is reported by the dispatch loop, which sees the
	// hangup, rather than by whichever thread happened to send the last request.
	private void FlushPending()
	{
		if (_display == 0)
		{
			return;
		}
		try
		{
			while (FlushCore())
			{
				var fd = new Posix.PollFd { Fd = FileDescriptor, Events = POLLOUT };
				if (Posix.poll(&fd, 1, -1) < 0 && Marshal.GetLastPInvokeError() is var error && error != EINTR)
				{
					throw new IOException($"Wayland poll failed: {Marshal.GetPInvokeErrorMessage(error)}");
				}
				if ((fd.Revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
				{
					throw new IOException("The Wayland connection closed.");
				}
			}
		}
		catch (IOException error)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Flushing Wayland requests failed: {error.Message}");
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

	internal void Register(WaylandProxy proxy)
	{
		using (Lock())
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
		using (Lock())
		{
			if (_thread == Thread.CurrentThread)
			{
				throw new InvalidOperationException("Dispose the display outside its event callbacks.");
			}
			_disposing = true;
		}
		StopDispatch();
		using (Lock())
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
