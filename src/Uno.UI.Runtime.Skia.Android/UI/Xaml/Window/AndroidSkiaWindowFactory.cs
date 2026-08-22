using System;
using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => false;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// TODO #13827: with multiple windows this must resolve the activity that owns the window
		// being created rather than the current foreground one.
		var activity = BaseActivity.Current as ApplicationActivity
			?? throw new InvalidOperationException("No foreground ApplicationActivity is available to host the window.");

		var wrapper = activity.Wrapper;
		wrapper.SetWindow(window, xamlRoot);

		// Registering the host adds it to the XamlRootMap, which is how consumers resolve the
		// owning activity from a XamlRoot.
		_ = new AndroidSkiaXamlRootHost(window, xamlRoot, wrapper);

		return wrapper;
	}
}
