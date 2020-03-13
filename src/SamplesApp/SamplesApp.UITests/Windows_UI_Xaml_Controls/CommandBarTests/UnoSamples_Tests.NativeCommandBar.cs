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
	public partial class NativeCommandBar_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void NativeCommandBar_Automated()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CommandBar.CommandBar_Native_With_Content");

			var myButton = _app.Marked("myButton");
			var result = _app.Marked("result");

			myButton.Tap();

			_app.WaitForText(result, "Clicked!");
		}
	}
}
