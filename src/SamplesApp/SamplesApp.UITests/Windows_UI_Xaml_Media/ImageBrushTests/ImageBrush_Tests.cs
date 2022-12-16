using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Media.ImageBrushTests
{
	[TestFixture]
	public partial class ImageBrush_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void When_ImageBrush_Source_URI_Changes()
		{
			Run("Uno.UI.Samples.UITests.ImageBrushTestControl.ImageBrushChangingURI");

			var border = _app.Marked("brCont");
			var txt = _app.Marked("txtStatus");

			_app.WaitForElement(border);
			_app.WaitForElement(txt);

			var screenRect = _app.GetPhysicalRect(border);

			using var before = TakeScreenshot("Before", ignoreInSnapshotCompare: true);

			_app.FastTap("btnImage1"); 
			
			_app.WaitForText("txtStatus", "Changed");

			using var after = TakeScreenshot("After");

			ImageAssert.AreNotEqual(before, after, screenRect);
		}
	}
}
