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
using Uno.UITests.Helpers;

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
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = _app.CreateQuery(q => q.WithClass("Windows_UI_Xaml_Controls_DatePickerSelector"));

			Console.WriteLine($"1: {theDatePicker.GetDependencyPropertyValue<string>("DataContext")}");

			_app.WaitForDependencyPropertyValue(theDatePicker, "DataContext", "UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models.DatePickerViewModel");

			// Open flyout
			theDatePicker.Tap();

			_app.WaitForElement(datePickerFlyout);

			_app.WaitForDependencyPropertyValue(datePickerFlyout, "DataContext", "UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.Models.DatePickerViewModel");

			_app.TapCoordinates(20, 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void DatePickerFlyout_HasContentTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = _app.CreateQuery(q => q.WithClass("Windows_UI_Xaml_Controls_DatePickerFlyoutPresenter"));

			// Open flyout
			theDatePicker.Tap();

			_app.WaitForDependencyPropertyValue(datePickerFlyout, "Content", "Windows.UI.Xaml.Controls.DatePickerSelector");

			_app.TapCoordinates(20, 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void DatePicker_Flyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePickerFlyout_Automated", skipInitialScreenshot: true);

			//DatePicker is broken: https://github.com/unoplatform/uno/issues/188
			//Using a Button with DatePickerFlyout to simulate a DatePicker
			var button = _app.Marked("TestDatePickerFlyoutButton");

			_app.WaitForElement(button);

			button.Tap();

			TakeScreenshot("DatePicker - Flyout", ignoreInSnapshotCompare: AppInitializer.GetLocalPlatform() == Platform.Android /*Status bar appears with clock*/);

			_app.TapCoordinates(20, 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePickerFlyout_MinYearProperlySets()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = _app.CreateQuery(q => q.WithClass("Windows_UI_Xaml_Controls_DatePickerSelector"));

			_app.WaitFor(() => theDatePicker.GetDependencyPropertyValue<string>("MinYear") != null);
			var minYear = theDatePicker.GetDependencyPropertyValue<string>("MinYear");

			// Open flyout
			theDatePicker.Tap();

			_app.WaitFor(() => datePickerFlyout.GetDependencyPropertyValue<string>("MinYear") == minYear);

			_app.TapCoordinates(20, 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePickerFlyout_MaxYearProperlySets()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theDatePicker"));

			var theDatePicker = _app.Marked("theDatePicker");
			var datePickerFlyout = _app.CreateQuery(q => q.WithClass("Windows_UI_Xaml_Controls_DatePickerSelector"));

			_app.WaitFor(() => theDatePicker.GetDependencyPropertyValue<string>("MaxYear") != null);
			var maxYear = theDatePicker.GetDependencyPropertyValue<string>("MaxYear");

			// Open flyout
			theDatePicker.Tap();

			//Assert
			_app.WaitFor(() => datePickerFlyout.GetDependencyPropertyValue<string>("MaxYear") == maxYear);

			_app.TapCoordinates(20, 20);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)] // Android is disabled https://github.com/unoplatform/uno/issues/1634
		public void DatePickerFlyout_Unloaded()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePickerFlyout_Unloaded", skipInitialScreenshot: true);

			var TestDatePickerFlyoutButton = _app.Marked("TestDatePickerFlyoutButton");
			var datePickerFlyout = _app.CreateQuery(q => q.WithClass("Windows_UI_Xaml_Controls_DatePickerSelector"));

			_app.WaitForElement(TestDatePickerFlyoutButton);

			TestDatePickerFlyoutButton.Tap();

			_app.WaitForElement(datePickerFlyout);

			// Load another sample to dismiss the popup
			Run("UITests.Shared.Windows_UI_Xaml_Controls.DatePicker.DatePicker_SampleContent", waitForSampleControl: false);

			_app.WaitForNoElement(datePickerFlyout);
		}
	}
}
