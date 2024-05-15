#if __ANDROID__ || __IOS__
namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeTimePickerFlyoutPresenter : FlyoutPresenter
{
	public NativeTimePickerFlyoutPresenter()
	{
		DefaultStyleKey = typeof(NativeTimePickerFlyoutPresenter);
	}
}
#endif
