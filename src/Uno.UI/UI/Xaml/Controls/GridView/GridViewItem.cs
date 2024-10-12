using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GridViewItem : SelectorItem
	{
		public GridViewItem()
		{
			Initialize();

			DefaultStyleKey = typeof(GridViewItem);
		}

		partial void Initialize();

		public GridViewItemTemplateSettings TemplateSettings { get; } = new();
	}
}
