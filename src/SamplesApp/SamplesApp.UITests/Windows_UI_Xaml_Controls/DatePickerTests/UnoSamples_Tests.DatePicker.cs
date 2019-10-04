using SamplesApp.UITests.TestFramework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.DatePickerTests
{
	[TestFixture]
	public partial class DatePickerTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
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

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void DatePicker_Flyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePickerFlyout_Automated");

			//DatePicker is broken: https://github.com/unoplatform/uno/issues/188
			//Using a Button with DatePickerFlyout to simulate a DatePicker
			var button = _app.Marked("TestDatePickerFlyoutButton");

			_app.WaitForElement(button);

			button.Tap();

			_app.Screenshot("DatePicker - Flyout");
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePickerFlyout_MinYearProperlySets()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent");

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = theDatePicker.Child;

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.AreEqual(datePickerFlyout.GetDependencyPropertyValue("MinYear").ToString(), theDatePicker.GetDependencyPropertyValue("MinYear").ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePickerFlyout_MaxYearProperlySets()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent");

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = theDatePicker.Child;

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.AreEqual(datePickerFlyout.GetDependencyPropertyValue("MaxYear").ToString(), theDatePicker.GetDependencyPropertyValue("MaxYear").ToString());
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePicker_TappingPresenterDismissesFlyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent");

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = theDatePicker.Child;

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.Equals("True", datePickerFlyout.GetDependencyPropertyValue("IsOpened").ToString());

			// Open flyout
			theDatePicker.Tap();

			//Assert
			Assert.Equals("False", datePickerFlyout.GetDependencyPropertyValue("IsOpened").ToString());
		}
	}
}
