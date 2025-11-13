using System.Drawing;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FrameworkElementTests
{
	public partial class UnoSamples_Test_FrameworkElement : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void Loaded_Unloaded_Validation()
		{
			Run("UITests.Shared.Windows_UI_Xaml.FrameworkElementTests.LoadEvents");

			var loadedResult = _app.Marked("loadedResult");
			var unloadedResult = _app.Marked("unloadedResult");

			_app.WaitForElement(loadedResult);

			_app.WaitForDependencyPropertyValue(loadedResult, "Text", "[OK] Loaded event received");
			_app.WaitForDependencyPropertyValue(unloadedResult, "Text", "[OK] Unloaded event received");
		}

		[Test]
		[AutoRetry]
		public void ItemsControl_LoadCount()
		{
			Run("UITests.Shared.Windows_UI_Xaml.FrameworkElementTests.ItemsControl_Loaded");

			var result = _app.Marked("result");

			_app.WaitForElement(result);

			_app.WaitForDependencyPropertyValue(result, "Text", "Loaded: 1");
		}

		[Test]
		[AutoRetry]
		public void FrameworkElement_NativeLayout()
		{
			Run("UITests.Shared.Windows_UI_Xaml.FrameworkElementTests.FrameworkElement_NativeLayout");

			var button1 = _app.Marked("button1");
			var button2 = _app.Marked("button2");

			var button1Result = _app.Query(button1).First();
			var button2Result = _app.Query(button2).First();

			button1Result.Rect.Width.Should().Be(button2Result.Rect.Width);
			button1Result.Rect.Height.Should().Be(button2Result.Rect.Height);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Browser, Platform.Android, Platform.iOS)] // Not supported on Skia yet.
		public void FrameworkElement_BackgroundSizing()
		{
			Run("UITests.Windows_UI_Xaml.FrameworkElementTests.DynamicBackgroundSizing");

			_app.WaitForElement("Border1");

			using var scrn = TakeScreenshot("Rendered", true);

			var semiRed = Color.FromArgb(0xFF, 0xFF, 0x7F, 0x7F);
			var semiRedAndGreen = Color.FromArgb(0xFF, 0x80, 0x40, 0x00);

			AssertHasColorAtCenterAndBorder("Border1", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("Border2", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("Border3", Color.Green, semiRedAndGreen);
			AssertHasColorAtCenterAndBorder("Border4", Color.Green, semiRed);

			AssertHasColorAtCenterAndBorder("ContentPresenter1", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("ContentPresenter2", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("ContentPresenter3", Color.Green, semiRedAndGreen);
			AssertHasColorAtCenterAndBorder("ContentPresenter4", Color.Green, semiRed);

			AssertHasColorAtCenterAndBorder("Grid1", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("Grid2", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("Grid3", Color.Green, semiRedAndGreen);
			AssertHasColorAtCenterAndBorder("Grid4", Color.Green, semiRed);

			AssertHasColorAtCenterAndBorder("RelativePanel1", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("RelativePanel2", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("RelativePanel3", Color.Green, semiRedAndGreen);
			AssertHasColorAtCenterAndBorder("RelativePanel4", Color.Green, semiRed);

			AssertHasColorAtCenterAndBorder("StackPanel1", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("StackPanel2", Color.Green, semiRed);
			AssertHasColorAtCenterAndBorder("StackPanel3", Color.Green, semiRedAndGreen);
			AssertHasColorAtCenterAndBorder("StackPanel4", Color.Green, semiRed);

			void AssertHasColorAtCenterAndBorder(string element, Color centerColor, Color borderColor)
			{
				var rect = _app.GetPhysicalRect(element);
				var borderCenterOffset = rect.Width / 50f * 7.5f;

				const byte tolerance = 6;

				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.CenterY, centerColor, tolerance);
				ImageAssert.HasColorAt(scrn, rect.X + borderCenterOffset, rect.CenterY, borderColor, tolerance);
				ImageAssert.HasColorAt(scrn, rect.Right - borderCenterOffset, rect.CenterY, borderColor, tolerance);
				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.Y + borderCenterOffset, borderColor, tolerance);
				ImageAssert.HasColorAt(scrn, rect.CenterX, rect.Bottom - borderCenterOffset, borderColor, tolerance);
			}
		}
	}
}
