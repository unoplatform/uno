using System;
using System.Drawing;
using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
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
