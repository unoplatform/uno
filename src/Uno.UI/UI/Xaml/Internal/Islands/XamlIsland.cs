using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIsland : Panel
{
	private readonly ContentManager _contentManager;

	public XamlIsland()
	{
		_contentManager = new(this, false);
		InitializeRoot(WinUICoreServices.Instance);
	}

	public UIElement Content
	{
		get => _contentManager.Content;
		set
		{
			_contentManager.Content = value;

			var rootElement = _contentManager.RootElement;
			SetPublicRootVisual(rootElement, null, null);

			UIElement.LoadingRootElement(_contentRoot.VisualTree.PublicRootVisual!);

			_contentRoot.VisualTree.PublicRootVisual!.XamlRoot!.InvalidateMeasure();

			UIElement.RootElementLoaded(_contentRoot.VisualTree.PublicRootVisual!);
		}
	}
}
