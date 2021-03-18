using System;
using System.Linq;
using Uno.Disposables;
using Foundation;
using UIKit;
using Uno.UI.Extensions;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.UI;
using Windows.Globalization;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerSelector
	{
		private UIDatePicker _picker;
		private NSDate _initialValue;
		private NSDate _newValue;

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>().FirstOrDefault();
			if (_picker == null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"No {nameof(UIDatePicker)} was found in the visual hierarchy.");
				}

				return;
			}

			_picker.Mode = UIDatePickerMode.Date;
			_picker.TimeZone = NSTimeZone.LocalTimeZone;
			_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);

			UpdatePickerStyle();

			UpdatePickerValue(Date, animated: false);

			_picker.ValueChanged += OnPickerValueChanged;

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view.
			var parent = _picker.FindFirstParent<FrameworkElement>();
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}

			UpdateMinMaxYears();
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_picker.ValueChanged -= OnPickerValueChanged;
			_picker = null;
		}

		private void OnPickerValueChanged(object sender, EventArgs e)
		{
			_newValue = _picker.Date;
		}

		partial void OnDateChangedPartialNative(DateTimeOffset oldDate, DateTimeOffset newDate)
		{
			if (newDate < MinYear)
			{
				Date = MinYear;
			}
			else if (newDate > MaxYear)
			{
				Date = MaxYear;
			}

			if (_picker != null)
			{
				// Animate to cover up the small delay in setting the date when the flyout is opened
				UpdatePickerValue(Date, animated: !UIDevice.CurrentDevice.CheckSystemVersion(10, 0));
			}
		}

		partial void OnMinYearChangedPartialNative(DateTimeOffset oldMinYear, DateTimeOffset newMinYear)
			=> UpdateMinMaxYears();

		partial void OnMaxYearChangedPartialNative(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear)
			=> UpdateMinMaxYears();

		private void UpdateMinMaxYears()
		{
			if (_picker == null)
			{
				return;
			}

			// TODO: support non-gregorian calendars

			var winCalendar = new Windows.Globalization.Calendar(
				new string[0],
				Windows.Globalization.CalendarIdentifiers.Gregorian,
				Windows.Globalization.ClockIdentifiers.TwentyFourHour);

			var calendar = new NSCalendar(NSCalendarType.Gregorian);

			winCalendar.SetDateTime(MaxYear);
			winCalendar.Month = winCalendar.LastMonthInThisYear;
			winCalendar.Day = winCalendar.LastDayInThisMonth;

			var maximumDateComponents = new NSDateComponents
			{
				Day = winCalendar.Day,
				Month = winCalendar.Month,
				Year = winCalendar.Year
			};

			_picker.MaximumDate = calendar.DateFromComponents(maximumDateComponents);

			winCalendar.SetDateTime(MinYear);
			winCalendar.Month = winCalendar.FirstMonthInThisYear;
			winCalendar.Day = winCalendar.FirstDayInThisMonth;

			var minimumDateComponents = new NSDateComponents
			{
				Day = winCalendar.Day,
				Month = winCalendar.Month,
				Year = winCalendar.Year
			};

			_picker.MinimumDate = calendar.DateFromComponents(minimumDateComponents);
		}

		internal void SaveValue()
		{
			if (_picker != null)
			{
				if (_newValue != null && _newValue != _initialValue)
				{
					Date = ConvertFromNative(_newValue);
					_initialValue = _newValue;
				}

				_picker.EndEditing(false);
			}
		}

		internal void Cancel()
		{
			if (_initialValue is {} initialDate)
			{
				_picker?.SetDate(initialDate, false);
			}
			_picker?.EndEditing(false);
		}

		private void UpdatePickerValue(DateTimeOffset value, bool animated = false)
		{
			var components = new NSDateComponents()
			{
				Year = value.Year,
				Month = value.Month,
				Day = value.Day,
			};
			var date = _picker.Calendar.DateFromComponents(components);

			_picker.SetDate(date, animated);
			_initialValue = date;
		}

		private DateTimeOffset ConvertFromNative(NSDate value)
		{
			var components = _picker.Calendar.Components(NSCalendarUnit.Year | NSCalendarUnit.Month | NSCalendarUnit.Day, value);
			var date = new DateTimeOffset(
				(int)components.Year, (int)components.Month, (int)components.Day,
				Date.Hour, Date.Minute, Date.Second, Date.Millisecond,
				Date.Offset
			);

			return date;
		}

		private void UpdatePickerStyle()
		{
			if (_picker == null)
			{
				return;
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
			{
				_picker.PreferredDatePickerStyle = FeatureConfiguration.DatePicker.UseLegacyStyle
																			? UIDatePickerStyle.Wheels
																			: UIDatePickerStyle.Inline;
			}
		}
	}
}
