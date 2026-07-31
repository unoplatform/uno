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

	// A scroll is considered over once nothing has moved for this long, at which point the buffer is dumped.
	private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(400);

	internal enum SampleKind : byte
	{
		/// <summary>A recorded frame: what the next presented picture will show.</summary>
		Frame = 0,

		/// <summary>A pointer sample arriving from the OS, before any frame alignment.</summary>
		Input = 1,
	}

	private readonly record struct Sample(long TimestampUs, double Value, SampleKind Kind, byte Phase);

	private static readonly Sample[] _samples = new Sample[Capacity];
	private static readonly Stopwatch _clock = Stopwatch.StartNew();
	private static readonly object _gate = new();

	private static int _count;
	private static long _lastActivityUs;
	private static bool _dumpPending;
	private static double _lastFrameValue;
	private static bool _announced;

	internal static bool IsEnabled => Uno.UI.FeatureConfiguration.ScrollViewer.EnableDiagnostics;

	/// <summary>Phase tag, so the dump distinguishes drag from inertia from wheel.</summary>
	internal static byte CurrentPhase { get; set; }

	internal const byte PhaseIdle = 0;
	internal const byte PhaseDrag = 1;
	internal const byte PhaseInertia = 2;
	internal const byte PhaseWheel = 3;

	/// <summary>
	/// Records a frame sample only when the position actually moved. The sampler runs on every frame
	/// (a Rendering subscription forces continuous rendering), so recording unconditionally would keep
	/// the settle timer permanently fresh and the buffer would never be dumped.
	/// </summary>
	internal static void RecordFrameIfMoved(double value)
	{
		if (!IsEnabled)
		{
			return;
		}

		if (Math.Abs(value - _lastFrameValue) < 0.01)
		{
			return;
		}

		_lastFrameValue = value;
		Record(SampleKind.Frame, value);
	}

	internal static void Record(SampleKind kind, double value)
	{
		if (!IsEnabled)
		{
			return;
		}

		var nowUs = _clock.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);

		lock (_gate)
		{
			if (!_announced)
			{
				_announced = true;
				typeof(ScrollDiagnostics).Log().Error("SCROLLDIAG ARMED");
			}

			if (_count < Capacity)
			{
				_samples[_count++] = new Sample(nowUs, value, kind, CurrentPhase);
			}

			_lastActivityUs = nowUs;
			_dumpPending = true;
		}
	}

	/// <summary>Call once per frame; dumps and clears the buffer once the scroll has settled.</summary>
	/// <summary>Call once per frame; dumps and clears the buffer once the scroll has settled.</summary>
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
				return;
			}

			snapshot = new Sample[_count];
			Array.Copy(_samples, snapshot, _count);
			count = _count;
			_count = 0;
			_dumpPending = false;
		}

		// One log entry per batch, not one for the whole buffer: Android's logger drops entries over
		// roughly 4 KB, which silently loses the entire dump.
		var log = typeof(ScrollDiagnostics).Log();
		log.Error($"SCROLLDIAG BEGIN samples={count}");

		var sb = new StringBuilder(1024);
		for (var i = 0; i < count; i++)
		{
			var s = snapshot[i];
			sb.Append(s.Kind == SampleKind.Frame ? 'F' : 'I')
				.Append(' ').Append(s.Phase)
				.Append(' ').Append(s.TimestampUs.ToString(CultureInfo.InvariantCulture))
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

}
