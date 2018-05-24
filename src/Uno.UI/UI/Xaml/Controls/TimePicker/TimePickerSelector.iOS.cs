using Foundation;
using System;
using System.Linq;
using UIKit;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.Globalization;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerSelector
	{
		private UIDatePicker _picker;

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>().FirstOrDefault();

			var parent = _picker.FindFirstParent<FrameworkElement>();

			if (_picker == null)
			{
				this.Log().DebugIfEnabled(() => $"No {nameof(UIDatePicker)} was found in the visual hierarchy.");

				return;
			}

			_picker.Mode = UIDatePickerMode.Time;

			// The time zone must be the same as DatePickerSelector
			// otherwise, if a DatePicker and a TimePicker are present
			// with 2 different time zones, they will interfer one with the other
			// and result in weird behaviors (like decrementing the year when a month changes).
			_picker.TimeZone = NSTimeZone.LocalTimeZone;
			_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);

			SetTime(Time);

			OnClockIdentifierChangedPartialNative(null, ClockIdentifier);

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view. 
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}
		}

		internal void UpdateTime()
		{
			if (_picker == null)
			{
				return;
			}

			var offset = TimeSpan.FromSeconds(_picker.TimeZone.GetSecondsFromGMT);
			var timeSpan = _picker.Date.ToTimeSpan().Add(offset);

			Time = timeSpan;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
		}

		partial void OnTimeChangedPartialNative(TimeSpan oldTime, TimeSpan newTime)
		{
			SetTime(newTime);
		}

		private void SetTime(TimeSpan newTime)
		{
			if (_picker == null)
			{
				return;
			}

			// Because the UIDatePicker is set to use LocalTimeZone,
			// we need to get the offset and apply it to the requested time.
			var offset = TimeSpan.FromSeconds(_picker.TimeZone.GetSecondsFromGMT);

			// Because the UIDatePicker applies the local timezone offset automatically when we set it,
			// we need to compensate with a negated offset. This will show the time
			// as if it was provided with no offset.
			var timeWithOffset = newTime.Add(offset.Negate());

			var nsDate = timeWithOffset.ToNSDate();

			_picker.SetDate(nsDate, animated: false);
		}

		partial void OnClockIdentifierChangedPartialNative(string oldClockIdentifier, string newClockIdentifier)
		{
			if (_picker == null)
			{
				return;
			}

			var localeID = newClockIdentifier == ClockIdentifiers.TwelveHour 
				? "en" 
				: "fr";

			_picker.Locale = new NSLocale(localeID);
		}
	}
}
