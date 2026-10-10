#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;
using Uno.UI.Composition;

namespace Uno.UI.Runtime.X11;

/// <summary>
/// A window's vsync from the X Present extension: a NotifyMSC request completes on the vblank of the CRTC showing
/// the window, with that vblank's time (UST) and counter (MSC). It serves every graphics context alike, since it
/// needs nothing from how the window is drawn.
/// </summary>
/// <remarks>
/// It has a connection of its own, used only from the render thread, so its events never pass through the Xlib
/// event loops and waiting on them can't race those loops for the socket. Requests are sent only for frames, so an
/// idle window gets no events. Needs libxcb-present (installed with Mesa); without it, or without the extension
/// (old servers), <see cref="TryCreate"/> returns null.
/// </remarks>
internal sealed unsafe class X11PresentVsync : IDisposable
{
	private const byte GenericEvent = 35;
	private const ushort PresentCompleteNotify = 1;
	private const byte PresentCompleteKindNotifyMsc = 1;
	private const uint PresentCompleteNotifyMask = 2;
	private const int ClockMonotonic = 1;

	// No display refreshes slower than this: a longer interval is a server faking vblanks for a window it doesn't
	// show (1Hz for a window on no CRTC, or a hidden one on Xwayland).
	private static readonly long MaxPeriod = Stopwatch.Frequency / 20;
	// A rate change of more than this restarts the period estimate.
	private const double PeriodChangeTolerance = 0.05;

	private readonly nint _connection;
	private readonly uint _window;
	private readonly byte _presentOpcode;
	private readonly int _fd;
	private uint _serial;

	// The latest vsync the server reported, as a Stopwatch time and as MSC.
	private long _lastVsync;
	private ulong _lastMsc;
	// The refresh period in Stopwatch ticks, measured over as many vsyncs as the rate held for (UST is in whole
	// microseconds, too coarse for one interval), and where that measure starts.
	private long _period;
	private long _baseVsync;
	private ulong _baseMsc;

	private X11PresentVsync(nint connection, uint window, byte presentOpcode)
	{
		_connection = connection;
		_window = window;
		_presentOpcode = presentOpcode;
		_fd = Xcb.xcb_get_file_descriptor(connection);
	}

	/// <summary>The refresh period in Stopwatch ticks, 0 until two vsyncs were seen.</summary>
	public long Period => _period;

	/// <summary>The latest vsync the server reported, as a Stopwatch time; 0 before the first.</summary>
	public long LastVsync => _lastVsync;

	public static X11PresentVsync? TryCreate(string? displayName, nint window)
	{
		if (!Xcb.IsAvailable)
		{
			typeof(X11PresentVsync).LogInfo()?.Info("libxcb-present is not available; frames are paced by a timer, not by vsync.");
			return null;
		}

		var name = displayName is null ? 0 : Marshal.StringToHGlobalAnsi(displayName);
		nint connection;
		try
		{
			connection = Xcb.xcb_connect((byte*)name, null);
		}
		finally
		{
			Marshal.FreeHGlobal(name);
		}

		if (connection == 0 || Xcb.xcb_connection_has_error(connection) != 0)
		{
			typeof(X11PresentVsync).LogWarn()?.Warn("Could not open an X connection for vsync; frames are paced by a timer.");
			Xcb.xcb_disconnect(connection);
			return null;
		}

		// xcb_query_extension_reply_t: response_type, pad, sequence (2), length (4), present, major_opcode, ...
		var extension = (byte*)Xcb.xcb_get_extension_data(connection, Xcb.PresentId);
		if (extension is null || extension[8] == 0)
		{
			typeof(X11PresentVsync).LogInfo()?.Info("The X server has no Present extension; frames are paced by a timer, not by vsync.");
			Xcb.xcb_disconnect(connection);
			return null;
		}

		var xid = (uint)window;
		Xcb.xcb_present_select_input(connection, Xcb.xcb_generate_id(connection), xid, PresentCompleteNotifyMask);
		Xcb.xcb_flush(connection);

		return new X11PresentVsync(connection, xid, extension[9]);
	}

	/// <summary>
	/// The latest vsync at or before <paramref name="now"/>, extrapolated from the last one the server reported, or
	/// 0 when no rate is known yet.
	/// </summary>
	public long GetLatestVsync(long now)
		=> _period == 0 ? 0 : FrameClock.LatestVsyncAtOrBefore(_lastVsync, _period, now);

