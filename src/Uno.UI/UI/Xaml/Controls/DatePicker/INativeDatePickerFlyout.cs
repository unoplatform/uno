namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Implemented by a platform-provided <see cref="DatePickerFlyout"/> so that <see cref="DatePicker"/> can forward
/// its Uno-specific settings without referencing the platform assembly that supplies the flyout.
/// </summary>
internal interface INativeDatePickerFlyout
{
	bool UseNativeMinMaxDates { get; set; }
}
