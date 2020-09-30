using System;
using System.Collections.Generic;
using System.Drawing;
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
	[ActivePlatforms(Platform.Android, Platform.Browser)] // Disabled for iOS: https://github.com/unoplatform/uno/issues/1955
	public partial class DoubleAnimation_Tests : SampleControlUITestBase
	{
		private const string _finalStateOpacityTestControl = "UITests.Windows_UI_Xaml_Media_Animation.DoubleAnimation_FinalState_Opacity";

		[Test] [AutoRetry] public void When_Opacity_Completed_With_FillBehaviorStop_Then_Rollback() => TestOpacityFinalState();
		[Test] [AutoRetry] public void When_Opacity_Completed_With_FillBehaviorHold_Then_Hold() => TestOpacityFinalState();
		[Test] [AutoRetry] public void When_Opacity_Paused_With_FillBehaviorStop_Then_Hold() => TestOpacityFinalState();
		[Test] [AutoRetry] public void When_Opacity_Paused_With_FillBehaviorHold_Then_Hold() => TestOpacityFinalState();
		[Test] [AutoRetry] public void When_Opacity_Canceled_With_FillBehaviorStop_Then_Rollback() => TestOpacityFinalState();
		[Test] [AutoRetry] public void When_Opacity_Canceled_With_FillBehaviorHold_Then_Rollback() => TestOpacityFinalState();

		private void TestOpacityFinalState([CallerMemberName] string testName = null)
		{
			var match = Regex.Match(testName, @"When_Opacity_(?<type>\w+)_With_FillBehavior(?<fill>\w+)_Then_(?<expected>\w+)");
			if (!match.Success)
			{
				throw new InvalidOperationException("Invalid test name.");
			}

			var type = match.Groups["type"].Value;
			var fill = match.Groups["fill"].Value;
			var expected = match.Groups["expected"].Value;

			bool isSame = false, isGray = false, isDifferent = false;
			switch (type)
			{
				case "Completed" when expected == "Hold":
					isGray = true;
					break;

				case "Completed" when expected == "Rollback":
					isSame = true;
					break;

				case "Paused":
					isDifferent = true;
					break;

				case "Canceled":
					isSame = true;
					break;

				default:
					throw new InvalidOperationException("Invalid test name.");
			}

			Run(_finalStateOpacityTestControl, skipInitialScreenshot: true);

			using var initial = TakeScreenshot("Initial", ignoreInSnapshotCompare: true);
			var element = _app.WaitForElement($"{type}AnimationHost_{fill}").Single().Rect;

			_app.Marked("StartButton").FastTap();
			_app.WaitForDependencyPropertyValue(_app.Marked("Status"), "Text", "Completed");

			// Assert
			using var final = TakeScreenshot("Final", ignoreInSnapshotCompare: true);

			if (isSame)
			{
				ImageAssert.AreEqual(initial, final, element);
			}
			else if (isGray)
			{
				ImageAssert.HasColorAt(final, element.CenterX, element.CenterY, Color.LightGray);
			}
			else if (isDifferent)
			{
				ImageAssert.AreNotEqual(initial, final, element);
				ImageAssert.DoesNotHaveColorAt(final, element.CenterX, element.CenterY, Color.LightGray);
			}
		}
	}
}