	/// <summary>
	/// Blocks until the next vsync whose MSC is <paramref name="divisor"/> vsyncs on from the last one reported, so
	/// frames keep the same phase.
	/// </summary>
	/// <returns>The vsync's Stopwatch time, or null when the server didn't report a usable one in time.</returns>
	public long? WaitForVsync(int divisor, out string? failure)
	{
		// Frames run back to back: the vsync is measured against the last one. After a pause it starts afresh: the
		// MSC may not have counted it (Xwayland counts only the window's own frames).
		var isContinuous = _lastMsc != 0 && Stopwatch.GetTimestamp() - _lastVsync < 4 * divisor * Math.Max(_period, Stopwatch.Frequency / 240);

		var serial = ++_serial;
		Xcb.xcb_present_notify_msc(_connection, _window, serial, 0, (ulong)divisor, _lastMsc % (ulong)divisor);
		if (Xcb.xcb_flush(_connection) <= 0)
		{
			failure = "lost the X connection";
			return null;
		}

		// Long enough for the vsyncs asked for at any real refresh rate, short enough not to stall on a server that
		// stopped reporting them.
		var timeout = Math.Clamp(_period * divisor * 3, Stopwatch.Frequency / 20, Stopwatch.Frequency / 4);
		var deadline = Stopwatch.GetTimestamp() + timeout;

		while (true)
		{
			if (TryReadCompletion(serial, out var ust, out var msc))
			{
				return OnVsync(ust, msc, isContinuous, out failure);
			}

			var remaining = deadline - Stopwatch.GetTimestamp();
			if (remaining <= 0 || Xcb.xcb_connection_has_error(_connection) != 0)
			{
				failure = remaining <= 0 ? "timed out" : "lost the X connection";
				return null;
			}

			X11Helper.Pollfd pollfd = new() { fd = _fd, events = X11Helper.POLLIN };
			_ = X11Helper.poll(&pollfd, 1, (int)Math.Max(1, remaining * 1000 / Stopwatch.Frequency));
		}
	}

	private bool TryReadCompletion(uint serial, out ulong ust, out ulong msc)
	{
		while (Xcb.xcb_poll_for_event(_connection) is var e && e is not null)
		{
			try
			{
				// xcb_present_complete_notify_event_t, as xcb lays it out in memory: the full sequence is inserted
				// at 32, after the UST, which moves the MSC to 36.
				if ((e[0] & 0x7f) == GenericEvent
					&& e[1] == _presentOpcode
					&& *(ushort*)(e + 8) == PresentCompleteNotify
					&& e[10] == PresentCompleteKindNotifyMsc
					&& *(uint*)(e + 20) == serial)
				{
					ust = *(ulong*)(e + 24);
					msc = *(ulong*)(e + 36);
					return true;
				}
			}
			finally
			{
				NativeMemory.Free(e);
			}
		}

		ust = msc = 0;
		return false;
	}

	private long? OnVsync(ulong ust, ulong msc, bool isContinuous, out string? failure)
	{
		// UST is CLOCK_MONOTONIC microseconds. It goes through its age, so nothing assumes that is Stopwatch's clock.
		Libc.clock_gettime(ClockMonotonic, out var now);
		var ageInMicroseconds = (long)now.tv_sec * 1_000_000 + (long)now.tv_nsec / 1_000 - (long)ust;
		if (ageInMicroseconds is < 0 or > 1_000_000)
		{
			failure = $"reported a vsync {ageInMicroseconds / 1000.0:F1}ms old";
			return null;
		}

		var vsync = Stopwatch.GetTimestamp() - ageInMicroseconds * Stopwatch.Frequency / 1_000_000;

		if (!isContinuous)
		{
			_baseVsync = _lastVsync = vsync;
			_baseMsc = _lastMsc = msc;
			failure = null;
			return vsync;
		}

		// A request the server couldn't queue on a vblank completes at once, without the MSC moving on.
		if (msc <= _lastMsc)
		{
			failure = "completed without a new vsync";
			return null;
		}

		var interval = (vsync - _lastVsync) / (long)(msc - _lastMsc);
		if (interval > MaxPeriod)
		{
			_lastVsync = vsync;
			_lastMsc = msc;
			failure = $"reported vsyncs {interval * 1000.0 / Stopwatch.Frequency:F0}ms apart";
			return null;
		}

		UpdatePeriod(interval, vsync, msc);

		_lastVsync = vsync;
		_lastMsc = msc;
		failure = null;
		return vsync;
	}

