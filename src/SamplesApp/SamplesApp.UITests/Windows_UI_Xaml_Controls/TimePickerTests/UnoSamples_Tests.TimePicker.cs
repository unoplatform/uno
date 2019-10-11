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

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.TimePickerTests
{
	[TestFixture]
	public partial class TimePickerTests_Tests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[Ignore("Not available yet")]
		public void TimePickerFlyout_DiscardChanges()
		{
			Run("Uno.UI.Samples.Content.UITests.TimePicker.TimePicker_Automated");

			_app.WaitForElement(_app.Marked("btnApplyNewTime"));

			var txtSelectedTime = _app.Marked("txtSelectedTime");
			var myTimePicker = _app.Marked("myTimePicker");

			// Assert initial state 
			Assert.AreEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());

			_app.SetOrientationPortrait();

			// Open and dismiss flyout
			myTimePicker.Tap();
			var myDevice = _app.Device.GetType();
			if (_app.Device.GetType().Name.Contains("Android"))
			{
				_app.TapCoordinates(988, 1625);
				_app.Wait(2);
				_app.TapCoordinates(988, 1625);

				_app.Find("Cancel").Tap();
				_app.Wait(2);
			}
			else
			{
				// To do Task Number: - 155260 complete test case for IOS.
			}
			//Assert final state
			Assert.AreEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());
		}

		[Test]
		[AutoRetry]
		[Ignore("Not available yet")]
		public void TimePickerFlyout_ApplyChanges()
		{
			Run("Uno.UI.Samples.Content.UITests.TimePicker.TimePicker_Automated");

			_app.WaitForElement(_app.Marked("btnApplyNewTime"));

			var txtSelectedTime = _app.Marked("txtSelectedTime");
			var myTimePicker = _app.Marked("myTimePicker");

			// Assert initial state 
			Assert.AreEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
			Assert.AreEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());

			_app.SetOrientationPortrait();

			// Open, change ime and click on ok to apply changes and to close flyout
			myTimePicker.Tap();
			var myDevice = _app.Device.GetType();
			if (_app.Device.GetType().Name.Contains("Android"))
			{
				_app.TapCoordinates(988, 1625);
				_app.Wait(2);
				_app.TapCoordinates(988, 1625);

				_app.Find("OK").Tap();
				_app.Wait(2);

				//Assert final state
				Assert.AreNotEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
				Assert.AreNotEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());
			}
			else
			{
				// To do Task Number: - 155260 complete test case for IOS.KD
			}
		}

		[Test]
		[AutoRetry]
		public void TimePickerFlyout_DoesntApplyDefaultTime()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample1");

			_app.WaitForElement(_app.Marked("theTimePicker"));
			var theTimePicker = _app.Marked("theTimePicker");

			// Assert initial state
			if (DateTime.Now.TimeOfDay != new TimeSpan(12, 0, 0))
			{
				Assert.AreEqual("12:00:00", theTimePicker.GetDependencyPropertyValue("Time")?.ToString());
			}
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TimePickerFlyout_HasDataContextTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample2");

			_app.WaitForElement(_app.Marked("theTimePicker"));

			var theTimePicker = _app.Marked("theTimePicker");
			var timePickerFlyout = theTimePicker.Child;

			// Open flyout
			theTimePicker.Tap();

			//Assert
			Assert.IsNotNull(theTimePicker.GetDependencyPropertyValue("DataContext"));
			Assert.IsNotNull(timePickerFlyout.GetDependencyPropertyValue("DataContext"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void TimePickerFlyout_HasContentTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample2");

			_app.WaitForElement(_app.Marked("theTimePicker"));

			var theTimePicker = _app.Marked("theTimePicker");
			var timePickerFlyout = theTimePicker.Child;

			// Open flyout
			theTimePicker.Tap();

			//Assert
			Assert.IsNotNull(timePickerFlyout.GetDependencyPropertyValue("Content"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TimePicker_Flyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.TimePicker_Flyout_Automated");

			var picker = _app.Marked("TestTimePicker");

			_app.WaitForElement(picker);

			picker.Tap();

			_app.Screenshot("TimePicker - Flyout");
		}
	}
}
