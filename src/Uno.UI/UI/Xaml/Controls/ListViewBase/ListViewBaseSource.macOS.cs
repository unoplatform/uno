using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;
using AppKit;
using Foundation;

namespace Windows.UI.Xaml.Controls
{
	[Bindable]
	public partial class ListViewBaseSource : NSCollectionViewDataSource
	{
		public override NSCollectionViewItem GetItem(NSCollectionView collectionView, NSIndexPath indexPath)
		{
			return new ListViewBaseInternalContainer(); //TODO
		}
	}
}
