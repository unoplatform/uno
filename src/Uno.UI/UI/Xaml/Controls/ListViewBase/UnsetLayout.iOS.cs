using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;
using Foundation;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// This is set as a placeholder in the constructor of <see cref="NativeListViewBase"/>. It should normally never be used when the list is actually visible.
	/// </summary>
	public class UnsetLayout : VirtualizingPanelLayout
	{
		public override Orientation ScrollOrientation => Orientation;

		internal override bool SupportsDynamicItemSizes => false;

		protected override nfloat LayoutItemsInGroup(int group, nfloat availableBreadth, ref CGRect frame, bool createLayoutInfo, Dictionary<NSIndexPath, CGSize?> oldItemSizes)
		{
			return 0;
		}
	}
}
