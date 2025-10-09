using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost
{
	public AndroidSkiaXamlRootHost(XamlRoot xamlRoot)
	{
		XamlRootMap.Register(xamlRoot, this);
	}

	void IXamlRootHost.InvalidateRender()
	{
		ApplicationActivity.Instance?.InvalidateRender();
	}

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;
}
