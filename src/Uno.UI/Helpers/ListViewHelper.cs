using System;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;

namespace Uno.UI.Helpers
{
#if __IOS__ || __ANDROID__
	public static partial class ListViewHelper
	{
		public static NativeListViewBase GetNativePanel(ListViewBase lvb) => lvb.NativePanel;

#if __IOS__
		public static void InstantScrollToIndex(this ListViewBase lvb, int index, UIKit.UICollectionViewScrollPosition scrollPosition = UIKit.UICollectionViewScrollPosition.Top)
		{
			var indexPath = lvb.GetIndexPathFromIndex(index)?.ToNSIndexPath() ?? throw new IndexOutOfRangeException();

			lvb.NativePanel?.ScrollToItem(indexPath, scrollPosition, animated: false);
		}

		public static void SmoothScrollToIndex(this ListViewBase lvb, int index, UIKit.UICollectionViewScrollPosition scrollPosition = UIKit.UICollectionViewScrollPosition.Top)
		{
			var indexPath = lvb.GetIndexPathFromIndex(index)?.ToNSIndexPath() ?? throw new IndexOutOfRangeException();

			lvb.NativePanel?.ScrollToItem(indexPath, scrollPosition, animated: true);
		}
#elif __ANDROID__
		public static void InstantScrollToIndex(this ListViewBase lvb, int index)
		{
			lvb.NativePanel?.ScrollToPosition(index);
		}

		public static void SmoothScrollToIndex(this ListViewBase lvb, int index)
		{
			lvb.NativePanel?.SmoothScrollToPosition(index);
		}
#endif
	}
#endif
}
