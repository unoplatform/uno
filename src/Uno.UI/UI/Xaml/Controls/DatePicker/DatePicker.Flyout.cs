using System;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	partial class DatePicker
	{


#if __IOS__ || __ANDROID__
		public static readonly DependencyProperty UseNativeStyleProperty = DependencyProperty.Register(
			"UseNativeStyle",
			typeof(bool),
			typeof(DatePicker),
			new PropertyMetadata(!FeatureConfiguration.Style.UseUWPDefaultStyles));

		public bool UseNativeStyle
		{
			get { return (bool)GetValue(UseNativeStyleProperty); }
			set { SetValue(UseNativeStyleProperty, value); }
		}

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
