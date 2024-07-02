using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Uno.UI.Xaml.Controls;

internal static class SelectionExports
{
	internal static IAsyncOperation<TimeSpan?> XamlControls_GetTimePickerSelection(
		TimePicker pSourceTimePicker,
		object pPlacementTarget)
	{
		// Pointer to the operation that this function will return to
		// the caller (a on-page TimePicker)		

		TimePicker spTimePicker;

		// The DatePickerFlyout we will create and invoke, and the
		// IAsyncAction it will return.

		// Collection of properties needed to be copied from
		// the DatePicker into the DatePickerFlyout		

		// PickerFlyoutBase statics, used to retrieve title attached property.		
		spTimePicker = pSourceTimePicker;

		var spPlacementTargetAsFE = pPlacementTarget as FrameworkElement;

		var spTimePickerFlyout = TimePicker.CreateFlyout(spTimePicker);

		// Copy over all the relevant properties to show the
		// Flyout with.
		var clockIdentifier = spTimePicker.ClockIdentifier;
		var minuteInterval = spTimePicker.MinuteIncrement;
		var ts = spTimePicker.Time;
		var overlayMode = spTimePicker.LightDismissOverlayMode;
		var soundMode = PlatformHelpers.GetEffectiveSoundMode(spTimePicker);

		// See if the calling TimePicker has an attached Title, if not
		// retrieve the default phrase.
		var title = PickerFlyoutBase.GetTitle(spTimePicker);

		if (title == null)
		{
			title = ResourceAccessor.GetLocalizedStringResource("TEXT_TIMEPICKERFLYOUT_TITLE") ?? "";
		}

		var spFlyoutBase = spTimePickerFlyout;
		PickerFlyoutBase.SetTitle(spTimePickerFlyout, title);
		spTimePickerFlyout.ClockIdentifier = clockIdentifier;
		spTimePickerFlyout.MinuteIncrement = minuteInterval;
		spTimePickerFlyout.Time = ts;
		spFlyoutBase.LightDismissOverlayMode = overlayMode;
		spFlyoutBase.ElementSoundMode = soundMode;

		// Actually cause the window to popup. This is the async operation that
		// will return with the Flyout is dismissed
		return spTimePickerFlyout.ShowAtAsync(spPlacementTargetAsFE);
	}
}
