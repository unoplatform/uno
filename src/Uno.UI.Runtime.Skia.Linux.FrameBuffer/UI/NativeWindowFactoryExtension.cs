#nullable enable

using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Windows.UI.Xaml;

namespace Uno.UI.Runtime.Skia.Gtk.Extensions.UI.Xaml.Controls;

internal class NativeWindowFactoryExtension : INativeWindowFactoryExtension
{
	private readonly IXamlRootHost _host;

	internal NativeWindowFactoryExtension(IXamlRootHost host)
	{
		_host = host;
	}

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// TODO:MZ: Prevent calling this multiple times
		FrameBufferWindowWrapper.Instance.SetWindow(window, xamlRoot);
		FrameBufferManager.XamlRootMap.Register(xamlRoot, _host);

		return FrameBufferWindowWrapper.Instance;
	}
}
