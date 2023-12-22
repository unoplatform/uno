#nullable enable

using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement)
	{
		UIElement.LoadingRootElement(rootElement);

		xamlRoot.InvalidateMeasure();
		xamlRoot.InvalidateArrange();

		UIElement.RootElementLoaded(rootElement);
	}
}
