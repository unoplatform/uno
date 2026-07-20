#nullable enable

using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidSkiaXamlRootHost : IXamlRootHost, IAccessibilityOwner
{
	private readonly XamlRoot _xamlRoot;
	private readonly AndroidSkiaAccessibility _accessibility;

	public AndroidSkiaXamlRootHost(XamlRoot xamlRoot)
	{
		_xamlRoot = xamlRoot;
		XamlRootMap.Register(xamlRoot, this);
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
		if (ApplicationActivity.RenderView is { ExploreByTouchHelper: { } helper })
		{
			_accessibility.Configure(helper);
		}
	}

	void IXamlRootHost.InvalidateRender()
	{
		ApplicationActivity.Instance?.InvalidateRender();
	}

	UIElement? IXamlRootHost.RootElement
		=> _xamlRoot.Content as UIElement ?? _xamlRoot.VisualTree.RootElement;
}
