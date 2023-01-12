using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class IconElement
    {
		partial void RegisterSubView(UIElement child)
		{
			AddChild(child);
		}
	}
}
