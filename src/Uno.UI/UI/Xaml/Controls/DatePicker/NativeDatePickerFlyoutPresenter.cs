using System;
using Windows.UI.Xaml.Controls;
using NotImplementedException = System.NotImplementedException;

#if __ANDROID__ || __IOS__
namespace Windows.UI.Xaml.Controls
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

		DateTimeOffset IDatePickerFlyoutPresenter.GetDate()
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
