using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.AppleUIKit;
using Uno.UI.Runtime.Skia.AppleUIKit.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Hosting;

internal class AppleUIKitXamlRootHost : IAppleUIKitXamlRootHost
{
	public AppleUIKitXamlRootHost(XamlRoot xamlRoot)
	{
		AppManager.XamlRootMap.Register(xamlRoot, this);
	}

	void IXamlRootHost.InvalidateRender()
	{
	}

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;
}
