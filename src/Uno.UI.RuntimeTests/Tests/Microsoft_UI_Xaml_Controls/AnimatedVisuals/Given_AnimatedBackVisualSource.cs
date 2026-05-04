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
	// Portable compositor accessor: WinUI doesn't expose Compositor.GetSharedCompositor; load a
	// throwaway Border into the visual tree and pull the compositor off its visual.
	private static async Task<Compositor> GetCompositorAsync()
	{
		var anchor = new Border { Width = 1, Height = 1 };
		await UITestHelper.Load(anchor);
		await TestServices.WindowHelper.WaitForIdle();
		return ElementCompositionPreview.GetElementVisual(anchor).Compositor;
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TryCreateAnimatedVisual_Returns_NonNull()
	{
		var source = new AnimatedBackVisualSource();
		var compositor = await GetCompositorAsync();

		var visual = source.TryCreateAnimatedVisual(compositor, out var diagnostics);

		Assert.IsNotNull(visual, "AnimatedBackVisualSource should return a non-null IAnimatedVisual");
		Assert.IsNotNull(visual.RootVisual, "RootVisual must be non-null");
		Assert.AreEqual(48f, visual.Size.X, "Width must be 48");
		Assert.AreEqual(48f, visual.Size.Y, "Height must be 48");
		Assert.IsTrue(visual.Duration.Ticks > 0, "Duration must be > 0");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RootVisual_Has_Children()
	{
		var source = new AnimatedBackVisualSource();
		var compositor = await GetCompositorAsync();

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
		var compositor = await GetCompositorAsync();

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

		// Use SetColorProperty (the IDL-exposed API) instead of the Uno-only Foreground setter
		// so this test stays portable to native WinUI.
		source.SetColorProperty("Foreground", Windows.UI.Color.FromArgb(0xFF, 0xE8, 0x10, 0x23)); // strong red
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_ExpressionAnimation_References_PropertySet()
	{
		// End-to-end check: an ExpressionAnimation referencing `_.Progress` should re-evaluate
		// when the property set's Progress changes.
		// Excluded on native WinUI: the compositor only re-evaluates expression animations for
		// visuals attached to a render tree, whereas Uno's expression engine re-evaluates
		// synchronously through the CompositionObject context system. The behavior the rest of
		// the suite cares about (bound progress driving the animated visual) is covered by the
		// integration tests above.
		var compositor = await GetCompositorAsync();
		var source = compositor.CreateContainerVisual();
		source.Properties.InsertScalar("Progress", 0f);

		var target = compositor.CreateContainerVisual();

		var expression = compositor.CreateExpressionAnimation("_.Progress");
		expression.SetReferenceParameter("_", source);

		// The animation animates the "Opacity" property of `target` based on `source.Progress`.
		target.StartAnimation("Opacity", expression);

		// Don't assert the initial bound value — WinUI applies bound expression values on the
		// next composition tick rather than synchronously inside StartAnimation. The test below
		// covers the actual behavior we care about: re-evaluation on source change.
		await TestServices.WindowHelper.WaitForIdle();
		await TestServices.WindowHelper.WaitForIdle();

		source.Properties.InsertScalar("Progress", 0.75f);
		await TestServices.WindowHelper.WaitForIdle();
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

		// Look up markers via the IAnimatedVisualSource2.Markers dictionary so the test stays
		// portable to native WinUI (the M_* constants are Uno-only conveniences).
		var markers = ((AnimatedBackVisualSource)player.Source).Markers;

		// Cap the wait at a few seconds — if the play hangs we want a clear failure rather
		// than the runtime-test runner timing out the whole suite.
		var play = player.PlayAsync(markers["NormalToPressed_Start"], markers["NormalToPressed_End"], false).AsTask();
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

		var play2 = player.PlayAsync(markers["PressedToNormal_Start"], markers["PressedToNormal_End"], false).AsTask();
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
	public async Task When_ChevronUpDownSmall_Strokes_With_Default_Thickness()
	{
		// Regression: AnimatedChevronUpDownSmallVisualSource (and any other Lottie-generated
		// source that strokes a path without explicitly setting StrokeThickness) relied on the
		// WinUI default of 1.0. Uno's CompositionSpriteShape defaulted to 0, so the chevron
		// rendered nothing. This test confirms the stroked chevron actually paints visible
		// pixels at progress 0 (NormalOff state).
		var player = new AnimatedVisualPlayer
		{
			Width = 96,
			Height = 96,
			AutoPlay = false,
			Source = new AnimatedChevronUpDownSmallVisualSource(),
		};
		var border = new Border
		{
			Width = 96,
			Height = 96,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = player,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Chevron source should load.");

		var screenshot = await UITestHelper.ScreenShot(border);

		bool foundStroke = false;
		for (int x = 0; x < 96 && !foundStroke; x++)
		{
			for (int y = 0; y < 96 && !foundStroke; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 200 && pixel.G < 200 && pixel.B < 200)
				{
					foundStroke = true;
				}
			}
		}

		Assert.IsTrue(foundStroke, "Chevron should render visible stroke pixels with the default StrokeThickness of 1.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AnimatedIcon_State_Changes_Plays_Animation()
	{
		// Regression coverage: AnimatedIcon previously hard-coded the source visual to null on
		// Uno (so the FallbackIconSource was always used), and m_progressPropertySet had no
		// owner so its keyframe animation never ticked. Both bugs combined to make the
		// CheckBox / Expander tick / chevron snap instantly to their destination state.
		var icon = new AnimatedIcon
		{
			Width = 48,
			Height = 48,
			Source = new AnimatedAcceptVisualSource(),
		};
		icon.SetValue(AnimatedIcon.StateProperty, "NormalOff");

		var border = new Border
		{
			Width = 48,
			Height = 48,
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
			Child = icon,
		};

		await UITestHelper.Load(border);
		await TestServices.WindowHelper.WaitForIdle();

		// Trigger a state change. The icon should drive Progress over time toward the
		// NormalOn marker rather than snapping there in a single tick.
		icon.SetValue(AnimatedIcon.StateProperty, "NormalOn");

		// One WaitForIdle gives the compositor a single tick — at this point the keyframe
		// animation should have started but should NOT have completed yet (durations are
		// usually 100-300ms for Lottie state transitions).
		await TestServices.WindowHelper.WaitForIdle();

		// Now wait long enough for the animation to actually finish.
		await Task.Delay(TimeSpan.FromMilliseconds(800));
		await TestServices.WindowHelper.WaitForIdle();

		var screenshot = await UITestHelper.ScreenShot(border);

		// At NormalOn the checkmark should be drawn — at least some non-white pixels in the
		// central region.
		bool foundMark = false;
		for (int x = 8; x < 40 && !foundMark; x++)
		{
			for (int y = 8; y < 40 && !foundMark; y++)
			{
				var pixel = screenshot.GetPixel(x, y);
				if (pixel.R < 200 || pixel.G < 200 || pixel.B < 200)
				{
					foundMark = true;
				}
			}
		}

		Assert.IsTrue(foundMark, "AnimatedIcon's NormalOn state should leave the checkmark drawn.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ScopedBatch_Waits_For_Animation()
	{
		// Regression: CompositionScopedBatch.End() previously fired Completed synchronously,
		// which broke AnimatedIcon's PlaySegment flow (the icon plays a keyframe animation
		// inside a scoped batch and counts on Completed firing when the animation actually
		// stops, not when End() returns). Symptom: clicking a CheckBox showed no draw-the-tick
		// animation because OnAnimationCompleted ran instantly and pinned Progress to 1.
		var compositor = await GetCompositorAsync();
		var anchor = new Border { Width = 1, Height = 1 };
		await UITestHelper.Load(anchor);
		await TestServices.WindowHelper.WaitForIdle();

		var visual = ElementCompositionPreview.GetElementVisual(anchor);
		visual.Properties.InsertScalar("Progress", 0f);

		var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
		var completedTcs = new TaskCompletionSource<object>();
		batch.Completed += (_, _) => completedTcs.TrySetResult(null);

		var animation = compositor.CreateScalarKeyFrameAnimation();
		animation.Duration = TimeSpan.FromMilliseconds(200);
		animation.InsertKeyFrame(0, 0f);
		animation.InsertKeyFrame(1, 1f);
		animation.IterationBehavior = AnimationIterationBehavior.Count;
		animation.IterationCount = 1;

		visual.Properties.StartAnimation("Progress", animation);
		batch.End();

		// Completed should NOT have fired yet — animation still running.
		Assert.IsFalse(completedTcs.Task.IsCompleted, "ScopedBatch.Completed must wait for the animation to finish.");

		var done = await Task.WhenAny(completedTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
		Assert.AreSame(completedTcs.Task, done, "ScopedBatch.Completed should fire within 5 seconds after the animation finishes.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Settings_Source_Plays_Without_Crash()
	{
		// Regression: AnimatedSettingsVisualSource animates RotationAngleInDegrees on a sprite
		// shape, which CompositionShape.SetAnimatableProperty didn't handle and was forwarded
		// to base SetAnimatableProperty -> TryUpdateFromProperties -> threw "Unable to set
		// property 'RotationAngleInDegrees'" the moment the icon was hosted.
		var player = new AnimatedVisualPlayer
		{
			Width = 48,
			Height = 48,
			AutoPlay = false,
			Source = new AnimatedSettingsVisualSource(),
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

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Settings source should load.");

		// Drive a progress change to fan out into RotationAngleInDegrees animations.
		player.SetProgress(0.5);
		await TestServices.WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PlayAsync_From_Button_Click()
	{
		// Mirrors the AnimatedBackVisualSourceSamplePage button: an AnimatedVisualPlayer hosted
		// inside a Button whose Click handler calls PlayAsync twice. Validates that the bound
		// progress chain still drives the icon when the player is nested in a control.
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
			Content = player,
			Padding = new Thickness(0),
			BorderThickness = new Thickness(0),
			Background = new SolidColorBrush(Microsoft.UI.Colors.White),
		};

		await UITestHelper.Load(button);
		await TestServices.WindowHelper.WaitForIdle();
		Assert.IsTrue(player.IsAnimatedVisualLoaded, "Player nested in Button should load.");

		var markers = ((AnimatedBackVisualSource)player.Source).Markers;

		var play1 = player.PlayAsync(markers["NormalToPressed_Start"], markers["NormalToPressed_End"], false).AsTask();
		var step1 = await Task.WhenAny(play1, Task.Delay(TimeSpan.FromSeconds(5)));
		Assert.AreSame(play1, step1, "Normal→Pressed PlayAsync should complete within 5 seconds when nested in a Button.");

		var play2 = player.PlayAsync(markers["PressedToNormal_Start"], markers["PressedToNormal_End"], false).AsTask();
		var step2 = await Task.WhenAny(play2, Task.Delay(TimeSpan.FromSeconds(5)));
		Assert.AreSame(play2, step2, "Pressed→Normal PlayAsync should complete within 5 seconds when nested in a Button.");

		await TestServices.WindowHelper.WaitForIdle();

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

		Assert.IsTrue(foundBlack, "Icon should remain visible after the simulated click sequence.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Source_Switched_While_Playing()
	{
		// Regression: switching to a different IAnimatedVisualSource while a play was in flight
		// crashed because the outgoing source's bound expression chain was driven through one
		// last propagation pass with cross-typed values.
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

		var markers = ((AnimatedBackVisualSource)player.Source).Markers;
		_ = player.PlayAsync(markers["NormalToPressed_Start"], markers["NormalToPressed_End"], false);

		// Swap to a different source mid-play. Must not throw.
		player.Source = new AnimatedAcceptVisualSource();
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsTrue(player.IsAnimatedVisualLoaded, "New source should be loaded after a mid-play switch.");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Source_Switched_Many_Times()
	{
		// Regression: VisualCollection.RemoveAll raised a Reset NotifyCollectionChangedEventArgs
		// with non-null changedItems, which violates the BCL contract and threw on the second
		// switch. This test exercises the swap path multiple times.
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

		IAnimatedVisualSource[] sources =
		{
			new AnimatedAcceptVisualSource(),
			new AnimatedFindVisualSource(),
			new AnimatedSettingsVisualSource(),
			new AnimatedBackVisualSource(),
		};

		foreach (var source in sources)
		{
			player.Source = source;
			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsTrue(player.IsAnimatedVisualLoaded, $"Switching to {source.GetType().Name} should load.");
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ContainerShape_Renders_Children()
	{
		// Verify our newly-added CompositionContainerShape actually paints its child shapes.
		var compositor = await GetCompositorAsync();
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
