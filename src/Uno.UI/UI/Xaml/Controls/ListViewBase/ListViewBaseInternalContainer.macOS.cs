using System;
using System.Collections.Generic;
using System.Text;
using AppKit;

namespace Windows.UI.Xaml.Controls
{
	internal partial class ListViewBaseInternalContainer : NSCollectionViewItem
	{
		public ContentControl Content { get; set; }
		public NativeListViewBase Owner { get; internal set; } //Stub

		public override void LoadView()
		{
			View = Content;
		}

		internal IDisposable InterceptSetNeedsLayout()
		{
			return null;
		}

		internal void ClearMeasuredSize()
		{
			// Stub
		}
	}
}
