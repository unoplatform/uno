using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

partial class ContentManager
{
	private void InternalSetContent(UIElement value)
	{
		if (_rootVisual == null)
		{
			_rootBorder = new Border();
			CoreServices.Instance.PutVisualRoot(_rootBorder);
			_rootVisual = CoreServices.Instance.MainRootVisual;

			if (_rootVisual == null)
			{
				throw new InvalidOperationException("The root visual could not be created.");
			}

			ApplicationActivity.Instance?.SetContentView(_rootVisual);
		}
		_rootBorder.Child = _content = value;
	}
}
