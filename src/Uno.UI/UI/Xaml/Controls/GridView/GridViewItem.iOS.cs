using System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class GridViewItem : SelectorItem
	{
		partial void Initialize()
		{
			ClipsToBounds = true;
		}
	}
}

