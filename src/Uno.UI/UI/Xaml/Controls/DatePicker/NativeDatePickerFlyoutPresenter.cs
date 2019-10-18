using Windows.UI.Xaml.Controls;

#if __ANDROID__ || __IOS__
namespace Windows.UI.Xaml.Controls
{
	partial class NativeDatePickerFlyoutPresenter : FlyoutPresenter
	{
		public NativeDatePickerFlyoutPresenter()
		{
			DefaultStyleKey = typeof(NativeDatePickerFlyoutPresenter);
		}
	}
}
#endif
