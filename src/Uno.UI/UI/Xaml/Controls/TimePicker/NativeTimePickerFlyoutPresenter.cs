#if __ANDROID__ || __APPLE_UIKIT__
namespace Microsoft.UI.Xaml.Controls;

internal partial class NativeTimePickerFlyoutPresenter : FlyoutPresenter
{
	public NativeTimePickerFlyoutPresenter()
	{
		DefaultStyleKey = typeof(NativeTimePickerFlyoutPresenter);
	}
}
#endif
