#nullable enable

using Windows.Devices.Input;
using Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal interface IWpfHost
	{
		XamlRoot? XamlRoot { get; }

		WpfCanvas? NativeOverlayLayer { get;}

		void ReleasePointerCapture(PointerIdentifier pointer);
		
		void SetPointerCapture(PointerIdentifier pointer);
	}
}
