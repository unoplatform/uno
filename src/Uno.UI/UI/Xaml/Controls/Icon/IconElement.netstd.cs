using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class IconElement
	{
		partial void UnregisterSubView()
		{
			ClearChildren();
		}

		partial void RegisterSubView(UIElement child)
		{
			AddChild(child);
		}
	}
}
