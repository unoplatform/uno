using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// An basic implementation for an item container
	/// </summary>
	/// <remarks>This container supports vertical scrolling and stretching for the whole item.</remarks>
	public partial class ListViewItem : SelectorItem
	{
		public ListViewItem()
		{
			DefaultStyleKey = typeof(ListViewItem);
		}

		// WinUI does not dim disabled ListViewItem content (its native ListViewItemPresenter ignores
		// the disabled opacity); suppress the default template's "Disabled" opacity dim to match.
		private protected override bool UsesDisabledVisualState => false;

		[global::Uno.NotImplemented]
		public ListViewItemTemplateSettings TemplateSettings { get; } = new();

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ListViewItemAutomationPeer(this);
		}
	}
}
