using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests
{
	[TestFixture]
	public partial class Flyout_Tests : PopupUITestBase
	{
		[Test]
		[AutoRetry]
		[Ignore("Not available yet")]
		public void FlyoutTest_BottomPlacement_WithSmallerAnchor_DoesntDefaultToFull()
		{
			Run("Uno.UI.Samples.Content.UITests.Flyout.Flyout_Simple");

			_app.WaitForElement(_app.Marked("FlyoutToBottomButton"));

			// Can't find popup that is outside of page.
			//var button = _app.Marked("FlyoutToBottomButton");
			//_app.FastTap(button);

			//_app.Wait(1);
			//_app.WaitForElement(_app.Marked("BottomFlyout"));
			//var flyoutRect = _app.Marked("BottomFlyout").FirstResult().Rect;

			//// Assert initial state 
			//Assert.AreEqual("0", flyoutRect.X.ToString());
		}

		[Test]
		[AutoRetry]
        public void FlyoutTest_DataBoundButton_CommandExecutes()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Flyout.Flyout_ButtonInContent");

			var flyoutButton = _app.Marked("FlyoutButton");
			var dataBoundButton = _app.Marked("DataBoundButton");
			var dataBoundText = _app.Marked("DataBoundText");

			_app.WaitForElement(flyoutButton);
			_app.FastTap(flyoutButton);

			_app.WaitForElement(dataBoundButton);
			Assert.AreEqual("Button not clicked", dataBoundText.GetText());

			_app.FastTap(dataBoundButton);
			Assert.AreEqual("Button was clicked", dataBoundText.GetText());
			
			_app.TapCoordinates(10, 100);
		}

		[Test]
		[AutoRetry]
		public void FlyoutTest_Target()
		{
			Run("Uno.UI.Samples.Content.UITests.Flyout.Flyout_Target");

			var result = _app.Marked("result");
			var innerContent = _app.Marked("innerContent");
			var target1 = _app.Marked("target1");
			var target2 = _app.Marked("target2");
			var flyoutFull = _app.Marked("flyoutFull");

			_app.WaitForElement(result);

			{
				var target1Result = _app.WaitForElement(target1).First();

				_app.FastTap(target1);

				var innerContentResult = _app.WaitForElement(innerContent).First();

				Assert.IsTrue(target1Result.Rect.X <= innerContentResult.Rect.X);
				Assert.IsTrue(target1Result.Rect.Width > innerContentResult.Rect.Width);

				_app.TapCoordinates(50, 100);
			}

			{
				var target2Result = _app.WaitForElement(target2).First();

				_app.FastTap(target2);

				var innerContentResult = _app.WaitForElement(innerContent).First();

				Assert.IsTrue(target2Result.Rect.X <= innerContentResult.Rect.X);
				Assert.IsTrue(target2Result.Rect.Width > innerContentResult.Rect.Width);

				_app.TapCoordinates(50, 100);
			}

			{
				_app.FastTap(flyoutFull);

				var innerContentResult = _app.WaitForElement(innerContent).First();

				var rect = base.GetScreenDimensions();

				Assert.AreEqual(innerContentResult.Rect.CenterX, rect.CenterX, 1);

				if (AppInitializer.GetLocalPlatform() == Platform.Browser)
				{
					// Flyout positioning does not take proper app bar positioning yet.
					Assert.AreEqual(innerContentResult.Rect.CenterY, rect.CenterY, 1);
				}

				_app.TapCoordinates(10, 100);
			}
		}

		[Test]
		[AutoRetry]
		public void FlyoutTest_Unloaded()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Flyout.Flyout_Unloaded");

			var outerButton = _app.Marked("outerButton");
			var innerButton = _app.Marked("innerButton");

			_app.FastTap(outerButton);
			_app.WaitForElement(innerButton);

			_app.FastTap(innerButton);

			_app.WaitForNoElement(outerButton);
			_app.WaitForNoElement(innerButton);
		}

		[Test]
		[AutoRetry]
		public void FlyoutTest_Simple_FlyoutsCanBeDismissed()
		{
			Run("Uno.UI.Samples.Content.UITests.Flyout.Flyout_Simple");

			var majorStepIndex = 0;
			var testableFlyoutButtons = new string[]
			{
				"LeftFlyoutButton",
				"RightFlyoutButton",
				"BottomFlyoutButton",
				"TopFlyoutButton",
				//"FullFlyoutButton", // unclosable without native back button/gesture
				"CenteredFullFlyoutButton",
				//"FullOverlayFlyoutButton", // unclosable without native back button/gesture
				"WithOffsetFlyoutButton",
			}.ToDictionary(x => x, x => _app.Marked(x));

			// initial state
			_app.WaitForElement(testableFlyoutButtons.First().Value);
			var initialScreenshot = TakeScreenshot($"{majorStepIndex++} Initial State", ignoreInSnapshotCompare: true);

			var dismissArea = GetDismissAreaCenter();
			foreach (var button in testableFlyoutButtons)
			{
				// show flyout
				button.Value.Tap();
				var flyoutOpenedScreenshot = TakeScreenshot($"{majorStepIndex} {button.Key} 0 Opened", ignoreInSnapshotCompare: true);

				// dismiss flyout
				_app.TapCoordinates(dismissArea.X, dismissArea.Y);
				var flyoutDismissedScreenshot = TakeScreenshot($"{majorStepIndex} {button.Key} 1 Dismissed", ignoreInSnapshotCompare: true);

				// compare
				ImageAssert.AreNotEqual(flyoutOpenedScreenshot, initialScreenshot);
				ImageAssert.AreEqual(flyoutDismissedScreenshot, initialScreenshot);

				majorStepIndex++;
			}

			(float X, float Y) GetDismissAreaCenter()
			{
				// the dismiss area is safe to click, since no flyout should block this area (except: FullFlyoutButton, FullOverlayFlyoutButton)
				/* [LeftFlyoutButton]
				 * ...
				 * [FullOverlayFlyoutButton]
				 *         (dismiss area)
				 *       [WithOffsetFlyoutButton margin=100] */
				var rect1 = _app.GetRect("FullOverlayFlyoutButton");
				var rect2 = _app.GetRect("WithOffsetFlyoutButton");

				return (rect2.CenterX, (rect1.Bottom + rect2.Y) / 2);
			}
		}
	}
}
