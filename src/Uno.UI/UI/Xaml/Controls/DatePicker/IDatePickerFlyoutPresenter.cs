using System;

using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	internal interface IDatePickerFlyoutPresenter
	{
		void PullPropertiesFromOwner(DatePickerFlyout pOwner);
		void SetAcceptDismissButtonsVisibility(bool isVisible);
		DateTime GetDate();
	}
}
