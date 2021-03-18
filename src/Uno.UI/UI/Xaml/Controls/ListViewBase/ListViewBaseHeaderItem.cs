using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBaseHeaderItem : ContentControl
	{
		public ListViewBaseHeaderItem()
		{
			DefaultStyleKey = typeof(ListViewBaseHeaderItem);
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
