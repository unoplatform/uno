using System;
using System.IO;
using NUnit.Framework;
using SamplesApp.UITests.Extensions;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media_Imaging
{
	[TestFixture]
	[ActivePlatforms(Platform.iOS, Platform.Android)]
	public partial class RenderTargetBitmap_Tests : SampleControlUITestBase
	{
		private const int TestTimeout = 7 * 60 * 1000;
		[Test]
		[AutoRetry]
		[Timeout(TestTimeout)]
		public void When_Render_Border()
			=> Validate("Border");

		public void Validate(string controlName, PixelTolerance? tolerance = null)
		{
			Run("UITests.Windows_UI_Xaml_Media_Imaging.RenderTargetBitmaps", skipInitialScreenshot: true);

			var ctrl = new QueryEx(q => q.Marked("_renderTargetTestRoot"));

			var expectedDirectory = Path.Combine(
				TestContext.CurrentContext.TestDirectory,
				nameof(Windows_UI_Xaml_Media_Imaging) + "/RenderTargetBitmap_Tests_ExpectedResults");
			var actualDirectory = Path.Combine(
				TestContext.CurrentContext.WorkDirectory,
				nameof(Windows_UI_Xaml_Media_Imaging),
				nameof(RenderTargetBitmap_Tests),
				controlName);

			tolerance = tolerance ?? (new PixelTolerance()
				.WithColor(132) // We are almost only trying to detect edges
				.WithOffset(3, 3, LocationToleranceKind.PerPixel)
				.Discrete(2));

			ctrl.SetDependencyPropertyValue("RunTest", controlName);
			_app.WaitFor(() => !string.IsNullOrWhiteSpace(ctrl.GetDependencyPropertyValue<string>("TestResult"))
				, timeout: TimeSpan.FromMinutes(1));

			var testResultsRaw = ctrl.GetDependencyPropertyValue<string>("TestResult");

			var testResults = testResultsRaw.Split(';');
			Assert.That(testResults.Length, Is.EqualTo(2));

			var isSuccess = testResults[0] == "SUCCESS";
			var data = Convert.FromBase64String(testResults[1]);

			Assert.IsNotEmpty(data, "Invalid data");

			var target = Path
				.Combine(actualDirectory, controlName + (isSuccess ? ".png" : ".txt"))
				.GetNormalizedLongPath();
			var targetFile = new FileInfo(target);

			targetFile.Directory.Create();
			File.WriteAllBytes(target, data);
			SetOptions(targetFile, new ScreenshotOptions { IgnoreInSnapshotCompare = true });
			TestContext.AddTestAttachment(target, controlName);

			if (!isSuccess)
			{
				Assert.Fail($"No test result for {controlName}.");
			}

			var expected = new FileInfo(Path.Combine(expectedDirectory, $"{controlName}.png"));
			if (!expected.Exists)
			{
				Assert.Fail($"Expected screenshot does not exists ({expected.FullName})");
			}

			var scale = _app.GetDisplayScreenScaling();
			ImageAssert.AreEqual(expected, ImageAssert.FirstQuadrant,
				targetFile, ImageAssert.FirstQuadrant,
				scale, tolerance.Value);
		}
	}
}
