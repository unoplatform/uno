using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using Combinatorial.MSTest;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_DoubleAnimation
	{
		[TestMethod]
		public void When_SeekAlignedToLastTick()
		{
			var target = new Border();
			var doubleAnimation = new DoubleAnimation()
			{
				From = 50d,
				To = 100d,
				EnableDependentAnimation = true
			};
			Storyboard.SetTarget(doubleAnimation, target);
			Storyboard.SetTargetProperty(doubleAnimation, "Height");
			var sb = new Storyboard()
			{
				Children =
				{
					doubleAnimation
				}
			};

			sb.Begin();
			sb.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(50));
		}

		[TestMethod]
#if __ANDROID__
		[Ignore("In this scenario, droid doesnt ReportEachFrame(), so we won't be able to read the animated values to evaluate this test.")]
#endif
		public async Task When_RepeatForever_WithoutFrom()
		{
			// droid: The fix is still valid for android, because it will now be reading from non-animated value as well.
			// However, that doesnt change anything (it worked before), because the animated was never commited into the property details.

			// note: Without an actual rendered target, playing the storyboard will not
			// affect the actual value effectively voiding this test.
			var target = new TextBlock() { Text = "asdasd" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				// From = ..., // left out for this test
				To = 312,
				Duration = TimeSpan.FromMilliseconds(500),
				FillBehavior = FillBehavior.HoldEnd,
				RepeatBehavior = RepeatBehavior.Forever,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);
			storyboard.Begin();

			var values = new List<double>();
			// The delay below are more of a suggestion. We cant realistically expected
			// to land exactly on the end of loop at 500ms. But the idea here is to measure
			// that the animated value doesn't freeze/stuck at DoubleAnimation.To of 312
			// after certain iterations. So the lack of precision is actually desired here.
			await WaitAndSnapshotValue(100); // 0
			await WaitAndSnapshotValue(400); // 1 iteration 1 done
			await WaitAndSnapshotValue(500); // 2 iteration 2 done
			await WaitAndSnapshotValue(500); // 3 iteration 3 done
			await WaitAndSnapshotValue(500); // 4 iteration 4 done
			await WaitAndSnapshotValue(100); // 5
			await WaitAndSnapshotValue(100); // 6
			await WaitAndSnapshotValue(100); // 7
			await WaitAndSnapshotValue(100); // 8
			await WaitAndSnapshotValue(100); // 9 iteration 5 done

			const double SignificantChangePer100MS = 312 / 5 * 0.5; // 0.5 is margin of error
			var last5Recorded = values.Skip(values.Count - 5).ToArray();
			var deltas = last5Recorded.Zip(last5Recorded.Skip(1), (a, b) => Math.Abs(a - b));
			var closeEnough = Comparer<double>.Create((x, y) => // allows for +-1 to be equal
				Math.Abs(x - y) < 1 ? 0 : (x > y ? 1 : -1)
			);

			Assert.IsTrue(deltas.Any(x => x > SignificantChangePer100MS), "animated values (last half) should be changing: " + string.Join(", ", last5Recorded));
			CollectionAssert.AreNotEqual(
				Enumerable.Repeat(312d, 5).ToArray(),
				last5Recorded,
				closeEnough,
				"animated values should not freeze/stuck at end value (312) or near that value: " + string.Join(", ", values)
			);

			async Task WaitAndSnapshotValue(int millisecondsDelay)
			{
				await Task.Delay(millisecondsDelay);
				values.Add(transform.X);
			}

			storyboard.Stop();
		}

		[TestMethod]
		public async Task When_RepeatForever_ShouldLoop() // Previously flaky on Skia due to RepeatBehavior.Forever bug, now fixed
		{
			async Task Do()
			{
				// On CI, the measurement at 100ms seem to be too unreliable on Android & MacOS.
				// Stretch the test by 5x greatly improve the stability. When testing locally, we can used 1x to save time (5s vs 25s).
				int timeResolutionScaling =
#if !DEBUG && __ANDROID__
					5;
#else
					1;
#endif

#if !DEBUG && __SKIA__
				if (OperatingSystem.IsMacOS())
				{
					timeResolutionScaling = 5;
				}
#endif
				var target = new Microsoft.UI.Xaml.Shapes.Rectangle
				{
					Stretch = Stretch.Fill,
					Fill = new SolidColorBrush(Colors.SkyBlue),
					Width = 50,
					Height = 50,
				};
				WindowHelper.WindowContent = target;
				await WindowHelper.WaitForLoaded(target);
				await WindowHelper.WaitForIdle();

				var animation = new DoubleAnimation
				{
					EnableDependentAnimation = true,
					From = 0,
					To = 50,
					RepeatBehavior = RepeatBehavior.Forever,
					Duration = TimeSpan.FromMilliseconds(500 * timeResolutionScaling),
				}.BindTo(target, nameof(Rectangle.Width));
				var storyboard = animation.ToStoryboard();
				storyboard.Begin();

				// In an ideal world, the measurements would be [0 or 50,10,20,30,40] repeated 10 times.
				var list = new List<double>();
				for (int i = 0; i < 50; i++)
				{
					list.Add(NanToZero(target.Width));
					await Task.Delay(100 * timeResolutionScaling);
				}

				var delta = list.Zip(list.Skip(1), (a, b) => b - a).ToArray();
				var averageIncrement = delta.Where(x => x > 0).Average();
				var drops = delta.Select((x, i) => new { Delta = x, Index = i })
					.Where(x => x.Delta < 0)
					.Select(x => x.Index)
					.ToArray();
				var incrementSizes = drops.Zip(drops.Skip(1), (a, b) => b - a - 1).ToArray(); // -1 to exclude the drop itself

				var context = new StringBuilder()
					.AppendLine("list: " + string.Join(", ", list.Select(x => x.ToString("0.#"))))
					.AppendLine("delta: " + string.Join(", ", delta.Select(x => x.ToString("+0.#;-0.#;0"))))
					.AppendLine("averageIncrement: " + averageIncrement)
					.AppendLine("drops: " + string.Join(", ", drops.Select(x => x.ToString("0.#"))))
					.AppendLine("incrementSizes: " + string.Join(", ", incrementSizes.Select(x => x.ToString("0.#"))))
					.ToString();

				// This 500ms animation is expected to climb from 0 to 50, reset to 0 instantly, and repeat forever.
				// Given that we are taking 5measurements per cycle, we can expect the followings:
				Assert.AreEqual(10d, averageIncrement, 2.5, $"Expected an rough average of increment (excluding the drop) of 10 (+-25% error margin).\n" + context);
				Assert.IsGreaterThanOrEqualTo(8, incrementSizes.Count(x => x >= 3), $"Expected at least 10sets (-2 error margin: might miss first and/or last) of continuous increments in size of 4 (+-1 error margin: sliding slot).\n" + context);

				double NanToZero(double value) => double.IsNaN(value) ? 0 : value;

				storyboard.Stop();
			}

			await TestHelper.RetryAssert(Do, 10);
		}

		[TestMethod]
		public async Task When_StartingFrom_AnimatedValue() // value from completed(filling) animation
		{
			var translate = new TranslateTransform();
			var border = new Border()
			{
				Background = new SolidColorBrush(Colors.Pink),
				Margin = new Thickness(0, 50, 0, 0),
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			// Start an animation. Its final value will serve as
			// the inferred starting value for the next animation.
			var animation0 = new DoubleAnimation
			{
				// From = should be 0
				To = 50,
				Duration = new Duration(TimeSpan.FromSeconds(2)),
			}.BindTo(translate, nameof(translate.Y));
			await animation0.ToStoryboard().RunAsync(timeout: animation0.Duration.TimeSpan + TimeSpan.FromSeconds(1));
			await Task.Delay(1000);

			// Start an second animation which should pick up from current animated value.
			var animation1 = new DoubleAnimation
			{
				// From = should be 50 from animation #0
				To = -50,
				Duration = new Duration(TimeSpan.FromSeconds(5)),
			}.BindTo(translate, nameof(translate.Y));
			animation1.ToStoryboard().Begin();
			await Task.Delay(125);

			// ~125ms into a 5s animation where the value is animating from 50 to -50,
			// the value should be still positive.
			var y = GetTranslateY(translate, isStillAnimating: true);
			Assert.IsGreaterThan(0, y, $"Expecting Translate.Y to be still positive: {y}");
		}

		[TestMethod]
		public async Task When_StartingFrom_AnimatingValue() // value from mid animation
		{
			var translate = new TranslateTransform();
			var border = new Border()
			{
				Background = new SolidColorBrush(Colors.Pink),
				Margin = new Thickness(0, 50, 0, 0),
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			translate.Y = 50;
			await WindowHelper.WaitForIdle();
			await Task.Delay(1000);
			var threshold = GetTranslateY(translate); // snapshot the value

			// Start an animation. Its animating value will serve as
			// the inferred starting value for the next animation.
			var animation0 = new DoubleAnimation
			{
				From = 100,
				To = 105,
				Duration = new Duration(TimeSpan.FromSeconds(5)),
			}.BindTo(translate, nameof(translate.Y));
			animation0.ToStoryboard().Begin();
			await Task.Delay(125);

			// Start an second animation which should pick up from current animating value.
			var animation1 = new DoubleAnimation
			{
				// From = should be around 100~105 from animation #0
				To = 50,
				Duration = new Duration(TimeSpan.FromSeconds(5)),
			}.BindTo(translate, nameof(translate.Y));
			animation1.ToStoryboard().Begin();
			await Task.Delay(125);

			var value = GetTranslateY(translate, isStillAnimating: true);
			if (value is double y)
			{
				// Animation #1 should be animating from around[100~105] to 50, and not from 0 (unanimated Local value).
				Assert.IsGreaterThan(50, y, $"Expecting Translate.Y to be still positive: {y}");
			}
			else
			{
				Assert.Fail($"Translate.Y is not a double: value={value}");
			}
		}

		[TestMethod]
		[CombinatorialData]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public async Task When_OverridingFillingValue_WithLocalValue(bool skipToFill)
		{
			var translate = new TranslateTransform();
			var border = new Border()
			{
				Background = new SolidColorBrush(Colors.Pink),
				Margin = new Thickness(0, 50, 0, 0),
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			// Animate the value to fill.
			var animation0 = new DoubleAnimation
			{
				To = 100,
				Duration = new Duration(TimeSpan.FromSeconds(1)),
			}.BindTo(translate, nameof(translate.Y));
			if (skipToFill)
			{
				animation0.ToStoryboard().SkipToFill();
			}
			else
			{
				await animation0.ToStoryboard().RunAsync();
			}
			var beforeValue = translate.Y;

			// Set a new local value
			translate.Y = 312.0;
			var afterValue = translate.Y;

			Assert.AreEqual(100.0, beforeValue, "before: Should be animated to 100");
			Assert.AreEqual(312.0, afterValue, "after: Should be set to 312");
		}

#if __ANDROID__
		[TestMethod]
		public async Task When_EasingFunction()
		{
			var translate = new TranslateTransform();
			var border = new Border()
			{
				Background = new SolidColorBrush(Colors.Pink),
				Margin = new Thickness(0, 50, 0, 0),
				Width = 50,
				Height = 50,
				RenderTransform = translate,
			};
			WindowHelper.WindowContent = border;
			await WindowHelper.WaitForLoaded(border);
			await WindowHelper.WaitForIdle();

			var myEasingFunction = new MyEasingFunction();
			var animation = new DoubleAnimation
			{
				To = 100,
				Duration = new Duration(TimeSpan.FromSeconds(0.1)),
				EasingFunction = myEasingFunction,
			}.BindTo(translate, nameof(translate.Y));

			await animation.ToStoryboard().RunAsync();

			Assert.IsTrue(myEasingFunction.WasCalledByAndroidTimeInterpolator);
		}

		private sealed class MyEasingFunction : EasingFunctionBase
		{
			public bool WasCalledByAndroidTimeInterpolator { get; private set; } = new();

			private protected override double EaseInCore(double normalizedTime)
			{
				WasCalledByAndroidTimeInterpolator |= Environment.StackTrace.Contains("EasingFunctionBase.AndroidTimeInterpolator.GetInterpolation");
				return normalizedTime;
			}
		}
#endif

		[TestMethod]
		public async Task When_Default_Duration_Is_One_Second()
		{
			// WinUI: DoubleAnimation without explicit Duration defaults to 1 second (NULL_DURATION_DEFAULT).
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

			// DoubleAnimation with no Duration property set
			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			var sw = System.Diagnostics.Stopwatch.StartNew();
			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			sw.Stop();

			// Should have taken approximately 1 second (WinUI default)
			Assert.IsTrue(sw.ElapsedMilliseconds >= 800,
				$"Default 1s animation should take at least 800ms, took {sw.ElapsedMilliseconds}ms");
			Assert.AreEqual(100.0, translate.Y, 1.0, "Fill value should be 100");
		}

		[TestMethod]
		public async Task When_FillBehavior_Stop_ClearsValue()
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

			translate.Y = 20.0;

			var animation = new DoubleAnimation
			{
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(200)),
				FillBehavior = FillBehavior.Stop,
			}.BindTo(translate, nameof(translate.Y));

			await animation.ToStoryboard().RunAsync();

			// After FillBehavior.Stop animation completes, the animated value is cleared
			// and the property should return to its local value (20).
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(20.0, translate.Y, "After FillBehavior.Stop, property should return to local value");
		}

		[TestMethod]
		public async Task When_AutoReverse_GoesForwardAndBack()
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

			var midpoints = new List<double>();

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				AutoReverse = true,
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			var storyboard = animation.ToStoryboard();
			storyboard.Begin();

			// Sample mid-forward: ~200ms into a 500ms duration should be between 0 and 100
			await Task.Delay(200);
			midpoints.Add(translate.Y);

			// Wait for full 1-second cycle (500ms forward + 500ms backward) to complete
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));

			// After AutoReverse completes with HoldEnd, the fill value should be the FROM value (0)
			// because the reversed animation ends at the starting point.
			Assert.IsTrue(midpoints[0] > 5.0 && midpoints[0] < 95.0,
				$"Mid-point value {midpoints[0]} should be between 5 and 95 (animation in progress)");
			Assert.AreEqual(0.0, translate.Y, 1.0, $"After AutoReverse fill, value should be ~0 (from value), was {translate.Y}");
		}

		[TestMethod]
		public async Task When_BeginTime_DelaysStart()
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

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				BeginTime = TimeSpan.FromMilliseconds(300),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			var storyboard = animation.ToStoryboard();
			storyboard.Begin();

			// After 100ms, the animation hasn't started yet (BeginTime = 300ms)
			await Task.Delay(100);
			var beforeStart = translate.Y;

			// Wait for animation to complete
			await storyboard.RunAsync(timeout: TimeSpan.FromSeconds(3));
			var afterComplete = translate.Y;

			Assert.AreEqual(0.0, beforeStart, 1.0,
				$"Before BeginTime, value should still be at base (0), was {beforeStart}");
			Assert.AreEqual(100.0, afterComplete, 1.0,
				$"After animation, fill value should be 100, was {afterComplete}");
		}

		[TestMethod]
		public async Task When_By_AnimatesRelativeToCurrentValue()
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

			translate.Y = 30.0;
			await WindowHelper.WaitForIdle();

			// By = 70 means animate from current value (30) by 70 → to 100
			var animation = new DoubleAnimation
			{
				By = 70,
				Duration = new Duration(TimeSpan.FromMilliseconds(500)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			await animation.ToStoryboard().RunAsync();

			Assert.AreEqual(100.0, translate.Y, 1.0,
				$"By animation should animate to From+By = 30+70 = 100, was {translate.Y}");
		}

		[TestMethod]
		public async Task When_Stop_While_Filling_ClearsValue()
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

			translate.Y = 20.0;

			var animation = new DoubleAnimation
			{
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(200)),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			var storyboard = animation.ToStoryboard();
			await storyboard.RunAsync();

			// Animation is now filling at 100
			Assert.AreEqual(100.0, translate.Y, 1.0, $"Fill value should be 100, was {translate.Y}");

			// Stop the storyboard - should clear the animated value and return to local
			storyboard.Stop();
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(20.0, translate.Y, 1.0,
				$"After Stop, property should return to local value (20), was {translate.Y}");
		}

		[TestMethod]
		public async Task When_RepeatBehavior_Count_CompletesAtExpectedTime()
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

			var animation = new DoubleAnimation
			{
				From = 0,
				To = 100,
				Duration = new Duration(TimeSpan.FromMilliseconds(200)),
				RepeatBehavior = new RepeatBehavior(3),
				FillBehavior = FillBehavior.HoldEnd,
			}.BindTo(translate, nameof(translate.Y));

			// The total duration should be 3 * 200ms = 600ms
			var sw = System.Diagnostics.Stopwatch.StartNew();
			await animation.ToStoryboard().RunAsync(timeout: TimeSpan.FromSeconds(3));
			sw.Stop();

			// After 3 repetitions with HoldEnd, the value should be at To (100)
			Assert.AreEqual(100.0, translate.Y, 1.0,
				$"After 3 repetitions, fill value should be 100, was {translate.Y}");
			Assert.IsTrue(sw.ElapsedMilliseconds >= 550,
				$"3 iterations of 200ms each should take ~600ms, took {sw.ElapsedMilliseconds}ms");
		}

		private static double GetTranslateY(TranslateTransform translate, bool isStillAnimating = false) =>
#if !__ANDROID__
			translate.Y;
#else
			isStillAnimating
				// On android, animation may target a native property implementing the behavior instead of the specified dependency property.
				// We need to retrieve the value of that native property, as reading the dp value will just give the final value.
				? ViewHelper.PhysicalToLogicalPixels((double)translate.View.TranslationY)
				// And, when the animation is completed, this native value is reset even for HoldEnd animation.
				: translate.Y;
#endif
	}
}
