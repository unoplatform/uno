// MUX Reference: TabViewAutomationPeer.cpp, commit 542e6f9

using System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TabViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{

		public TabViewAutomationPeer(TabView owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Selection)
			{
				return this;
			}
			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(TabView);
		}

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

		bool ISelectionProvider.CanSelectMultiple => false;

		bool ISelectionProvider.IsSelectionRequired => true;

		IRawElementProviderSimple[] ISelectionProvider.GetSelection()
		{
			var tabView = Owner as TabView;
			if (tabView != null)
			{
				var tabViewItem = tabView.ContainerFromIndex(tabView.SelectedIndex) as TabViewItem;
				if (tabViewItem != null)
				{
					var peer = FrameworkElementAutomationPeer.CreatePeerForElement(tabViewItem);
					if (peer != null)
					{
						return new[] { ProviderFromPeer(peer) };
					}
				}
			}
			return Array.Empty<IRawElementProviderSimple>();
		}
	}
}
