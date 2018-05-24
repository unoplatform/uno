#if XAMARIN_ANDROID
using Android.App;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerFlyout : FlyoutBase // TODO: Inherit from PickerFlyoutBase
	{
		private Dialog _dialog;

		internal protected override void Open()
		{
			if (UseAlertDialogWorkAround())
			{
				ShowUsingAlertDialog();
			}
			else
			{
				ShowUsingTimePickerDialog();
			}
		}

		internal protected override void Close()
		{
			_dialog?.Dismiss();
		}

		private bool UseAlertDialogWorkAround()
		{
			// WARNING Adding any case that uses API lower than 6.0 will require the use of deprecated properties 
			// on the TimePicker i.e. CurrentHour and CurrentMinute, server build rules may need to be modified or
			// a #prama directive may be needed.

			var ver = Android.OS.Build.VERSION.Release;
			//On Samsung devices >= 6.0 the TimePickerDialog is not displayed properly in Landscape.
			return Android.OS.Build.VERSION.Release.StartsWith("6.") && //Any 6.x.x version				
				   Android.OS.Build.Manufacturer.Contains("samsung", StringComparison.OrdinalIgnoreCase);
		}

		private void ShowUsingTimePickerDialog()
		{
			_dialog = new TimePickerDialog(
					ContextHelper.Current,
					(sender, e) => Time = new TimeSpan(Time.Days, e.HourOfDay, e.Minute, Time.Seconds, Time.Milliseconds),
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
			var androidTimePicker = new Android.Widget.TimePicker(ContextHelper.Current);

			//Hour and Minute properties are not supported on Android before 6.0
			androidTimePicker.Hour = Time.Hours;
			androidTimePicker.Minute = Time.Minutes;

			androidTimePicker.SetIs24HourView(new Java.Lang.Boolean(ClockIdentifier == ClockIdentifiers.TwentyFourHour));

			_dialog = new AlertDialog.Builder(ContextHelper.Current)
				.SetPositiveButton(
					Android.Resource.String.Ok,
					(sender, eventArgs) => 
						Time = new TimeSpan(Time.Days, androidTimePicker.Hour, androidTimePicker.Minute, Time.Seconds, Time.Milliseconds)
				)
				.SetNegativeButton(
					Android.Resource.String.Cancel,
					(sender, eventArgs) => { /*Nothing to do*/ }
				)
				.SetView(androidTimePicker)
				.Show();

			_dialog.DismissEvent += OnDismiss;
		}

		private void OnDismiss(object sender, EventArgs e)
		{
			Hide(canCancel: false);
		}
	}
}
#endif