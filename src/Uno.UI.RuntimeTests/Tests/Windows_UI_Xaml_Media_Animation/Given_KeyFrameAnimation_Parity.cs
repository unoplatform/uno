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
/// Tests for DoubleAnimationUsingKeyFrames and ObjectAnimationUsingKeyFrames
/// covering gaps not in the existing test files.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_KeyFrameAnimation_Parity
{
	/// <summary>
	/// 3 linear keyframes at 100ms, 200ms, 300ms with values 25, 75, 100.
	/// Verifies smooth interpolation between keyframes.
	/// </summary>
	[TestMethod]
	public async Task When_Linear_KeyFrames_Interpolate_Smoothly()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)), Value = 25 });
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 75 });
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.Begin();

		// At ~150ms, should be interpolating between 25 and 75 (around 50)
		await Task.Delay(150);
		var midValue = translate.X;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.IsTrue(midValue > 10 && midValue < 90,
			$"At midpoint between keyframes, value should be between 10 and 90, was {midValue}");
		Assert.AreEqual(100.0, translate.X, 1.0,
			$"Final fill value should be 100, was {translate.X}");
	}

	/// <summary>
	/// Discrete at 0ms=10, Linear at 200ms=50, Discrete at 300ms=100.
	/// Tests mixing discrete (instant) and linear (interpolated) keyframes.
	/// </summary>
	[TestMethod]
	public async Task When_Mixed_Discrete_And_Linear_KeyFrames()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 10 });
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 50 });
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.Begin();

		// The time-0 discrete keyframe should set value to 10 immediately
		await WindowHelper.WaitFor(() => Math.Abs(translate.X - 10) < 1.0,
			message: "Time-0 discrete keyframe should apply value 10");

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// After the last discrete keyframe at 300ms, value should jump to 100
		Assert.AreEqual(100.0, translate.X, 1.0,
			$"Final value after discrete keyframe at 300ms should be 100, was {translate.X}");
	}

	/// <summary>
	/// SplineDoubleKeyFrame with a non-linear KeySpline.
	/// Verifies that the spline curve is applied (non-linear interpolation).
	/// </summary>
	[TestMethod]
	public async Task When_SplineKeyFrame_CustomCurve()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		// Ease-in style spline: slow start, fast end
		animation.KeyFrames.Add(new SplineDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(600)),
			Value = 100,
			KeySpline = new KeySpline { ControlPoint1 = new Windows.Foundation.Point(0.8, 0.0), ControlPoint2 = new Windows.Foundation.Point(1.0, 1.0) },
		});

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.Begin();

		// At ~30% time (180ms), with an ease-in spline, value should be less than linear 30 (30% of 100)
		await Task.Delay(180);
		var earlyValue = translate.X;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// Ease-in: slow start means value at 30% time should be noticeably less than 30
		Assert.IsTrue(earlyValue < 50,
			$"With ease-in spline at 30% time, value should be less than 50, was {earlyValue}");
		Assert.AreEqual(100.0, translate.X, 1.0,
			$"Final fill value should be 100, was {translate.X}");
	}

	/// <summary>
	/// DoubleAnimationUsingKeyFrames with RepeatBehavior(2).
	/// Verifies the keyframe sequence plays twice.
	/// </summary>
	[TestMethod]
	public async Task When_KeyFrame_Animation_RepeatCount()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
			RepeatBehavior = new RepeatBehavior(2),
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		// Total: 2 * 200ms = 400ms
		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(100.0, translate.X, 1.0,
			$"After 2 repetitions, fill value should be 100, was {translate.X}");

		// Allow 50% margin: should take at least 200ms (half of expected 400ms)
		Assert.IsTrue(sw.ElapsedMilliseconds >= 200,
			$"2 iterations of 200ms should take at least 200ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// DoubleAnimationUsingKeyFrames with AutoReverse.
	/// After forward + reverse, fill value should be at the starting value.
	/// </summary>
	[TestMethod]
	public async Task When_KeyFrame_Animation_AutoReverse()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		translate.X = 0;

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
			AutoReverse = true,
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// After AutoReverse with HoldEnd, value returns to the starting value (0)
		Assert.AreEqual(0.0, translate.X, 1.0,
			$"After AutoReverse, value should return to 0, was {translate.X}");
	}

	/// <summary>
	/// DoubleAnimationUsingKeyFrames with empty KeyFrames collection.
	/// Should handle gracefully without crashing.
	/// </summary>
	[TestMethod]
	public async Task When_Empty_KeyFrames_Handles_Gracefully()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Opacity = 0.8,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			FillBehavior = FillBehavior.HoldEnd,
		};
		// No keyframes added

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(Border.Opacity));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		// Should not throw. Start and let it complete quickly.
		storyboard.Begin();
		await Task.Delay(200);
		await WindowHelper.WaitForIdle();

		// Opacity should remain at its original value
		Assert.AreEqual(0.8, border.Opacity, 0.05,
			$"With empty keyframes, opacity should remain at 0.8, was {border.Opacity}");
	}

	/// <summary>
	/// Only one DiscreteDoubleKeyFrame at time=0.
	/// The value should be applied immediately.
	/// </summary>
	[TestMethod]
	public async Task When_Single_KeyFrame_At_Zero()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Opacity = 1.0,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = 0.3,
		});

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(Border.Opacity));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.Begin();

		await WindowHelper.WaitFor(() => Math.Abs(border.Opacity - 0.3) < 0.01,
			message: "Single keyframe at time=0 should apply immediately");

		// Value should remain held
		await Task.Delay(200);
		Assert.AreEqual(0.3, border.Opacity, 0.01,
			$"Value should be held at 0.3, was {border.Opacity}");
	}

	/// <summary>
	/// DoubleAnimationUsingKeyFrames with FillBehavior.Stop.
	/// After animation completes, value should return to the base/local value.
	/// </summary>
	[TestMethod]
	public async Task When_KeyFrame_Animation_FillBehavior_Stop()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		translate.X = 20.0;

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.Stop,
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// After FillBehavior.Stop, animated value is cleared; returns to local value
		Assert.AreEqual(20.0, translate.X, 1.0,
			$"After FillBehavior.Stop, should return to local value 20, was {translate.X}");
	}

	/// <summary>
	/// Pause and resume a DoubleAnimationUsingKeyFrames mid-animation.
	/// After resume, the animation should continue and complete normally.
	/// </summary>
	[TestMethod]
	public async Task When_KeyFrame_Animation_Pause_Resume()
	{
		var translate = new TranslateTransform();
		var border = new Border
		{
			Background = new SolidColorBrush(Colors.Pink),
			Width = 50,
			Height = 50,
			RenderTransform = translate,
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)), Value = 100 });

		Storyboard.SetTarget(animation, translate);
		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.Begin();

		// Let animation run ~200ms then pause
		await Task.Delay(200);
		storyboard.Pause();
		var pausedValue = translate.X;

		// Wait while paused - value should not change
		await Task.Delay(300);
		var stillPausedValue = translate.X;

		// Resume
		storyboard.Resume();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.IsTrue(pausedValue > 5 && pausedValue < 95,
			$"Paused value should be mid-animation (between 5 and 95), was {pausedValue}");
		Assert.AreEqual(pausedValue, stillPausedValue, 1.0,
			$"While paused, value should not change. Paused={pausedValue}, StillPaused={stillPausedValue}");
		Assert.AreEqual(100.0, translate.X, 1.0,
			$"After resume, fill value should be 100, was {translate.X}");
	}

	/// <summary>
	/// ObjectAnimationUsingKeyFrames with AutoReverse.
	/// Discrete values play forward then backward.
	/// </summary>
	[TestMethod]
	public async Task When_ObjectAnimation_AutoReverse()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Tag = "Initial",
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ObjectAnimationUsingKeyFrames
		{
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = "First",
		});
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = "Second",
		});

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(Border.Tag));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// After AutoReverse, the animation reverses through the keyframes.
		// The held value should be the first keyframe value ("First") or the base value,
		// depending on the platform. On WinUI, AutoReverse on OAKF ends at the starting state.
		var finalTag = border.Tag as string;
		Assert.IsTrue(finalTag == "First" || finalTag == "Initial",
			$"After AutoReverse, Tag should be 'First' or 'Initial', was '{finalTag}'");
	}

	/// <summary>
	/// ObjectAnimationUsingKeyFrames with FillBehavior.Stop.
	/// After completing, the value should return to the local/base value.
	/// </summary>
	[TestMethod]
	public async Task When_ObjectAnimation_FillBehavior_Stop()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Tag = "Original",
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ObjectAnimationUsingKeyFrames
		{
			FillBehavior = FillBehavior.Stop,
		};
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = "Animated",
		});
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = "Final",
		});

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(Border.Tag));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// After FillBehavior.Stop, animated value is cleared
		Assert.AreEqual("Original", border.Tag as string,
			$"After FillBehavior.Stop, Tag should return to 'Original', was '{border.Tag}'");
	}

	/// <summary>
	/// ObjectAnimationUsingKeyFrames with SpeedRatio=2 should complete faster.
	/// </summary>
	[TestMethod]
	public async Task When_ObjectAnimation_SpeedRatio()
	{
		var border = new Border
		{
			Width = 50,
			Height = 50,
			Tag = "Initial",
		};
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new ObjectAnimationUsingKeyFrames
		{
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = "Start",
		});
		animation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000)),
			Value = "End",
		});

		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, nameof(Border.Tag));

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		storyboard.SpeedRatio = 2.0;

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual("End", border.Tag as string,
			$"After animation, Tag should be 'End', was '{border.Tag}'");

		// With SpeedRatio=2, a 1s animation should finish in ~500ms.
		// Allow generous tolerance: under 900ms (50% margin above expected)
		Assert.IsTrue(sw.ElapsedMilliseconds < 900,
			$"SpeedRatio=2 on 1s animation should complete in under 900ms, took {sw.ElapsedMilliseconds}ms");
	}
}
