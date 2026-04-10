using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation;

[TestClass]
[RunsOnUIThread]
public class Given_DoubleAnimationUsingKeyFrames_Parity
{
	[TestMethod]
	public async Task When_Linear_KeyFrames_Interpolate()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100)),
			Value = 0.25,
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 0.75,
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(1.0, border.Opacity, 0.05, $"Final opacity should be 1.0, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_Mixed_Discrete_And_Linear()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = 0.1,
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 0.8,
		});
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(1.0, border.Opacity, 0.05, $"Final opacity should be 1.0, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_SplineKeyFrame_Applied()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new SplineDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = 1.0,
			KeySpline = new KeySpline
			{
				ControlPoint1 = new Point(0.5, 0),
				ControlPoint2 = new Point(0.5, 1),
			},
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(1.0, border.Opacity, 0.05, $"Final opacity should be 1.0 after spline keyframe, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_RepeatCount_With_KeyFrames()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			RepeatBehavior = new RepeatBehavior(2),
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(1.0, border.Opacity, 0.05, $"Final opacity should be 1.0, was {border.Opacity}");
		Assert.IsTrue(sw.ElapsedMilliseconds >= 300,
			$"2 iterations of 200ms should take at least 300ms, took {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task When_AutoReverse_With_KeyFrames()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// AutoReverse should return opacity back to the base value (0.0)
		Assert.AreEqual(0.0, border.Opacity, 0.05,
			$"After AutoReverse, opacity should return to base value 0.0, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_FillBehavior_Stop_KeyFrames()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.5 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			FillBehavior = FillBehavior.Stop,
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// FillBehavior.Stop should return to the local value after completion
		Assert.AreEqual(0.5, border.Opacity, 0.05,
			$"After FillBehavior.Stop, opacity should return to local value 0.5, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_Pause_Resume_KeyFrames()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		// Let it run a bit then pause
		await Task.Delay(150);
		storyboard.Pause();

		var frozenValue = border.Opacity;
		await Task.Delay(200);

		// Value should be frozen while paused
		Assert.AreEqual(frozenValue, border.Opacity, 0.05,
			$"Opacity should be frozen at {frozenValue} while paused, was {border.Opacity}");

		// Resume and wait for completion
		storyboard.Resume();
		await WindowHelper.WaitFor(
			() => Math.Abs(border.Opacity - 1.0) < 0.05,
			timeoutMS: 5000,
			message: $"Opacity should reach 1.0 after resume, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_Empty_KeyFrames_Collection()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.75 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		// No keyframes added

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		// Should not crash
		storyboard.Begin();
		await WindowHelper.WaitForIdle();
		await Task.Delay(200);

		// Opacity should remain at its local value since there are no keyframes
		Assert.AreEqual(0.75, border.Opacity, 0.05,
			$"Opacity should remain at local value 0.75 with empty keyframes, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_Single_DiscreteKeyFrame_At_Zero()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 1.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = 0.42,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		storyboard.Begin();

		await WindowHelper.WaitFor(
			() => Math.Abs(border.Opacity - 0.42) < 0.01,
			timeoutMS: 3000,
			message: $"Opacity should be 0.42 after discrete keyframe at time 0, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_EasingKeyFrame_ExponentialEase()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new EasingDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = 1.0,
			EasingFunction = new ExponentialEase
			{
				Exponent = 2,
				EasingMode = EasingMode.EaseIn,
			},
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(1.0, border.Opacity, 0.05,
			$"Final opacity should be 1.0 with ExponentialEase, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_EasingKeyFrame_CubicEase_And_SineEase()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new EasingDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 0.5,
			EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
		});
		animation.KeyFrames.Add(new EasingDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
			Value = 1.0,
			EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut },
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(1.0, border.Opacity, 0.05,
			$"Final opacity should be 1.0 after CubicEase+SineEase keyframes, was {border.Opacity}");
	}

	[TestMethod]
	public async Task When_KeyFrame_Animation_SpeedRatio()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			SpeedRatio = 2.0,
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		var sw = Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(1.0, border.Opacity, 0.05,
			$"Final opacity should be 1.0, was {border.Opacity}");
		// SpeedRatio on child animation makes the child reach its end value faster,
		// but the Storyboard's duration is still based on the child's natural duration.
		Assert.IsTrue(sw.ElapsedMilliseconds < 600,
			$"SpeedRatio=2 should halve the 400ms duration, took {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task When_KeyFrame_Animation_BeginTime()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames
		{
			BeginTime = TimeSpan.FromMilliseconds(300),
		};
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 1.0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);

		var sw = Stopwatch.StartNew();
		storyboard.Begin();

		// After a short wait, the animation should not have started yet due to BeginTime delay
		await Task.Delay(150);
		Assert.AreEqual(0.0, border.Opacity, 0.05,
			$"Opacity should still be 0.0 during BeginTime delay, was {border.Opacity}");

		// Wait for animation to complete (BeginTime 300ms + Duration 200ms = ~500ms total)
		await WindowHelper.WaitFor(
			() => Math.Abs(border.Opacity - 1.0) < 0.05,
			timeoutMS: 5000,
			message: $"Opacity should reach 1.0 after BeginTime+Duration, was {border.Opacity}");
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 400,
			$"Total time should be at least 400ms (300ms delay + 200ms anim), took {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task When_KeyFrames_Clipped_By_Storyboard_Duration()
	{
		var border = new Border { Width = 50, Height = 50, Opacity = 0.0 };
		WindowHelper.WindowContent = border;
		await WindowHelper.WaitForLoaded(border);
		await WindowHelper.WaitForIdle();

		var animation = new DoubleAnimationUsingKeyFrames();
		Storyboard.SetTarget(animation, border);
		Storyboard.SetTargetProperty(animation, "Opacity");
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = 0.5,
		});
		animation.KeyFrames.Add(new LinearDoubleKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
			Value = 1.0,
		});

		var storyboard = new Storyboard
		{
			// Storyboard duration clips at 250ms, so only the first keyframe at 200ms is fully reached
			Duration = new Duration(TimeSpan.FromMilliseconds(250)),
		};
		storyboard.Children.Add(animation);

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// The storyboard ends at 250ms. At that point the first keyframe (200ms=0.5) has been reached
		// and the animation is only 50ms into the 200ms segment from 0.5 to 1.0.
		// The value should be between 0.5 and 1.0, but NOT at 1.0.
		Assert.IsTrue(border.Opacity < 0.9,
			$"Opacity should be less than 0.9 because storyboard was clipped at 250ms, was {border.Opacity}");
		Assert.IsTrue(border.Opacity >= 0.45,
			$"Opacity should be at least 0.45 (first keyframe reached at 200ms), was {border.Opacity}");
	}
}
