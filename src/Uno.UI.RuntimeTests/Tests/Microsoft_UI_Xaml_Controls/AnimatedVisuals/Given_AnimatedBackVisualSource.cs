#if __SKIA__
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.AnimatedVisuals;

[TestClass]
public class Given_AnimatedBackVisualSource
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_TryCreateAnimatedVisual_Returns_NonNull()
	{
		var source = new AnimatedBackVisualSource();
		var compositor = Compositor.GetSharedCompositor();

		var visual = source.TryCreateAnimatedVisual(compositor, out var diagnostics);

		Assert.IsNotNull(visual, "AnimatedBackVisualSource should return a non-null IAnimatedVisual");
		Assert.IsNotNull(visual.RootVisual, "RootVisual must be non-null");
		Assert.AreEqual(48f, visual.Size.X, "Width must be 48");
		Assert.AreEqual(48f, visual.Size.Y, "Height must be 48");
		Assert.IsTrue(visual.Duration.Ticks > 0, "Duration must be > 0");
	}

	[TestMethod]
	[RunsOnUIThread]
	public void When_RootVisual_Has_Children()
	{
		var source = new AnimatedBackVisualSource();
		var compositor = Compositor.GetSharedCompositor();

		var visual = source.TryCreateAnimatedVisual(compositor, out _);
		var root = visual.RootVisual as ContainerVisual;

		Assert.IsNotNull(root, "RootVisual should be a ContainerVisual");
		Assert.IsTrue(root.Children.Count > 0, "Root should have at least one child container");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Rendered_At_Progress_Zero()
	{
		var source = new AnimatedBackVisualSource();
		var compositor = Compositor.GetSharedCompositor();

		var visual = source.TryCreateAnimatedVisual(compositor, out _);

		// Wrap the animated visual into a host so we can render it.
		var host = compositor.CreateContainerVisual();
		host.Size = new Vector2(48, 48);
		host.Children.InsertAtTop(visual.RootVisual);

		// Force the Progress property to 0 (default already).
		visual.RootVisual.Properties.InsertScalar("Progress", 0f);

		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};
		ElementCompositionPreview.SetElementChildVisual(border, host);

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(border);

		// Sample the center of the icon — at progress 0, the back arrow path should colour
		// at least some of the central pixels.
		bool foundBlack = false;
		for (int x = 18; x <= 30 && !foundBlack; x++)
		{
			for (int y = 18; y <= 30 && !foundBlack; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlack = true;
				}
			}
		}

		Assert.IsTrue(foundBlack, "Expected the icon's black pixels to render in the central region at progress 0.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Hosted_In_AnimatedVisualPlayer()
	{
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = new AnimatedBackVisualSource(),
		};
		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "IsAnimatedVisualLoaded should be true after loading.");

		var screenshot = await UITestHelper.ScreenShot(border);

		bool foundBlack = false;
		for (int x = 18; x <= 30 && !foundBlack; x++)
		{
			for (int y = 18; y <= 30 && !foundBlack; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlack = true;
				}
			}
		}

		Assert.IsTrue(foundBlack, "Expected the icon's black pixels to render through AnimatedVisualPlayer.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Player_Smaller_Than_Natural_Size()
	{
		// Mirrors the sample's setup: 24x24 player, Stretch=Uniform, source's natural size is 48x48.
		var player = new AnimatedVisualPlayer
		{
			Width = 24,
			Height = 24,
			AutoPlay = false,
			Stretch = Stretch.Uniform,
			Source = new AnimatedBackVisualSource(),
		};
		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Player should be loaded.");
		Assert.IsTrue(player.ActualWidth >= 24 - 1 && player.ActualWidth <= 24 + 1, $"Expected ActualWidth ~24, got {player.ActualWidth}");

		var screenshot = await UITestHelper.ScreenShot(border);

		// In a 48x48 border, the 24x24 player is centered (default alignment) — that puts it
		// at (12,12)..(36,36). The back arrow lives in roughly the center.
		bool foundBlack = false;
		for (int x = 12; x <= 36 && !foundBlack; x++)
		{
			for (int y = 12; y <= 36 && !foundBlack; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlack = true;
				}
			}
		}

		Assert.IsTrue(foundBlack, "Expected back-arrow to render when player is smaller than the natural size.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Inside_Button()
	{
		// Reproduce the exact wrap shape used by the sample: a Button hosting an AnimatedVisualPlayer.
		var player = new AnimatedVisualPlayer
		{
			Width = 24,
			Height = 24,
			AutoPlay = false,
			Stretch = Stretch.Uniform,
			Source = new AnimatedBackVisualSource(),
		};
		var button = new Microsoft.UI.Xaml.Controls.Button
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			BorderThickness = new Thickness(0),
			Padding = new Thickness(0),
			Content = player,
		};

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Player inside Button should be loaded.");

		var screenshot = await UITestHelper.ScreenShot(button);
		bool foundBlack = false;
		for (int x = 0; x < (int)button.ActualWidth && !foundBlack; x++)
		{
			for (int y = 0; y < (int)button.ActualHeight && !foundBlack; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlack = true;
				}
			}
		}

		Assert.IsTrue(foundBlack, "Expected back-arrow to render when hosted inside a Button.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SetProgress_Updates_Visual()
	{
		// At progress 0 the back arrow shows the static layer; advancing to ~0.19 fires the
		// scale/offset animations of the press transition layer. Verify the icon repaints when
		// SetProgress is called by sampling pixels before and after.
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = new AnimatedBackVisualSource(),
		};
		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		var initial = await UITestHelper.ScreenShot(border);

		player.SetProgress(0.5);
		await TestServices.WindowHelper.WaitForIdle();

		var advanced = await UITestHelper.ScreenShot(border);

		// Sample a few pixels and ensure that the rendered output isn't identical to the initial
		// state — at least one pixel should differ between the two progress values because
		// shape animations have moved.
		bool changed = false;
		for (int x = 8; x < 40 && !changed; x++)
		{
			for (int y = 8; y < 40 && !changed; y++)
			{
				if (initial.GetPixel(x, y).ToString() != advanced.GetPixel(x, y).ToString())
				{
					changed = true;
				}
			}
		}

		Assert.IsTrue(changed, "SetProgress should move animated content; expected pixel-level differences between progress 0 and 0.5.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Foreground_Changes_Color_Updates()
	{
		// The Lottie-generated source binds the brush color to a Vector4 stored in the
		// CompositionPropertySet. Updating Foreground should re-evaluate the color brush via
		// the bound expression animation.
		var source = new AnimatedBackVisualSource();
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = source,
		};
		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Black foreground (default).
		var blackShot = await UITestHelper.ScreenShot(border);

		source.Foreground = Windows.UI.Color.FromArgb(0xFF, 0xE8, 0x10, 0x23); // strong red
		await TestServices.WindowHelper.WaitForIdle();

		var redShot = await UITestHelper.ScreenShot(border);

		// At progress 0 the back-arrow is rendered. The rendered foreground should now be red,
		// so somewhere in the icon area we should see a strongly-red pixel.
		bool foundRed = false;
		for (int x = 8; x < 40 && !foundRed; x++)
		{
			for (int y = 8; y < 40 && !foundRed; y++)
			{
				var pixel = redShot.GetPixel(x, y);
				if (pixel.R > 150 && pixel.G < 50 && pixel.B < 60)
				{
					foundRed = true;
				}
			}
		}

		Assert.IsTrue(foundRed, "Updating Foreground should change the rendered color of the icon.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ExpressionAnimation_References_PropertySet()
	{
		// End-to-end check: an ExpressionAnimation referencing `_.Progress` should re-evaluate
		// when the property set's Progress changes.
		var compositor = Compositor.GetSharedCompositor();
		var source = compositor.CreateContainerVisual();
		source.Properties.InsertScalar("Progress", 0f);

		var target = compositor.CreateContainerVisual();

		var expression = compositor.CreateExpressionAnimation("_.Progress");
		expression.SetReferenceParameter("_", source);

		// The animation animates the "Opacity" property of `target` based on `source.Progress`.
		target.StartAnimation("Opacity", expression);

		Assert.AreEqual(0f, target.Opacity, 0.001f, "Initial bound value should be 0.");

		source.Properties.InsertScalar("Progress", 0.75f);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.AreEqual(0.75f, target.Opacity, 0.001f, "Expression should re-evaluate after referenced property set changes.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PlayAsync_Renders_Final_State()
	{
		// Reproduce the sample's button-click behavior: play press transition then settle in
		// the press state, then play press-to-normal and settle. The icon must remain visible
		// through both transitions and at the final progress.
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = new AnimatedBackVisualSource(),
		};
		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Cap the wait at a few seconds — if the play hangs we want a clear failure rather
		// than the runtime-test runner timing out the whole suite.
		var play = player.PlayAsync(AnimatedBackVisualSource.M_NormalToPressed_Start, AnimatedBackVisualSource.M_NormalToPressed_End, false).AsTask();
		var completed = await Task.WhenAny(play, Task.Delay(TimeSpan.FromSeconds(5)));
		Assert.AreSame(play, completed, "Normal→Pressed PlayAsync did not complete within 5 seconds.");
		await TestServices.WindowHelper.WaitForIdle();

		var pressedShot = await UITestHelper.ScreenShot(border);
		bool foundBlackPressed = false;
		for (int x = 8; x < 40 && !foundBlackPressed; x++)
		{
			for (int y = 8; y < 40 && !foundBlackPressed; y++)
			{
				var pixel = pressedShot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlackPressed = true;
				}
			}
		}
		Assert.IsTrue(foundBlackPressed, "Icon should remain visible after Normal→Pressed PlayAsync.");

		var play2 = player.PlayAsync(AnimatedBackVisualSource.M_PressedToNormal_Start, AnimatedBackVisualSource.M_PressedToNormal_End, false).AsTask();
		var completed2 = await Task.WhenAny(play2, Task.Delay(TimeSpan.FromSeconds(5)));
		Assert.AreSame(play2, completed2, "Pressed→Normal PlayAsync did not complete within 5 seconds.");
		await TestServices.WindowHelper.WaitForIdle();

		var normalShot = await UITestHelper.ScreenShot(border);
		bool foundBlackNormal = false;
		for (int x = 8; x < 40 && !foundBlackNormal; x++)
		{
			for (int y = 8; y < 40 && !foundBlackNormal; y++)
			{
				var pixel = normalShot.GetPixel(x, y);
				if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100)
				{
					foundBlackNormal = true;
				}
			}
		}
		Assert.IsTrue(foundBlackNormal, "Icon should remain visible after Pressed→Normal PlayAsync.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContainerShape_Renders_Children()
	{
		// Verify our newly-added CompositionContainerShape actually paints its child shapes.
		var compositor = Compositor.GetSharedCompositor();
		var visual = compositor.CreateShapeVisual();
		visual.Size = new Vector2(48, 48);

		var container = compositor.CreateContainerShape();

		var rectGeometry = compositor.CreateRectangleGeometry();
		rectGeometry.Size = new Vector2(20, 20);

		var child = compositor.CreateSpriteShape(rectGeometry);
		child.Offset = new Vector2(14, 14);
		child.FillBrush = compositor.CreateColorBrush(Microsoft.UI.Colors.Black);

		container.Shapes.Add(child);
		visual.Shapes.Add(container);

		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};
		ElementCompositionPreview.SetElementChildVisual(border, visual);

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(border);
		var pixel = screenshot.GetPixel(24, 24);
		Assert.IsTrue(pixel.R < 100, $"Expected black pixel at (24,24) but got {pixel}.");
	}
}
#endif
