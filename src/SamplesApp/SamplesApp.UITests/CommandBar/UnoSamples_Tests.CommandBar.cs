using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.CommandBar
{
	[TestFixture]
	public partial class UnoSamples_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.Android, Platform.iOS)]
		public void CommandBar_LongTitle_Validation()
		{
			Run("Uno.UI.Samples.Content.UITests.CommandBar.CommandBar_LongTitle");

			_app.WaitForElement(_app.Marked("TextBlockWidthTest"));

			// Initial state
			_app.Screenshot("CommandBar - LongTitle - 1 - Initial State");

			// Set orientation Landscape
			_app.SetOrientationLandscape();
			_app.Screenshot("CommandBar - LongTitle - 2 - Orientation Landscape");

			// Set orientation Portrait
			_app.SetOrientationPortrait();
			_app.Screenshot("CommandBar - LongTitle - 3 - Orientation Portrait");

			// Set orientation Landscape (Again)
			_app.SetOrientationLandscape();
			_app.Screenshot("CommandBar - LongTitle - 4 - Orientation Landscape");
		}
	}
}
