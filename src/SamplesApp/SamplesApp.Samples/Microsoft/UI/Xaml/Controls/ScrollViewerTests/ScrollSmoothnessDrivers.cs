#nullable enable

#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Input.Preview.Injection;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests;

/// <summary>
/// Synthetic scroll input, injected into Uno's input pipeline on a fixed schedule so every platform replays the same
/// gesture. Timestamps carry the schedule, not the delivery time, like a real digitizer's would.
/// </summary>
internal static class ScrollSmoothnessDrivers
{
	public static readonly string[] Scenarios =
	{
		"wheel-burst",
		"wheel-slow",
		"touchpad",
		"touch-fling",
		"touch-drag",
		"key-down",
		"page-down",
		"changeview",
	};

	/// <summary>
	/// Records while something outside the app scrolls (a person, adb, CDP touch events), so the host's native input
	/// path is measured too. Not part of "all".
	/// </summary>
	public const string ExternalScenario = "external";

	public static int ExternalDurationMs { get; set; } = 5000;

	private readonly record struct Step(double AtMs, Action<InputInjector> Inject, string Kind);

	public static async Task RunAsync(string scenario, Control sv, ScrollSmoothnessProbe probe, CancellationToken ct)
	{
		var bounds = sv.TransformToVisual(null).TransformBounds(new Rect(0, 0, sv.ActualWidth, sv.ActualHeight));
		var center = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
		var scrollable = ScrollSmoothnessProbe.GetScrollableSize(sv);
		var vertical = scrollable.Vertical >= scrollable.Horizontal;

		// UNO_SCROLL_PROBE_TOUCH_CONTENT_Y pins where a touch starts, in the scrolled content's coordinates.
		Point? touchStart = null;
		if (Environment.GetEnvironmentVariable("UNO_SCROLL_PROBE_TOUCH_CONTENT_Y") is { } contentY
			&& double.TryParse(contentY, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y)
			&& GetContent(sv) is { } pinned)
		{
			touchStart = pinned.TransformToVisual(null).TransformPoint(new Point(pinned.ActualSize.X / 2, y));
		}

		switch (scenario)
		{
			case "wheel-burst":
				// A fast spin: ten detents 40 ms apart.
				await PlayAsync(WheelSteps(center, vertical, count: 10, spacingMs: 40, delta: -120), probe, ct);
				break;
			case "wheel-slow":
				await PlayAsync(WheelSteps(center, vertical, count: 4, spacingMs: 350, delta: -120), probe, ct);
				break;
			case "touchpad":
				// Precision touchpad: small deltas at the digitizer's rate, decaying like a two-finger flick.
				await PlayAsync(TouchpadSteps(center, vertical), probe, ct);
				break;
			case "touch-fling":
				// 25 px per 8 ms sample = 3125 px/s, released while still moving.
				await PlayAsync(TouchSteps(touchStart, center, bounds, vertical, sampleMs: 8, pxPerSample: 25, samples: 16, holdBeforeReleaseMs: 0), probe, ct);
				break;
			case "touch-drag":
				// 3 px per 8 ms sample = 375 px/s (whole pixels: injected positions are integers), stopped before release.
				await PlayAsync(TouchSteps(touchStart, center, bounds, vertical, sampleMs: 8, pxPerSample: 3, samples: 150, holdBeforeReleaseMs: 150), probe, ct);
				break;
			case "key-down":
				// Keyboard auto-repeat at ~30 Hz.
				await PlayKeysAsync(sv, VirtualKey.Down, count: 20, spacingMs: 33, probe, ct);
				break;
			case "page-down":
				await PlayKeysAsync(sv, VirtualKey.PageDown, count: 3, spacingMs: 450, probe, ct);
				break;
			case "changeview":
				probe.MarkInput("changeview");
				ScrollBy(sv, vertical ? 0 : 3000, vertical ? 3000 : 0, animate: true);
				break;
			case ExternalScenario:
				var bounds2 = sv.TransformToVisual(null).TransformBounds(new Rect(0, 0, sv.ActualWidth, sv.ActualHeight));
				Console.WriteLine(FormattableString.Invariant($"[scroll-probe] external-start {{\"x\":{bounds2.X:F0},\"y\":{bounds2.Y:F0},\"width\":{bounds2.Width:F0},\"height\":{bounds2.Height:F0}}}"));
				await Task.Delay(ExternalDurationMs, ct);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null);
		}
	}

	private static UIElement? GetContent(Control scroller)
		=> scroller switch
		{
			ScrollViewer sv => sv.Presenter?.Content as UIElement,
			ScrollView view => view.ScrollPresenter?.Content,
			_ => null,
		};

	public static void ScrollBy(Control scroller, double dx, double dy, bool animate)
	{
		switch (scroller)
		{
			case ScrollViewer sv:
				sv.ChangeView(sv.HorizontalOffset + dx, sv.VerticalOffset + dy, null, disableAnimation: !animate);
				break;
			case ScrollView view:
				view.ScrollBy(dx, dy, new ScrollingScrollOptions(animate ? ScrollingAnimationMode.Enabled : ScrollingAnimationMode.Disabled));
				break;
		}
	}

	public static void ScrollToOrigin(Control scroller)
	{
		switch (scroller)
		{
			case ScrollViewer sv:
				sv.ChangeView(0, 0, null, disableAnimation: true);
				break;
			case ScrollView view:
				view.ScrollTo(0, 0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
				break;
		}
	}

	private static IEnumerable<Step> WheelSteps(Point center, bool vertical, int count, double spacingMs, int delta)
	{
		yield return new(0, i => MoveMouseTo(i, center), "move");
		for (var n = 0; n < count; n++)
		{
			yield return new(10 + n * spacingMs, i => i.InjectMouseInput(new[]
			{
				new InjectedInputMouseInfo
				{
					MouseOptions = vertical ? InjectedInputMouseOptions.Wheel : InjectedInputMouseOptions.HWheel,
					DeltaY = vertical ? delta : 0,
					DeltaX = vertical ? 0 : -delta,
					TimeOffsetInMilliseconds = (uint)(n == 0 ? 10 : spacingMs),
				}
			}), "wheel");
		}
	}

	private static IEnumerable<Step> TouchpadSteps(Point center, bool vertical)
	{
		yield return new(0, i => MoveMouseTo(i, center), "move");
		const int sampleMs = 8;
		var t = 10.0;
		for (var n = 0; n < 60; n++)
		{
			// 30 px of deltas per sample decaying to ~2; |delta| < 120 marks it as a precise device.
			var delta = -(int)Math.Max(2, Math.Round(30 * Math.Pow(0.95, n)));
			yield return new(t, i => i.InjectMouseInput(new[]
			{
				new InjectedInputMouseInfo
				{
					MouseOptions = vertical ? InjectedInputMouseOptions.Wheel : InjectedInputMouseOptions.HWheel,
					DeltaY = vertical ? delta : 0,
					DeltaX = vertical ? 0 : -delta,
					TimeOffsetInMilliseconds = sampleMs,
				}
			}), "touchpad");
			t += sampleMs;
		}
	}

	private static uint _nextTouchId = 1;
	private static ulong _touchFrame;

	// UNO_SCROLL_PROBE_REUSE_TOUCH_ID=1 keeps pointer id 1 for every gesture, like digitizers that recycle ids.
	private static readonly bool _reuseTouchId = Environment.GetEnvironmentVariable("UNO_SCROLL_PROBE_REUSE_TOUCH_ID") == "1";

	private static IEnumerable<Step> TouchSteps(Point? start, Point center, Rect bounds, bool vertical, int sampleMs, int pxPerSample, int samples, int holdBeforeReleaseMs)
	{
		var id = _reuseTouchId ? 1 : _nextTouchId++;
		// Start below/right of the center so the drag towards the origin stays inside the viewport.
		var travel = pxPerSample * samples;
		var x = (int)center.X;
		var y = (int)center.Y;
		if (start is { } s)
		{
			(x, y) = ((int)s.X, (int)s.Y);
		}
		else
		{
			if (vertical)
			{
				y += Math.Min(travel / 2, (int)center.Y / 2);
			}
			else
			{
				x += Math.Min(travel / 2, (int)center.X / 2);
			}

			// Keep the press inside the scroller: below it sits the sample's description panel.
			x = Math.Min(x, (int)(bounds.Right - 20));
			y = Math.Min(y, (int)(bounds.Bottom - 20));
		}

		const InjectedInputPointerOptions contact = InjectedInputPointerOptions.FirstButton | InjectedInputPointerOptions.InRange | InjectedInputPointerOptions.InContact | InjectedInputPointerOptions.Primary;

		yield return new(0, i => i.InjectTouchInput(new[] { Touch(id, x, y, contact | InjectedInputPointerOptions.New | InjectedInputPointerOptions.PointerDown, 0) }), "touch-down");

		var t = 0.0;
		for (var n = 1; n <= samples; n++)
		{
			t += sampleMs;
			var (px, py) = vertical ? (x, y - n * pxPerSample) : (x - n * pxPerSample, y);
			yield return new(t, i => i.InjectTouchInput(new[] { Touch(id, px, py, contact | InjectedInputPointerOptions.Update, (uint)sampleMs) }), "touch-move");
		}

		var (ex, ey) = vertical ? (x, y - samples * pxPerSample) : (x - samples * pxPerSample, y);
		if (holdBeforeReleaseMs > 0)
		{
			// A finger resting still keeps reporting its position.
			for (var held = sampleMs; held <= holdBeforeReleaseMs; held += sampleMs)
			{
				t += sampleMs;
				yield return new(t, i => i.InjectTouchInput(new[] { Touch(id, ex, ey, contact | InjectedInputPointerOptions.Update, (uint)sampleMs) }), "touch-hold");
			}
		}

		t += sampleMs;
		yield return new(t, i => i.InjectTouchInput(new[] { Touch(id, ex, ey, InjectedInputPointerOptions.FirstButton | InjectedInputPointerOptions.PointerUp, (uint)sampleMs) }), "touch-up");
	}

	private static InjectedInputTouchInfo Touch(uint id, int x, int y, InjectedInputPointerOptions options, uint offsetMs)
		=> new()
		{
			PointerInfo = new InjectedInputPointerInfo
			{
				PointerId = id,
				PerformanceCount = ++_touchFrame,
				PixelLocation = new InjectedInputPoint { PositionX = x, PositionY = y },
				PointerOptions = options,
				TimeOffsetInMilliseconds = offsetMs,
			},
		};

	private static void MoveMouseTo(InputInjector injector, Point target)
	{
		var current = injector.Mouse.Position;
		injector.InjectMouseInput(new[]
		{
			new InjectedInputMouseInfo
			{
				MouseOptions = InjectedInputMouseOptions.Move,
				DeltaX = (int)(target.X - current.X),
				DeltaY = (int)(target.Y - current.Y),
			}
		});
	}

	private static async Task PlayKeysAsync(Control sv, VirtualKey key, int count, double spacingMs, ScrollSmoothnessProbe probe, CancellationToken ct)
	{
		sv.Focus(FocusState.Programmatic);

		var steps = new List<Step>(count);
		for (var n = 0; n < count; n++)
		{
			// Raised on the scroller itself: a panel of rectangles has nothing focusable inside, and the key must not
			// be taken by a focused list item first (ListView turns PageDown into a selection move).
			steps.Add(new(n * spacingMs, _ =>
			{
				sv.SafeRaiseEvent(UIElement.KeyDownEvent, new KeyRoutedEventArgs(sv, key, VirtualKeyModifiers.None));
				sv.SafeRaiseEvent(UIElement.KeyUpEvent, new KeyRoutedEventArgs(sv, key, VirtualKeyModifiers.None));
			}, "key"));
		}

		await PlayAsync(steps, probe, ct);
	}

	/// <summary>
	/// Delivers each step on the UI thread as close as possible to its scheduled time. Off the browser a timing thread
	/// wakes the dispatcher; in the browser (no threads) a 1 ms delay loop polls, so late steps arrive bunched, much
	/// like coalesced browser pointer events.
	/// </summary>
	private static async Task PlayAsync(IEnumerable<Step> steps, ScrollSmoothnessProbe probe, CancellationToken ct)
	{
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Input injection is not available on this platform.");
		var dispatcher = DispatcherQueue.GetForCurrentThread();
		var start = Stopwatch.GetTimestamp();
		var pending = new Queue<Step>(steps);

		void RunDue()
		{
			var nowMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
			while (pending.Count > 0 && pending.Peek().AtMs <= nowMs)
			{
				var step = pending.Dequeue();
				probe.MarkInput(step.Kind);
				step.Inject(injector);
			}
		}

		if (OperatingSystem.IsBrowser())
		{
			while (pending.Count > 0)
			{
				ct.ThrowIfCancellationRequested();
				RunDue();
				await Task.Delay(1, ct);
			}

			return;
		}

		var done = new TaskCompletionSource();
		var thread = new Thread(() =>
		{
			var times = new List<double>();
			foreach (var s in pending)
			{
				times.Add(s.AtMs);
			}

			foreach (var at in times)
			{
				// Sleep coarsely, then spin the last stretch: Thread.Sleep alone is ~15 ms granular on Windows.
				while (Stopwatch.GetElapsedTime(start).TotalMilliseconds < at - 2 && !ct.IsCancellationRequested)
				{
					Thread.Sleep(1);
				}

				while (Stopwatch.GetElapsedTime(start).TotalMilliseconds < at && !ct.IsCancellationRequested)
				{
					Thread.SpinWait(50);
				}

				dispatcher.TryEnqueue(DispatcherQueuePriority.High, RunDue);
			}

			dispatcher.TryEnqueue(DispatcherQueuePriority.High, () =>
			{
				RunDue();
				done.TrySetResult();
			});
		})
		{
			IsBackground = true,
			Name = "ScrollSmoothness input timing",
		};
		thread.Start();

		await done.Task.WaitAsync(ct);
	}
}
#endif
