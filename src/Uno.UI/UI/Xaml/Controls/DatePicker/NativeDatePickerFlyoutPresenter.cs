using System;
using Microsoft.UI.Xaml.Controls;
using NotImplementedException = System.NotImplementedException;

using DateTime = Windows.Foundation.WindowsFoundationDateTime;

#if __ANDROID__ || __IOS__
namespace Microsoft.UI.Xaml.Controls
{
	partial class NativeDatePickerFlyoutPresenter : FlyoutPresenter, IDatePickerFlyoutPresenter
	{
		public NativeDatePickerFlyoutPresenter()
		{
			DefaultStyleKey = typeof(NativeDatePickerFlyoutPresenter);
		}

		void IDatePickerFlyoutPresenter.PullPropertiesFromOwner(DatePickerFlyout pOwner)
		{

		}

		void IDatePickerFlyoutPresenter.SetAcceptDismissButtonsVisibility(bool isVisible)
		{

		}

		DateTime IDatePickerFlyoutPresenter.GetDate()
		{
#if __IOS__
			if (Content is DatePickerSelector selector)
			{
				selector.SaveValue();
				return selector.Date;
			}
#endif
			return default;
		}
	}
}
#endif
