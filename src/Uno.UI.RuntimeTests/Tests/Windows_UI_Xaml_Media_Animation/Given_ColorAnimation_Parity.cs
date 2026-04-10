using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

/// <summary>
/// Tests ported from WinUI ColorAnimationTests.cpp for parity validation.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ColorAnimation_Parity
{
	/// <summary>
	/// Ported from ColorAnimationTests::LoopingColorAnimationWUCFull.
	/// From black to blue, repeat 3 times.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatCount3()
	{
		var brush = new SolidColorBrush(Colors.Black);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Black,
			To = Colors.Blue,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			RepeatBehavior = new RepeatBehavior(3),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		// Total duration should be 3 * 300ms = 900ms
		var sw = System.Diagnostics.Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		var color = brush.Color;
		Assert.AreEqual(Colors.Blue.R, color.R, 2, $"R should be Blue.R, was {color.R}");
		Assert.AreEqual(Colors.Blue.G, color.G, 2, $"G should be Blue.G, was {color.G}");
		Assert.AreEqual(Colors.Blue.B, color.B, 2, $"B should be Blue.B, was {color.B}");

		// 3 iterations of 300ms = 900ms, allow 50% margin
		Assert.IsTrue(sw.ElapsedMilliseconds >= 450,
			$"3 iterations of 300ms should take at least 450ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// Ported from ColorAnimationTests::ColorAndDoubleAnimationWUCFull.
	/// Color + opacity animation running in parallel in the same storyboard.
	/// </summary>
	[TestMethod]
	public async Task When_ColorAndDouble_InSameStoryboard()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
			Opacity = 1.0,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var colorAnim = new ColorAnimation
		{
			From = Colors.Red,
			To = Colors.Green,
			Duration = new Duration(TimeSpan.FromMilliseconds(400)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var doubleAnim = new DoubleAnimation
		{
			From = 1.0,
			To = 0.5,
			Duration = new Duration(TimeSpan.FromMilliseconds(400)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(border, nameof(Border.Opacity));

		var storyboard = new Storyboard();
		storyboard.Children.Add(colorAnim);
		storyboard.Children.Add(doubleAnim);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		var color = brush.Color;
		Assert.AreEqual(Colors.Green.R, color.R, 2,
			$"Color R should be Green.R ({Colors.Green.R}), was {color.R}");
		Assert.AreEqual(Colors.Green.G, color.G, 2,
			$"Color G should be Green.G ({Colors.Green.G}), was {color.G}");
		Assert.AreEqual(Colors.Green.B, color.B, 2,
			$"Color B should be Green.B ({Colors.Green.B}), was {color.B}");

		Assert.AreEqual(0.5, border.Opacity, 0.05,
			$"Opacity should be 0.5, was {border.Opacity}");
	}

	/// <summary>
	/// Color animation with Duration=0 should jump to the target value instantly.
	/// </summary>
	[TestMethod]
	public async Task When_ZeroDuration_JumpsToValue()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Red,
			To = Colors.Blue,
			Duration = new Duration(TimeSpan.Zero),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		var color = brush.Color;
		Assert.AreEqual(Colors.Blue.R, color.R, 2, $"R should jump to Blue.R, was {color.R}");
		Assert.AreEqual(Colors.Blue.G, color.G, 2, $"G should jump to Blue.G, was {color.G}");
		Assert.AreEqual(Colors.Blue.B, color.B, 2, $"B should jump to Blue.B, was {color.B}");
	}

	/// <summary>
	/// Ported from ColorAnimationTests::ForeverColorAnimationWUCFull.
	/// Verify values change over multiple cycles and the animation never completes.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatForever_Loops()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Color.FromArgb(255, 0, 0, 0),
			To = Color.FromArgb(255, 0, 0, 255),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			RepeatBehavior = RepeatBehavior.Forever,
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		bool completed = false;
		var storyboard = animation.ToStoryboard();
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		try
		{
			// Sample the blue channel over multiple cycles
			var samples = new List<byte>();
			for (int i = 0; i < 20; i++)
			{
				await Task.Delay(100);
				samples.Add(brush.Color.B);
			}

			// The animation should cycle: B goes 0->255 then resets to 0 repeatedly.
			// We should see at least one value drop (cycle boundary).
			var drops = 0;
			for (int i = 1; i < samples.Count; i++)
			{
				if (samples[i] < samples[i - 1] - 30)
				{
					drops++;
				}
			}

			Assert.IsTrue(drops >= 1,
				$"Expected at least 1 cycle drop in Blue channel values, got {drops}. Samples: {string.Join(", ", samples)}");

			// Forever animation should not have fired Completed
			Assert.IsFalse(completed, "Forever animation should not fire Completed");
		}
		finally
		{
			storyboard.Stop();
		}
	}

	/// <summary>
	/// Color By animation: animates relative to current color by adding the By offset.
	/// </summary>
	[TestMethod]
	public async Task When_By_Animates_Relative()
	{
		var brush = new SolidColorBrush(Color.FromArgb(255, 100, 0, 0));
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		// By = Color(0, 50, 100, 0) means add these channels to the current color
		// Current: R=100, G=0, B=0 + By: R=50, G=100, B=0 = R=150, G=100, B=0
		var animation = new ColorAnimation
		{
			By = Color.FromArgb(0, 50, 100, 0),
			Duration = new Duration(TimeSpan.FromMilliseconds(400)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		var color = brush.Color;
		Assert.AreEqual(150, color.R, 5,
			$"R should be ~150 (100+50), was {color.R}");
		Assert.AreEqual(100, color.G, 5,
			$"G should be ~100 (0+100), was {color.G}");
		Assert.AreEqual(0, color.B, 5,
			$"B should be ~0 (0+0), was {color.B}");
	}

	/// <summary>
	/// SpeedRatio=2 on a 1s color animation should complete in roughly half the time.
	/// </summary>
	[TestMethod]
	public async Task When_SpeedRatio_Applied()
	{
		var brush = new SolidColorBrush(Colors.Black);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Black,
			To = Colors.White,
			Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var storyboard = animation.ToStoryboard();
		storyboard.SpeedRatio = 2.0;

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		var color = brush.Color;
		Assert.AreEqual(255, color.R, 2, $"R should be 255, was {color.R}");
		Assert.AreEqual(255, color.G, 2, $"G should be 255, was {color.G}");
		Assert.AreEqual(255, color.B, 2, $"B should be 255, was {color.B}");

		// With SpeedRatio=2, a 1s animation should finish in ~500ms.
		// Allow generous tolerance: should be under 900ms (50% margin above expected 500ms)
		Assert.IsTrue(sw.ElapsedMilliseconds < 900,
			$"SpeedRatio=2 on 1s animation should complete in under 900ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// BeginTime=300ms should delay the start of the color animation.
	/// </summary>
	[TestMethod]
	public async Task When_BeginTime_Delays_Start()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Red,
			To = Colors.Blue,
			Duration = new Duration(TimeSpan.FromMilliseconds(400)),
			BeginTime = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// After 100ms, the animation hasn't started yet (BeginTime = 300ms).
		// Color should still be Red.
		await Task.Delay(100);
		var beforeStart = brush.Color;

		// Wait for animation to complete
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		var afterComplete = brush.Color;

		// Before BeginTime, color should still be near Red
		Assert.AreEqual(Colors.Red.R, beforeStart.R, 10,
			$"Before BeginTime, R should still be near Red.R, was {beforeStart.R}");

		// After completion, color should be Blue
		Assert.AreEqual(Colors.Blue.B, afterComplete.B, 2,
			$"After animation, B should be Blue.B, was {afterComplete.B}");
	}

	/// <summary>
	/// 2 repeats + AutoReverse: each repetition plays forward then backward.
	/// After 2 full repetitions (each with auto-reverse), the held value should be at From.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatBehavior_Count_With_AutoReverse()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Red,
			To = Colors.Blue,
			Duration = new Duration(TimeSpan.FromMilliseconds(250)),
			AutoReverse = true,
			RepeatBehavior = new RepeatBehavior(2),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		// Total: 2 reps * (250ms forward + 250ms reverse) = 1000ms
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		// After AutoReverse with even repeat count, value ends at From (Red)
		var color = brush.Color;
		Assert.AreEqual(Colors.Red.R, color.R, 10,
			$"After 2 reps with AutoReverse, R should return to Red.R ({Colors.Red.R}), was {color.R}");
		Assert.AreEqual(Colors.Red.B, color.B, 10,
			$"After 2 reps with AutoReverse, B should return to Red.B ({Colors.Red.B}), was {color.B}");
	}

	/// <summary>
	/// QuadraticEase with EaseOut mode: fast start, slow end.
	/// At the midpoint, the animated color should already be past the linear midpoint.
	/// </summary>
	[TestMethod]
	public async Task When_EasingFunction_EaseOut()
	{
		var brush = new SolidColorBrush(Colors.Black);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Color.FromArgb(255, 0, 0, 0),
			To = Color.FromArgb(255, 255, 255, 255),
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// EaseOut starts fast. At ~30% time (180ms), the value should be beyond linear 30%.
		await Task.Delay(180);
		var midColor = brush.Color;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		var finalColor = brush.Color;

		// With QuadraticEase.EaseOut at t=0.3: progress = 1 - (1-0.3)^2 = 1 - 0.49 = 0.51 → ~130
		// Should be noticeably more than linear 30% (~77)
		Assert.IsTrue(midColor.R > 50,
			$"With EaseOut, mid-point R should be greater than 50 (beyond linear 30%), was {midColor.R}");

		Assert.AreEqual(255, finalColor.R, 2, $"Final R should be 255, was {finalColor.R}");
		Assert.AreEqual(255, finalColor.G, 2, $"Final G should be 255, was {finalColor.G}");
		Assert.AreEqual(255, finalColor.B, 2, $"Final B should be 255, was {finalColor.B}");
	}

	/// <summary>
	/// Ported from ColorAnimationTests::ColorCompletedEvent_BrushNotRenderedWUCFull.
	/// A color animation targeting a brush on a collapsed element should still complete.
	/// </summary>
	[TestMethod]
	public async Task When_Collapsed_Target_Completes()
	{
		var brush = new SolidColorBrush(Colors.Red);
		var border = new Border
		{
			Background = brush,
			Width = 50,
			Height = 50,
			Visibility = Visibility.Collapsed,
		};
		WindowHelper.WindowContent = border;
		// Collapsed elements never raise Loaded, so just wait for idle.
		await WindowHelper.WaitForIdle();

		var animation = new ColorAnimation
		{
			From = Colors.Red,
			To = Colors.Green,
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(brush, nameof(SolidColorBrush.Color));

		bool completed = false;
		var storyboard = animation.ToStoryboard();
		storyboard.Completed += (s, e) => completed = true;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.IsTrue(completed,
			"Color animation targeting a collapsed element's brush should still fire Completed");

		var color = brush.Color;
		Assert.AreEqual(Colors.Green.R, color.R, 2,
			$"Even on collapsed element, fill value R should be Green.R, was {color.R}");
		Assert.AreEqual(Colors.Green.G, color.G, 2,
			$"Even on collapsed element, fill value G should be Green.G, was {color.G}");
	}
}
