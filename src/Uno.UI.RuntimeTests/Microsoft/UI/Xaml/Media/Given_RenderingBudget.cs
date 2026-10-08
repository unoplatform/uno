#if __SKIA__
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

/// <summary>
/// Rendering budget tests: each scenario counts the work the renderer does (frames, damaged area, path booleans,
/// measures, objects left alive) and reports it through <see cref="RuntimeTestMetrics"/>, which CI turns into a pull
/// request comment. Counts, not timings: timings vary several-fold between CI runs, counts do not.
/// </summary>
/// <remarks>
/// The budgets are report-only for now. A test fails only when its scenario did not run (nothing scrolled, nothing
/// animated); a value over its budget is flagged in the report. See specs/rendering-budget-tests/spec.md.
/// </remarks>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
public class Given_RenderingBudget
{
	// A static page has nothing to repaint: anything above a couple of frames a second is a redraw loop.
	private const double IdleFramesPerSecondBudget = 2;

	// Each scroll step should cost one frame that repaints the viewport, with no per-frame path booleans. The 6.7
	// regression (#23984) ran an SKPath.Op per clipped visual on every scroll frame and damaged the whole window.
	private const double ScrollFramesPerStepBudget = 1.5;
	private const double ScrollDamagePerViewportBudget = 1.25;
	private const double ScrollPathOpsPerStepBudget = 1;
	private const double ScrollMeasuresPerStepBudget = 40;

	// An indeterminate ProgressRing animates every frame, but should repaint only its own bounds (#24792).
	private const double RingDamagePerRingAreaBudget = 2;
	private const double RingPathOpsPerFrameBudget = 1;

	// Nothing on screen animates once an animated subtree is removed (#25054).
	private const double RemovedAnimationFramesPerSecondBudget = 2;

	private const int ScrollSteps = 30;
	private const double ScrollStepPixels = 37; // Not a multiple of the item height, so steps land mid-item.

	[TestMethod]
	public async Task When_Idle_Then_No_Frames()
	{
		var page = new StackPanel
		{
			Spacing = 8,
			Children =
			{
				new TextBlock { Text = "Idle page" },
				new Button { Content = "Button" },
				new CheckBox { Content = "CheckBox", IsChecked = true },
				new Border { Width = 120, Height = 40, CornerRadius = new CornerRadius(8), Background = new SolidColorBrush(Colors.SteelBlue) },
			}
		};

		await UITestHelper.Load(page);
		await TestServices.WindowHelper.WaitForIdle();
		await Task.Delay(TimeSpan.FromMilliseconds(500)); // Loading settles in a frame or two after idle.

		using var probe = new FrameProbe(page);
		await Task.Delay(TimeSpan.FromSeconds(1));

		RuntimeTestMetrics.Record("idle.frames-per-second", probe.Frames, IdleFramesPerSecondBudget);
		RuntimeTestMetrics.Record("idle.full-window-frames", probe.FullWindowFrames, 0);
	}

	[TestMethod]
	public async Task When_Scrolling_List_Then_One_Frame_Per_Step()
	{
		var list = new ListView
		{
			Width = 300,
			Height = 300,
			ItemsSource = Enumerable.Range(0, 500).Select(i => $"Item {i}").ToArray(),
			ItemTemplate = CreateRoundedItemTemplate(),
		};
		var page = new Grid { Width = 600, Height = 500, Children = { list } };

		await UITestHelper.Load(page);
		var scroller = list.FindFirstDescendant<ScrollViewer>();
		Assert.IsNotNull(scroller, "The ListView has no ScrollViewer.");
		await TestServices.WindowHelper.WaitForIdle();

		using var probe = new FrameProbe(page);
		for (var step = 1; step <= ScrollSteps; step++)
		{
			scroller.ChangeView(null, step * ScrollStepPixels, null, disableAnimation: true);
			await probe.WaitForNextFrameAsync();
			await TestServices.WindowHelper.WaitForIdle();
		}

		Assert.IsTrue(scroller.VerticalOffset > 0, "The list did not scroll.");

		var viewportArea = list.ActualWidth * list.ActualHeight;
		RuntimeTestMetrics.Record("scroll.frames-per-step", (double)probe.Frames / ScrollSteps, ScrollFramesPerStepBudget);
		RuntimeTestMetrics.Record("scroll.damage-per-viewport", probe.MeanDamagedArea / viewportArea, ScrollDamagePerViewportBudget, "x");
		RuntimeTestMetrics.Record("scroll.full-window-frames", probe.FullWindowFrames, 0);
		RecordPathOps("scroll.path-ops-per-step", probe, ScrollSteps, ScrollPathOpsPerStepBudget);
		RuntimeTestMetrics.Record("scroll.measures-per-step", (double)probe.Measures / ScrollSteps, ScrollMeasuresPerStepBudget);
	}

