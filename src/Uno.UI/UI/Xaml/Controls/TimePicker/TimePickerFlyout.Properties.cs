#nullable enable

using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

partial class TimePickerFlyout
{
	/// <summary>
	/// Gets or sets the clock system to use.
	/// </summary>
	public string ClockIdentifier
	{
		get => (string)this.GetValue(ClockIdentifierProperty);
		set => SetValue(ClockIdentifierProperty, value);
	}

	public static DependencyProperty ClockIdentifierProperty { get; } =
		DependencyProperty.Register(
			nameof(ClockIdentifier),
			typeof(string),
			typeof(TimePickerFlyout),
			new FrameworkPropertyMetadata(
				defaultValue: (string)Windows.Globalization.ClockIdentifiers.TwelveHour,
				options: FrameworkPropertyMetadataOptions.None)
		);

	public int MinuteIncrement
	{
		get => (int)GetValue(MinuteIncrementProperty);
		set => SetValue(MinuteIncrementProperty, value);
	}

	public static DependencyProperty MinuteIncrementProperty { get; } =
		DependencyProperty.Register(
			nameof(MinuteIncrement),
			typeof(int),
			typeof(TimePickerFlyout),
			new FrameworkPropertyMetadata(
				defaultValue: (int)1,
				options: FrameworkPropertyMetadataOptions.None)
		);

	public TimeSpan Time
	{
		get => (TimeSpan)this.GetValue(TimeProperty);
		set => SetValue(TimeProperty, value);
	}

	public static DependencyProperty TimeProperty { get; } =
		DependencyProperty.Register(
			nameof(Time),
			typeof(TimeSpan),
			typeof(TimePickerFlyout),
			new FrameworkPropertyMetadata(
				defaultValue: (TimeSpan)DateTime.Now.TimeOfDay,
				options: FrameworkPropertyMetadataOptions.None)
		);

	/// <summary>
	/// Occurs when the user has selected a time in the time picker flyout.
	/// </summary>
	public event TypedEventHandler<TimePickerFlyout, TimePickedEventArgs> TimePicked;
}
