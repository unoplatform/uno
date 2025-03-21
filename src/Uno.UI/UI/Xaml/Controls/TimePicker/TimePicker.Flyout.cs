using System;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;
using Uno.UI;

namespace Windows.UI.Xaml.Controls;

partial class TimePicker
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
		typeof(TimePicker),
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
		typeof(TimePicker),
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
	/// FlyoutPresenterStyle is an Uno-only property to allow the styling of the TimePicker's FlyoutPresenter.
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
			typeof(TimePicker),
			new FrameworkPropertyMetadata(
				default(Style),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	internal static TimePickerFlyout CreateFlyout(TimePicker timePicker)
	{
		var useNativeStyle =
#if __IOS__ || __ANDROID__
			timePicker.UseNativeStyle;
#else
			false;
#endif

		var flyout = useNativeStyle ?
			new NativeTimePickerFlyout() :
			new TimePickerFlyout();

		if (useNativeStyle)
		{
#if __IOS__
			flyout.Placement = timePicker.FlyoutPlacement;
#endif
			if (timePicker.FlyoutPresenterStyle is not null)
			{
				flyout.TimePickerFlyoutPresenterStyle = timePicker.FlyoutPresenterStyle;
			}

			void OnPicked(PickerFlyoutBase snd, TimePickedEventArgs evt)
			{
				timePicker.SelectedTime = evt.NewTime;
				timePicker.Time = evt.NewTime;

				if (evt.NewTime != evt.OldTime)
				{
					timePicker.TimeChanged?.Invoke(timePicker, new TimePickerValueChangedEventArgs(evt.NewTime, evt.OldTime));
				}

				flyout.TimePicked -= OnPicked;
			}

			flyout.TimePicked += OnPicked;
		}

		return flyout;
	}
}
