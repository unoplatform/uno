#nullable enable

using Windows.Devices.Input;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal interface IWpfHost
	{
		XamlRoot? XamlRoot { get; }

		void ReleasePointerCapture(PointerIdentifier pointer);
		
		void SetPointerCapture(PointerIdentifier pointer);
	}
}
