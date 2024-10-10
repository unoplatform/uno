using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;
using Query = System.Func<Uno.UITest.IAppQuery, Uno.UITest.IAppQuery>;

namespace SamplesApp.UITests.Microsoft_UI_Xaml_Controls.ColorPickerTests
{
	public partial class Given_ColorPicker : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void When_Add_And_Remove_From_VisualTree()
		{
			Run("UITests.Microsoft_UI_Xaml_Controls.ColorPickerTests.ColorPickerInElevatedView", skipInitialScreenshot: true);

			var focusButton = _app.Marked("focus");
			var openButton = _app.Marked("open");
			var closeButton = _app.Marked("close");
			_app.Tap(focusButton);
			var before = _app.Screenshot("before");

			_app.Tap(openButton);
			_app.WaitForElement(closeButton);
			_app.Tap(closeButton);
			_app.WaitForNoElement(closeButton);
			_app.Tap(focusButton);
			var after = _app.Screenshot("after");

			ImageAssert.AreEqual(before, after);
		}
	}
}
