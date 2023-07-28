using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void CustomSetContent(UIElement content)
	{
		//if (_rootVisual is null)
		//{
		//_rootBorder = new Border();
		//CoreServices.Instance.PutVisualRoot(_rootBorder);
		//	//_rootVisual = CoreServices.Instance.MainRootVisual;

		//	if (_rootVisual?.XamlRoot is null)
		//	{
		//		throw new InvalidOperationException("The root visual was not created.");
		//	}
		//}

		if (_rootBorder != null)
		{
			_rootBorder.Child = _content = content;
		}

		TryLoadRootVisual();
	}

	private void TryLoadRootVisual()
	{
		if (PublicRootElement?.XamlRoot?.VisualTree?.RootElement is not { } rootElement)
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

	//partial void OnContentChangedPartial(XamlIslandRoot xamlIslandRoot)
	//{
	//	// Ensure the root element of the XamlIsland is loaded.
	//	UIElement.LoadingRootElement(_root);
	//	UIElement.RootElementLoaded(_root);
	//}
}
