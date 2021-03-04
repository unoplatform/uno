using System;
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

#if __IOS__ || __ANDROID__

		private Lazy<DatePickerFlyout> _lazyFlyout;

		private DatePickerFlyout _flyout => _lazyFlyout.Value;

#else
		private readonly DatePickerFlyout _flyout = new DatePickerFlyout();

#endif

		private void InitPartial()
		{
#if __IOS__ || __ANDROID__
			DatePickerFlyout CreateFlyout()
			{
				var f = UseNativeStyle
					? new NativeDatePickerFlyout()
					: new DatePickerFlyout();

				f.DatePicked += OnPicked;

				return f;
			}

			_lazyFlyout = new Lazy<DatePickerFlyout>(CreateFlyout);
#else
			_flyout.DatePicked += OnPicked;
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
		}
	}
}
