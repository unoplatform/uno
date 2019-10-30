using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// An basic implementation for an item container
	/// </summary>
	/// <remarks>This container supports vertical scrolling and stretching for the whole item.</remarks>
	public partial class ListViewItem : SelectorItem
	{
		internal sealed override bool HasPointerOverPressedState => true;

		public ListViewItem()
		{
		}

		[global::Uno.NotImplemented]
		public global::Windows.UI.Xaml.Controls.Primitives.ListViewItemTemplateSettings TemplateSettings { get; } = new Primitives.ListViewItemTemplateSettings();

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ListViewItemAutomationPeer(this);
		}
	}
}
