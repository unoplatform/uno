using System;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.ProgressRingTests
{
#if !HAS_UNO_WINUI
	[Ignore("Lottie for SkiaSharp is not available for UWP")]
#endif
	public partial class Given_DeterminateProgressRings : SampleControlUITestBase
	{
		private const string red = "#FF0000";
		private const string green = "#008000";

		[Test]
		[AutoRetry]
		[TestCase(0, new[] { red, red, red, red })]
		[TestCase(25, new[] { red, green, red, red })]
		[TestCase(50, new[] { red, green, green, red })]
		[TestCase(75, new[] { red, green, green, green })]
		[TestCase(100, new[] { green, green, green, green })]
		public void Detereminate_ProgressRing_Validation(float value, string[] colors)
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.ProgressRing.WinUIDeterminateProgressRing");

			_app.WaitForElement("ProgressRing");

			var topLeftTargetRect = _app.GetPhysicalRect("TopLeftTarget");
			var topRightTargetRect = _app.GetPhysicalRect("TopRightTarget");
			var bottomLeftTargetRect = _app.GetPhysicalRect("BottomLeftTarget");
			var bottomRightTargetRect = _app.GetPhysicalRect("BottomRightTarget");

			SetComboBox("ProgressValue", value.ToString());

			_app.Wait(TimeSpan.FromSeconds(1 + value / 25f)); //Wait for animations to finish

			using var snapshot = TakeScreenshot($"Progress-Ring-Value-{value}");

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At($"top-left-{value}-progress", topLeftTargetRect.CenterX, topLeftTargetRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(colors[0]),
				ExpectedPixels
					.At($"top-right-{value}-progress", topRightTargetRect.CenterX, topRightTargetRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(colors[1]),
				ExpectedPixels
					.At($"bottom-right-{value}-progress", bottomRightTargetRect.CenterX, bottomRightTargetRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(colors[2]),
				ExpectedPixels
					.At($"bottom-left-{value}-progress", bottomLeftTargetRect.CenterX, bottomLeftTargetRect.CenterY)
					.WithPixelTolerance(1, 1)
					.Pixel(colors[3])
			);
		}

		private void SetComboBox(string comboBoxName, string item)
		{
			Console.WriteLine("Setting '" + comboBoxName + "' to '" + item + "'");
			var comboBox = _app.Marked(comboBoxName);
			var _ = comboBox.SetDependencyPropertyValue("SelectedItem", item);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)] // ImageAssert.AreEqual @ line 98 https://github.com/unoplatform/uno/issues/9080
		public void TestProgressRing_InitialState()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.ProgressRing.WinUIProgressRing_Features");

			_app.WaitForElement("dyn8");

			using var screenshot = TakeScreenshot("scrn", ignoreInSnapshotCompare: true);

			_app.Marked("dynamicValue").SetDependencyPropertyValue("Value", "90");
			_app.Marked("dynamicValue").SetDependencyPropertyValue("Value", "30");

			using var screenshot2 = TakeScreenshot("scrn2", ignoreInSnapshotCompare: true);

			var rects = Enumerable
				.Range(1, 8)
				.Select(i => "dyn" + i)
				.Select(marked => _app.GetPhysicalRect(marked))
				.ToArray();

			var i = 1;
			foreach (var rect in rects)
			{
				ImageAssert.AreNotEqual(screenshot2, screenshot, rect);
				_app.Marked("dyn" + i++).SetDependencyPropertyValue("Opacity", "0");
			}

			using var screenshot3 = TakeScreenshot("scrn3", ignoreInSnapshotCompare: true);

			foreach (var rect in rects)
			{
				// Ensure initial state is not empty
				ImageAssert.AreNotEqual(screenshot3, screenshot, rect);
			}
		}
	}
}
