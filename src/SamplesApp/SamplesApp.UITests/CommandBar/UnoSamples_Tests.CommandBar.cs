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

			var isLandscape = GetIsCurrentRotationLandscape("ThePage");

			try
			{

				_app.WaitForElement(_app.Marked("TextBlockWidthTest"));

				// Initial state
				TakeScreenshot("CommandBar - LongTitle - 1 - Initial State");

				// Set orientation Landscape
				_app.SetOrientationLandscape();
				TakeScreenshot("CommandBar - LongTitle - 2 - Orientation Landscape");

				// Set orientation Portrait
				_app.SetOrientationPortrait();
				TakeScreenshot("CommandBar - LongTitle - 3 - Orientation Portrait");

				// Set orientation Landscape (Again)
				_app.SetOrientationLandscape();
				TakeScreenshot("CommandBar - LongTitle - 4 - Orientation Landscape");
			}
			finally
			{
				// Reset orientation to original value
				if (isLandscape)
				{
					_app.SetOrientationLandscape();
				}
				else
				{
					_app.SetOrientationPortrait();
				}
			}
		}
	}
}
