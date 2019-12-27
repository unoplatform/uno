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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ComboBoxTests
{
	[TestFixture]
	public partial class XamlCommandBar_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void XamlCommandBar_Automated()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Xaml_Automated");

			var shuffleButton = _app.Marked("shuffleButton");
			var playButton = _app.Marked("playButton");
			var appBarContentButton = _app.Marked("appBarContentButton");
			var clickResult = _app.Marked("clickResult");

			_app.WaitForElement(_app.Marked("shuffleButton"));
			_app.WaitForDependencyPropertyValue(shuffleButton, "IsChecked", false);

			TakeScreenshot("Initial");

			_app.Tap(shuffleButton);
			_app.WaitForDependencyPropertyValue(shuffleButton, "IsChecked", true);
			_app.WaitForDependencyPropertyValue(clickResult, "Text", "Shuffle");

			TakeScreenshot("AfterShuffle");

			_app.Tap(playButton);
			_app.WaitForDependencyPropertyValue(clickResult, "Text", "Play");

			TakeScreenshot("AfterPlay");

			_app.Tap(appBarContentButton);
			_app.WaitForDependencyPropertyValue(clickResult, "Text", "Click me");

			TakeScreenshot("AfterClickMe");
		}
	}
}
