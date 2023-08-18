#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UIKit;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void CustomSetContent(UIElement value)
	{
		if (_rootVisual == null)
		{

			_mainController.View.AddSubview(_rootVisual);
			_rootVisual.Frame = _mainController.View.Bounds;
			_rootVisual.AutoresizingMask = UIViewAutoresizing.All;
		}

		_rootBorder.Child?.RemoveFromSuperview();
		_rootBorder.Child = null;
		_rootBorder.Child = _content = value;
	}
}
