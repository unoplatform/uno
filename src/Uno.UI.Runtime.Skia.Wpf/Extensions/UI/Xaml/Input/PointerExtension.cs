using System;
using Uno.UI.Runtime.Skia.Wpf.Hosting;
using Uno.UI.Xaml.Input;
using Windows.Devices.Input;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Wpf.Extensions.UI.Xaml.Input
{
	internal class PointerExtension : IPointerExtension
	{
		public void ReleasePointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot)
		{
			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			host?.ReleasePointerCapture(pointer);
		}
		
		public void SetPointerCapture(PointerIdentifier pointer, XamlRoot xamlRoot)
		{
			var host = XamlRootMap.GetHostForRoot(xamlRoot);
			host?.SetPointerCapture(pointer);
		}
	}
}
