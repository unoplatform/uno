using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void CustomSetContent(UIElement value)
	{
		//if (_rootVisual == null)
		//{
		//	_rootBorder = new Border();
		//	var coreServices = Uno.UI.Xaml.Core.CoreServices.Instance;
		//	coreServices.PutVisualRoot(_rootBorder);
		//	_rootVisual = coreServices.MainRootVisual;

		//	if (_rootVisual == null)
		//	{
		//		throw new InvalidOperationException("The root visual could not be created.");
		//	}

			_mainController.View.AddSubview(_rootVisual);
			_rootVisual.Frame = _mainController.View.Bounds;
			_rootVisual.AutoresizingMask = UIViewAutoresizing.All;
		}

		_rootBorder.Child?.RemoveFromSuperview();
		_rootBorder.Child = null;
		_rootBorder.Child = _content = value;
	}
}
