using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml.WindowTests
{
	[TestFixture]
	public partial class WindowTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android)] // Tests display of Android native view
		public void When_StatusBarTranslucent_PaddingTop()
		{
			Run("UITests.Windows_UI_Xaml.WindowTests.Window_PaddingTop_SBT", skipInitialScreenshot: true);

			var blockSBT = _app.Marked("blockSBT");
			_app.WaitForElement(blockSBT);

			var screenRectBefore = _app.GetRect("blockSBT");

			var screenRectBeforex = screenRectBefore.X;
			var screenRectBeforey = screenRectBefore.Y;

			//Only to developer see the position of screenRectBefore
			var before = TakeScreenshot("Before");

			_app.FastTap("boxSBT");

			//Wait Show KeyBoard after TextBox Click
			_app.Wait(TimeSpan.FromMilliseconds(500));

			var screenRectAfter = _app.GetRect("blockSBT");

			var screenRectAfterx = screenRectAfter.X;
			var screenRectAftery = screenRectAfter.Y;

			//Only to developer see the position of screenRectAfter
			var after = TakeScreenshot("After");

			Assert.AreEqual((screenRectBeforex, screenRectBeforey), (screenRectAfterx, screenRectAftery));

		}
	}
}
