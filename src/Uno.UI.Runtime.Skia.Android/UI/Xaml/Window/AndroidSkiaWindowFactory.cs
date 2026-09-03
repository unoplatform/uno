using Microsoft.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaWindowFactory : INativeWindowFactoryExtension
{
	public bool SupportsMultipleWindows => true;

	public bool SupportsClosingCancellation => false;

	public INativeWindowWrapper CreateWindow(Window window, XamlRoot xamlRoot)
	{
		// The activity that started the app already built a wrapper early in its lifecycle, before
		// any managed Window existed. The first window to be created is the one it drives, so it
		// adopts that wrapper rather than orphaning it.
		if (BaseActivity.Current is ApplicationActivity { Wrapper.Window: null } activity)
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

		// Registering the host adds it to the XamlRootMap, which is how consumers resolve the
		// owning activity from a XamlRoot.
		_ = new AndroidSkiaXamlRootHost(window, xamlRoot, wrapper);

		return wrapper;
	}
}
