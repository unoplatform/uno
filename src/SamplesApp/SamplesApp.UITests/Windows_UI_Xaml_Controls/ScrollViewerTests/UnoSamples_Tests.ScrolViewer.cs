using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Drawing;
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

			selectMode.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.FastTap();
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

			selectMode.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.X + 10, sut.Rect.Y + 110, sut.Rect.X + 10, sut.Rect.Y + 10);

			validate.FastTap();
			TakeScreenshot("Result", ignoreInSnapshotCompare: true);
			_app.WaitForDependencyPropertyValue(result, "Text", "SUCCESS");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)] // IsIntermediate is not supported on Wasm yet
		public void ScrollViewer_Clipping()
		{
			Run("UITests.Windows_UI_Xaml_Controls.ScrollViewerTests.ScrollViewer_Clipping");

			var sut = _app.WaitForElement(_app.Marked("scrollViewer1")).Single();
			var updateButton = _app.Marked("UpdateButton");

			var updateButtonRect = _app.WaitForElement(_app.Marked("intentionallyBlank")).Single().Rect;

			updateButton.FastTap();

			//Drag upward
			_app.DragCoordinates(sut.Rect.CenterX, sut.Rect.CenterY, sut.Rect.CenterX, sut.Rect.CenterY - (sut.Rect.Height / 2));

			var res = TakeScreenshot("Result", ignoreInSnapshotCompare: true);

			ImageAssert.AreNotEqual(
				expected: res,
				expectedRect: new Rectangle((int)sut.Rect.X, (int)sut.Rect.Bottom - 20, (int)updateButtonRect.Width, (int)updateButtonRect.Height),
				actual: res,
				actualRect: new Rectangle((int)updateButtonRect.X, (int)updateButtonRect.Y, (int)updateButtonRect.Width, (int)updateButtonRect.Height)
			);
		}
	}
}
