using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Linq;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlipViewTests
{
	[TestFixture]
	public partial class FlipView_Tests : SampleControlUITestBase
	{
		// Green, Blue, Red, Fuchsia, Orange
		const string ButtonColors = "#008000,#0000FF,#FF0000,#FF00FF,#FFA500";

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)]
		public void FlipView_WithButtons_FlipForward()
		{
			FlipView_WithButtons_BtnNavTest(1, true);
		}

		[Test]
		[AutoRetry(tryCount: 1)]
		[ActivePlatforms(Platform.Browser)]
		public void FlipView_WithButtons_FlipBackward()
		{
			FlipView_WithButtons_BtnNavTest(0, true, false);
		}

		private void FlipView_WithButtons_BtnNavTest(int expectedIndex, params bool[] forwardNavigations)
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			var flipview = _app.Marked("SUT");
			var nextButton = _app.Marked("NextButtonHorizontal");
			var previousButton = _app.Marked("PreviousButtonHorizontal");

			// using tap to ensure navigation buttons are visible, since we cant mouse-over
			_app.WaitForElement(flipview);
			_app.FastTap(flipview);

			// perform navigations
			foreach (var navigation in forwardNavigations.Select((x, i) => new { Step = i, Forward = x }))
			{
				string GetCurrentNavigationContext() =>
					$"step#{navigation.Step} of" + new string(forwardNavigations
						.Select(x => x ? 'N' : 'P').ToArray())
						.Insert(navigation.Step, "*");

				// tap previous or next
				var targetButton = navigation.Forward ? nextButton : previousButton;
				_app.WaitForElement(targetButton, $"Timeout waiting for the {(navigation.Forward ? "next" : "previous")} button in {GetCurrentNavigationContext()}");
				_app.FastTap(targetButton);
			}

			var flipviewRect = _app.GetPhysicalRect(flipview);
			var navigationContext = new string(forwardNavigations.Select(x => x ? 'N' : 'P').ToArray());
			var previousButtonRect = _app.GetPhysicalRect(previousButton);

			// check for selection index && content
			Assert.AreEqual(expectedIndex, flipview.GetDependencyPropertyValue<int>("SelectedIndex"));

			var rect = _app.Query("SUT").Single().Rect;

			// Get rid of Button hover color so we can assert the actual button background.
			_app.TapCoordinates(rect.Right + 5, rect.Bottom + 5);

			using (var screenshot = TakeScreenshot($"Post_{navigationContext}_Navigation", ignoreInSnapshotCompare: true))
			{
				ImageAssert.HasColorAt(
					screenshot,
					flipviewRect.X + previousButtonRect.Width + 2,
					flipviewRect.CenterY,
					ButtonColors.Split(',').ElementAtOrDefault(expectedIndex) ?? throw new IndexOutOfRangeException($"{nameof(expectedIndex)} is out of range")
				);
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void FlipView_WithButtons_FlipForward_Swipe()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			var left = _app.GetPhysicalRect("LeftBorder");

			QueryEx sut = "SUT";

			var sutRect = _app.Query(sut).Single().Rect;

			_app.WaitForElement("Button1");

			_app.DragCoordinates((sutRect.CenterX + sutRect.Right) / 2, sutRect.CenterY, left.CenterX, sutRect.CenterY);

			_app.WaitForElement("Button2");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void FlipView_WithButtons_FlipBackward_Swipe()
		{
			Run("UITests.Windows_UI_Xaml_Controls.FlipView.FlipView_Buttons");

			QueryEx sut = "SUT";

			var right = _app.GetPhysicalRect("RightBorder");
			var left = _app.GetPhysicalRect("LeftBorder");

			var sutRect = _app.Query(sut).Single().Rect;

			_app.WaitForElement("Button1");

			_app.DragCoordinates((sutRect.CenterX + sutRect.Right) / 2, sutRect.CenterY, left.CenterX, sutRect.CenterY);

			_app.WaitForElement("Button2");

			_app.DragCoordinates((sutRect.CenterX + sutRect.X) / 2, sutRect.CenterY, right.CenterX, sutRect.CenterY);

			_app.WaitForElement("Button1");
		}
	}
}
