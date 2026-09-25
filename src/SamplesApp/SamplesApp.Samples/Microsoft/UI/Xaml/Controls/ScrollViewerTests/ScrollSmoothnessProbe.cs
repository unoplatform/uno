#nullable enable

#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

/// <summary>
/// Records what a scroll actually looked like: the offset each frame recorded, and when (and how often) each frame
/// reached the screen. Smoothness is judged on presented frames, not on ticks or offset updates.
/// </summary>
internal sealed class ScrollSmoothnessProbe
{
	private readonly record struct Recorded(long Sequence, long Timestamp, double X, double Y);

	private readonly record struct Presented(long Sequence, long Timestamp, bool Unchanged);

	private readonly FrameworkElement _scroller;
	private readonly CompositionTarget _target;
	private readonly List<Recorded> _records = new(4096);
	private readonly List<Presented> _presents = new(4096);
	private readonly List<(long Timestamp, string Kind)> _inputs = new(1024);
	private readonly object _presentGate = new();
	private long _startTimestamp;
	private bool _running;

	/// <param name="scroller">A <see cref="ScrollViewer"/> or a <see cref="ScrollView"/>.</param>
	public ScrollSmoothnessProbe(FrameworkElement scroller)
	{
		_scroller = scroller;
		_target = (CompositionTarget)(scroller.Visual.CompositionTarget
			?? throw new InvalidOperationException("The scroller is not in a live visual tree."));
	}

	public bool IsRunning => _running;

	public void Start()
	{
		_records.Clear();
		_inputs.Clear();
		lock (_presentGate)
		{
			_presents.Clear();
		}

		_startTimestamp = Stopwatch.GetTimestamp();
		_running = true;
		_target.FrameRendered += OnFrameRendered;
		_target.FramePresented += OnFramePresented;

		// Seed with the resting position so the first moving frame has something to diff against.
		OnFrameRendered();
	}

	public void Stop()
	{
		_running = false;
		_target.FrameRendered -= OnFrameRendered;
		_target.FramePresented -= OnFramePresented;
	}

	public void MarkInput(string kind) => _inputs.Add((Stopwatch.GetTimestamp(), kind));

	/// <summary>Timestamp of the last frame whose recorded offset differs from its predecessor, or null.</summary>
	public long? LastMotionTimestamp
	{
		get
		{
			for (var i = _records.Count - 1; i > 0; i--)
			{
				if (_records[i].X != _records[i - 1].X || _records[i].Y != _records[i - 1].Y)
				{
					return _records[i].Timestamp;
				}
			}

			return null;
		}
	}

	private void OnFrameRendered()
	{
		var offset = GetRenderedOffset();
		_records.Add(new(_target.LastRecordedSequence, Stopwatch.GetTimestamp(), offset.X, offset.Y));
	}

	private void OnFramePresented(object? sender, FramePresentedInfo info)
	{
		lock (_presentGate)
		{
			_presents.Add(new(info.Sequence, info.Timestamp, info.Unchanged));
		}
	}

	// What the frame just recorded, read off the content visual (composition animations tick at record start): the
	// ScrollViewer moves its content through AnchorPoint, the ScrollView's InteractionTracker through Translation.
	// Both lead the controls' offset properties while a move is animated.
	private Vector2 GetRenderedOffset()
	{
		var content = _scroller switch
		{
			ScrollViewer sv => sv.Presenter?.Content as UIElement,
			ScrollView view => view.ScrollPresenter?.Content,
			_ => null,
		};

		if (content is null)
		{
			return default;
		}

		var visual = content.Visual;
		var offset = -visual.AnchorPoint;
		if (visual.Properties.TryGetVector3("Translation", out var translation) == Microsoft.UI.Composition.CompositionGetValueStatus.Succeeded)
		{
			offset -= new Vector2(translation.X, translation.Y);
		}

		return offset;
	}

	public static (double Horizontal, double Vertical) GetScrollableSize(FrameworkElement scroller)
		=> scroller switch
		{
			ScrollViewer sv => (sv.ScrollableWidth, sv.ScrollableHeight),
			ScrollView view => (view.ScrollableWidth, view.ScrollableHeight),
			_ => (0, 0),
		};

