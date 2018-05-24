using System;
using System.Linq;
using Uno.Disposables;
using Foundation;
using UIKit;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerSelector
	{
		private UIDatePicker _picker;
		private readonly SerialDisposable _dateChangedSubscription = new SerialDisposable();

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>()
				.FirstOrDefault();

			var parent = _picker.FindFirstParent<FrameworkElement>();

			if (_picker != null)
			{
				_picker.Mode = UIDatePickerMode.Date;
				_picker.TimeZone = NSTimeZone.LocalTimeZone;
				_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);
				_picker.SetDate(Date.Date.ToNSDate(), animated: false);

				//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view. 
				if (parent != null)
				{
					parent.RemoveChild(_picker);
					parent.AddSubview(_picker);
				}

				RegisterValueChanged();
			}
		}

		private void RegisterValueChanged()
		{
			_dateChangedSubscription.Disposable = null;

			var picker = _picker;

			EventHandler handler = (s, e) =>
			{
				// When the timezone is east of GMT (negative offset) the UIDatePicker returns a Date that's [1 day]
				// after the visible selection from the spinner.
				// Also, when we change the month from summer to winter or vice-versa (when daylight saving changes),
				// the day is off by ± 1h. For that we always add 12 hours to avoid changing day (because midnight -1h is actually the day before).
				// Once we have a "rounded" datetime (around midday) we set the Date into the dependency property injecting
				// the timezone from the date picker (which is the local timezone)
				var dateTime = picker.Date.ToDateTime();
				var offset = TimeSpan.FromSeconds(picker.TimeZone.GetSecondsFromGMT);
				DateTime rounded;
				if (offset.TotalSeconds < 0)
				{
					rounded = dateTime.AddHours(-12);
				}
				else
				{
					rounded = dateTime.AddHours(12);
				}
				var final = new DateTimeOffset(rounded.Date, offset);
				Date = final;
			};

			picker.ValueChanged += handler;

			_dateChangedSubscription.Disposable = Disposable.Create(() => picker.ValueChanged -= handler);
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();
			_dateChangedSubscription.Disposable = null;
			_picker = null;
		}

		partial void OnDateChangedPartialNative(DateTimeOffset oldDate, DateTimeOffset newDate)
		{
			// We have to change the DateTimeOffset to something readable by the UIDatePicker.
			// First, we remove the offset to keep only the date.
			// Then, we add 24 hours (1 day) when the offset is negative (timezone east of GMT) because the UIDatePicker is wrong by 1 day in this case.
			var ajustedDate = newDate
				.ToOffset(TimeSpan.Zero)
				.Add(newDate.Offset);

			if (newDate.Offset.TotalSeconds < 0)
			{
				ajustedDate = ajustedDate.AddHours(24);
			}
			var nsDate = ajustedDate.Date.ToNSDate();

			// Animate to cover up the small delay in setting the date when the flyout is opened
			var animated = !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);

			_picker?.SetDate(nsDate, animated: animated);
		}

		partial void OnMinYearChangedPartialNative(DateTimeOffset oldMinYear, DateTimeOffset newMinYear)
		{
			if (_picker == null)
			{
				return;
			}

			var calendar = new NSCalendar(NSCalendarType.Gregorian);
			var minimumDateComponents = new NSDateComponents
			{
				Day = newMinYear.Day,
				Month = newMinYear.Month,
				Year = newMinYear.Year
			};

			_picker.MinimumDate = calendar.DateFromComponents(minimumDateComponents);
		}

		partial void OnMaxYearChangedPartialNative(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear)
		{
			if (_picker == null)
			{
				return;
			}
			
			var calendar = new NSCalendar(NSCalendarType.Gregorian);
			var maximumDateComponents = new NSDateComponents
			{
				Day = newMaxYear.Day,
				Month = newMaxYear.Month,
				Year = newMaxYear.Year
			};

			_picker.MaximumDate = calendar.DateFromComponents(maximumDateComponents);
		}
	}
}
