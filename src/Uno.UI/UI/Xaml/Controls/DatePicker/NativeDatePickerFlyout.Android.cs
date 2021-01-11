using Android.App;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeDatePickerFlyout : DatePickerFlyout
	{
		private DatePickerDialog _dialog;

		public NativeDatePickerFlyout()
		{
			RegisterPropertyChangedCallback(DateProperty, OnDateChanged);
		}

		protected internal override void Open()
		{
			// Note: Month needs to be -1 since on Android months go from 0-11
			// http://developer.android.com/reference/android/app/DatePickerDialog.OnDateSetListener.html#onDateSet(android.widget.DatePicker, int, int, int)

			_dialog = new DatePickerDialog(
				ContextHelper.Current,
				OnDateSet,
				Date.Year,
				Date.Month - 1,
				Date.Day
			);

			//Removes title that is unnecessary as it is a duplicate -> http://stackoverflow.com/questions/33486643/remove-title-from-datepickerdialog 
			_dialog.SetTitle("");

			var minYearCalendar = Calendar.Instance;
			minYearCalendar.Set(MinYear.Year, MinYear.Month - 1, MinYear.Day, MinYear.Hour, MinYear.Minute, MinYear.Second);
			_dialog.DatePicker.MinDate = minYearCalendar.TimeInMillis;

			var maxYearCalendar = Calendar.Instance;
			maxYearCalendar.Set(MaxYear.Year, MaxYear.Month - 1, MaxYear.Day, MaxYear.Hour, MaxYear.Minute, MaxYear.Second);
			_dialog.DatePicker.MaxDate = maxYearCalendar.TimeInMillis;

			_dialog.DismissEvent += OnDismiss;
			_dialog.Show();
		}

		private void OnDateChanged(DependencyObject sender, DependencyProperty dp)
		{
			var date = Date.Date;
			_dialog?.UpdateDate(date);
		}

		private void OnDismiss(object sender, EventArgs e)
		{
			Hide(canCancel: false);
		}

		internal protected override void Close()
		{
			_dialog?.Dismiss();
		}

		private void OnDateSet(object sender, DatePickerDialog.DateSetEventArgs e)
		{
			var newValue = new DateTimeOffset(e.Date + Date.TimeOfDay, Date.Offset);
			var oldValue = Date;

			Date = newValue;
			DatePicked?.Invoke(this, new DatePickedEventArgs(newValue, oldValue));
		}
	}
}
