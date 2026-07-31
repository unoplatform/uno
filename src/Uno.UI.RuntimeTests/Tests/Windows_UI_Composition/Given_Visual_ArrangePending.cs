using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
using Windows.UI;
using Rectangle = System.Drawing.Rectangle;

#if __SKIA__
using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using SkiaSharp;
using Uno.UI.Composition;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Visual_ArrangePending
{
	// A child its parent measured but never arranged has no layout slot. Text is laid out from measure,
	// so its ink exists independently of any arrange, and a visual's content is not bounded by its Size —
	// several such children would stack at the parent's origin. WinUI paints nothing for an element its
	// parent didn't arrange, so this runs on the WinUI head too and pins that parity.
	// A TextBlock (not a Border) is required: a Border paints only inside its Size, which is 0x0 while
	// unarranged, so it would pass whether or not the suppression works.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_Child_Never_Arranged_Then_It_Does_Not_Paint()
	{
		var suppressed = new NeverArrangesChildrenPanel
		{
			Background = new SolidColorBrush(Colors.White),
			Children = { MakeInk() },
		};

		// Same content in a panel that arranges normally: proves the assertion below can fail, and that
		// the suppression is not simply hiding everything.
		var control = new StackPanel
		{
			Background = new SolidColorBrush(Colors.White),
			Children = { MakeInk() },
		};

		try
		{
			await UITestHelper.Load(new StackPanel { Children = { suppressed, control } });

			var controlShot = await UITestHelper.ScreenShot(control);
			ImageAssert.HasColorInRectangle(
				controlShot,
				new Rectangle(0, 0, (int)controlShot.Width, (int)controlShot.Height),
				Colors.Black,
				tolerance: 100);

			var suppressedShot = await UITestHelper.ScreenShot(suppressed);
			ImageAssert.DoesNotHaveColorInRectangle(
				suppressedShot,
				new Rectangle(0, 0, (int)suppressedShot.Width, (int)suppressedShot.Height),
				Colors.Black,
				tolerance: 100);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// The flag is set from the layout system, not through the composition property pipeline, so nothing
	// requests a frame on its behalf. The composition target is the only observation point for that: the
	// screenshot helpers render synchronously and would pass whether or not a frame was ever scheduled.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Render suppression before the first arrange is specific to the Skia compositor.")]
#endif
	public void When_ArrangePending_Changes_Then_A_New_Frame_Is_Requested()
	{
#if __SKIA__
		var compositor = Compositor.GetSharedCompositor();
		var visual = compositor.CreateSpriteVisual();
		var target = new FrameRequestRecorder();
		visual.CompositionTarget = target;

		// A regular composition property does request a frame; proves the recorder observes requests at all,
		// so the assertions below can't pass vacuously.
		visual.Opacity = 0.5f;
		Assert.AreNotEqual(0, target.NewFrameRequests, "A composition property change requested no frame, the test is not observing frame requests.");

		var baseline = target.NewFrameRequests;
		visual.IsArrangePending = true;
		Assert.AreNotEqual(baseline, target.NewFrameRequests, "Suppressing requested no frame, so the vacated region would keep the previous frame's pixels until an unrelated invalidation.");

		baseline = target.NewFrameRequests;
		visual.IsArrangePending = false;
		Assert.AreNotEqual(baseline, target.NewFrameRequests, "Unsuppressing requested no frame, so the element would stay unpainted until an unrelated invalidation.");
#endif
	}

	private static TextBlock MakeInk() => new()
	{
		Text = "██████",
		FontSize = 24,
		Foreground = new SolidColorBrush(Colors.Black),
	};

	private partial class NeverArrangesChildrenPanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (var child in Children)
			{
				child.Measure(availableSize);
			}

			return new Size(200, 60);
		}

		// Deliberately arranges nothing.
		protected override Size ArrangeOverride(Size finalSize) => finalSize;
	}

#if __SKIA__
	private sealed class FrameRequestRecorder : ICompositionTarget
	{
		public int NewFrameRequests { get; private set; }

		public double RasterizationScale => 1;

		public event EventHandler RasterizationScaleChanged
		{
			add { }
			remove { }
		}

		public void RequestNewFrame() => NewFrameRequests++;

		public void AddDamage(SKRect bounds) { }

		public void AddDamage(SKPath region) { }

		public void TryRedirectForManipulation(Windows.UI.Input.PointerPoint pointerPoint, InteractionTracker tracker) { }
	}
#endif
}