	private void UpdatePeriod(long interval, long vsync, ulong msc)
	{
		// A refresh-rate or CRTC change: measure again from the vsync before it.
		if (_period == 0 || Math.Abs(interval - _period) > _period * PeriodChangeTolerance)
		{
			_baseVsync = _lastVsync;
			_baseMsc = _lastMsc;
			_period = interval;
			return;
		}

		_period = (vsync - _baseVsync) / (long)(msc - _baseMsc);
	}

	public void Dispose() => Xcb.xcb_disconnect(_connection);

	private static class Libc
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct Timespec
		{
			public nint tv_sec;
			public nint tv_nsec;
		}

		[DllImport("libc")]
		public static extern int clock_gettime(int clockId, out Timespec time);
	}

	/// <summary>libxcb and libxcb-present, resolved at run time so a system without them only loses vsync.</summary>
	private static class Xcb
	{
		public static readonly delegate* unmanaged<byte*, int*, nint> xcb_connect;
		public static readonly delegate* unmanaged<nint, int> xcb_connection_has_error;
		public static readonly delegate* unmanaged<nint, void> xcb_disconnect;
		public static readonly delegate* unmanaged<nint, int> xcb_get_file_descriptor;
		public static readonly delegate* unmanaged<nint, uint> xcb_generate_id;
		public static readonly delegate* unmanaged<nint, int> xcb_flush;
		public static readonly delegate* unmanaged<nint, byte*> xcb_poll_for_event;
		public static readonly delegate* unmanaged<nint, nint, nint> xcb_get_extension_data;
		public static readonly delegate* unmanaged<nint, uint, uint, uint, uint> xcb_present_select_input;
		public static readonly delegate* unmanaged<nint, uint, uint, ulong, ulong, ulong, uint> xcb_present_notify_msc;
		public static readonly nint PresentId;

		public static bool IsAvailable => xcb_present_notify_msc is not null;

		static Xcb()
		{
			if (!NativeLibrary.TryLoad("libxcb.so.1", out var xcb) || !NativeLibrary.TryLoad("libxcb-present.so.0", out var present))
			{
				return;
			}

			try
			{
				xcb_connect = (delegate* unmanaged<byte*, int*, nint>)NativeLibrary.GetExport(xcb, nameof(xcb_connect));
				xcb_connection_has_error = (delegate* unmanaged<nint, int>)NativeLibrary.GetExport(xcb, nameof(xcb_connection_has_error));
				xcb_disconnect = (delegate* unmanaged<nint, void>)NativeLibrary.GetExport(xcb, nameof(xcb_disconnect));
				xcb_get_file_descriptor = (delegate* unmanaged<nint, int>)NativeLibrary.GetExport(xcb, nameof(xcb_get_file_descriptor));
				xcb_generate_id = (delegate* unmanaged<nint, uint>)NativeLibrary.GetExport(xcb, nameof(xcb_generate_id));
				xcb_flush = (delegate* unmanaged<nint, int>)NativeLibrary.GetExport(xcb, nameof(xcb_flush));
				xcb_poll_for_event = (delegate* unmanaged<nint, byte*>)NativeLibrary.GetExport(xcb, nameof(xcb_poll_for_event));
				xcb_get_extension_data = (delegate* unmanaged<nint, nint, nint>)NativeLibrary.GetExport(xcb, nameof(xcb_get_extension_data));
				PresentId = NativeLibrary.GetExport(present, "xcb_present_id");
				xcb_present_select_input = (delegate* unmanaged<nint, uint, uint, uint, uint>)NativeLibrary.GetExport(present, nameof(xcb_present_select_input));
				xcb_present_notify_msc = (delegate* unmanaged<nint, uint, uint, ulong, ulong, ulong, uint>)NativeLibrary.GetExport(present, nameof(xcb_present_notify_msc));
			}
			catch (EntryPointNotFoundException)
			{
				xcb_present_notify_msc = null;
			}
		}
	}
}
