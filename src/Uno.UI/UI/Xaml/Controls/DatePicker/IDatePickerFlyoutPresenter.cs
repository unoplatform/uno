using System;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface IDatePickerFlyoutPresenter
	{
		void PullPropertiesFromOwner(DatePickerFlyout pOwner);
		void SetAcceptDismissButtonsVisibility(bool isVisible);
		DateTimeOffset GetDate();
	}
}
