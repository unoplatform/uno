using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Animation
{
	[TestFixture]
	public partial class DoubleAnimation_Tests : SampleControlUITestBase
	{
		private const string _finalStateTransformsTestControl = "UITests.Windows_UI_Xaml_Media_Animation.DoubleAnimation_FinalState_Transforms";

		[Test] [AutoRetry] public void When_Transforms_Completed_With_FillBehaviorStop_Then_Rollback() => TestTransformsFinalState();
		[Test] [AutoRetry] public void When_Transforms_Completed_With_FillBehaviorHold_Then_Hold() => TestTransformsFinalState();
		[Test] [AutoRetry] public void When_Transforms_Paused_With_FillBehaviorStop_Then_Hold() => TestTransformsFinalState();
		[Test] [AutoRetry] public void When_Transforms_Paused_With_FillBehaviorHold_Then_Hold() => TestTransformsFinalState();
		[Test] [AutoRetry] public void When_Transforms_Canceled_With_FillBehaviorStop_Then_Rollback() => TestTransformsFinalState();
		[Test] [AutoRetry] public void When_Transforms_Canceled_With_FillBehaviorHold_Then_Rollback() => TestTransformsFinalState();

		private void TestTransformsFinalState([CallerMemberName] string testName = null)
		{
			var match = Regex.Match(testName, @"When_Transforms_(?<type>\w+)_With_FillBehavior(?<fill>\w+)_Then_(?<expected>\w+)");
			if (!match.Success)
			{
				throw new InvalidOperationException("Invalid test name.");
			}

			var type = match.Groups["type"].Value;
			var fill = match.Groups["fill"].Value;
			var expected = match.Groups["expected"].Value;

			int expectedDelta, tolerance = 0;
			switch (type)
			{
				case "Completed" when expected == "Hold":
					expectedDelta = 150;
					break;

				case "Completed" when expected == "Rollback":
					expectedDelta = 0;
					break;

				case "Paused":
					expectedDelta = 150 / 2;
					tolerance = 50; // We only want to validate that the element is not at 0 or 150
					break;

				case "Canceled":
					expectedDelta = 0;
					break;

				default:
					throw new InvalidOperationException("Invalid test name.");
			}

			Run(_finalStateTransformsTestControl, skipInitialScreenshot: true);

			using var initial = TakeScreenshot("Initial", ignoreInSnapshotCompare: true);
			var initialLocation = _app.WaitForElement($"{type}AnimationHost_{fill}").Single().Rect;

			var scale = ((int)initialLocation.Width) / 50;
			expectedDelta *= scale;
			tolerance *= scale;

			_app.Marked("StartButton").FastTap();
			_app.WaitForDependencyPropertyValue(_app.Marked("Status"), "Text", "Completed");

			// Assert
			using var final = TakeScreenshot("Final", ignoreInSnapshotCompare: true);
			var finalLocation = _app.WaitForElement($"{type}AnimationHost_{fill}").Single().Rect;
			var actualDelta = finalLocation.Y - initialLocation.Y;

			// For some reason, the finalLocation might not reflect the effective location of the control, 
			// instead we rely on pixel validation ...
			// Assert.IsTrue(Math.Abs(actualDelta - expectedDelta) <= tolerance);
			if (expectedDelta > 0)
			{
				ImageAssert.AreNotEqual(initial, final, initialLocation);
			}
			else
			{
				ImageAssert.AreEqual(initial, final, initialLocation);
			}
		}
	}
}
