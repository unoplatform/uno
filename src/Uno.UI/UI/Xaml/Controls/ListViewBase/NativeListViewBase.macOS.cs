using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using CoreGraphics;
using Uno;
using Uno.UI;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : NSCollectionView, IHasSizeThatFits
	{
		[NotImplemented]
		internal CGPoint ContentOffset { get; set; } // TODO: implement

		internal ListViewBaseSource Source
		{
			get => base.DataSource as ListViewBaseSource;
			set => base.DataSource = value;
		}
		[NotImplemented]
		public ScrollBarVisibility HorizontalScrollBarVisibility { get; internal set; } //TODO: implement (https://stackoverflow.com/questions/49724516/how-to-hide-nscollectionview-scroll-indicator/54062881 ?)
		[NotImplemented]
		public ScrollBarVisibility VerticalScrollBarVisibility { get; internal set; } //TODO: implement

		public AutomationPeer GetAutomationPeer()
		{
			return null;
		}

		public string GetAccessibilityInnerText()
		{
			return null;
		}

		public CGSize SizeThatFits(CGSize size) => NativeLayout?.SizeThatFits(size) ?? default(CGSize);
	}

	internal partial class BlockLayout : Border
	{
		public override bool NeedsLayout
		{
			set
			{
				// Block
			}
		}
	}
}
