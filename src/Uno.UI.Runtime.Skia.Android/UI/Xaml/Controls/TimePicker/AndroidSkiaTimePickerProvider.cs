using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaTimePickerProvider : ISkiaNativeTimePickerProviderExtension
{
	public TimePickerFlyout CreateNativeTimePickerFlyout() => new NativeTimePickerFlyout();
}
