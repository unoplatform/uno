using System;
using System.Linq;
using Uno.Disposables;
using Foundation;
using UIKit;
using Uno.UI.Extensions;
using Uno.Extensions;
using Uno.Foundation.Logging;

using Uno.UI;
using Windows.Globalization;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePickerSelector
	{
		public static DependencyProperty UseNativeMinMaxDatesProperty { get; } = DependencyProperty.Register(
			"UseNativeMinMaxDates",
			typeof(bool),
			typeof(DatePickerSelector),
			new FrameworkPropertyMetadata(false, propertyChangedCallback: OnUseNativeMinMaxDatesChanged));

		/// <summary>
		/// Setting this to true will interpret MinYear/MaxYear as MinDate and MaxDate.
		/// </summary>
		public bool UseNativeMinMaxDates
		{
			get => (bool)GetValue(UseNativeMinMaxDatesProperty);
			set => SetValue(UseNativeMinMaxDatesProperty, value);
		}

		private static void OnUseNativeMinMaxDatesChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
		{
			if (o is DatePickerSelector selector)
			{
				selector.UpdateMinMaxYears();
			}
		}


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
			OverrideUIDatePickerTheme(this);

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

			var winCalendar = new global::Windows.Globalization.Calendar(
				Array.Empty<string>(),
				global::Windows.Globalization.CalendarIdentifiers.Gregorian,
				global::Windows.Globalization.ClockIdentifiers.TwentyFourHour);

			var calendar = new NSCalendar(NSCalendarType.Gregorian);

			winCalendar.SetDateTime(MaxYear);
			if (!UseNativeMinMaxDates)
			{
				winCalendar.Month = winCalendar.LastMonthInThisYear;
				winCalendar.Day = winCalendar.LastDayInThisMonth;
			}

			var maximumDateComponents = new NSDateComponents
			{
				Day = winCalendar.Day,
				Month = winCalendar.Month,
				Year = winCalendar.Year
			};

			_picker.MaximumDate = calendar.DateFromComponents(maximumDateComponents);

			winCalendar.SetDateTime(MinYear);
			if (!UseNativeMinMaxDates)
			{
				winCalendar.Month = winCalendar.FirstMonthInThisYear;
				winCalendar.Day = winCalendar.FirstDayInThisMonth;
			}

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
			if (_initialValue is { } initialDate)
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

			if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
			{
				_picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;

				return;
			}

			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 4))
			{
				_picker.PreferredDatePickerStyle = FeatureConfiguration.DatePicker.UseLegacyStyle
																			? UIDatePickerStyle.Wheels
																			: UIDatePickerStyle.Inline;
			}
		}

		internal static void OverrideUIDatePickerTheme(UIView picker)
		{
			// Force the background of the UIDatePicker to allow for proper
			// readability.
			UIDatePicker.Appearance.BackgroundColor =
				CoreApplication.RequestedTheme == SystemTheme.Dark
				? UIColor.Black
				: UIColor.White;

			picker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Dark
				? UIUserInterfaceStyle.Dark
				: UIUserInterfaceStyle.Light;

			// UIDatePicker does not allow for fine-grained control of its appearance:
			// https://developer.apple.com/documentation/uikit/uidatepicker
			//
			// UIDatePicker.Appearance.TintColor does not have any effect either, even when set
			// before the control is attached to the visual tree.
			//
			// Additionally, as of iOS 15, the following action does not have an effect on the UIDatePicker
			// _picker.SetValueForKey(UIColor.Yellow, new NSString("textColor"));
		}
	}
}
