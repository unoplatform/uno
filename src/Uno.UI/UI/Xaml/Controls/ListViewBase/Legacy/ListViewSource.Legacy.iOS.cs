using System;
using System.Linq;
using Uno.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Collections.Generic;

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

