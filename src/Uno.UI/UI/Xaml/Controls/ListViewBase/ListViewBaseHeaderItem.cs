using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ListViewBaseHeaderItem : ContentControl
	{
		internal ListViewBaseHeaderItem()
		{
			DefaultStyleKey = typeof(ListViewBaseHeaderItem);
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}
