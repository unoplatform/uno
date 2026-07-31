#nullable enable

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Records per-frame scroll telemetry into a ring buffer and dumps it once the scroll settles.
/// </summary>
/// <remarks>
/// Buffered rather than logged per frame on purpose: emitting a log line inside the frame would
/// perturb the very timing being measured, which on a phone is the same order as the effect under
/// investigation. Enable with <see cref="Uno.UI.FeatureConfiguration.ScrollViewer.EnableDiagnostics"/>.
/// </remarks>
internal static class ScrollDiagnostics
{
	private const int Capacity = 4096;

	// Flush well before the buffer is full so a crash mid-scroll still yields most of the capture.
	private const int FlushThreshold = 600;

	// A scroll is considered over once nothing has moved for this long, at which point the buffer is dumped.
	private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(400);

	internal enum SampleKind : byte
	{
		/// <summary>A recorded frame: what the next presented picture will show.</summary>
		Frame = 0,

		/// <summary>A pointer sample arriving from the OS, before any frame alignment.</summary>
		Input = 1,
	}

	private readonly record struct Sample(long TimestampUs, long FrameUs, double Value, SampleKind Kind, byte Phase);

	private static readonly Sample[] _samples = new Sample[Capacity];
	private static readonly Stopwatch _clock = Stopwatch.StartNew();
	private static readonly object _gate = new();

	private static int _count;
	private static long _lastActivityUs;
	private static bool _dumpPending;
	private static bool _announced;
	private static global::System.Threading.Timer? _dumpTimer;
	private static int _idleTicks;

	/// <summary>
	/// The dump is driven by a threadpool timer, not by the frame callback or a DispatcherQueue:
	/// Record is called from more than one thread, so GetForCurrentThread() can return null, and
	/// whether frames keep arriving after a scroll settles depends on what else is animating. The
	/// dump only snapshots under a lock and logs, so it has no thread affinity.
	/// </summary>
	private static void StartDumpTimer()
	{
		_dumpTimer ??= new global::System.Threading.Timer(
			static _ => TryDump(),
			null,
			dueTime: 250,
			period: 250);
	}

	internal static bool IsEnabled => Uno.UI.FeatureConfiguration.ScrollViewer.EnableDiagnostics;

	/// <summary>Phase tag, so the dump distinguishes drag from inertia from wheel.</summary>
	internal static byte CurrentPhase { get; set; }

	internal const byte PhaseIdle = 0;
	internal const byte PhaseDrag = 1;
	internal const byte PhaseInertia = 2;
	internal const byte PhaseWheel = 3;

	internal static void Record(SampleKind kind, double value, long frameTimestampTicks = 0)
	{
		if (!IsEnabled)
		{
			return;
		}

		var nowUs = _clock.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
		var flushNow = false;

		lock (_gate)
		{
			if (!_announced)
			{
				_announced = true;
				typeof(ScrollDiagnostics).Log().Error("SCROLLDIAG ARMED");
				StartDumpTimer();
			}

			if (_count < Capacity)
			{
				_samples[_count++] = new Sample(nowUs, frameTimestampTicks / 10, value, kind, CurrentPhase);
			}

			_lastActivityUs = nowUs;
			_dumpPending = true;
			flushNow = _count >= FlushThreshold;
		}

		if (flushNow)
		{
			// Flush partway rather than only on settle: the buffer lives in memory, so anything that
			// kills the process before the scroll ends takes the whole capture with it.
			Dump();
		}
	}

	/// <summary>Call once per frame; dumps and clears the buffer once the scroll has settled.</summary>
	/// <summary>Call once per frame; dumps and clears the buffer once the scroll has settled.</summary>
	/// <summary>Writes and clears whatever is buffered, regardless of whether the scroll has settled.</summary>
	private static void Dump()
	{
		Sample[] snapshot;
		int count;

		lock (_gate)
		{
			if (_count == 0)
			{
				return;
			}

			snapshot = new Sample[_count];
			Array.Copy(_samples, snapshot, _count);
			count = _count;
			_count = 0;
			_dumpPending = false;
		}

		Emit(snapshot, count);
	}

	private static void Emit(Sample[] snapshot, int count)
	{
		var log = typeof(ScrollDiagnostics).Log();
		log.Error($"SCROLLDIAG BEGIN samples={count}");

		var sb = new StringBuilder(1024);
		for (var i = 0; i < count; i++)
		{
			var s = snapshot[i];
			sb.Append(s.Kind == SampleKind.Frame ? 'F' : 'I')
				.Append(' ').Append(s.Phase)
				.Append(' ').Append(s.TimestampUs.ToString(CultureInfo.InvariantCulture))
				.Append(' ').Append(s.FrameUs.ToString(CultureInfo.InvariantCulture))
				.Append(' ').Append(s.Value.ToString("F3", CultureInfo.InvariantCulture))
				.Append(';');

			if (sb.Length > 900 || i == count - 1)
			{
				log.Error("SCROLLDIAG " + sb.ToString());
				sb.Clear();
			}
		}

		log.Error("SCROLLDIAG END");
	}

	internal static void TryDump()
	{
		if (!IsEnabled)
		{
			return;
		}

		Sample[] snapshot;
		int count;

		lock (_gate)
		{
			if (!_dumpPending || _count == 0)
			{
				return;
			}

			var idleUs = _clock.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000) - _lastActivityUs;
			if (idleUs < SettleDelay.TotalMilliseconds * 1000)
			{
				if (++_idleTicks % 40 == 0)
				{
					typeof(ScrollDiagnostics).Log().Error(
						$"SCROLLDIAG WAITING count={_count} idleMs={idleUs / 1000}");
				}

				return;
			}

			_idleTicks = 0;

			snapshot = new Sample[_count];
			Array.Copy(_samples, snapshot, _count);
			count = _count;
			_count = 0;
			_dumpPending = false;
		}

		Emit(snapshot, count);
	}

}
