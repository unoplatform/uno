using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[TestFixture]
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android, Platform.Browser)]
		public void WriteableBitmap_Invalidate()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.ImageTests.ImageSourceWriteableBitmapInvalidate");

			// Request to render
			var button = _app.Marked("_update");
			_app.WaitForElement(button);
			button.Tap();

			// Take screenshot
			_app.Screenshot("WriteableBitmap_Invalidate - Result");
		}
	}
}
