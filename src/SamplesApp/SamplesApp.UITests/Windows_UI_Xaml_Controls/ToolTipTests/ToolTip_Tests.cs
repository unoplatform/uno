using System;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ToolTipTests
{
	[TestFixture]
	public partial class ToolTip_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // Android is disabled https://github.com/unoplatform/uno/issues/1630
		public void NoToolTip_On_Open()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ToolTip.TextOnlyToolTipSample");

			_app.Marked("richToolTip").FirstResult().Should().BeNull("Initial state");

			_app.Marked("rect2").FastTap();

			_app.Marked("richToolTip").FirstResult().Should().BeNull("Right after first click");

			Thread.Sleep(1200);

			_app.Marked("rect1").FastTap();

			Thread.Sleep(200);

			_app.Marked("rect2").FastTap();

			_app.Marked("richToolTip").FirstResult().Should().BeNull("Right after second click");

			Thread.Sleep(1200);

			_app.Marked("richToolTip").FirstResult().Should().NotBeNull("1.2s after click, should have a tooltip");

			TakeScreenshot("opened-tooltip");

			Thread.Sleep(7200);

			_app.Marked("richToolTip").FirstResult().Should().BeNull("tooltip delay expired");

			_app.Marked("rect1").FastTap();

			Thread.Sleep(1200);

			TakeScreenshot("opened-textonly-tooltip");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Only WASM supports mouse-based tests for now
		public void ToolTip_Large_Text()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ToolTip.ToolTip_Long_Text");

			const string ButtonWithTooltip = "ButtonWithTooltip";
			const string BorderInsideToolTip = "BorderInsideToolTip";

			_app.WaitForElement(ButtonWithTooltip);
			var buttonRect = _app.GetRect(ButtonWithTooltip);

			var upperRect = _app.GetRect("UpperLocator");
			var lowerRect = _app.GetRect("LowerLocator");

			using var before = TakeScreenshot("Before", ignoreInSnapshotCompare: true);

			var lowerHoverY = buttonRect.Bottom - buttonRect.Height / 10;
			_app.TapCoordinates(buttonRect.CenterX, lowerHoverY);

			_app.WaitForElement(BorderInsideToolTip);

			using var lowerToolTip = TakeScreenshot("Lower ToolTip");

			ImageAssert.AreEqual(before, lowerToolTip, lowerRect);
			ImageAssert.AreEqual(before, lowerToolTip, upperRect); // ToolTip should be just above mouse, shouldn't extend above Button bounds
			ImageAssert.AreNotEqual(before, lowerToolTip, buttonRect);

			_app.FastTap("DummyButton");

			_app.WaitForNoElement(BorderInsideToolTip);

			var upperHoverY = buttonRect.Y + buttonRect.Height / 10;
			_app.TapCoordinates(buttonRect.CenterX, upperHoverY);

			_app.WaitForElement(BorderInsideToolTip);

			using var upperToolTip = TakeScreenshot("Upper ToolTip");

			ImageAssert.AreEqual(before, upperToolTip, lowerRect);
			ImageAssert.AreNotEqual(before, upperToolTip, upperRect); // Mouse is close to top of button, ToolTip should extend above Button bounds
			ImageAssert.AreNotEqual(before, upperToolTip, buttonRect);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser)] // Only WASM supports mouse-based tests for now
		public void ToolTip_WhenHostCollapsed_Test()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ToolTip.ToolTip_CollapsedHost");
			var hideToggle = _app.Marked("ToggleHide"); // ToggleButton "Hide" that will hide itself
			var tooltipText = _app.Marked("TooltipText"); // TextBlock within ToggleHide's Tooltip
			const int TooltipShowDelay = 1000; // FeatureConfiguration.ToolTip.ShowDelay (in ms)

			_app.WaitForElement(hideToggle);

			hideToggle.Tap();

			// normally, as the cursor enter hideToggle's hitbox to press it,
			// the tooltip will start to appears after 1s (TooltipShowDelay)
			// and, it will remain visible for another 1s.
			// (moving within the hitbox will reset the remaining duration back to 1s again, but we are not)
			_app.Wait(TimeSpan.FromMilliseconds(TooltipShowDelay + 300));

			// however, since we made the host control to disappear (Visibility=Collapsed),
			// its tooltip should disappear as well.
			tooltipText.ShouldNotBeVisible();
		}
	}
}
