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

			// Assert inital state 
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

			// Assert inital state 
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
		public void TimePickerFlyout_DoesntApplyDefaultTime()
		{
			Run("SamplesApp.Samples.TimePicker.Sample1");

			_app.WaitForElement(_app.Marked("theTimePicker"));
			var theTimePicker = _app.Marked("theTimePicker");

			// Assert initial state
			if (DateTime.Now.TimeOfDay != new TimeSpan(12, 0, 0))
			{
				Assert.AreEqual("12:00:00", theTimePicker.GetDependencyPropertyValue("Time")?.ToString());
			}
		}
	}
}
