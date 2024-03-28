using System;
using Uno;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePicker
	{
#if __IOS__ || __ANDROID__
		private const bool DEFAULT_NATIVE_STYLE = true;
#else
		private const bool DEFAULT_NATIVE_STYLE = false;
#endif

		[UnoOnly]
		public static DependencyProperty UseNativeStyleProperty { get; } = DependencyProperty.Register(
			"UseNativeStyle",
			typeof(bool),
			typeof(DatePicker),
			new FrameworkPropertyMetadata(DEFAULT_NATIVE_STYLE));

		/// <summary>
		/// [UnoOnly] If we should use the native picker for the platform.
		/// IMPORTANT: must be set before the first time the picker is opened.
		/// </summary>
		[UnoOnly]
		public bool UseNativeStyle
		{
			get => (bool)GetValue(UseNativeStyleProperty);
			set => SetValue(UseNativeStyleProperty, value);
		}

		[UnoOnly]
		public static DependencyProperty UseNativeMinMaxDatesProperty { get; } = DependencyProperty.Register(
			"UseNativeMinMaxDates",
			typeof(bool),
			typeof(DatePicker),
			new FrameworkPropertyMetadata(false));

		/// <summary>
		/// [UnoOnly] When using native pickers (through the UseNativeStyle property),
		/// setting this to true will interpret MinYear/MaxYear as MinDate and MaxDate.
		/// </summary>
		/// <remarks>
		/// This property has no effect when not using native pickers.
		/// </remarks>
		[UnoOnly]
		public bool UseNativeMinMaxDates
		{
			get => (bool)GetValue(UseNativeMinMaxDatesProperty);
			set => SetValue(UseNativeMinMaxDatesProperty, value);
		}

		/// <summary>
		/// FlyoutPresenterStyle is an Uno-only property to allow the styling of the DatePicker's FlyoutPresenter.
		/// </summary>
		[UnoOnly]
		public Style FlyoutPresenterStyle
		{
			get => (Style)this.GetValue(FlyoutPresenterStyleProperty);
			set => this.SetValue(FlyoutPresenterStyleProperty, value);
		}

		[UnoOnly]
		public static DependencyProperty FlyoutPresenterStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(FlyoutPresenterStyle),
				typeof(Style),
				typeof(DatePicker),
				new FrameworkPropertyMetadata(
					default(Style),
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));


		private Lazy<DatePickerFlyout> _lazyFlyout;

		private DatePickerFlyout _flyout => _lazyFlyout.Value;

		private void InitPartial()
		{
#if __IOS__ || __ANDROID__
			DatePickerFlyout CreateFlyout()
			{
				var f = UseNativeStyle
					? new NativeDatePickerFlyout()
					: CreateManagedDatePickerFlyout();

				f.DatePicked += OnPicked;

				return f;
			}

			_lazyFlyout = new Lazy<DatePickerFlyout>(CreateFlyout);
#else
			_lazyFlyout = new Lazy<DatePickerFlyout>(CreateManagedDatePickerFlyout);
#endif

			void OnPicked(DatePickerFlyout snd, DatePickedEventArgs evt)
			{
				SelectedDate = evt.NewDate;
				Date = evt.NewDate;

				if (evt.NewDate != evt.OldDate)
				{
					DateChanged?.Invoke(this, new DatePickerValueChangedEventArgs(evt.NewDate, evt.OldDate));
				}
			}

			DatePickerFlyout CreateManagedDatePickerFlyout()
			{
				var flyout = new DatePickerFlyout();

				if (FlyoutPresenterStyle is { } style)
				{
					flyout.DatePickerFlyoutPresenterStyle = style;
				}

				flyout.DatePicked += OnPicked;

				return flyout;
			}
		}
	}
}
