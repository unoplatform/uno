using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GridViewItem : SelectorItem
	{
		public GridViewItem()
		{
			Initialize();

			DefaultStyleKey = typeof(GridViewItem);
		}

		partial void Initialize();

		// WinUI does not dim disabled GridViewItem content (its native presenter ignores the disabled
		// opacity); suppress the default template's "Disabled" opacity dim to match.
		private protected override bool UsesDisabledVisualState => false;

		public GridViewItemTemplateSettings TemplateSettings { get; } = new();

		protected override Automation.Peers.AutomationPeer OnCreateAutomationPeer()
			=> new Automation.Peers.GridViewItemAutomationPeer(this);
	}
}
