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

		[global::Uno.NotImplemented]
		public global::Microsoft.UI.Xaml.Controls.Primitives.ListViewItemTemplateSettings TemplateSettings { get; } = new Primitives.ListViewItemTemplateSettings();

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ListViewItemAutomationPeer(this);
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
			if (Selector is ListView lv)
			{
				ApplyMultiSelectState(lv.SelectionMode == ListViewSelectionMode.Multiple);
			}
		}
	}
}
