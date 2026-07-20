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
		var activity = ResolveHostActivity()
			?? throw new InvalidOperationException("No live ApplicationActivity is available to host the window.");

		var wrapper = activity.Wrapper;
		wrapper.SetWindow(window, xamlRoot);

		// The XamlRootMap is how consumers resolve the owning activity from a XamlRoot.
		XamlRootMap.Register(xamlRoot, new AndroidSkiaXamlRootHost(window, wrapper, xamlRoot));

		return wrapper;
	}

	// TODO #8341: with multiple windows this must resolve the activity that owns the window
	// being created rather than an ambient one.
	// BaseActivity.Current is null while paused (e.g. OnLaunched awaiting a permission prompt),
	// so fall back to the most recently active live activity, which stays set until destroyed.
	internal static ApplicationActivity? ResolveHostActivity()
		=> BaseActivity.Current as ApplicationActivity
			?? (ContextHelper.TryGetCurrent(out var context) ? context as ApplicationActivity : null);
}
