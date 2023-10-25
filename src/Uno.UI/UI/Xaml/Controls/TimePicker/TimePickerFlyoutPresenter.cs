#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents the visual container for the TimePickerFlyout.
/// </summary>
public partial class TimePickerFlyoutPresenter : FlyoutPresenter
{
#if !UNO_REFERENCE_API
	internal TimePickerFlyoutPresenter()
	{
		DefaultStyleKey = typeof(TimePickerFlyoutPresenter);
	}
#endif
}
