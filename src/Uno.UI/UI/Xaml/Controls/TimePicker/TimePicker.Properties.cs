using System;
using Windows.Foundation;
using Windows.Globalization;

namespace Windows.UI.Xaml.Controls;

partial class TimePicker
{
	/// <summary>
	/// Gets or sets the clock system to use.
	/// </summary>
	public string ClockIdentifier
	{
		get => (string)GetValue(ClockIdentifierProperty);
		set => SetValue(ClockIdentifierProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the ClockIdentifier dependency property.
	/// </summary>
	public static DependencyProperty ClockIdentifierProperty { get; } =
		DependencyProperty.Register(
			nameof(ClockIdentifier),
			typeof(string),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(
				defaultValue: (string)ClockIdentifiers.TwelveHour,
				options: FrameworkPropertyMetadataOptions.None,
				propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnClockIdentifierChangedPartial((string)e.OldValue, (string)e.NewValue)
			)
		);

	partial void OnClockIdentifierChangedPartial(string oldValue, string newValue);

	/// <summary>
	/// Gets or sets the content for the control's header.
	/// </summary>
	public object Header
	{
		get => GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(default(object)));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the control's header.
	/// </summary>
	public DataTemplate HeaderTemplate
	{
		get => (DataTemplate)GetValue(HeaderTemplateProperty);
		set => SetValue(HeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the HeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty HeaderTemplateProperty { get; } =
		DependencyProperty.Register(
			nameof(HeaderTemplate),
			typeof(DataTemplate),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(default(DataTemplate)));

	/// <summary>
	/// Gets or sets a value that specifies whether the area outside of a light-dismiss UI is darkened.
	/// </summary>
	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)GetValue(LightDismissOverlayModeProperty);
		set => SetValue(LightDismissOverlayModeProperty, value);
	}

	/// <summary>
	/// Identifies the LightDismissOverlayMode dependency property.
	/// </summary>
	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

	/// <summary>
	/// Gets or sets a value that indicates the time increments shown in the minute picker. 
	/// For example, 15 specifies that the TimePicker minute control displays only the choices 00, 15, 30, 45.
	/// </summary>
	public int MinuteIncrement
	{
		get => (int)GetValue(MinuteIncrementProperty);
		set => SetValue(MinuteIncrementProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the MinuteIncrement dependency property.
	/// </summary>
	public static DependencyProperty MinuteIncrementProperty { get; } =
		DependencyProperty.Register(
			nameof(MinuteIncrement),
			typeof(int),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(
				defaultValue: 1,
				options: FrameworkPropertyMetadataOptions.None,
				propertyChangedCallback: (s, e) => ((TimePicker)s)?.OnMinuteIncrementChanged((int)e.OldValue, (int)e.NewValue),
				coerceValueCallback: (s, e, _) =>
				{
					var value = (int)e;

					if (value < 1)
					{
						return 1;
					}

					if (value > 30)
					{
						return 30;
					}

					return value;
				}));

	/// <summary>
	/// Gets or sets the time currently selected in the time picker.
	/// </summary>
	public TimeSpan? SelectedTime
	{
		get => (TimeSpan?)GetValue(SelectedTimeProperty);
		set => SetValue(SelectedTimeProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedTime dependency property.
	/// </summary>
	public static DependencyProperty SelectedTimeProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedTime),
			typeof(TimeSpan?),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				FrameworkPropertyMetadataOptions.None));

	/// <summary>
	/// Gets or sets the time currently set in the time picker.
	/// </summary>
	public TimeSpan Time
	{
		get => (TimeSpan)GetValue(TimeProperty);
		set => SetValue(TimeProperty, value);
	}

	/// <summary>
	/// Gets the identifier for the Time dependency property.
	/// </summary>
	public static DependencyProperty TimeProperty { get; } =
		DependencyProperty.Register(
			nameof(Time),
			typeof(TimeSpan),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(
				defaultValue: new TimeSpan(-1),
				options: FrameworkPropertyMetadataOptions.None));

	/// <summary>
	/// Occurs when the value of the Time property is changed.
	/// </summary>
	public event EventHandler<TimePickerValueChangedEventArgs> TimeChanged;

	/// <summary>
	/// Occurs when the value of the SelectedTime property is changed.
	/// </summary>
	public event TypedEventHandler<TimePicker, TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged;
}
