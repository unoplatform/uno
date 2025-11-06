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
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Skia)]
		public async Task When_RepeatForever_ShouldLoop() // Flaky - #9080
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
				Assert.IsTrue(incrementSizes.Count(x => x >= 3) >= 8, $"Expected at least 10sets (-2 error margin: might miss first and/or last) of continuous increments in size of 4 (+-1 error margin: sliding slot).\n" + context);

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
			Assert.IsTrue(y > 0, $"Expecting Translate.Y to be still positive: {y}");
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
				Assert.IsTrue(y > 50, $"Expecting Translate.Y to be still positive: {y}");
			}
			else
			{
				Assert.Fail($"Translate.Y is not a double: value={value}");
			}
		}

		[TestMethod]
		[CombinatorialData]
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

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_AutoReverse_True()
		{
			var target = new TextBlock() { Text = "Test AutoReverse" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 100,
				Duration = TimeSpan.FromMilliseconds(500),
				AutoReverse = true,
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// Wait for quarter of the animation (forward phase)
			await Task.Delay(250);
			var valueAt25Percent = transform.X;

			// Wait for just past halfway (should be near the end of forward phase)
			await Task.Delay(300);
			var valueAt55Percent = transform.X;

			// Wait for 75% of total duration (should be in reverse phase)
			await Task.Delay(250);
			var valueAt80Percent = transform.X;

			// Wait for completion
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

			// Final value should be back at start (0) with HoldEnd
			var finalValue = transform.X;

			// Verify animation went forward then backward
			Assert.IsTrue(valueAt25Percent > 0 && valueAt25Percent < 100,
				$"At 25%, value should be between 0 and 100, got {valueAt25Percent}");
			Assert.IsTrue(valueAt55Percent > valueAt25Percent,
				$"At 55%, value should be greater than at 25%, got {valueAt55Percent} vs {valueAt25Percent}");
			Assert.IsTrue(valueAt80Percent < valueAt55Percent,
				$"At 80% (reverse phase), value should be less than at 55%, got {valueAt80Percent} vs {valueAt55Percent}");
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_AutoReverse_False()
		{
			var target = new TextBlock() { Text = "Test No AutoReverse" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 100,
				Duration = TimeSpan.FromMilliseconds(500),
				AutoReverse = false,
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// Wait for completion
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2000);

			// Final value should stay at end (100) with HoldEnd and no AutoReverse
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue - 100) < 10,
				$"Final value should be close to 100, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_AutoReverse_WithRepeat()
		{
			var target = new TextBlock() { Text = "Test AutoReverse with Repeat" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 50,
				Duration = TimeSpan.FromMilliseconds(250),
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(2), // Repeat twice (4 total half-cycles)
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard();
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// Wait for completion (2 repeats * 2 phases * 250ms = 1000ms)
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2500);

			// Final value should be back at start (0)
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0 after repeating with AutoReverse, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_AutoReverse_OnStoryboard()
		{
			var target = new TextBlock() { Text = "Test AutoReverse on Storyboard" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 100,
				Duration = TimeSpan.FromMilliseconds(500),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard()
			{
				AutoReverse = true // AutoReverse on Storyboard level
			};
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// Wait for completion (500ms forward + 500ms reverse = 1000ms)
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2500);

			// Final value should be back at start (0) when Storyboard has AutoReverse
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0 after Storyboard AutoReverse, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_AutoReverse_OnBothStoryboardAndAnimation()
		{
			var target = new TextBlock() { Text = "Test AutoReverse on both levels" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 100,
				Duration = TimeSpan.FromMilliseconds(250),
				AutoReverse = true, // AutoReverse on animation
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard()
			{
				AutoReverse = true // AutoReverse on Storyboard too
			};
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// With AutoReverse on both:
			// Animation: 250ms forward + 250ms reverse = 500ms
			// Storyboard AutoReverse: repeats that 500ms sequence in reverse
			// Total: 500ms + 500ms = 1000ms
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2500);

			// Final value should be back at start (0)
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0 with nested AutoReverse, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_RepeatBehavior_OnStoryboardWithAutoReverse()
		{
			var target = new TextBlock() { Text = "Test RepeatBehavior on Storyboard with AutoReverse" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 50,
				Duration = TimeSpan.FromMilliseconds(200),
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard()
			{
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(2) // Repeat the (forward+reverse) cycle twice
			};
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// 2 repeats * (200ms forward + 200ms reverse) = 2 * 400ms = 800ms
			await WindowHelper.WaitFor(() => completed, timeoutMS: 2500);

			// Final value should be back at start (0)
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0 after Storyboard repeat with AutoReverse, got {finalValue}");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS)]
		public async Task When_RepeatBehavior_OnBothLevelsWithAutoReverse()
		{
			var target = new TextBlock() { Text = "Test RepeatBehavior on both levels with AutoReverse" };
			WindowHelper.WindowContent = target;
			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(target);

			var transform = new TranslateTransform();
			target.RenderTransform = transform;

			var animation = new DoubleAnimation()
			{
				From = 0,
				To = 50,
				Duration = TimeSpan.FromMilliseconds(100),
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(2), // Animation repeats twice (200ms total)
				FillBehavior = FillBehavior.HoldEnd,
			};
			Storyboard.SetTarget(animation, transform);
			Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));

			var storyboard = new Storyboard()
			{
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(2) // Storyboard repeats its children twice
			};
			storyboard.Children.Add(animation);

			bool completed = false;
			storyboard.Completed += (s, e) => completed = true;

			storyboard.Begin();

			// Animation: 2 repeats * (100ms forward + 100ms reverse) = 400ms
			// Storyboard: 2 repeats * (400ms forward + 400ms reverse) = 1600ms total
			await WindowHelper.WaitFor(() => completed, timeoutMS: 3000);

			// Final value should be back at start (0)
			var finalValue = transform.X;
			Assert.IsTrue(Math.Abs(finalValue) < 10,
				$"Final value should be close to 0 after nested repeat with AutoReverse, got {finalValue}");
		}
	}
}
