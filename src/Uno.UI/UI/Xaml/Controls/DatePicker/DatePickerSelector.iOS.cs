using System;
using System.Linq;
using Uno.Disposables;
using Foundation;
using UIKit;
using Uno.UI.Extensions;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerSelector
	{
		private UIDatePicker _picker;
		private NSDate _initialValue;
		private NSDate _newValue;

		protected override void OnLoaded()
		{
			base.OnLoaded();

			_picker = this.FindSubviewsOfType<UIDatePicker>().FirstOrDefault();
			if (_picker == null)
			{
				this.Log().DebugIfEnabled(() => $"No {nameof(UIDatePicker)} was found in the visual hierarchy.");
				return;
			}

			_picker.Mode = UIDatePickerMode.Date;
			_picker.TimeZone = NSTimeZone.LocalTimeZone;
			_picker.Calendar = new NSCalendar(NSCalendarType.Gregorian);
			UpdatePickerValue(Date, animated: false);

			_picker.ValueChanged += OnPickerValueChanged;

			//Removing the date picker and adding it is what enables the lines to appear. Seems to be a side effect of adding it as a view.
			var parent = _picker.FindFirstParent<FrameworkElement>();
			if (parent != null)
			{
				parent.RemoveChild(_picker);
				parent.AddSubview(_picker);
			}

			UpdatMinMaxYears();
		}

		protected override void OnUnloaded()
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
			// Animate to cover up the small delay in setting the date when the flyout is opened
			var animated = !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);

			if (newDate < MinYear)
			{
				Date = MinYear;
			}
			else if (newDate > MaxYear)
			{
				Date = MaxYear;
			}

			// todo: replace with UpdatePickerValue
			_picker?.SetDate(
				DateTime.SpecifyKind(Date.DateTime, DateTimeKind.Local).ToNSDate(),
				animated: animated
			);
		}

		partial void OnMinYearChangedPartialNative(DateTimeOffset oldMinYear, DateTimeOffset newMinYear)
			=> UpdatMinMaxYears();

		partial void OnMaxYearChangedPartialNative(DateTimeOffset oldMaxYear, DateTimeOffset newMaxYear)
			=> UpdatMinMaxYears();

		private void UpdatMinMaxYears()
		{
			if (_picker == null)
			{
				return;
			}

			var calendar = new NSCalendar(NSCalendarType.Gregorian);
			var maximumDateComponents = new NSDateComponents
			{
				Day = MaxYear.Day,
				Month = MaxYear.Month,
				Year = MaxYear.Year
			};

			_picker.MaximumDate = calendar.DateFromComponents(maximumDateComponents);

			var minimumDateComponents = new NSDateComponents
			{
				Day = MinYear.Day,
				Month = MinYear.Month,
				Year = MinYear.Year
			};

			_picker.MinimumDate = calendar.DateFromComponents(minimumDateComponents);
		}

		internal void SaveValue()
		{
			if (_picker != null)
			{
				if (_newValue != null && _newValue != _initialValue)
				{
					var value = GetValueFromPicker();
					if (Date.Year != value.Year || Date.Month != value.Month || Date.Day != value.Day) // fixme: compare date-only
					{
						Date = value;
						_initialValue = _newValue;
					}
				}

				_picker.EndEditing(false);
			}
		}

		internal void Cancel()
		{
			_picker?.SetDate(_initialValue, false);
			_picker?.EndEditing(false);
		}

		private void UpdatePickerValue(DateTimeOffset value, bool animated = false)
		{
			var date = value.Date.ToNSDate(); // fixme
			_picker?.SetDate(date, animated);
			_initialValue = date;
		}

		private DateTimeOffset GetValueFromPicker()
		{
			return new DateTimeOffset(_picker.Date.ToDateTime()); // fixme
		}
	}
}
