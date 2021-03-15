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

		public static DependencyProperty UseNativeStyleProperty { get; } = DependencyProperty.Register(
			"UseNativeStyle",
			typeof(bool),
			typeof(DatePicker),
			new PropertyMetadata(DEFAULT_NATIVE_STYLE));

		/// <summary>
		/// If we should use the native picker for the platform.
		/// IMPORTANT: must be set before the first time the picker is opened.
		/// </summary>
		public bool UseNativeStyle
		{
			get => (bool)GetValue(UseNativeStyleProperty);
			set => SetValue(UseNativeStyleProperty, value);
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
				var flyout = new DatePickerFlyout() { DatePickerFlyoutPresenterStyle = FlyoutPresenterStyle };
				flyout.DatePicked += OnPicked;

				return flyout;
			}
		}
	}
}
