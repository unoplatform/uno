using System;
using System.Collections.Generic;
using System.Linq;
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
public class Given_PointAnimation
{
	[TestMethod]
	public async Task When_FromTo_AnimatesPoint()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(10, 20),
			To = new Point(100, 200),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		var center = ellipse.Center;
		Assert.AreEqual(100.0, center.X, 1.0, $"X should be 100, was {center.X}");
		Assert.AreEqual(200.0, center.Y, 1.0, $"Y should be 200, was {center.Y}");
	}

	[TestMethod]
	public async Task When_ToOnly_AnimatesFromCurrentValue()
	{
		var ellipse = new EllipseGeometry { Center = new Point(30, 40), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0, $"X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0, $"Y should be 100, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_By_AnimatesRelativeToCurrentValue()
	{
		var ellipse = new EllipseGeometry { Center = new Point(20, 30), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Green),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			By = new Point(50, 70),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(70.0, ellipse.Center.X, 1.0, $"X should be 20+50=70, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0, $"Y should be 30+70=100, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_AutoReverse_ReturnsToFrom()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Purple),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(10, 20),
			To = new Point(100, 200),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			AutoReverse = true,
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		// After AutoReverse with HoldEnd, value returns to the From value
		Assert.AreEqual(10.0, ellipse.Center.X, 1.0, $"X should return to From (10), was {ellipse.Center.X}");
		Assert.AreEqual(20.0, ellipse.Center.Y, 1.0, $"Y should return to From (20), was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_Default_Duration_Is_One_Second()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Orange),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(0, 0),
			To = new Point(100, 100),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		sw.Stop();

		Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
			$"Default 1s animation should take at least 800ms, took {sw.ElapsedMilliseconds}ms");
		Assert.AreEqual(100.0, ellipse.Center.X, 1.0, $"X should be 100, was {ellipse.Center.X}");
	}

	[TestMethod]
	public async Task When_FillBehavior_Stop_ClearsValue()
	{
		var ellipse = new EllipseGeometry { Center = new Point(25, 35), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Yellow),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			FillBehavior = FillBehavior.Stop,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		await WindowHelper.WaitForIdle();

		// After FillBehavior.Stop, the value should return to the local value
		Assert.AreEqual(25.0, ellipse.Center.X, 1.0,
			$"After FillBehavior.Stop, X should return to local value (25), was {ellipse.Center.X}");
		Assert.AreEqual(35.0, ellipse.Center.Y, 1.0,
			$"After FillBehavior.Stop, Y should return to local value (35), was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_EasingFunction_Applied()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Cyan),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(0, 0),
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// EaseIn starts slow, so at ~30% time the value should be less than 30% of the way
		await Task.Delay(150);
		var midPoint = ellipse.Center;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

		// With QuadraticEase.EaseIn, at 30% time, progress = 0.3^2 = 0.09 (9%)
		// So value should be noticeably less than 30
		Assert.IsTrue(midPoint.X < 30.0,
			$"With EaseIn, mid-point X should be less than 30, was {midPoint.X}");
		Assert.AreEqual(100.0, ellipse.Center.X, 1.0, $"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0, $"Final Y should be 100, was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_Stop_While_Filling_ClearsValue()
	{
		var ellipse = new EllipseGeometry { Center = new Point(15, 25), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Brown),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(80, 90),
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(80.0, ellipse.Center.X, 1.0, $"Fill X should be 80, was {ellipse.Center.X}");

		storyboard.Stop();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(15.0, ellipse.Center.X, 1.0,
			$"After Stop, X should return to local value (15), was {ellipse.Center.X}");
		Assert.AreEqual(25.0, ellipse.Center.Y, 1.0,
			$"After Stop, Y should return to local value (25), was {ellipse.Center.Y}");
	}

	[TestMethod]
	public async Task When_RepeatBehavior_Count()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Pink),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(0, 0),
			To = new Point(50, 50),
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			RepeatBehavior = new RepeatBehavior(3),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		sw.Stop();

		Assert.AreEqual(50.0, ellipse.Center.X, 1.0, $"Final X should be 50, was {ellipse.Center.X}");
		Assert.IsTrue(sw.ElapsedMilliseconds >= 550,
			$"3 iterations of 200ms should take ~600ms, took {sw.ElapsedMilliseconds}ms");
	}
}
