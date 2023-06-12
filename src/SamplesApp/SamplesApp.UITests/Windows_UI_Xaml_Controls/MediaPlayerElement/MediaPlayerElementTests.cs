using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;
using Uno.UITests.Helpers;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.MediaPlayerElementTests;

[TestFixture]
class MediaPlayerElementTests : SampleControlUITestBase
{
	[Test]
	[AutoRetry]
	public void MediaPlayerElement_PlayPauseStopTest()
	{
		Run("UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement.MediaPlayerElement_Original");

		_app.WaitForElement(_app.Marked("MpeOriginal"));

		TakeScreenshot("Initial MpeOriginal");

		// step 1: Test Auto Play
		var itemQuery = _app.Marked("TimeElapsedElement");
		_app.WaitForTextGreaterThanTimeSpan(itemQuery, TimeSpan.FromSeconds(4));
		TakeScreenshot("TimeElapsedElement AutoPlay 04 seconds");

		// step 2: press button PlayPause 
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Pause Tapped");

		// step 3: Test Stop
		_app.Tap(_app.Marked("StopButton"));
		TakeScreenshot("Stop Tapped");

		// step 4: press button PlayPause
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Play Tapped");

		_app.WaitForTextGreaterThanTimeSpan(itemQuery, TimeSpan.FromSeconds(2));
		TakeScreenshot("TimeElapsedElement Play 02 seconds");
	}
}