	public ScrollSmoothnessResult Analyze(string scenario)
	{
		Presented[] presents;
		lock (_presentGate)
		{
			presents = _presents.ToArray();
		}

		var bySequence = new Dictionary<long, Recorded>(_records.Count);
		foreach (var r in _records)
		{
			bySequence[r.Sequence] = r;
		}

		var scrollable = GetScrollableSize(_scroller);
		var vertical = scrollable.Vertical >= scrollable.Horizontal;
		double Axis(Recorded r) => vertical ? r.Y : r.X;

		// A frame the host presents before the probe saw it record (or after it stopped) carries no offset.
		var frames = new List<(Presented P, Recorded R)>(presents.Length);
		foreach (var p in presents)
		{
			if (bySequence.TryGetValue(p.Sequence, out var r))
			{
				frames.Add((p, r));
			}
		}

		var result = new ScrollSmoothnessResult { Scenario = scenario, Records = _records.Count, Presents = frames.Count };
		if (frames.Count < 3)
		{
			return result;
		}

		// The motion window runs from the first to the last presented frame whose offset moved.
		int first = -1, last = -1;
		for (var i = 1; i < frames.Count; i++)
		{
			if (Axis(frames[i].R) != Axis(frames[i - 1].R))
			{
				if (first < 0)
				{
					first = i - 1;
				}

				last = i;
			}
		}

		if (first < 0)
		{
			return result;
		}

		var window = frames.GetRange(first, last - first + 1);
		static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

		var intervals = new List<double>(window.Count);
		for (var i = 1; i < window.Count; i++)
		{
			intervals.Add(Ms(window[i].P.Timestamp - window[i - 1].P.Timestamp));
		}

		var sortedIntervals = intervals.OrderBy(static x => x).ToArray();
		var period = Percentile(sortedIntervals, 0.5);

		result.MotionMs = Ms(window[^1].P.Timestamp - window[0].P.Timestamp);
		result.MotionPx = Math.Abs(Axis(window[^1].R) - Axis(window[0].R));
		result.MovingFrames = window.Count;
		result.Fps = result.MotionMs > 0 ? (window.Count - 1) * 1000.0 / result.MotionMs : 0;
		result.IntervalP50 = period;
		result.IntervalP95 = Percentile(sortedIntervals, 0.95);
		result.IntervalP99 = Percentile(sortedIntervals, 0.99);
		result.IntervalMax = sortedIntervals[^1];
		result.LongFrames = intervals.Count(x => x > period * 1.5);

		var direction = Math.Sign(Axis(window[^1].R) - Axis(window[0].R));
		var residuals = new List<double>(window.Count);
		for (var i = 1; i < window.Count; i++)
		{
			var (p, r) = window[i];
			var (pp, pr) = window[i - 1];
			var step = Axis(r) - Axis(pr);

			if (p.Sequence == pp.Sequence)
			{
				result.RepeatedFrames++;
			}
			else if (step == 0)
			{
				result.StalledFrames++;
			}
			else if (Math.Sign(step) != direction && Math.Abs(step) > 0.01)
			{
				result.BackwardSteps++;
				result.MaxBackwardPx = Math.Max(result.MaxBackwardPx, Math.Abs(step));
			}

			result.MaxStepPx = Math.Max(result.MaxStepPx, Math.Abs(step));
		}

		// Judder: how far each frame sits from a local quadratic through its two neighbours on each side, at the
		// times those frames were presented. Constant velocity and constant deceleration leave no residual, so what
		// remains is uneven motion as the eye sees it: offsets sampled at the wrong time, dropped or doubled steps.
		for (var i = 2; i + 2 < window.Count; i++)
		{
			residuals.Add(QuadraticResidual(window, i, Axis));
		}

		if (residuals.Count > 0)
		{
			var sortedResiduals = residuals.OrderBy(static x => x).ToArray();
			result.JudderRmsPx = Math.Sqrt(residuals.Average(static x => x * x));
			result.JudderP95Px = Percentile(sortedResiduals, 0.95);
			result.JudderMaxPx = sortedResiduals[^1];
		}

		var latencies = window
			.Where(static f => f.P.Timestamp >= f.R.Timestamp)
			.Select(f => Ms(f.P.Timestamp - f.R.Timestamp))
			.OrderBy(static x => x)
			.ToArray();
		if (latencies.Length > 0)
		{
			result.RecordToPresentP50 = Percentile(latencies, 0.5);
			result.RecordToPresentP95 = Percentile(latencies, 0.95);
		}

		// First input to the first presented frame that moved: the "did it react" latency.
		if (_inputs.Count > 0)
		{
			var firstInput = _inputs[0].Timestamp;
			var firstMove = window.Skip(1).FirstOrDefault(f => f.R.Timestamp >= firstInput);
			if (firstMove.P.Timestamp != 0)
			{
				result.FirstInputToMoveMs = Ms(firstMove.P.Timestamp - firstInput);
			}
		}

		result.Trace = window
			.Select(f => (Ms(f.P.Timestamp - _startTimestamp), Axis(f.R), f.P.Sequence))
			.ToArray();

		return result;
	}

	private static double QuadraticResidual(List<(Presented P, Recorded R)> window, int center, Func<Recorded, double> axis)
	{
		double s0 = 0, s1 = 0, s2 = 0, s3 = 0, s4 = 0, y0 = 0, y1 = 0, y2 = 0;
		var t0 = window[center].P.Timestamp;
		for (var k = center - 2; k <= center + 2; k++)
		{
			var t = (window[k].P.Timestamp - t0) * 1000.0 / Stopwatch.Frequency;
			var y = axis(window[k].R);
			var t2 = t * t;
			s0 += 1; s1 += t; s2 += t2; s3 += t2 * t; s4 += t2 * t2;
			y0 += y; y1 += y * t; y2 += y * t2;
		}

		// Normal equations of y = a + b t + c t², solved by Cramer's rule; the fitted value at t = 0 is a.
		var det = s0 * (s2 * s4 - s3 * s3) - s1 * (s1 * s4 - s3 * s2) + s2 * (s1 * s3 - s2 * s2);
		if (Math.Abs(det) < 1e-12)
		{
			return 0;
		}

		var a = (y0 * (s2 * s4 - s3 * s3) - s1 * (y1 * s4 - s3 * y2) + s2 * (y1 * s3 - s2 * y2)) / det;
		return Math.Abs(axis(window[center].R) - a);
	}

