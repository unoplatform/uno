using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.WUXProgressRingTests
{
	public class UnoSamples_WUXProgressRing_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ProgressRing_IsEnabled_Running()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ProgressRing.WindowsProgressRing_GH1220");

			var isEnabledTest = _app.Marked("isEnabledTest");
			var placeHolder = _app.Marked("placeHolder");

			var before = TakeScreenshot("before", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });
			var placeholderRect = _app.Query(placeHolder).First().Rect;
			var isEnabledTestRect = _app.Query(isEnabledTest).First().Rect;

			ImageAssert.AreNotEqual(before, placeholderRect, before, isEnabledTestRect);

			isEnabledTest.SetDependencyPropertyValue("IsEnabled", "True");

			var after = TakeScreenshot("after", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });

			ImageAssert.AreNotEqual(before, placeholderRect, after, isEnabledTestRect);
		}

		[Test]
		[AutoRetry()]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void ProgressRing_Visibility_Collapsed()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ProgressRing.WindowsProgressRing_GH1220");

			var isVisible = _app.Marked("isVisible");
			var visibilityTest = _app.Marked("visibilityTest");
			var placeHolder = _app.Marked("placeHolder");
			var collapsedTestBorder = _app.Marked("collapsedTestBorder");

			var placeholderRect = _app.Query(placeHolder).First().Rect;
			var collapsedTestBorderRect = _app.Query(collapsedTestBorder).FirstOrDefault()?.Rect;

			Assert.AreEqual(0f, collapsedTestBorderRect?.Height ?? 0f);

			isVisible.SetDependencyPropertyValue("IsChecked", "True");

			collapsedTestBorderRect = _app.Query(collapsedTestBorder).First().Rect;
			Assert.AreNotEqual(0f, collapsedTestBorderRect.Height);

			var after = TakeScreenshot("after", new ScreenshotOptions() { IgnoreInSnapshotCompare = true });

			var visibilityTestRect = _app.Query(visibilityTest).First().Rect;
			ImageAssert.AreNotEqual(after, placeholderRect, after, visibilityTestRect);
		}
	}
}
