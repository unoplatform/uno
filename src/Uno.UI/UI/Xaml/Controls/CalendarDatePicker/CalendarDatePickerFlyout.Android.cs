#if XAMARIN_ANDROID
using Android.App;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Uno.UI;
using Uno.UI.Common;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
    public partial class CalendarDatePickerFlyout : PickerFlyoutBase
    {
        private DatePickerDialog _dialog;

        internal protected override void Open()
        {
			// Note: Month needs to be -1 since on Android months go from 0-11
			// http://developer.android.com/reference/android/app/DatePickerDialog.OnDateSetListener.html#onDateSet(android.widget.DatePicker, int, int, int)

			DateTimeOffset tempDate = DateTimeOffset.Now; //  Date.GetValueOrDefault(DateTimeOffset.Now);

            _dialog = new DatePickerDialog(
                ContextHelper.Current,
                OnDateSet,
				tempDate.Year,
				tempDate.Month - 1,
				tempDate.Day
            );

            //Removes title that is unnecessary as it is a duplicate -> http://stackoverflow.com/questions/33486643/remove-title-from-datepickerdialog 
            _dialog.SetTitle("");

            var minYearCalendar = Calendar.Instance;
            minYearCalendar.Set(MinDate.Year, MinDate.Month - 1, MinDate.Day, MinDate.Hour, MinDate.Minute, MinDate.Second);
			_dialog.DatePicker.MinDate = minYearCalendar.TimeInMillis;

            var maxYearCalendar = Calendar.Instance;
            maxYearCalendar.Set(MaxDate.Year, MaxDate.Month - 1, MaxDate.Day, MaxDate.Hour, MaxDate.Minute, MaxDate.Second);
            _dialog.DatePicker.MaxDate = maxYearCalendar.TimeInMillis;

	    	_dialog.DismissEvent += OnDismiss;
            _dialog.Show();
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
			DateTimeOffset tempDate = Date.GetValueOrDefault(DateTimeOffset.Now);
#pragma warning disable CS0618 // Type or member is obsolete
			Date = new DateTimeOffset(e.Year, e.MonthOfYear + 1, e.DayOfMonth, tempDate.Hour, tempDate.Minute, tempDate.Second, tempDate.Millisecond, tempDate.Offset);
#pragma warning restore CS0618 // Type or member is obsolete
		}
    }
}

#endif
