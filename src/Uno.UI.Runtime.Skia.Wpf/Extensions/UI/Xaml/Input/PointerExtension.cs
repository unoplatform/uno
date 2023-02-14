using Uno.UI.Xaml.Input;
using Uno.UI.XamlHost.Skia.Wpf.Hosting;
using Windows.Devices.Input;
using Microsoft.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Input
{
	internal class PointerExtension : IPointerExtension
	{
		public void ReleasePointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot)
		{
			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			host?.ReleasePointerCapture();
		}

		public void SetPointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot)
		{
			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			host?.SetPointerCapture();
		}
	}
}
