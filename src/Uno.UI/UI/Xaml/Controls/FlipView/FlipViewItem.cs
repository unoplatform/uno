using System;
using System.Drawing;
using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlipViewItem : SelectorItem
	{
		public FlipViewItem()
		{
			DefaultStyleKey = typeof(FlipViewItem);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new FlipViewItemAutomationPeer(this);
		}
	}
}
