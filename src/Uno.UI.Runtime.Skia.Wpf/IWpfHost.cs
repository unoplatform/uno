#nullable enable

using Windows.Devices.Input;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf
{
	internal interface IWpfHost
	{
		bool IsIsland { get; }

		UIElement? RootElement { get; }

		XamlRoot? XamlRoot { get; }

		WpfCanvas? NativeOverlayLayer { get;}

		public bool IgnorePixelScaling { get; }

		void ReleasePointerCapture();
		
		void SetPointerCapture();

		void InvalidateRender();
	}
}
