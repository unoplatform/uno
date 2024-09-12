using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests
{
	[TestFixture]
	partial class Button_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[Ignore("Fails on Fluent styles #17272")]
		public void Validate_UseUWPDefaultStyles()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.Button.Button_UseUWPDefaultStyles");

			_app.WaitForText("ResultsTextBlock", "Native view found");
		}
	}
}
