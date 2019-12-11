using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[TestFixture]
	public class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_WhenSync_RunNormalAndCompletesWithNonIntermediate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_UpdatesMode");

			var sut = _app.WaitForElement(_app.Marked("_scroll")).Single();
			var selectMode = _app.Marked("_setSync");
			var validate = _app.Marked("_validate");
			var result = _app.Marked("_result");

			selectMode.Tap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.Tap();
			TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_WhenAsync_RunIdleAndCompletesWithNonIntermediate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_UpdatesMode");

			var sut = _app.WaitForElement(_app.Marked("_scroll")).Single();
			var selectMode = _app.Marked("_setAsync");
			var validate = _app.Marked("_validate");
			var result = _app.Marked("_result");

			selectMode.Tap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.Tap();
			TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}
	}
}
