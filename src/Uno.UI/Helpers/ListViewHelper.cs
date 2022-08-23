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
		public static void SmoothScroll(this ListViewBase lvb, int index, UIKit.UICollectionViewScrollPosition scrollPosition = UIKit.UICollectionViewScrollPosition.Top)
		{
			var indexPath = lvb.GetIndexPathFromIndex(index)?.ToNSIndexPath() ?? throw new IndexOutOfRangeException();

			lvb.NativePanel.ScrollToItem(indexPath, scrollPosition, animated: true);
		}
#elif __ANDROID__
		public static void SmoothScroll(this ListViewBase lvb, int index)
		{
			lvb.NativePanel.SmoothScrollToPosition(index);
		}
#endif
	}
#endif
}
