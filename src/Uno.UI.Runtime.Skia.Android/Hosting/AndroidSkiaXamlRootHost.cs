using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost
{
	private readonly Window _window;
	private readonly NativeWindowWrapper _wrapper;

	public AndroidSkiaXamlRootHost(Window window, XamlRoot xamlRoot, NativeWindowWrapper wrapper)
	{
		_window = window;
		_wrapper = wrapper;
		XamlRootMap.Register(xamlRoot, this);
	}

	// Resolved through the wrapper (not stored) so it follows the activity currently
	// driving the window across activity re-creation.
	internal ApplicationActivity Activity => _wrapper.CurrentActivity;

	void IXamlRootHost.InvalidateRender() => Activity.InvalidateRender();

	UIElement? IXamlRootHost.RootElement => _window.RootElement;

	/// <summary>
	/// Resolves the <see cref="ApplicationActivity"/> that owns the window hosting the given <see cref="XamlRoot"/>.
	/// </summary>
	internal static ApplicationActivity? GetActivity(XamlRoot? xamlRoot)
		=> xamlRoot is not null && XamlRootMap.GetHostForRoot(xamlRoot) is AndroidSkiaXamlRootHost host
			? host.Activity
			: null;
}