	[TestMethod]
	public async Task When_ProgressRing_Active_Then_Damage_Stays_Within_Ring()
	{
		var ring = new ProgressRing { Width = 40, Height = 40, IsActive = true };
		var page = new Grid
		{
			Width = 600,
			Height = 400,
			Children =
			{
				new TextBlock { Text = "Loading...", Margin = new Thickness(0, 0, 0, 80), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
				ring,
			}
		};

		await UITestHelper.Load(page);
		await Task.Delay(TimeSpan.FromMilliseconds(300)); // Let the ring's animation start.

		using var probe = new FrameProbe(page);
		await Task.Delay(TimeSpan.FromSeconds(1));

		Assert.IsTrue(probe.Frames > 0, "The ProgressRing did not render any frame.");

		var ringArea = ring.ActualWidth * ring.ActualHeight;
		RuntimeTestMetrics.Record("progressring.frames-per-second", probe.Frames);
		RuntimeTestMetrics.Record("progressring.damage-per-ring-area", probe.MeanDamagedArea / ringArea, RingDamagePerRingAreaBudget, "x");
		RuntimeTestMetrics.Record("progressring.full-window-frames", probe.FullWindowFrames, 0);
		RecordPathOps("progressring.path-ops-per-frame", probe, probe.Frames, RingPathOpsPerFrameBudget);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25054")]
	public async Task When_Animated_Subtree_Removed_Then_Rendering_Stops()
	{
		var host = new Grid { Width = 200, Height = 200 };
		await UITestHelper.Load(host);

		var (subtree, removeSubtree) = AddSubtreeWithInfiniteAnimation(host);
		try
		{
			await Task.Delay(TimeSpan.FromMilliseconds(200));

			removeSubtree();
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(TimeSpan.FromMilliseconds(300));

			using (var probe = new FrameProbe(host))
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				RuntimeTestMetrics.Record("removed-animation.frames-per-second", probe.Frames, RemovedAnimationFramesPerSecondBudget);
			}

			await RecordAliveAsync("removed-animation", subtree);
		}
		finally
		{
			// While #25054 is open the detached animation runs forever and would keep every later test in this run
			// rendering at display rate, so it is stopped here whatever this test measured.
			(subtree.Single(o => o.Name == nameof(Visual)).Reference.Target as Visual)?.StopAnimation("Opacity");
		}
	}

	[TestMethod]
	public async Task When_Page_Removed_Then_Released()
	{
		// One-time static caches can keep a type's first instance alive (the first ToggleSwitch of a process stays
		// referenced), and whether this test creates that first instance depends on the tests that ran before it. A
		// warm-up page absorbs those, so the measured page reports only retention that grows with each page.
		await LoadAndUnloadPageAsync();
		var objects = await LoadAndUnloadPageAsync();

		await RecordAliveAsync("page-removal", objects);
	}

	[MethodImpl(MethodImplOptions.NoInlining)] // Keeps the page out of the caller's async state machine.
	private static async Task<TrackedObject[]> LoadAndUnloadPageAsync()
	{
		var (page, objects) = CreatePage();
		await UITestHelper.Load(page);
		await Task.Delay(TimeSpan.FromMilliseconds(300));

		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitForIdle();

		return objects;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (FrameworkElement page, TrackedObject[] objects) CreatePage()
	{
		var bitmap = new WriteableBitmap(256, 256);
		var image = new Image { Source = bitmap, Width = 128, Height = 128 };
		var list = new ListView
		{
			Height = 200,
			ItemsSource = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToArray(),
			ItemTemplate = CreateRoundedItemTemplate(),
		};
		var ring = new ProgressRing { Width = 32, Height = 32, IsActive = true };
		var textBox = new TextBox { Text = "Text", Width = 200 };
		var toggle = new ToggleSwitch { IsOn = true };
		var page = new StackPanel { Spacing = 8, Children = { image, list, ring, textBox, toggle } };

		var objects = new object[] { page, image, bitmap, list, ring, textBox, toggle };
		return (page, objects.Select(TrackedObject.Of).ToArray());
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (TrackedObject[] subtree, Action remove) AddSubtreeWithInfiniteAnimation(Panel host)
	{
		var child = new Border { Width = 40, Height = 40, Background = new SolidColorBrush(Colors.Orange) };
		var panel = new StackPanel { Children = { child } };
		host.Children.Add(panel);

		// The repro from #25054: an infinite composition animation on a visual below the root of the removed subtree.
		var visual = ElementCompositionPreview.GetElementVisual(child);
		var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(0f, 1f);
		animation.InsertKeyFrame(1f, 0.2f);
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.IterationBehavior = AnimationIterationBehavior.Forever;
		visual.StartAnimation("Opacity", animation);

		var panelRef = new WeakReference(panel);
		var subtree = new[] { new TrackedObject(nameof(StackPanel), panelRef), TrackedObject.Of(child), new TrackedObject(nameof(Visual), new WeakReference(visual)) };
		return (subtree, () => host.Children.Remove((UIElement)panelRef.Target!));
	}

	private static DataTemplate CreateRoundedItemTemplate()
		=> (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
			"""
			<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
				<Border Height="40" Margin="2" CornerRadius="6" Background="LightSteelBlue">
					<TextBlock Text="{Binding}" VerticalAlignment="Center" Margin="8,0" />
				</Border>
			</DataTemplate>
			""");

	private static void RecordPathOps(string name, FrameProbe probe, int divisor, double budget)
	{
#if UNO_DRAWING_SKIA
		RuntimeTestMetrics.Record(name, divisor == 0 ? probe.PathOps : (double)probe.PathOps / divisor, budget);
#endif
	}

	/// <summary>
	/// Collects, then records how many of <paramref name="objects"/> are still alive, and which: a leak report that
	/// names the type saves a debugging session.
	/// </summary>
	private static async Task RecordAliveAsync(string scenario, TrackedObject[] objects)
	{
		// The collection loop of Given_BindingMemoryLeak: some targets only clear weak references after a yield, and
		// dispatcher-deferred disposals need an idle pass.
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
		while (DateTime.UtcNow < deadline && objects.Any(o => o.Reference.IsAlive))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);
			await Task.Yield();
			await TestServices.WindowHelper.WaitForIdle();
		}

		var alive = objects.Where(o => o.Reference.IsAlive).ToArray();
		RuntimeTestMetrics.Record($"{scenario}.objects-alive", alive.Length, 0, $"/{objects.Length}");
		foreach (var o in alive)
		{
			RuntimeTestMetrics.Record($"{scenario}.alive.{o.Name}", 1, 0);
		}
	}

	private sealed record TrackedObject(string Name, WeakReference Reference)
	{
		public static TrackedObject Of(object o) => new(o.GetType().Name, new WeakReference(o));
	}

	/// <summary>
	/// Counts the frames rendered while it is alive, and what they damaged, through the internal FrameRendered event.
	/// Unlike CompositionTarget.Rendering, subscribing to it does not make the window render continuously.
	/// </summary>
	private sealed class FrameProbe : IDisposable
	{
		private readonly CompositionTarget _target;
		private readonly int _measuresAtStart = UIElement.LayoutMeasureCoreCount;
#if UNO_DRAWING_SKIA
		private readonly int _pathOpsAtStart = SkiaGeometrySource2D.PathOpCount;
#endif
		private TaskCompletionSource? _nextFrame;
		private double _damagedArea;

		public FrameProbe(UIElement element)
		{
			_target = (CompositionTarget)element.Visual.CompositionTarget!;
			_target.FrameRendered += OnFrameRendered;
		}

		public int Frames { get; private set; }

		/// <summary>Frames whose damage covered the whole window, the signature of a forced full repaint.</summary>
		public int FullWindowFrames { get; private set; }

		public double MeanDamagedArea => Frames == 0 ? 0 : _damagedArea / Frames;

		public int Measures => UIElement.LayoutMeasureCoreCount - _measuresAtStart;

#if UNO_DRAWING_SKIA
		public int PathOps => SkiaGeometrySource2D.PathOpCount - _pathOpsAtStart;
#endif

		public async Task WaitForNextFrameAsync()
		{
			var next = _nextFrame ??= new TaskCompletionSource();
			await Task.WhenAny(next.Task, Task.Delay(TimeSpan.FromSeconds(2)));
		}

		private void OnFrameRendered()
		{
			Frames++;

			var area = 0.0;
			foreach (var rect in _target.LastRecordedDamage ?? [])
			{
				area += rect.Width * rect.Height; // Damage rects never overlap (DamageRegion.Detach merges them).
			}
			_damagedArea += area;

			var frame = _target.LastRecordedFrameRect;
			if (area >= frame.Width * frame.Height * 0.99)
			{
				FullWindowFrames++;
			}

			var next = _nextFrame;
			_nextFrame = null;
			next?.TrySetResult();
		}

		public void Dispose() => _target.FrameRendered -= OnFrameRendered;
	}
}
#endif
