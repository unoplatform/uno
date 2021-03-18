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
			try
			{

				Run("Uno.UI.Samples.Content.UITests.TimePicker.TimePicker_Automated", skipInitialScreenshot: true);

				_app.WaitForElement(_app.Marked("btnApplyNewTime"));

				var txtSelectedTime = _app.Marked("txtSelectedTime");
				var myTimePicker = _app.Marked("myTimePicker");

				// Assert initial state 
				Assert.AreEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
				Assert.AreEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());

				_app.SetOrientationPortrait();

				// Open and dismiss flyout
				myTimePicker.FastTap();
				var myDevice = _app.Device.GetType();
				if (_app.Device.GetType().Name.Contains("Android"))
				{
					_app.TapCoordinates(988, 1625);
					_app.Wait(2);
					_app.TapCoordinates(988, 1625);

					_app.Find("Cancel").FastTap();
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
			finally
			{
				_app.SetOrientationLandscape();
			}
		}

		[Test]
		[AutoRetry]
		[Ignore("Not available yet")]
		public void TimePickerFlyout_ApplyChanges()
		{
			try
			{
				Run("Uno.UI.Samples.Content.UITests.TimePicker.TimePicker_Automated", skipInitialScreenshot: true);

				_app.WaitForElement(_app.Marked("btnApplyNewTime"));

				var txtSelectedTime = _app.Marked("txtSelectedTime");
				var myTimePicker = _app.Marked("myTimePicker");

				// Assert initial state 
				Assert.AreEqual("14:50", txtSelectedTime.GetDependencyPropertyValue("Text")?.ToString());
				Assert.AreEqual("14:50:00", myTimePicker.GetDependencyPropertyValue("Time")?.ToString());

				_app.SetOrientationPortrait();

				// Open, change ime and click on ok to apply changes and to close flyout
				myTimePicker.FastTap();
				var myDevice = _app.Device.GetType();
				if (_app.Device.GetType().Name.Contains("Android"))
				{
					_app.TapCoordinates(988, 1625);
					_app.Wait(2);
					_app.TapCoordinates(988, 1625);

					_app.Find("OK").FastTap();
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
			finally
			{
				_app.SetOrientationLandscape();
			}
		}

		[Test]
		[AutoRetry]
		public void TimePickerFlyout_DoesntApplyDefaultTime()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample1", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theTimePicker"));
			var theTimePicker = _app.Marked("theTimePicker");

			// Assert initial state
			if (DateTime.Now.TimeOfDay != new TimeSpan(12, 0, 0))
			{
				Assert.AreEqual("12:00:00", theTimePicker.GetDependencyPropertyValue("Time")?.ToString());
			}

			// Dismiss the flyout
			_app.TapCoordinates(10, 10);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TimePickerFlyout_HasDataContextTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample2", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theTimePicker"));

			var theTimePicker = _app.Marked("theTimePicker");
			var timePickerFlyout = theTimePicker.Child;

			// Open flyout
			theTimePicker.FastTap();

			//Assert
			Assert.IsNotNull(theTimePicker.GetDependencyPropertyValue("DataContext"));
			Assert.IsNotNull(timePickerFlyout.GetDependencyPropertyValue("DataContext"));

			// Dismiss the flyout
			_app.TapCoordinates(10, 10);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void TimePickerFlyout_HasContentTest()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.Sample2", skipInitialScreenshot: true);

			_app.WaitForElement(_app.Marked("theTimePicker"));

			var theTimePicker = _app.Marked("theTimePicker");
			var timePickerFlyout = theTimePicker.Child;

			// Open flyout
			theTimePicker.FastTap();

			//Assert
			Assert.IsNotNull(timePickerFlyout.GetDependencyPropertyValue("Content"));

			// Dismiss the flyout
			_app.TapCoordinates(10, 10);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TimePicker_Flyout()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.TimePicker_Flyout_Automated", skipInitialScreenshot: true);

			var picker = _app.Marked("TestTimePicker");

			_app.WaitForElement(picker);

			picker.FastTap();

			TakeScreenshot("TimePicker - Flyout", ignoreInSnapshotCompare: true);

			// Dismiss the flyout
			_app.TapCoordinates(10, 10);
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS)]
		public void TimePicker_Flyout_Reloaded()
		{
			Run("UITests.Shared.Windows_UI_Xaml_Controls.TimePicker.TimePicker_Flyout_Automated_Reload", skipInitialScreenshot: true);

			var picker = _app.Marked("TestTimePicker");

			_app.WaitForElement(picker);

			picker.FastTap();

			// Wait for the picker to appear
			_app.WaitForElement(x => x.Class("UIPickerView"));

			TakeScreenshot("TimePicker - Flyout", ignoreInSnapshotCompare: true);

			// Dismiss the flyout
			_app.Tap(x => x.Marked("AcceptButton"));
		}

		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Android)]
		public void TimePicker_Header()
		{
			Run("UITests.Windows_UI_Xaml_Controls.TimePicker.TimePicker_Header");

			var headerContentTextBlock = _app.Marked("TimePickerHeaderContent");
			_app.WaitForElement(headerContentTextBlock);

			Assert.AreEqual("This is a TimePicker Header", headerContentTextBlock.GetDependencyPropertyValue("Text").ToString());
		}
	}
}
