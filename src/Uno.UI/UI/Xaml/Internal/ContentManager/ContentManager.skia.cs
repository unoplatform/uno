#nullable enable

using Uno.UI.Dispatching;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	static partial void LoadRootElementPlatform(XamlRoot xamlRoot, UIElement rootElement)
	{
		xamlRoot.InvalidateMeasure();
		xamlRoot.InvalidateArrange();
	}
}
