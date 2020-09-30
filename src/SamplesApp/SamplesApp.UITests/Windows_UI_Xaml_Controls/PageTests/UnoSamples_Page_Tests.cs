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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.PivotTests
{
	public partial class UnoSamples_Page_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry()]
		public void When_Background_Static()
		{
			Run("UITests.Windows_UI_Xaml_Controls.PageTests.Page_Automated");

			_app.WaitForElement(_app.Marked("_empty"));

			using var content = TakeScreenshot("afterColor", ignoreInSnapshotCompare: true);

			var emptyRect = _app.GetPhysicalRect("_empty");
			var transparentRect = _app.GetPhysicalRect("_transparent");
			var solidRect = _app.GetPhysicalRect("_colored");

			var offset = LogicalToPhysical(30);
			ImageAssert.HasColorAt(content, (emptyRect.X + offset), (emptyRect.Y + offset), Color.Red);
			ImageAssert.HasColorAt(content, (transparentRect.X + offset), (transparentRect.Y + offset), Color.Red);
			ImageAssert.HasColorAt(content, (solidRect.X + offset), (solidRect.Y + offset), Color.Blue);
		}

		[Test]
		[AutoRetry()]
		public void When_Background_Updated()
		{
			Run("UITests.Windows_UI_Xaml_Controls.PageTests.Page_Update_Background");

			_app.WaitForElement("TargetPage");

			var rect = _app.GetPhysicalRect("TargetPage");

			using var before = TakeScreenshot("Before SolidColorBrush.Color update", ignoreInSnapshotCompare: true);
			ImageAssert.HasColorAt(before, rect.CenterX, rect.CenterY, Color.Blue);
			
			_app.FastTap("AdvanceTestButton");

			_app.WaitForText("StatusTextBlock", "Color changed");

			using var after = TakeScreenshot("After SolidColorBrush.Color update");
			ImageAssert.HasColorAt(after, rect.CenterX, rect.CenterY, Color.Green);
		}
	}
}
