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
public class Given_PointAnimationUsingKeyFrames_Parity
{
	private (EllipseGeometry ellipse, Path path) CreateEllipsePath(Point initialCenter)
	{
		var ellipse = new EllipseGeometry { Center = initialCenter, RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Width = 300,
			Height = 300,
		};
		return (ellipse, path);
	}

	[TestMethod]
	public async Task When_Linear_KeyFrames_Interpolate_XY()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)),
			Value = new Point(50, 50),
		});
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = new Point(100, 200),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 2.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(200.0, ellipse.Center.Y, 2.0,
			$"Final Y should be 200, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_Mixed_Discrete_And_Linear_Point()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new DiscretePointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = new Point(10, 20),
		});
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = new Point(100, 100),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 2.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 2.0,
			$"Final Y should be 100, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_RepeatCount_With_PointKeyFrames()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			RepeatBehavior = new RepeatBehavior(2),
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = new Point(50, 50),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		var sw = Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(50.0, ellipse.Center.X, 2.0,
			$"Final X should be 50, was {ellipse.Center.X}");
		Assert.IsTrue(sw.ElapsedMilliseconds >= 300,
			$"2 iterations of 200ms should take at least 300ms, took {sw.ElapsedMilliseconds}ms");
	}

	[TestMethod]
	public async Task When_AutoReverse_With_PointKeyFrames()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = new Point(100, 100),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		// AutoReverse should return to starting point (0,0)
		Assert.AreEqual(0.0, ellipse.Center.X, 2.0,
			$"After AutoReverse, X should return to 0, was {ellipse.Center.X}");
		Assert.AreEqual(0.0, ellipse.Center.Y, 2.0,
			$"After AutoReverse, Y should return to 0, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_FillBehavior_Stop_PointKeyFrames()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(25, 35));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			FillBehavior = FillBehavior.Stop,
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(200)),
			Value = new Point(100, 100),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
		await WindowHelper.WaitForIdle();

		// FillBehavior.Stop should return to local value
		Assert.AreEqual(25.0, ellipse.Center.X, 2.0,
			$"After FillBehavior.Stop, X should return to local value 25, was {ellipse.Center.X}");
		Assert.AreEqual(35.0, ellipse.Center.Y, 2.0,
			$"After FillBehavior.Stop, Y should return to local value 35, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_Pause_Resume_PointKeyFrames()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500)),
			Value = new Point(100, 200),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// Let it run a bit then pause
		await Task.Delay(150);
		storyboard.Pause();

		var frozenCenter = ellipse.Center;
		await Task.Delay(200);

		// Value should be frozen while paused
		Assert.AreEqual(frozenCenter.X, ellipse.Center.X, 2.0,
			$"X should be frozen at {frozenCenter.X} while paused, was {ellipse.Center.X}");
		Assert.AreEqual(frozenCenter.Y, ellipse.Center.Y, 2.0,
			$"Y should be frozen at {frozenCenter.Y} while paused, was {ellipse.Center.Y}");

		// Resume and wait for completion
		storyboard.Resume();
		await WindowHelper.WaitFor(
			() => Math.Abs(ellipse.Center.X - 100.0) < 3.0 && Math.Abs(ellipse.Center.Y - 200.0) < 3.0,
			timeoutMS: 5000,
			message: $"Center should reach (100,200) after resume, was ({ellipse.Center.X},{ellipse.Center.Y})");
	}

	[TestMethod]
	public async Task When_SplinePointKeyFrame_Applied()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new SplinePointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = new Point(100, 100),
			KeySpline = new KeySpline
			{
				ControlPoint1 = new Point(0.5, 0),
				ControlPoint2 = new Point(0.5, 1),
			},
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 2.0,
			$"Final X should be 100 after spline keyframe, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 2.0,
			$"Final Y should be 100 after spline keyframe, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_EasingPointKeyFrame_Applied()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new EasingPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300)),
			Value = new Point(100, 100),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 2.0,
			$"Final X should be 100 after easing keyframe, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 2.0,
			$"Final Y should be 100 after easing keyframe, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_Single_DiscretePointKeyFrame_At_Zero()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
		};
		animation.KeyFrames.Add(new DiscretePointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = new Point(42, 84),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		await WindowHelper.WaitFor(
			() => Math.Abs(ellipse.Center.X - 42.0) < 1.0 && Math.Abs(ellipse.Center.Y - 84.0) < 1.0,
			timeoutMS: 3000,
			message: $"Center should be (42,84) after discrete keyframe at time 0, was ({ellipse.Center.X},{ellipse.Center.Y})");
	}

	[TestMethod]
	public async Task When_PointKeyFrame_SpeedRatio()
	{
		var (ellipse, path) = CreateEllipsePath(new Point(0, 0));
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimationUsingKeyFrames
		{
			EnableDependentAnimation = true,
			SpeedRatio = 2.0,
		};
		animation.KeyFrames.Add(new LinearPointKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(400)),
			Value = new Point(100, 100),
		});
		Storyboard.SetTarget(animation, ellipse);
		Storyboard.SetTargetProperty(animation, nameof(EllipseGeometry.Center));

		var sw = Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(100.0, ellipse.Center.X, 2.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 2.0,
			$"Final Y should be 100, was {ellipse.Center.Y}");
		// SpeedRatio on child animation makes the child reach its end value faster,
		// but the Storyboard's duration is still based on the child's natural duration.
		Assert.IsTrue(sw.ElapsedMilliseconds < 600,
			$"SpeedRatio=2 should halve the 400ms duration, took {sw.ElapsedMilliseconds}ms");
	}
}
