using Android.App;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Core;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeDatePickerFlyout : DatePickerFlyout
	{
		private bool _programmaticallyDismissed;
		private DatePickerDialog _dialog;

		public static DependencyProperty UseNativeMinMaxDatesProperty { get; } = DependencyProperty.Register(
			"UseNativeMinMaxDates",
			typeof(bool),
			typeof(NativeDatePickerFlyout),
			new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Setting this to true will interpret MinYear/MaxYear as MinDate and MaxDate.
		/// </summary>
		public bool UseNativeMinMaxDates
		{
			get => (bool)GetValue(UseNativeMinMaxDatesProperty);
			set => SetValue(UseNativeMinMaxDatesProperty, value);
		}

		public NativeDatePickerFlyout()
		{
			this.RegisterPropertyChangedCallback(DateProperty, OnDateChanged);
		}

		internal bool IsNativeDialogOpen => _dialog?.IsShowing ?? false;

		protected internal override void Open()
		{
			var date = Date;
			// If we're setting the date to the null sentinel value,
			// we'll instead set it to the current date for the purposes
			// of where to place the user's position in the looping selectors.
			if (date.Ticks == DatePicker.DEFAULT_DATE_TICKS)
			{
				var temp = new global::Windows.Globalization.Calendar();
				var calendar = new global::Windows.Globalization.Calendar(
					temp.Languages,
					CalendarIdentifier,
					temp.GetClock());
				calendar.SetToNow();
				date = calendar.GetDateTime();
			}

			var themeResourceId = CoreApplication.RequestedTheme == Uno.Helpers.Theming.SystemTheme.Light ? global::Android.Resource.Style.ThemeDeviceDefaultLightDialog : global::Android.Resource.Style.ThemeDeviceDefaultDialog;
			// Note: Month needs to be -1 since on Android months go from 0-11
			// http://developer.android.com/reference/android/app/DatePickerDialog.OnDateSetListener.html#onDateSet(android.widget.DatePicker, int, int, int)
			_dialog = new DatePickerDialog(
				ContextHelper.Current,
				themeResourceId,
				OnDateSet,
				date.Year,
				date.Month - 1,
				date.Day
			);

			//Removes title that is unnecessary as it is a duplicate -> http://stackoverflow.com/questions/33486643/remove-title-from-datepickerdialog 
			_dialog.SetTitle("");

			if (UseNativeMinMaxDates)
			{
				_dialog.DatePicker.MinDate = MinYear.ToUnixTimeMilliseconds();
				_dialog.DatePicker.MaxDate = MaxYear.ToUnixTimeMilliseconds();
			}
			else
			{
				var minYearCalendar = Calendar.Instance;
				minYearCalendar.Set(MinYear.Year, MinYear.Month - 1, MinYear.Day, MinYear.Hour, MinYear.Minute, MinYear.Second);
				_dialog.DatePicker.MinDate = minYearCalendar.TimeInMillis;

				var maxYearCalendar = Calendar.Instance;
				maxYearCalendar.Set(MaxYear.Year, MaxYear.Month - 1, MaxYear.Day, MaxYear.Hour, MaxYear.Minute, MaxYear.Second);
				_dialog.DatePicker.MaxDate = maxYearCalendar.TimeInMillis;
			}

			_dialog.DismissEvent += OnDismiss;
			_dialog.Show();

			AddToOpenFlyouts();
		}

		private void OnDateChanged(DependencyObject sender, DependencyProperty dp)
		{
			var date = Date.Date;
			_dialog?.UpdateDate(date);
		}

		private void OnDismiss(object sender, EventArgs e)
		{
			if (!_programmaticallyDismissed)
			{
				Hide(canCancel: false);
				RemoveFromOpenFlyouts();
			}
		}

		private protected override void OnClosed()
		{
			_programmaticallyDismissed = true;
			_dialog?.Dismiss();
			base.OnClosed();
			_programmaticallyDismissed = false;
		}

		private void OnDateSet(object sender, DatePickerDialog.DateSetEventArgs e)
		{
			var newValue = new DateTimeOffset(e.Date + Date.TimeOfDay, Date.Offset);
			var oldValue = Date;

			Date = newValue;
			_datePicked?.Invoke(this, new DatePickedEventArgs(newValue, oldValue));
		}
	}
}
