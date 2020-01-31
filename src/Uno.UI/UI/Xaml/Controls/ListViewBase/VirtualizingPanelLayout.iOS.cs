using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class VirtualizingPanelLayout : UICollectionViewLayout
	{
		private ListViewBase XamlParent => Source.GetTarget()?.Owner?.XamlParent;

		/// <summary>
		/// Gets the CollectionView that owns this layout.
		/// </summary>
		/// <remarks>
		/// This property is present to avoid the interop cost, even at
		/// the expense of WeakReference dereference.
		/// </remarks>
		public new UICollectionView CollectionView => Source?.GetTarget()?.Owner ?? base.CollectionView;
	}
}
