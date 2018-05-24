using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBaseHeaderItem : ContentControl
	{
		protected override bool CanCreateTemplateWithoutParent { get; } = true;
	}
}