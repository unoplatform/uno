// MUX Reference: TabView.idl, commit 542e6f9

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TabViewItemAutomationPeer : ListViewItemAutomationPeer, ISelectionItemProvider
	{
		public TabViewItemAutomationPeer(TabViewItem owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.SelectionItem)
			{
				return this;
			}
			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(TabViewItem);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.TabItem;
		}

		protected override string GetNameCore()
		{
			string returnHString = base.GetNameCore();

			// If a name hasn't been provided by AutomationProperties.Name in markup:
			if (string.IsNullOrEmpty(returnHString))
			{
				var tvi = Owner as TabViewItem;
				if (tvi != null)
				{
					returnHString = SharedHelpers.TryGetStringRepresentationFromObject(tvi.Header);
				}
			}

			return returnHString;
		}


		public bool IsSelected
		{
			get
			{
				var tvi = Owner as TabViewItem;
				if (tvi != null)
				{
					return tvi.IsSelected;
				}

				return false;
			}
		}

		public IRawElementProviderSimple SelectionContainer
		{
			get
			{
				var parent = GetParentTabView();
				if (parent != null)
				{
					var peer = FrameworkElementAutomationPeer.CreatePeerForElement(parent);
					if (peer != null)
					{
						return ProviderFromPeer(peer);
					}
				}

				return null;
			}
		}

		public void AddToSelection()
		{
			Select();
		}

		public void RemoveFromSelection()
		{
			// Can't unselect in a TabView without knowing next selection
		}

		public void Select()
		{
			var owner = Owner as TabViewItem;
			if (owner != null)
		    {
				owner.IsSelected = true;
			}
		}

		private TabView GetParentTabView()
		{
			TabView parentTabView = null;

			TabViewItem tabViewItem = Owner as TabViewItem;
			if (tabViewItem != null)
			{
				parentTabView = tabViewItem.GetParentTabView();
			}
			return parentTabView;
		}
	}
}
