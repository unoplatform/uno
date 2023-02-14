using Windows.Devices.Input;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Input;

internal interface IPointerExtension
{
	void ReleasePointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot);

	void SetPointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot);
}
