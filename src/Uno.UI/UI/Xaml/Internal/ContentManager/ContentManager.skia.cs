#nullable enable

using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	internal static void TryLoadRootVisual(XamlRoot xamlRoot)
	{
		if (xamlRoot?.VisualTree?.RootElement is not { } rootElement) //TODO:MZ: What if the Content is null?
		{
			// TODO:MZ: Indicate failure!
			return;
		}

		//if (!_shown || !_windowCreated)
		//{
		//	return;
		//}

		void LoadRoot()
		{
			UIElement.LoadingRootElement(rootElement);

			rootElement.XamlRoot!.InvalidateMeasure();
			rootElement.XamlRoot!.InvalidateArrange();

			UIElement.RootElementLoaded(rootElement);
		}

		var dispatcher = rootElement.Dispatcher;

		if (dispatcher.HasThreadAccess)
		{
			LoadRoot();
		}
		else
		{
			_ = dispatcher.RunAsync(CoreDispatcherPriority.High, LoadRoot);
		}
	}
}
