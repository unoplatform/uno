using System;
using Uno.UI.Views.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBaseHeaderItem : ContentControl
	{
		public ListViewBaseHeaderItem() { }

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			SetNeedsLayout();
		}
	}
}

