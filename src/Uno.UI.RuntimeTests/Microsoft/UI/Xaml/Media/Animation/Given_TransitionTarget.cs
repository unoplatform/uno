#if HAS_UNO
using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_TransitionTarget
{
	private const byte Tolerance = 5;

	[TestMethod]
	public async Task When_Opacity_Multiplies_Element_Opacity()
	{
		var (root, sut) = CreateSetup();
		sut.Opacity = 0.8;
		await UITestHelper.Load(root);

		sut.TransitionTarget = new TransitionTarget { Opacity = 0.5 };
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.4f, sut.Visual.Opacity, 0.001f);
		var screenshot = await UITestHelper.ScreenShot(root);
		// Red at 0.4 over white.
		ImageAssert.HasColorAt(screenshot, 50, 50, Color.FromArgb(255, 255, 153, 153), Tolerance);
	}

	[TestMethod]
	public async Task When_Opacity_Out_Of_Range_Is_Clamped()
	{
		var (root, sut) = CreateSetup();
		sut.Opacity = 0.5;
		await UITestHelper.Load(root);

		sut.TransitionTarget = new TransitionTarget { Opacity = 3 };
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.5f, sut.Visual.Opacity, 0.001f);
	}

	[TestMethod]
	public async Task When_TranslateX_Moves_Pixels_But_Not_Layout()
	{
		var (root, sut) = CreateSetup();
		await UITestHelper.Load(root);
		var offsetBefore = sut.ActualOffset;
		var slotBefore = LayoutInformation.GetLayoutSlot(sut);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;
		transitionTarget.CompositeTransform!.TranslateX = 50;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(offsetBefore, sut.ActualOffset);
		Assert.AreEqual(slotBefore, LayoutInformation.GetLayoutSlot(sut));

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 125, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 175, 50, Colors.White, Tolerance);
	}

	[TestMethod]
	public async Task When_TranslateX_Animated_By_Reference()
	{
		var (root, sut) = CreateSetup();
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;

		var animation = new DoubleAnimation { To = 50, Duration = new Duration(TimeSpan.Zero) };
		Storyboard.SetTarget(animation, transitionTarget.CompositeTransform);
		Storyboard.SetTargetProperty(animation, "TranslateX");
		var storyboard = new Storyboard { Children = { animation } };
		storyboard.Begin();
		await TestServices.WindowHelper.WaitFor(() => transitionTarget.CompositeTransform!.TranslateX == 50);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 125, 50, Colors.Red, Tolerance);

		storyboard.Stop();
	}

	[TestMethod]
	public async Task When_ScaleX_About_TransformOrigin()
	{
		var (root, sut) = CreateSetup();
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget { TransformOrigin = new Point(1, 0.5) };
		sut.TransitionTarget = transitionTarget;
		transitionTarget.CompositeTransform!.ScaleX = 0.5;
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.Red, Tolerance);
	}

	[TestMethod]
	public async Task When_Transform_Composes_After_RenderTransform()
	{
		var (root, sut) = CreateSetup();
		sut.RenderTransform = new TranslateTransform { X = 20 };
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;
		transitionTarget.CompositeTransform!.TranslateX = 30;
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 45, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 55, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 145, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 155, 50, Colors.White, Tolerance);
	}

	[TestMethod]
	public async Task When_ClipTransform_Clips_Sticky_And_HitTest()
	{
		// The canvas doesn't clip its children, so the overflowing child shows whether the element-bounds clip is active.
		var overflow = new Rectangle { Width = 50, Height = 50, Fill = new SolidColorBrush(Colors.Blue) };
		Canvas.SetLeft(overflow, 120);
		Canvas.SetTop(overflow, 25);
		var sut = new Canvas
		{
			Width = 100,
			Height = 100,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.Red),
			Children = { overflow },
		};
		var root = new Grid { Width = 200, Height = 100, Background = new SolidColorBrush(Colors.White), Children = { sut } };
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsFalse(transitionTarget.HasClipAnimation);
		Assert.IsNull(sut.Visual.TransitionClip);
		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 145, 50, Colors.Blue, Tolerance);
		var rootPosition = root.TransformToVisual(null).TransformPoint(default);
		Assert.AreSame(overflow, HitTest(new Point(rootPosition.X + 145, rootPosition.Y + 50), root));

		transitionTarget.ClipTransform!.TranslateX = -50;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(transitionTarget.HasClipAnimation);
		screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 145, 50, Colors.White, Tolerance);

		Assert.AreSame(sut, HitTest(new Point(rootPosition.X + 25, rootPosition.Y + 50), root));
		Assert.AreSame(root, HitTest(new Point(rootPosition.X + 75, rootPosition.Y + 50), root));

		transitionTarget.ClipTransform.TranslateX = 0;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(transitionTarget.HasClipAnimation);
		screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.Red, Tolerance);
		// Still clipped to the element bounds.
		ImageAssert.HasColorAt(screenshot, 145, 50, Colors.White, Tolerance);
		Assert.AreSame(sut, HitTest(new Point(rootPosition.X + 75, rootPosition.Y + 50), root));
		Assert.AreSame(root, HitTest(new Point(rootPosition.X + 145, rootPosition.Y + 50), root));
	}

	[TestMethod]
	public async Task When_ClipTransform_About_ClipTransformOrigin()
	{
		var (root, sut) = CreateSetup();
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget { ClipTransformOrigin = new Point(1, 0) };
		sut.TransitionTarget = transitionTarget;
		transitionTarget.ClipTransform!.ScaleX = 0.5;
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 50, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.Red, Tolerance);
	}

	[TestMethod]
	public async Task When_ClipTransform_Intersects_Explicit_Clip()
	{
		var (root, sut) = CreateSetup();
		sut.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 100, 40) };
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;
		transitionTarget.ClipTransform!.TranslateX = -50;
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 25, 20, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 75, 20, Colors.White, Tolerance);
		ImageAssert.HasColorAt(screenshot, 25, 70, Colors.White, Tolerance);
	}

	[TestMethod]
	public async Task When_RightToLeft_Transform_Applies_Before_Flip()
	{
		// The element itself carries the RTL flip, so the order of the TransitionTarget transform and the flip is observable.
		var (root, sut) = CreateSetup();
		sut.FlowDirection = FlowDirection.RightToLeft;
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget();
		sut.TransitionTarget = transitionTarget;
		transitionTarget.CompositeTransform!.TranslateX = 20;
		await TestServices.WindowHelper.WaitForIdle();

		// A positive translation is flipped with the element, so it moves visually left.
		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 10, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 70, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 90, 50, Colors.White, Tolerance);
	}

	[TestMethod]
	public async Task When_No_TransitionTarget_Unaffected()
	{
		var (root, sut) = CreateSetup();
		sut.Opacity = 0.5;
		await UITestHelper.Load(root);

		Assert.IsNull(sut.TransitionTarget);
		Assert.IsNull(sut._renderTransform);
		Assert.IsNull(sut.Visual.TransitionClip);
		Assert.AreEqual(0.5f, sut.Visual.Opacity, 0.001f);
	}

	[TestMethod]
	public async Task When_TransitionTarget_Removed_Restores_Element()
	{
		var (root, sut) = CreateSetup();
		await UITestHelper.Load(root);

		var transitionTarget = new TransitionTarget { Opacity = 0.5 };
		sut.TransitionTarget = transitionTarget;
		transitionTarget.CompositeTransform!.TranslateX = 50;
		transitionTarget.ClipTransform!.TranslateX = -50;
		await TestServices.WindowHelper.WaitForIdle();

		sut.TransitionTarget = null;
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNull(sut.Visual.TransitionClip);
		Assert.AreEqual(1f, sut.Visual.Opacity, 0.001f);
		var screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.Red, Tolerance);
		ImageAssert.HasColorAt(screenshot, 125, 50, Colors.White, Tolerance);

		// The detached target no longer drives the element.
		transitionTarget.CompositeTransform.TranslateX = 100;
		transitionTarget.Opacity = 0;
		await TestServices.WindowHelper.WaitForIdle();
		Assert.AreEqual(1f, sut.Visual.Opacity, 0.001f);
		screenshot = await UITestHelper.ScreenShot(root);
		ImageAssert.HasColorAt(screenshot, 75, 50, Colors.Red, Tolerance);
	}

	private static (Grid root, Border sut) CreateSetup()
	{
		var sut = new Border
		{
			Width = 100,
			Height = 100,
			HorizontalAlignment = HorizontalAlignment.Left,
			Background = new SolidColorBrush(Colors.Red),
		};
		var root = new Grid
		{
			Width = 200,
			Height = 100,
			Background = new SolidColorBrush(Colors.White),
			Children = { sut },
		};
		return (root, sut);
	}

	private static UIElement HitTest(Point point, UIElement root)
		=> VisualTreeHelper.HitTest(point, root.XamlRoot?.VisualTree.RootElement).element;
}
#endif
