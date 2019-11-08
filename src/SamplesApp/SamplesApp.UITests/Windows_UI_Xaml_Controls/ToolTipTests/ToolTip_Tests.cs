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

			_app.Marked("rect2").Tap();

			_app.Marked("richToolTip").FirstResult().Should().BeNull("Right after first click");

			Thread.Sleep(1200);

			_app.Marked("rect1").Tap();

			Thread.Sleep(200);

			_app.Marked("rect2").Tap();

			_app.Marked("richToolTip").FirstResult().Should().BeNull("Right after second click");

			Thread.Sleep(1200);

			_app.Marked("richToolTip").FirstResult().Should().NotBeNull("1.2s after click, should have a tooltip");

			TakeScreenshot("opened-tooltip");

			Thread.Sleep(7200);

			_app.Marked("richToolTip").FirstResult().Should().BeNull("tooltip delay expired");

			_app.Marked("rect1").Tap();

			Thread.Sleep(1200);

			TakeScreenshot("opened-textonly-tooltip");
		}
	}
}