	private static double Percentile(double[] sorted, double p)
		=> sorted.Length == 0 ? 0 : sorted[Math.Min(sorted.Length - 1, (int)Math.Floor(p * sorted.Length))];
}

internal sealed class ScrollSmoothnessResult
{
	public string Scenario { get; set; } = "";
	public int Records { get; set; }
	public int Presents { get; set; }
	public int MovingFrames { get; set; }
	public double MotionMs { get; set; }
	public double MotionPx { get; set; }
	public double Fps { get; set; }
	public double IntervalP50 { get; set; }
	public double IntervalP95 { get; set; }
	public double IntervalP99 { get; set; }
	public double IntervalMax { get; set; }
	public int LongFrames { get; set; }
	public int RepeatedFrames { get; set; }
	public int StalledFrames { get; set; }
	public int BackwardSteps { get; set; }
	public double MaxBackwardPx { get; set; }
	public double MaxStepPx { get; set; }
	public double JudderRmsPx { get; set; }
	public double JudderP95Px { get; set; }
	public double JudderMaxPx { get; set; }
	public double RecordToPresentP50 { get; set; }
	public double RecordToPresentP95 { get; set; }
	public double FirstInputToMoveMs { get; set; }
	public (double TimeMs, double Offset, long Sequence)[] Trace { get; set; } = Array.Empty<(double, double, long)>();

	public string ToSummary()
		=> string.Create(CultureInfo.InvariantCulture,
			$"{Scenario,-14} fps {Fps,5:F1}  frame p50 {IntervalP50,5:F1} p95 {IntervalP95,5:F1} p99 {IntervalP99,5:F1} max {IntervalMax,6:F1}ms  long {LongFrames,3}  " +
			$"step max {MaxStepPx,5:F0}px repeat {RepeatedFrames,3} stall {StalledFrames,3} back {BackwardSteps,2}({MaxBackwardPx:F1}px)  " +
			$"judder rms {JudderRmsPx,5:F2} p95 {JudderP95Px,5:F2} max {JudderMaxPx,6:F2}px  rec→pres {RecordToPresentP50,4:F1}/{RecordToPresentP95,4:F1}ms  " +
			$"react {FirstInputToMoveMs,5:F1}ms  {MovingFrames} frames/{MotionMs:F0}ms/{MotionPx:F0}px");

	public string ToJson(string platform, bool includeTrace)
	{
		var sb = new StringBuilder();
		sb.Append(CultureInfo.InvariantCulture, $"{{\"platform\":\"{platform}\",\"scenario\":\"{Scenario}\",\"records\":{Records},\"presents\":{Presents},\"movingFrames\":{MovingFrames}");
		sb.Append(CultureInfo.InvariantCulture, $",\"motionMs\":{MotionMs:F1},\"motionPx\":{MotionPx:F1},\"fps\":{Fps:F2}");
		sb.Append(CultureInfo.InvariantCulture, $",\"intervalP50\":{IntervalP50:F2},\"intervalP95\":{IntervalP95:F2},\"intervalP99\":{IntervalP99:F2},\"intervalMax\":{IntervalMax:F2},\"longFrames\":{LongFrames}");
		sb.Append(CultureInfo.InvariantCulture, $",\"repeatedFrames\":{RepeatedFrames},\"stalledFrames\":{StalledFrames},\"backwardSteps\":{BackwardSteps},\"maxBackwardPx\":{MaxBackwardPx:F2},\"maxStepPx\":{MaxStepPx:F2}");
		sb.Append(CultureInfo.InvariantCulture, $",\"judderRmsPx\":{JudderRmsPx:F3},\"judderP95Px\":{JudderP95Px:F3},\"judderMaxPx\":{JudderMaxPx:F3}");
		sb.Append(CultureInfo.InvariantCulture, $",\"recordToPresentP50\":{RecordToPresentP50:F2},\"recordToPresentP95\":{RecordToPresentP95:F2},\"firstInputToMoveMs\":{FirstInputToMoveMs:F2}");
		if (includeTrace)
		{
			sb.Append(",\"trace\":[");
			for (var i = 0; i < Trace.Length; i++)
			{
				var (t, o, s) = Trace[i];
				sb.Append(CultureInfo.InvariantCulture, $"{(i == 0 ? "" : ",")}[{t:F2},{o:F2},{s}]");
			}
			sb.Append(']');
		}
		sb.Append('}');
		return sb.ToString();
	}
}
#endif
