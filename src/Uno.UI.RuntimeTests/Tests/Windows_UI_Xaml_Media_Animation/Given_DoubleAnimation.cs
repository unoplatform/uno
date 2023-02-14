using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	public class Given_DoubleAnimation
	{
		[TestMethod]
		[RunsOnUIThread]
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
		[RunsOnUIThread]
#if __ANDROID__
		[Ignore("In this scenario, droid doesnt ReportEachFrame(), so we won't be able to read the animated values to evaluate this test.")]
#endif
		public async Task When_RepeatForever_WithoutFrom_Asd()
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
		}
	}
}
