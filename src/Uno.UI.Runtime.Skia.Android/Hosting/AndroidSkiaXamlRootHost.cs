using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Uno.Foundation.Extensibility;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost
{
	// Published by whichever view negotiated (canvas, Vulkan or WebGPU), so this window's CompositionTarget can
	// resolve a backend before it records rather than being handed one mid-frame.
	private IDrawingFactory? _renderer;

	public AndroidSkiaXamlRootHost(XamlRoot xamlRoot)
	{
		XamlRootMap.Register(xamlRoot, this);
	}

	internal static void Publish(IDrawingFactory renderer)
	{
		if (Window.Current?.RootElement?.XamlRoot is { } xamlRoot
			&& XamlRootMap.GetHostForRoot(xamlRoot) is AndroidSkiaXamlRootHost host)
		{
			host._renderer = renderer;
		}
	}

	IDrawingFactory? IXamlRootHost.Renderer => _renderer;

	void IXamlRootHost.InvalidateRender()
	{
		ApplicationActivity.Instance?.InvalidateRender();
	}

	UIElement? IXamlRootHost.RootElement => Window.Current!.RootElement;
}
