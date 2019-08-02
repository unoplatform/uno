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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.DatePickerTests
{
	[TestFixture]
	public partial class DatePickerTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void DatePickerFlyout_HasDataContextTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent");

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = theDatePicker.Child;

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.IsNotNull(theDatePicker.GetDependencyPropertyValue("DataContext"));
			Assert.IsNotNull(datePickerFlyout.GetDependencyPropertyValue("DataContext"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void DatePickerFlyout_HasContentTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent");

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = theDatePicker.Child;

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.IsNotNull(datePickerFlyout.GetDependencyPropertyValue("Content"));
		}
	}
}
