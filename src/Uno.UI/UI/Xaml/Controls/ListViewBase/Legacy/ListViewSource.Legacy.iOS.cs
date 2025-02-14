using System;
using System.Linq;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Uno.UI.Controls.Legacy
{
	public partial class ListViewSource : ListViewBaseSource
	{
		public ListViewSource(ListView owner) : base(owner)
		{
		}

		protected override SelectorItem CreateSelectorItem()
		{
			return new ListViewItem() { ShouldHandlePressed = false };
		}

		protected override ListViewBaseHeaderItem CreateSectionHeaderItem()
		{
			return new ListViewHeaderItem();
		}
	}
}

