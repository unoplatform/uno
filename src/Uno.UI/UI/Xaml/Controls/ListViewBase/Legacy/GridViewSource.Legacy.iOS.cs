using System;
using System.Linq;
using Uno.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
#endif

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

