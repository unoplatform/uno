#nullable enable

using Windows.Devices.Input;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal interface IWpfHost
	{
		bool IsIsland { get; }

		Visual? Visual { get; }

		XamlRoot? XamlRoot { get; }

		WpfCanvas? NativeOverlayLayer { get;}

		public bool IgnorePixelScaling { get; }

		void ReleasePointerCapture(PointerIdentifier pointer);
		
		void SetPointerCapture(PointerIdentifier pointer);
	}
}
