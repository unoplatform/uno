using System;
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => false;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// TODO #8341: with multiple windows this must resolve the activity that owns the window
		// being created rather than the current foreground one.
		var activity = BaseActivity.Current as ApplicationActivity
			?? throw new InvalidOperationException("No foreground ApplicationActivity is available to host the window.");

		var wrapper = activity.Wrapper;
		wrapper.SetWindow(window, xamlRoot);

		// The XamlRootMap is how consumers resolve the owning activity from a XamlRoot.
		XamlRootMap.Register(xamlRoot, new AndroidSkiaXamlRootHost(window, wrapper));

		return wrapper;
	}
}
