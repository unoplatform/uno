using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidSkiaDatePickerProvider : ISkiaNativeDatePickerProviderExtension
{
	public DatePickerFlyout CreateNativeDatePickerFlyout() => new NativeDatePickerFlyout();
}
