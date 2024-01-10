using System;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GridViewItem : SelectorItem
	{
		partial void Initialize()
		{
			ClipsToBounds = true;
		}
	}
}

