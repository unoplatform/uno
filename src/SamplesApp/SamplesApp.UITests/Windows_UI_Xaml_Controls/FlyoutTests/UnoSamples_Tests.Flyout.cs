using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.FlyoutTests
{
	[TestFixture]
	public partial class Flyout_Tests : SampleControlUITestBase
	{
		[Test]
		[Ignore("Not available yet")]
		public void FlyoutTest_BottomPlacement_WithSmallerAnchor_DoesntDefaultToFull()
		{
			Run("Uno.UI.Samples.Content.UITests.Flyout.Flyout_Simple");

			_app.WaitForElement(_app.Marked("FlyoutToBottomButton"));

			// Can't find popup that is outside of page.
			//var button = _app.Marked("FlyoutToBottomButton");
			//_app.Tap(button);

			//_app.Wait(1);
			//_app.WaitForElement(_app.Marked("BottomFlyout"));
			//var flyoutRect = _app.Marked("BottomFlyout").FirstResult().Rect;

			//// Assert initial state 
			//Assert.AreEqual("0", flyoutRect.X.ToString());
		}
	}
}
