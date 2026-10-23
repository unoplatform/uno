#nullable enable

using System;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using AppExtensions = SamplesApp.UITests.Extensions.AppExtensions;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Imaging;

[TestFixture]
[ActivePlatforms(Platform.Android)]
[NonParallelizable]
public partial class ScreenshotBridge_Tests : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	public void When_Screenshot_Is_Requested_Then_Polling_Returns_Png_Without_Blocking_The_Activity()
	{
		Run(
			"UITests.Shared.Windows_UI.Xaml_Automation.AutomationProperties_AutomationId",
			waitForSampleControl: false,
			skipInitialScreenshot: true);

		Assert.That(
			_app.InvokeGeneric("browser:SampleRunner|GetScreenshot", "0")?.ToString(),
			Is.EqualTo("pending"),
			"The exported method must return before waiting for PixelCopy.");

		var result = AppExtensions.GetInAppScreenshotData(
			() => _app.InvokeGeneric("browser:SampleRunner|GetScreenshot", "0")?.ToString(),
			TimeSpan.FromSeconds(15));

		var bytes = Convert.FromBase64String(result);
		CollectionAssert.AreEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes.Take(8).ToArray());

		using var screenshot = TakeScreenshot("ScreenshotBridge_SecondRequest", ignoreInSnapshotCompare: true);
	}
}
