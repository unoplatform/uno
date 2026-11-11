#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Hosting;

[TestClass]
[RunsOnUIThread]
public class Given_ElementCompositionPreview_ImplicitShowHide
{
	private const int AnimationMilliseconds = 1000;

	[TestMethod]
	public async Task When_Hide_Animation_And_Collapsed_Then_Rendered_Until_Complete()
	{
		var (host, target) = await LoadTarget();
		ElementCompositionPreview.SetImplicitHideAnimation(target, CreateHoldOpacityAnimation(target, from: 1, to: 1));

		try
		{
			target.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(during, 10, 10, Colors.Red, tolerance: 5);

			await WaitForColor(host, 10, 10, expectRed: false);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Hide_Animation_Starts_From_Current_Opacity_And_Collapsed()
	{
		// Like CommunityToolkit's OpacityAnimation To="0": no keyframe at 0, so the animation starts from the
		// visual's opacity at the time the element is collapsed, which must still be its visible opacity.
		var (host, target) = await LoadTarget();
		var compositor = ElementCompositionPreview.GetElementVisual(target).Compositor;
		var opacity = compositor.CreateScalarKeyFrameAnimation();
		opacity.InsertExpressionKeyFrame(0.99f, "this.StartingValue");
		opacity.InsertKeyFrame(1, 0);
		opacity.Duration = TimeSpan.FromMilliseconds(AnimationMilliseconds);
		opacity.Target = nameof(Visual.Opacity);
		var group = compositor.CreateAnimationGroup();
		group.Add(opacity);
		ElementCompositionPreview.SetImplicitHideAnimation(target, group);

		try
		{
			target.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(during, 10, 10, Colors.Red, tolerance: 5);

			await WaitForColor(host, 10, 10, expectRed: false);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Hide_Animation_And_Removed_Then_Unloaded_After_Complete()
	{
		var (host, target) = await LoadTarget();
		ElementCompositionPreview.SetImplicitHideAnimation(target, CreateHoldOpacityAnimation(target, from: 1, to: 1));

		var unloaded = false;
		target.Unloaded += (_, _) => unloaded = true;

		try
		{
			host.Children.Remove(target);
			await TestServices.WindowHelper.WaitForIdle();

			// The element stays in the live tree, rendered, while its Hide animation plays.
			Assert.IsFalse(unloaded);
			Assert.IsTrue(target.IsLoaded);
			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(during, 10, 10, Colors.Red, tolerance: 5);

			await TestServices.WindowHelper.WaitFor(() => unloaded, timeoutMS: 5000);
			await WaitForColor(host, 10, 10, expectRed: false);
			Assert.IsNull(VisualTreeHelper.GetParent(target));
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Removed_During_Hide_Animation_Then_Can_Be_Added_Back()
	{
		var (host, target) = await LoadTarget();
		ElementCompositionPreview.SetImplicitHideAnimation(target, CreateHoldOpacityAnimation(target, from: 1, to: 1));

		try
		{
			host.Children.Remove(target);
			await TestServices.WindowHelper.WaitForIdle();

			host.Children.Add(target);
			await TestServices.WindowHelper.WaitFor(() => target.IsLoaded && VisualTreeHelper.GetParent(target) == host);

			await TestServices.WindowHelper.WaitForIdle();
			var after = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(after, 10, 10, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Show_Animation_And_Added_Then_Plays()
	{
		var host = new Grid { Width = 100, Height = 100 };
		await UITestHelper.Load(host);

		var target = CreateTarget();
		ElementCompositionPreview.SetImplicitShowAnimation(target, CreateHoldOpacityAnimation(target, from: 0, to: 1));

		try
		{
			host.Children.Add(target);
			await TestServices.WindowHelper.WaitForLoaded(target);
			await TestServices.WindowHelper.WaitForIdle();

			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.DoesNotHaveColorAt(during, 10, 10, Colors.Red, tolerance: 5);

			await WaitForColor(host, 10, 10, expectRed: true);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Show_Animation_And_Ancestor_Made_Visible_Then_Plays()
	{
		var (host, target) = await LoadTarget();
		ElementCompositionPreview.SetImplicitShowAnimation(target, CreateHoldOpacityAnimation(target, from: 0, to: 1));

		try
		{
			host.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			host.Visibility = Visibility.Visible;
			await TestServices.WindowHelper.WaitForIdle();

			var during = await UITestHelper.ScreenShot(host);
			ImageAssert.DoesNotHaveColorAt(during, 10, 10, Colors.Red, tolerance: 5);

			await WaitForColor(host, 10, 10, expectRed: true);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Show_Animation_Cleared_Then_Does_Not_Play()
	{
		var (host, target) = await LoadTarget();
		ElementCompositionPreview.SetImplicitShowAnimation(target, CreateHoldOpacityAnimation(target, from: 0, to: 1));
		ElementCompositionPreview.SetImplicitShowAnimation(target, null);

		try
		{
			target.Visibility = Visibility.Collapsed;
			await TestServices.WindowHelper.WaitForIdle();

			target.Visibility = Visibility.Visible;
			await TestServices.WindowHelper.WaitForIdle();

			var after = await UITestHelper.ScreenShot(host);
			ImageAssert.HasColorAt(after, 10, 10, Colors.Red, tolerance: 5);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static Border CreateTarget()
		=> new() { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = new SolidColorBrush(Colors.Red) };

	private static async Task<(Grid Host, Border Target)> LoadTarget()
	{
		var target = CreateTarget();
		var host = new Grid { Width = 100, Height = 100, Children = { target } };
		await UITestHelper.Load(host);
		return (host, target);
	}

	// Holds 'from' for most of the animation, then ends at 'to': observable without depending on frame timing.
	private static CompositionAnimationGroup CreateHoldOpacityAnimation(UIElement element, float from, float to)
	{
		var compositor = ElementCompositionPreview.GetElementVisual(element).Compositor;

		var opacity = compositor.CreateScalarKeyFrameAnimation();
		opacity.InsertKeyFrame(0, from);
		opacity.InsertKeyFrame(0.99f, from);
		opacity.InsertKeyFrame(1, to);
		opacity.Duration = TimeSpan.FromMilliseconds(AnimationMilliseconds);
		opacity.Target = nameof(Visual.Opacity);

		var group = compositor.CreateAnimationGroup();
		group.Add(opacity);
		return group;
	}

	private static async Task WaitForColor(FrameworkElement host, int x, int y, bool expectRed)
	{
		var deadline = DateTime.UtcNow.AddMilliseconds(AnimationMilliseconds * 5);
		while (true)
		{
			await TestServices.WindowHelper.WaitForIdle();
			var screenshot = await UITestHelper.ScreenShot(host);
			var pixel = screenshot.GetPixel(x, y);
			var isRed = pixel.R > 250 && pixel.G < 5 && pixel.B < 5 && pixel.A > 250;
			if (isRed == expectRed)
			{
				return;
			}

			if (DateTime.UtcNow > deadline)
			{
				Assert.Fail($"Expected the pixel at ({x}, {y}) {(expectRed ? "to" : "not to")} be red once the animation completed.");
			}

			await Task.Delay(100);
		}
	}
}
