using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => true;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// The activity that started the app already built a wrapper early in its lifecycle, before
		// any managed Window existed. The first window to be created is the one it drives, so it
		// adopts that wrapper rather than orphaning it.
		if (ResolveHostActivity() is { Wrapper.Window: null } activity)
		{
			return Bind(activity.Wrapper, window, xamlRoot);
		}

		// Every later window needs a task of its own to live in. The wrapper is created unbound and
		// only asks Android for that task when the window is actually activated (ShowCore), so
		// merely constructing a Window does not open one.
		return Bind(new NativeWindowWrapper(), window, xamlRoot);
	}

	private static NativeWindowWrapper Bind(NativeWindowWrapper wrapper, Window window, XamlRoot xamlRoot)
	{
		wrapper.SetWindow(window, xamlRoot);

		// The host owns the window's accessibility adapter, so a replaced window must release its own.
		wrapper.XamlRootHost?.Dispose();
		wrapper.XamlRootHost = new AndroidSkiaXamlRootHost(window, wrapper, xamlRoot);

		// The XamlRootMap is how consumers resolve the owning activity from a XamlRoot.
		XamlRootMap.Register(xamlRoot, wrapper.XamlRootHost);

		return wrapper;
	}

	// BaseActivity.Current is null while paused (e.g. OnLaunched awaiting a permission prompt),
	// so fall back to the most recently active live activity, which stays set until destroyed.
	internal static ApplicationActivity? ResolveHostActivity()
		=> BaseActivity.Current as ApplicationActivity
			?? (ContextHelper.TryGetCurrent(out var context) ? context as ApplicationActivity : null);
}
