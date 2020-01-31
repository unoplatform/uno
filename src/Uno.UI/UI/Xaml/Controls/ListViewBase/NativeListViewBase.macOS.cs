using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using CoreGraphics;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : NSCollectionView
	{
		internal CGPoint ContentOffset
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		internal ListViewBaseSource Source
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}
		public AutomationPeer GetAutomationPeer()
		{
			return null;
		}

		public string GetAccessibilityInnerText()
		{
			return null;
		}
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
