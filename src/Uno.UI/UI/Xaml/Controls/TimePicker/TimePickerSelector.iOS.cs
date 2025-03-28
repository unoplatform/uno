using Foundation;
using System;
using System.Linq;
using UIKit;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.UI;
using Uno.UI.Extensions;
using Windows.Globalization;

namespace Windows.UI.Xaml.Controls
{
	public partial class TimePickerSelector
	{
		private UIDatePicker _picker;
		private NSDate _initialTime;
		private NSDate _newDate;

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>().FirstOrDefault();

			if (_picker == null)
			{
				this.Log().Debug($"No {nameof(UIDatePicker)} was found in the visual hierarchy.");
				return;
			}

			// The time zone must be the same as DatePickerSelector
			// otherwise, if a DatePicker and a TimePicker are present
			// with 2 different time zones, they will interfer one with the other
			// and result in weird behaviors (like decrementing the year when a month changes).
			_picker.TimeZone = NSTimeZone.LocalTimeZone;
			_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);
			_picker.Mode = UIDatePickerMode.Time;

			UpdatePickerStyle();
			DatePickerSelector.OverrideUIDatePickerTheme(this);
			SetPickerTime(Time.RoundToNextMinuteInterval(MinuteIncrement));
			SetPickerClockIdentifier(ClockIdentifier);
			SaveInitialTime();
			_picker.ValueChanged += OnValueChanged;
			_picker.EditingDidBegin += OnEditingDidBegin;

			var parent = _picker.FindFirstParent<FrameworkElement>();

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view. 
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}
		}

		private void OnEditingDidBegin(object sender, EventArgs e)
		{
			//We don't want the keyboard to be shown. https://github.com/unoplatform/uno/issues/4611
			_picker?.ResignFirstResponder();
		}

		private void OnValueChanged(object sender, EventArgs e)
		{
			_newDate = _picker.Date;
		}

		public void Initialize()
		{
			UpdatePickerStyle();
			SetPickerClockIdentifier(ClockIdentifier);
			SetPickerMinuteIncrement(MinuteIncrement);
		}

		private void SetPickerMinuteIncrement(int minuteIncrement)
		{
			if (_picker != null)
			{
				_picker.MinuteInterval = minuteIncrement;
			}
		}

		private void SaveInitialTime() => _initialTime = _picker?.Date;

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

		private protected override void OnUnloaded()
		{
			_picker.ValueChanged -= OnValueChanged;

			base.OnUnloaded();
		}

		private void UpdatePickerStyle()
		{
			if (_picker == null)
			{
				return;
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				_picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;

				return;
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
			{
				_picker.PreferredDatePickerStyle = FeatureConfiguration.TimePicker.UseLegacyStyle
																			? UIDatePickerStyle.Wheels
																			: UIDatePickerStyle.Inline;
			}
		}
	}
}
