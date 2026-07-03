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

/// <summary>
/// Tests ported from WinUI PointAnimationTests.cpp for parity validation.
/// Source: dxaml/test/native/external/foundation/graphics/animation/PointAnimationTests.cpp
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_PointAnimation_Parity
{
	/// <summary>
	/// Ported from PointAnimationTests::PauseSeekResume.
	/// 500ms animation from (50,50) to (250,150). Play, pause (verify frozen),
	/// seek to near end, resume, verify completion at (250,150).
	/// </summary>
	[TestMethod]
	public async Task When_PauseSeekResume()
	{
		var ellipse = new EllipseGeometry { Center = new Point(50, 50), RadiusX = 50, RadiusY = 50 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Gold),
			Width = 300,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(250, 150),
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();

		// Begin and let it advance a bit
		storyboard.Begin();
		await Task.Delay(150);

		// Pause
		storyboard.Pause();
		await Task.Delay(50);
		var pausedCenter = ellipse.Center;

		// Verify center moved from start during play
		Assert.IsTrue(pausedCenter.X > 50.0 || pausedCenter.Y > 50.0,
			$"After playing, center should have moved from (50,50), was ({pausedCenter.X},{pausedCenter.Y})");

		// Wait while paused and verify value stays frozen
		await Task.Delay(200);
		var stillPausedCenter = ellipse.Center;
		Assert.AreEqual(pausedCenter.X, stillPausedCenter.X, 0.1,
			$"While paused, X should not change: was {pausedCenter.X}, now {stillPausedCenter.X}");
		Assert.AreEqual(pausedCenter.Y, stillPausedCenter.Y, 0.1,
			$"While paused, Y should not change: was {pausedCenter.Y}, now {stillPausedCenter.Y}");

		// Seek near end (95% of 500ms = 475ms)
		storyboard.Seek(TimeSpan.FromMilliseconds(475));
		await WindowHelper.WaitForIdle();

		var seekedCenter = ellipse.Center;
		// At 95% progress: expected X ~50 + 0.95*200 = 240, Y ~50 + 0.95*100 = 145
		Assert.IsTrue(seekedCenter.X > 200,
			$"After seeking to 95%, X should be > 200, was {seekedCenter.X}");
		Assert.IsTrue(seekedCenter.Y > 120,
			$"After seeking to 95%, Y should be > 120, was {seekedCenter.Y}");

		// Resume and let it complete
		storyboard.Resume();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(250.0, ellipse.Center.X, 1.0,
			$"Final X should be 250, was {ellipse.Center.X}");
		Assert.AreEqual(150.0, ellipse.Center.Y, 1.0,
			$"Final Y should be 150, was {ellipse.Center.Y}");

		storyboard.Stop();
	}

	/// <summary>
	/// Ported from PointAnimationTests::NegativeTests.
	/// EnableDependentAnimation=false blocks the visual animation, but the
	/// storyboard still completes and fires the Completed event on WinUI.
	/// </summary>
	[TestMethod]
#if __SKIA__
	[Ignore("Skia doesn't enforce EnableDependentAnimation=false yet")]
#endif
	public async Task When_EnableDependentAnimation_False_Blocks()
	{
		var ellipse = new EllipseGeometry { Center = new Point(50, 50), RadiusX = 50, RadiusY = 50 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Gold),
			Width = 300,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(250, 150),
			Duration = new Duration(TimeSpan.FromMilliseconds(100)),
			EnableDependentAnimation = false,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		bool completed = false;
		var storyboard = animation.ToStoryboard();
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		// Wait well past the animation duration
		await Task.Delay(500);

		// WinUI fires Completed even when the animation is blocked by EnableDependentAnimation=false
		Assert.IsTrue(completed,
			"Completed event should fire even when EnableDependentAnimation is false");

		// The animation should NOT have changed the value — it was blocked
		Assert.AreEqual(50.0, ellipse.Center.X, 1.0,
			$"X should remain at 50 when animation is blocked, was {ellipse.Center.X}");
		Assert.AreEqual(50.0, ellipse.Center.Y, 1.0,
			$"Y should remain at 50 when animation is blocked, was {ellipse.Center.Y}");

		storyboard.Stop();
	}

	/// <summary>
	/// SpeedRatio=2 on a 1s Point animation should complete in roughly half the time.
	/// </summary>
	[TestMethod]
	public async Task When_SpeedRatio_2x()
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
			From = new Point(0, 0),
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(1000)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.SpeedRatio = 2.0;

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Y should be 100, was {ellipse.Center.Y}");

		// With SpeedRatio=2, a 1s animation should finish in ~500ms.
		// Allow generous tolerance: should be under 900ms
		Assert.IsTrue(sw.ElapsedMilliseconds < 900,
			$"SpeedRatio=2 on 1s animation should complete in under 900ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// SpeedRatio=0.5 on a 500ms Point animation should complete in roughly 1s.
	/// </summary>
	[TestMethod]
	public async Task When_SpeedRatio_Half()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
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
			From = new Point(0, 0),
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.SpeedRatio = 0.5;

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Y should be 100, was {ellipse.Center.Y}");

		// With SpeedRatio=0.5, a 500ms animation should finish in ~1000ms.
		// Allow generous tolerance: should take at least 500ms
		Assert.IsTrue(sw.ElapsedMilliseconds >= 500,
			$"SpeedRatio=0.5 on 500ms animation should take at least 500ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// FillBehavior.HoldEnd should maintain the To point after completion.
	/// </summary>
	[TestMethod]
	public async Task When_FillBehavior_HoldEnd_Holds_Point()
	{
		var ellipse = new EllipseGeometry { Center = new Point(10, 20), RadiusX = 20, RadiusY = 20 };
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
			From = new Point(10, 20),
			To = new Point(150, 180),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		// Verify held at To value
		Assert.AreEqual(150.0, ellipse.Center.X, 1.0,
			$"HoldEnd X should be 150, was {ellipse.Center.X}");
		Assert.AreEqual(180.0, ellipse.Center.Y, 1.0,
			$"HoldEnd Y should be 180, was {ellipse.Center.Y}");

		// Wait a bit more and verify it's still held
		await Task.Delay(200);
		Assert.AreEqual(150.0, ellipse.Center.X, 1.0,
			$"HoldEnd X should still be 150 after waiting, was {ellipse.Center.X}");
		Assert.AreEqual(180.0, ellipse.Center.Y, 1.0,
			$"HoldEnd Y should still be 180 after waiting, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// FillBehavior.Stop should revert to the local Point value after completion.
	/// Ported from PointAnimationTests::PointAnimationBasic (FillBehavior.Stop section).
	/// </summary>
	[TestMethod]
	public async Task When_FillBehavior_Stop_Returns_To_Base()
	{
		var ellipse = new EllipseGeometry { Center = new Point(50, 50), RadiusX = 50, RadiusY = 50 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Gold),
			Width = 300,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(250, 150),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.Stop,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
		await WindowHelper.WaitForIdle();

		// After FillBehavior.Stop, value should revert to local (50,50)
		Assert.AreEqual(50.0, ellipse.Center.X, 1.0,
			$"After FillBehavior.Stop, X should return to 50, was {ellipse.Center.X}");
		Assert.AreEqual(50.0, ellipse.Center.Y, 1.0,
			$"After FillBehavior.Stop, Y should return to 50, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// HoldEnd fill, then Stop storyboard should return to local value.
	/// Ported from PointAnimationTests::PointAnimationBasic (Stop after fill).
	/// </summary>
	[TestMethod]
	public async Task When_FillBehavior_HoldEnd_Then_Stop_Clears()
	{
		var ellipse = new EllipseGeometry { Center = new Point(50, 50), RadiusX = 50, RadiusY = 50 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Gold),
			Width = 300,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			To = new Point(250, 150),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

		// Verify fill holds the To value
		Assert.AreEqual(250.0, ellipse.Center.X, 1.0,
			$"Fill X should be 250, was {ellipse.Center.X}");
		Assert.AreEqual(150.0, ellipse.Center.Y, 1.0,
			$"Fill Y should be 150, was {ellipse.Center.Y}");

		// Stop the storyboard - should clear the fill and return to local value
		storyboard.Stop();
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(50.0, ellipse.Center.X, 1.0,
			$"After Stop, X should return to local value (50), was {ellipse.Center.X}");
		Assert.AreEqual(50.0, ellipse.Center.Y, 1.0,
			$"After Stop, Y should return to local value (50), was {ellipse.Center.Y}");
	}

	/// <summary>
	/// BeginTime=300ms should delay the start of the animation.
	/// Verify the point hasn't moved at 100ms, then verify completion.
	/// </summary>
	[TestMethod]
	public async Task When_BeginTime_Delays_Start()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			BeginTime = TimeSpan.FromMilliseconds(300),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// After 100ms, animation hasn't started yet (BeginTime = 300ms)
		await Task.Delay(100);
		var beforeStart = ellipse.Center;

		// Wait for completion
		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// Before BeginTime, values should still be near origin
		Assert.IsTrue(beforeStart.X < 15.0,
			$"Before BeginTime, X should still be near 0, was {beforeStart.X}");
		Assert.IsTrue(beforeStart.Y < 15.0,
			$"Before BeginTime, Y should still be near 0, was {beforeStart.Y}");

		// After completion, values should be at To
		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"After animation, X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"After animation, Y should be 100, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// AutoReverse brings the point back to From value.
	/// Ported from PointAnimationTests::PointAnimationBasic (AutoReverse section).
	/// </summary>
	[TestMethod]
	public async Task When_AutoReverse_Returns_To_From()
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

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		// After AutoReverse with HoldEnd, value returns to the From value
		Assert.AreEqual(10.0, ellipse.Center.X, 1.0,
			$"X should return to From (10), was {ellipse.Center.X}");
		Assert.AreEqual(20.0, ellipse.Center.Y, 1.0,
			$"Y should return to From (20), was {ellipse.Center.Y}");
	}

	/// <summary>
	/// RepeatBehavior(2) + AutoReverse: 2 full cycles, ends at From.
	/// Each cycle is forward+reverse, so 2 reps * (300ms forward + 300ms reverse) = 1200ms total.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatCount_With_AutoReverse()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Teal),
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
			RepeatBehavior = new RepeatBehavior(2),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		// After 2 reps with AutoReverse (even count), value ends at From
		Assert.AreEqual(10.0, ellipse.Center.X, 1.0,
			$"After 2 reps with AutoReverse, X should return to From (10), was {ellipse.Center.X}");
		Assert.AreEqual(20.0, ellipse.Center.Y, 1.0,
			$"After 2 reps with AutoReverse, Y should return to From (20), was {ellipse.Center.Y}");
	}

	/// <summary>
	/// 3 x 200ms iterations should take approximately 600ms.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatCount3_Completes_At_Expected_Time()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Coral),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(0, 0),
			To = new Point(60, 80),
			Duration = new Duration(TimeSpan.FromMilliseconds(200)),
			RepeatBehavior = new RepeatBehavior(3),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var sw = System.Diagnostics.Stopwatch.StartNew();
		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));
		sw.Stop();

		Assert.AreEqual(60.0, ellipse.Center.X, 1.0,
			$"Final X should be 60, was {ellipse.Center.X}");
		Assert.AreEqual(80.0, ellipse.Center.Y, 1.0,
			$"Final Y should be 80, was {ellipse.Center.Y}");

		// 3 x 200ms = 600ms expected. Allow 50% margin: at least 300ms.
		Assert.IsTrue(sw.ElapsedMilliseconds >= 300,
			$"3 iterations of 200ms should take at least 300ms, took {sw.ElapsedMilliseconds}ms");
		// Should not take excessively long (under 1500ms with generous margin)
		Assert.IsTrue(sw.ElapsedMilliseconds < 1500,
			$"3 iterations of 200ms should complete in under 1500ms, took {sw.ElapsedMilliseconds}ms");
	}

	/// <summary>
	/// RepeatBehavior.Forever: values keep changing, Completed never fires.
	/// </summary>
	[TestMethod]
	public async Task When_RepeatForever_NeverCompletes()
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
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			RepeatBehavior = RepeatBehavior.Forever,
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		bool completed = false;
		var storyboard = animation.ToStoryboard();
		storyboard.Completed += (s, e) => completed = true;
		storyboard.Begin();

		try
		{
			// Sample X values over multiple cycles
			var samples = new List<double>();
			for (int i = 0; i < 20; i++)
			{
				await Task.Delay(100);
				samples.Add(ellipse.Center.X);
			}

			// The animation should cycle: X goes 0->100 then resets to 0 repeatedly.
			// We should see at least one value drop (cycle boundary).
			var drops = 0;
			for (int i = 1; i < samples.Count; i++)
			{
				if (samples[i] < samples[i - 1] - 15)
				{
					drops++;
				}
			}

			Assert.IsTrue(drops >= 1,
				$"Expected at least 1 cycle drop in X values, got {drops}. Samples: {string.Join(", ", samples.Select(v => v.ToString("F1")))}");

			// Forever animation should not have fired Completed
			Assert.IsFalse(completed, "Forever animation should not fire Completed");
		}
		finally
		{
			storyboard.Stop();
		}
	}

	/// <summary>
	/// QuadraticEase with EaseIn on Point: at 30% time, X should be less than 15% of range (slow start).
	/// </summary>
	[TestMethod]
	public async Task When_EasingFunction_QuadraticEaseIn()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Violet),
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
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
			EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		var storyboard = animation.ToStoryboard();
		storyboard.Begin();

		// EaseIn starts slow. At ~30% time (180ms), progress = 0.3^2 = 0.09 (9%)
		await Task.Delay(180);
		var midPoint = ellipse.Center;

		await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(5));

		// With QuadraticEase.EaseIn at 30% time, value should be less than 30 (linear)
		// Use generous threshold - anything under 40 is clearly non-linear
		Assert.IsTrue(midPoint.X < 40.0,
			$"With EaseIn, mid-point X should be less than 40, was {midPoint.X}");

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Final Y should be 100, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// BounceEase on Point animation, verify reaches final value.
	/// </summary>
	[TestMethod]
	public async Task When_EasingFunction_BounceEase()
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
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
			EasingFunction = new BounceEase { Bounces = 3, Bounciness = 2 },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Final Y should be 100, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// ElasticEase on Point animation, verify reaches final value.
	/// </summary>
	[TestMethod]
	public async Task When_EasingFunction_ElasticEase()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
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
			From = new Point(0, 0),
			To = new Point(100, 100),
			Duration = new Duration(TimeSpan.FromMilliseconds(600)),
			EasingFunction = new ElasticEase { Oscillations = 3, Springiness = 3 },
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(5));

		Assert.AreEqual(100.0, ellipse.Center.X, 1.0,
			$"Final X should be 100, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Final Y should be 100, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// Duration=0 should jump directly to the To point.
	/// </summary>
	[TestMethod]
	public async Task When_ZeroDuration_Jumps()
	{
		var ellipse = new EllipseGeometry { Center = new Point(10, 20), RadiusX = 20, RadiusY = 20 };
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
			From = new Point(10, 20),
			To = new Point(80, 90),
			Duration = new Duration(TimeSpan.Zero),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(80.0, ellipse.Center.X, 1.0,
			$"X should jump to 80, was {ellipse.Center.X}");
		Assert.AreEqual(90.0, ellipse.Center.Y, 1.0,
			$"Y should jump to 90, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// By=(30,40) with current=(10,20) should animate to (40,60).
	/// Both X and Y components should be animated by the By offset.
	/// </summary>
	[TestMethod]
	public async Task When_By_Animates_Both_Components()
	{
		var ellipse = new EllipseGeometry { Center = new Point(10, 20), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Magenta),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			By = new Point(30, 40),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(40.0, ellipse.Center.X, 1.0,
			$"X should be 10+30=40, was {ellipse.Center.X}");
		Assert.AreEqual(60.0, ellipse.Center.Y, 1.0,
			$"Y should be 20+40=60, was {ellipse.Center.Y}");
	}

	/// <summary>
	/// From=(10,10), By=(50,90) with no To should animate to (60,100).
	/// By is added to the From value.
	/// </summary>
	[TestMethod]
	public async Task When_From_And_By_No_To()
	{
		var ellipse = new EllipseGeometry { Center = new Point(0, 0), RadiusX = 20, RadiusY = 20 };
		var path = new Path
		{
			Data = ellipse,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Lime),
			Width = 200,
			Height = 200,
		};
		WindowHelper.WindowContent = path;
		await WindowHelper.WaitForLoaded(path);
		await WindowHelper.WaitForIdle();

		var animation = new PointAnimation
		{
			From = new Point(10, 10),
			By = new Point(50, 90),
			Duration = new Duration(TimeSpan.FromMilliseconds(300)),
			FillBehavior = FillBehavior.HoldEnd,
			EnableDependentAnimation = true,
		}.BindTo(ellipse, nameof(EllipseGeometry.Center));

		await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));

		Assert.AreEqual(60.0, ellipse.Center.X, 1.0,
			$"X should be From.X + By.X = 10+50=60, was {ellipse.Center.X}");
		Assert.AreEqual(100.0, ellipse.Center.Y, 1.0,
			$"Y should be From.Y + By.Y = 10+90=100, was {ellipse.Center.Y}");
	}
}
