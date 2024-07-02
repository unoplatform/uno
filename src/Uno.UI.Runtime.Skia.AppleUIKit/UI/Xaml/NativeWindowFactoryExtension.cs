using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.AppleUIKit.Hosting;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => UIApplication.SharedApplication.SupportsMultipleScenes;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// While we are currently not having something very useful in the root host, instantiating it has side effects that we need.
		// So this line isn't unnecessary ;)
		_ = new AppleUIKitXamlRootHost(xamlRoot);
		return new NativeWindowWrapper(window, xamlRoot);
	}
}
