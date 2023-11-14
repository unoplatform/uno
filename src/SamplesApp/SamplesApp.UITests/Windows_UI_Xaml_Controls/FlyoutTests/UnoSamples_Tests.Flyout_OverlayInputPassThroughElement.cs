using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests;

#if __SKIA__
[TestFixture]
partial class Flyout_Tests : SampleControlUITestBase
#else
partial class Flyout_Tests : PopupUITestBase
#endif
{
#if !IS_RUNTIME_UI_TESTS
	[Test][AutoRetry] public Task FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough_woOn() => FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough("woOn");
	[Test][AutoRetry] public Task FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough_woOff() => FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough("woOff");
	[Test][AutoRetry] public Task FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough_woAuto() => FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough("woAuto");
#else
	[Test]
	[AutoRetry]
	[DataRow("woOn")]
	[DataRow("woOff")]
	[DataRow("woAuto")]
#endif
	public async Task FlyoutTest_When_NoOverlayInputPassThroughElement_Then_DontPassThrough(string testCase)
	{
		await RunAsync("UITests.Shared.Windows_UI_Xaml_Controls.Flyout.Flyout_OverlayInputPassThroughElement");

		App.WaitForElement(testCase);

		var pink = App.GetLogicalRect("PinkBorder");
		var orange = App.GetLogicalRect("OrangeButton");
		var blocking = App.GetLogicalRect("BlockingLayer");
		var page = App.GetLogicalRect("PageRoot");

		// When tap on pink
		AssertDoesNotContains(await RunTest(testCase, blocking.X - 10, pink.CenterY), "PinkBorder");

		// When tap on orange
		AssertDoesNotContains(await RunTest(testCase, blocking.X + 10, orange.CenterY), "OrangeButton");

		// When tap on blocking at border level
		AssertDoesNotContains(await RunTest(testCase, blocking.X - 10, pink.CenterY), "PinkBorder");

		// When tap on blocking at button level
		AssertDoesNotContains(await RunTest(testCase, blocking.X + 10, orange.CenterY), "OrangeButton");

		// When tap out of the InputPassThroughElement
		AssertDoesNotContains(await RunTest(testCase, page.Y - 10, page.CenterY), "PageRoot", ignoreTapped: true);
	}

#if !IS_RUNTIME_UI_TESTS
	[Test][AutoRetry] public Task FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough_withOn() => FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough("withOn");
	[Test][AutoRetry] public Task FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough_withOff() => FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough("withOff");
	[Test][AutoRetry] public Task FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough_withAuto() => FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough("withAuto");
#else
	[Test]
	[AutoRetry]
	[DataRow("withOn")]
	[DataRow("withOff")]
	[DataRow("withAuto")]
#endif
	public async Task FlyoutTest_When_OverlayInputPassThroughElement_Then_PassThrough(string testCase)
	{
		await RunAsync("UITests.Shared.Windows_UI_Xaml_Controls.Flyout.Flyout_OverlayInputPassThroughElement");

		App.WaitForElement(testCase);

		var pink = App.GetLogicalRect("PinkBorder");
		var orange = App.GetLogicalRect("OrangeButton");
		var blocking = App.GetLogicalRect("BlockingLayer");
		var page = App.GetLogicalRect("PageRoot");

		// When tap on pink
		AssertContains(await RunTest(testCase, blocking.X - 10, pink.CenterY), "PinkBorder");

		// When tap on orange
		AssertDoesNotContains(await RunTest(testCase, blocking.X + 10, orange.CenterY), "OrangeButton");

		// When tap on blocking at border level
		AssertContains(await RunTest(testCase, blocking.X - 10, pink.CenterY), "PinkBorder");

		// When tap on blocking at button level
		AssertDoesNotContains(await RunTest(testCase, blocking.X + 10, orange.CenterY), "OrangeButton");

		// When tap out of the InputPassThroughElement
		AssertDoesNotContains(await RunTest(testCase, page.Y - 10, page.CenterY), "PageRoot", ignoreTapped: true);
	}

	private async Task<(string pressed, string tapped)> RunTest(string testCase, double x, double y)
	{
		App.FastTap("ClearLogButton");
		App.FastTap(testCase);

		await App.WaitForDependencyPropertyValueAsync(App.Marked("IsFlyoutOpened"), "Text", "True");

		App.TapCoordinates((float)x, (float)y);

		await App.WaitForDependencyPropertyValueAsync(App.Marked("IsFlyoutOpened"), "Text", "False");

		var pressed = App.Marked("PressedUnhandledOutput").GetDependencyPropertyValue<string>("Text");
		var tapped = App.Marked("TappedOutput").GetDependencyPropertyValue<string>("Text");

		return (pressed, tapped);
	}

	private void AssertContains((string pressed, string tapped) result, string element, bool ignoreTapped = false)
	{
		Assert.IsTrue(result.pressed.Contains(element, StringComparison.OrdinalIgnoreCase), $"{element} should have received pressed event");
		if (!ignoreTapped)
		{
			Assert.IsTrue(result.tapped.Contains(element, StringComparison.OrdinalIgnoreCase), $"{element} should have received tapped/clicked event");
		}
	}

	private void AssertDoesNotContains((string pressed, string tapped) result, string element, bool ignoreTapped = false)
	{
		Assert.IsFalse(result.pressed.Contains(element, StringComparison.OrdinalIgnoreCase), $"{element} should **not** have received pressed event");
		if (!ignoreTapped)
		{
			Assert.IsFalse(result.tapped.Contains(element, StringComparison.OrdinalIgnoreCase), $"{element} should **not** have received tapped/clicked event");
		}
	}
}
