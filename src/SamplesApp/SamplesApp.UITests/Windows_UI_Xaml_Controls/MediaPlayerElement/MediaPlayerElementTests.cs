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

		// step 0: Pause && Play
		// Due a browser validation the auto play do not works at first.
		// Then for now, set pause and then play.
		_app.Tap(_app.Marked("PlayPauseButton"));
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Pause And Play Tapped");

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

	[Test]
	[AutoRetry]
	public void MediaPlayerElement_SeekingTest()
	{
		Run("UITests.Shared.Windows_UI_Xaml_Controls.MediaPlayerElement.MediaPlayerElement_Original");

		_app.WaitForElement(_app.Marked("MpeOriginal"));
		TakeScreenshot("Initial MpeOriginal");

		// step 0: Pause && Play
		// Due a browser validation the auto play do not works at first.
		// Then for now, set pause and then play.
		_app.Tap(_app.Marked("PlayPauseButton"));
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Pause And Play Tapped");

		// step 1: Pause
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Pause Tapped");

		// step 2: Move Progress
		var progressSlider = _app.Marked("ProgressSlider");

		//Just works when in the same window/monitor.
		var rect = _app.Query(progressSlider).First().Rect;
		float middleX = rect.X + rect.Width / 2;
		float middleY = rect.Y + rect.Height / 2;

		// Realize o toque no meio do objeto
		_app.TapCoordinates(middleX, middleY);

		var itemQuery = _app.Marked("TimeElapsedElement");
		_app.WaitForTextGreaterThanTimeSpan(itemQuery, TimeSpan.FromSeconds(3));
		TakeScreenshot("TimeElapsedElement More than 03 seconds");

		// step 4: press button PlayPause
		_app.Tap(_app.Marked("PlayPauseButton"));
		TakeScreenshot("Play Tapped");
		_app.WaitForTextGreaterThanTimeSpan(itemQuery, TimeSpan.FromSeconds(8));
		TakeScreenshot("TimeElapsedElement Play 08 seconds");
	}
}
