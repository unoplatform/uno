using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.UI.Xaml.Controls;

internal class SelectionExports
{
	internal void XamlControls_GetTimePickerSelectionImpl(
		TimePicker pSourceTimePicker,
		object pPlacementTarget,
		_Outptr_ IInspectable **ppSelectionResultOperation)
	{
		// Pointer to the operation that this function will return to
		// the caller (a on-page TimePicker)		

		TimePicker spTimePicker;

		// The DatePickerFlyout we will create and invoke, and the
		// IAsyncAction it will return.

		// Collection of properties needed to be copied from
		// the DatePicker into the DatePickerFlyout
		string clockIdentifier;
		TimeSpan ts = default;
		int minuteInterval = 0;
		string tring title;
		var overlayMode = LightDismissOverlayMode.Off;
		var soundMode = ElementSoundMode.Default;

		// PickerFlyoutBase statics, used to retrieve title attached property.		
		spTimePicker = pSourceTimePicker;

		pPlacementTarget->QueryInterface<xaml::IFrameworkElement>(&spPlacementTargetAsFE));
		IFCPTR(spPlacementTargetAsFE.Get());

		var spTimePickerFlyout = new TimePickerFlyout();

		// Copy over all the relevant properties to show the
		// Flyout with.
		clockIdentifier = spTimePicker.ClockIdentifier;
		minuteInterval = spTimePicker.MinuteIncrement;
		ts = spTimePicker.Time;
		overlayMode = spTimePicker.LightDismissOverlayMode;
		soundMode = PlatformHelpers.GetEffectiveSoundMode(spTimePickerAsDO);

		// See if the calling TimePicker has an attached Title, if not
		// retrieve the default phrase.
		wf::GetActivationFactory(
			wrl_wrappers::HStringReference(RuntimeClass_Microsoft_UI_Xaml_Controls_Primitives_PickerFlyoutBase).Get(),
			&spPickerFlyoutBaseStatics));
		spPickerFlyoutBaseStatics->GetTitle(spTimePickerAsDO.Get(), title.GetAddressOf()));

		if (title == nullptr)
		{
			Private::FindStringResource(
				TEXT_TIMEPICKERFLYOUT_TITLE,
				title.GetAddressOf()));
		}

		spPickerFlyoutBaseStatics->SetTitle(spTimePickerFlyoutAsDO.Get(), title));
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
