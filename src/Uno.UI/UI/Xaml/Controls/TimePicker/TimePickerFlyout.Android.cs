#if XAMARIN_ANDROID
using System;
using System.Linq;
using Android.App;
using Android.OS;
using Uno.UI;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyout : FlyoutBase
	{
		private Dialog _dialog;
		private TimeSpan _initialTime;

		internal protected override void Open()
		{
			SaveInitialTime();

			//On Samsung devices >= 6.0 the TimePickerDialog is not displayed properly in Landscape.
			if (Build.VERSION.SdkInt == BuildVersionCodes.M &&
			    Build.Manufacturer.ToLower().IndexOf("samsung") >= 0)
			{
				ShowUsingAlertDialog();
			}
			else
			{
				ShowUsingTimePickerDialog();
			}
		}

		private void SaveInitialTime() => _initialTime = Time;

		internal protected override void Close() { base.Close(); }

		private void SaveTime(int hourOfDay, int minutes)
		{
			if (Time.Hours != hourOfDay || Time.Minutes != minutes)
			{
				Time = new TimeSpan(Time.Days, hourOfDay, minutes, Time.Seconds, Time.Milliseconds);
				SaveInitialTime();
			}
		}
		
		private void ShowUsingTimePickerDialog()
		{
			_dialog = new TimePickerDialog(
				      ContextHelper.Current,
				      (sender, e) => SaveTime(e.HourOfDay, e.Minute),
				      Time.Hours,
				      Time.Minutes,
				      ClockIdentifier == ClockIdentifiers.TwentyFourHour
				      );

			_dialog.DismissEvent += OnDismiss;
			_dialog.Show();
		}

		private void ShowUsingAlertDialog()
		{
			//On Samsung devices ?= 6.0 the TimepickerDialog is not displayed properly in Landscape.
			//Instead using this work around seems to work.
			var timePicker = new Android.Widget.TimePicker(ContextHelper.Current);

			//Hour and Minute properties are not supported on Android before 6.0
			if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
			{
				timePicker.Hour = Time.Hours;
				timePicker.Minute = Time.Minutes;
			}
			else
			{
#pragma warning disable CS0618 // Type or member is obsolete
				timePicker.CurrentHour = new Java.Lang.Integer(Time.Hours);
				timePicker.CurrentMinute = new Java.Lang.Integer(Time.Minutes);
#pragma warning restore CS0618 // Type or member is obsolete
			}

			timePicker.SetIs24HourView(new Java.Lang.Boolean(ClockIdentifier == ClockIdentifiers.TwentyFourHour));
		
			_dialog = new AlertDialog.Builder(ContextHelper.Current)
				.SetPositiveButton(
					Android.Resource.String.Ok,
					(sender, eventArgs) =>
						Time = new TimeSpan(Time.Days, timePicker.Hour, timePicker.Minute, Time.Seconds, Time.Milliseconds)
				)
				.SetNegativeButton(
					Android.Resource.String.Cancel,
					(sender, eventArgs) => { /*Nothing to do*/ }
				)
				.SetView(timePicker)
				.Show();

			_dialog.DismissEvent += OnDismiss;
		}

		private void OnDismiss(object sender, EventArgs e)
		{
			Hide(canCancel: false);
		}

		partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime){}
	}
}
#endif
