#if XAMARIN_IOS

using Foundation;
using System;
using System.Linq;
using UIKit;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Extensions;
using Windows.Globalization;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerSelector
	{
		private UIDatePicker _picker;
		private NSDate _initialTime;
		private NSDate _newDate;

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>().FirstOrDefault();

			if (_picker == null)
			{
				this.Log().DebugIfEnabled(() => $"No {nameof(UIDatePicker)} was found in the visual hierarchy.");
				return;
			}

			// The time zone must be the same as DatePickerSelector
			// otherwise, if a DatePicker and a TimePicker are present
			// with 2 different time zones, they will interfer one with the other
			// and result in weird behaviors (like decrementing the year when a month changes).
			_picker.TimeZone = NSTimeZone.LocalTimeZone;
			_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);
			_picker.Mode = UIDatePickerMode.Time;

			_picker.ValueChanged += OnValueChanged;
			
			var parent = _picker.FindFirstParent<FrameworkElement>();

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view. 
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}
		}
		
		private void OnValueChanged(object sender, EventArgs e)
		{
			_newDate = _picker.Date;
		}

		public void Initialize()
		{
			SetPickerClockIdentifier(ClockIdentifier);
			SetPickerMinuteIncrement(MinuteIncrement);
			SetPickerTime(Time.RoundToNextMinuteInterval(MinuteIncrement));
			SaveInitialTime();
		}

		private void SetPickerMinuteIncrement(int minuteIncrement)
		{
			if (_picker != null)
			{
				_picker.MinuteInterval = minuteIncrement;
			}
		}

		private void SaveInitialTime() => _initialTime = _picker.Date;

		internal void SaveTime()
		{
			if (_picker != null)
			{
				if ((_newDate != null) && _newDate != _initialTime)
				{
					var time = _newDate.ToTimeSpanOfDay(_picker.TimeZone.GetSecondsFromGMT);

					if (Time.Hours != time.Hours || Time.Minutes != time.Minutes)
					{
						Time = new TimeSpan(Time.Days, time.Hours, time.Minutes, 0, 0);
						SaveInitialTime();
					}
				}

				_picker.EndEditing(false);
			}
		}

		public void Cancel()
		{
			if (_picker != null)
			{
				_picker.SetDate(_initialTime, false);
				_picker.EndEditing(false);
			}
		}

		private void SetPickerClockIdentifier(string clockIdentifier)
		{
			if (_picker != null)
			{
				_picker.Locale = ToNSLocale(clockIdentifier);
			}
		}

		private void SetPickerTime(TimeSpan time)
		{
			_picker?.SetDate(time.ToNSDate(), animated: false);
		}

		partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier)
		{
			SetPickerClockIdentifier(newClockIdentifier);
		}

		partial void OnMinuteIncrementChangedPartialNative(int oldMinuteIncrement, int newMinuteIncrement)
		{
			SetPickerMinuteIncrement(newMinuteIncrement);
		}

		partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime)
		{
			SetPickerTime(newTime);
		}

		private static NSLocale ToNSLocale(string clockIdentifier)
		{
			var localeID = clockIdentifier == ClockIdentifiers.TwelveHour
							? "en"
							: "fr";

			return new NSLocale(localeID);
		}

		protected override void OnUnloaded()
		{
			_picker.ValueChanged -= OnValueChanged;

			base.OnUnloaded();
		}
	}
}
#endif
