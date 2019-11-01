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

			UpdatMinMaxYears();
		}

		private void RegisterValueChanged()
		{
			_dateChangedSubscription.Disposable = null;

			var picker = _picker;

			EventHandler handler = (s, e) =>
			{
				Date = new DateTimeOffset(picker.Date.ToDateTime());
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
			// Animate to cover up the small delay in setting the date when the flyout is opened
			var animated = !UIDevice.CurrentDevice.CheckSystemVersion(10, 0);

			if (newDate < MinYear )
			{
				Date = MinYear;
			}
			else if(newDate > MaxYear)
			{
				Date = MaxYear;
			}

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
	}
}
