#nullable enable

using System;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Imaging;

[TestFixture]
public partial class ScreenshotPolling_Tests
{
	private const string Capture = "iVBORw0KGgo=";

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void When_First_Capture_Fails_Then_A_Later_Success_Is_Not_Implicitly_Retried(
		bool pendingFirst, bool captureTimedOut)
	{
		var calls = 0;
		var failureCall = pendingFirst ? 2 : 1;
		Exception original = captureTimedOut
			? new TimeoutException("Original capture timed out.")
			: new InvalidOperationException("Original PixelCopy capture failed.");

		string GetScreenshot()
		{
			calls++;
			if (pendingFirst && calls == 1)
			{
				return "pending";
			}
			if (calls == failureCall)
			{
				throw original;
			}
			return Capture;
		}

		var error = Assert.Catch<Exception>(() =>
			AppExtensions.GetInAppScreenshotData(GetScreenshot, TimeSpan.FromSeconds(1)));

		Assert.AreSame(original, error);
		Assert.AreEqual(failureCall, calls, "The failed request must not be polled into a second capture.");
		Assert.AreEqual(Capture,
			AppExtensions.GetInAppScreenshotData(GetScreenshot, TimeSpan.FromSeconds(1)));
		Assert.AreEqual(failureCall + 1, calls, "A separate explicit request may capture successfully.");
	}

	[Test]
	public void When_Capture_Is_Pending_Then_Only_Pending_Responses_Are_Retried()
	{
		var calls = 0;
		var result = AppExtensions.GetInAppScreenshotData(
			() => ++calls < 3 ? "pending" : Capture,
			TimeSpan.FromSeconds(1));

		Assert.AreEqual(Capture, result);
		Assert.AreEqual(3, calls);
	}

	[Test]
	public void When_Pending_Capture_Exceeds_Deadline_Then_Polling_Stops()
	{
		var calls = 0;
		Assert.Throws<TimeoutException>(() =>
			AppExtensions.GetInAppScreenshotData(() =>
			{
				calls++;
				return "pending";
			}, TimeSpan.Zero));

		Assert.AreEqual(1, calls);
	}

	[TestCase(null)]
	[TestCase("")]
	public void When_Bridge_Returns_No_Result_Then_It_Is_Not_Retried(string? response)
	{
		var calls = 0;
		Assert.Throws<InvalidOperationException>(() =>
			AppExtensions.GetInAppScreenshotData(() =>
			{
				calls++;
				return response!;
			}, TimeSpan.FromSeconds(1)));

		Assert.AreEqual(1, calls);
	}
}
