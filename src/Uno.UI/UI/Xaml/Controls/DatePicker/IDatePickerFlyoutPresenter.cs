using System;

namespace Windows.UI.Xaml.Controls
{
	internal interface IDatePickerFlyoutPresenter
	{
		void PullPropertiesFromOwner(DatePickerFlyout pOwner);
		void SetAcceptDismissButtonsVisibility(bool isVisible);
		DateTimeOffset GetDate();
	}
}
