using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Foundation;

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class VirtualizingPanelLayout : NSCollectionViewLayout, DependencyObject
	{
		private ListViewBase XamlParent => Owner?.XamlParent;

		internal NSCollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath) => GetLayoutAttributesForItem(indexPath);

		internal NSCollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath) => GetLayoutAttributesForSupplementaryView(kind, indexPath);

		public new NativeListViewBase CollectionView => base.CollectionView as NativeListViewBase; //TODO: this should avoid interop, as with iOS implementation
	}
}
