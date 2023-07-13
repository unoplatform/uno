using System;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
		//if (!_shown)
		//{
		//	return;
		//}

		UIElement.LoadingRootElement(_rootVisual);

		_rootVisual.XamlRoot!.InvalidateMeasure();

		UIElement.RootElementLoaded(_rootVisual);
	}

	//partial void OnContentChangedPartial(XamlIslandRoot xamlIslandRoot)
	//{
	//	// Ensure the root element of the XamlIsland is loaded.
	//	UIElement.LoadingRootElement(_root);
	//	UIElement.RootElementLoaded(_root);
	//}
}
