#if XAMARIN_IOS

using System;
using System.Linq;
using Foundation;
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

			var parent = _picker.FindFirstParent<FrameworkElement>();

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view. 
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}
		}

		public void Initialize()
		{
			SaveInitialTime();
			SetPickerClockIdentifier(ClockIdentifier);
			SetPickerTime(Time);
		}

		private void SaveInitialTime() => _initialTime = _picker.Date;

		internal void SaveTime()
		{
			if (_picker != null)
			{
				if (_picker.Date != _initialTime)
				{
					var time = _picker.Date.ToTimeSpan(_picker.TimeZone.GetSecondsFromGMT);

					if (Time.Hours != time.Hours || Time.Minutes != time.Minutes)
					{
						Time = new TimeSpan(Time.Days, time.Hours, time.Minutes, Time.Seconds, Time.Milliseconds);
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
			if (_picker != null)
			{
				// Because the UIDatePicker is set to use LocalTimeZone,
				// we need to get the offset and apply it to the requested time.
				var offset = TimeSpan.FromSeconds(_picker.TimeZone.GetSecondsFromGMT);

				// Because the UIDatePicker applies the local timezone offset automatically when we set it,
				// we need to compensate with a negated offset. This will show the time
				// as if it was provided with no offset.
				var timeWithOffset = time.Add(offset.Negate());
				var nsDate = timeWithOffset.ToNSDate();

				_picker.SetDate(nsDate, animated: false);
			}
		}
		
		partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier)
		{
			SetPickerClockIdentifier(newClockIdentifier);
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
	}
}
#endif