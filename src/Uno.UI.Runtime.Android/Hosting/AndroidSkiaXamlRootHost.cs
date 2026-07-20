#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Runtime.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost, IAccessibilityOwner
{
	private readonly Window _window;
	private readonly NativeWindowWrapper _wrapper;
	private readonly AndroidSkiaAccessibility _accessibility;

	public AndroidSkiaXamlRootHost(Window window, NativeWindowWrapper wrapper, XamlRoot xamlRoot)
	{
		_window = window;
		_wrapper = wrapper;
		_accessibility = new AndroidSkiaAccessibility(xamlRoot);
		TryConfigureHelper();
	}

	// IAccessibilityOwner
	SkiaAccessibilityBase? IAccessibilityOwner.Accessibility => _accessibility;

	/// <summary>
	/// Tries to connect the accessibility adapter to the render view's helper.
	/// Idempotent via <see cref="AndroidSkiaAccessibility.Configure"/>.
	/// </summary>
	internal void TryConfigureHelper()
	{
		if (_wrapper.CurrentActivity?.RenderView is { ExploreByTouchHelper: { } helper })
		{
			_accessibility.Configure(helper);
		}
	}

	// Resolved through the wrapper (not stored) so it follows the activity currently
	// driving the window across activity re-creation.
	internal ApplicationActivity Activity => _wrapper.CurrentActivity;

	internal AndroidCorePointerInputSource PointerSource => _wrapper.PointerSource;

	internal AndroidKeyboardInputSource KeyboardSource => _wrapper.KeyboardSource;

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
