using System;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Uno.UI.Controls.Legacy
{
	public partial class GridViewSource : ListViewBaseSource
	{
		public GridViewSource(GridView owner = null) : base(owner)
		{

		}
		protected override SelectorItem CreateSelectorItem()
		{
			return new GridViewItem() { ShouldHandlePressed = false };
		}

		protected override ListViewBaseHeaderItem CreateSectionHeaderItem()
		{
			return new GridViewHeaderItem();
		}
	}
}

