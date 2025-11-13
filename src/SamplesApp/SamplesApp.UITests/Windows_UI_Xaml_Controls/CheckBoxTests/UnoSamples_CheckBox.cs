using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.ButtonTests
{
	public partial class UnoSamples_CheckBox : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		public void TwoStates()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.CheckBoxTests.CheckBox_Automated");

			var twoState01 = _app.Marked("twoState01");
			var result = _app.Marked("result");

			_app.WaitForElement(twoState01);

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Checked twoState01 True");

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Unchecked twoState01 False");

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Checked twoState01 True");
		}

		[Test]
		[AutoRetry]
		public void ThreeStates()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.CheckBoxTests.CheckBox_Automated");

			var twoState01 = _app.Marked("threeState01");
			var result = _app.Marked("result");

			_app.WaitForElement(twoState01);

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Checked threeState01 True");

			TakeScreenshot("Checked");

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Indeterminate threeState01 ");

			TakeScreenshot("Indeterminate");

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Unchecked threeState01 False");

			TakeScreenshot("Unchecked");

			_app.FastTap(twoState01);
			_app.WaitForText(result, "Checked threeState01 True");
		}
	}
}
