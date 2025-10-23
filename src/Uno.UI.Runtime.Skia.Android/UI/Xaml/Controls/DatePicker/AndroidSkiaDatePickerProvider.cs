using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaDatePickerProvider : ISkiaNativeDatePickerProviderExtension
{
	public DatePickerFlyout CreateNativeDatePickerFlyout() => new NativeDatePickerFlyout();
}
