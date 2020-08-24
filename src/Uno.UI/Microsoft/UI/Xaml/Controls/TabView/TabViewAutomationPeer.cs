using System;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public class TabViewAutomationPeer : FrameworkElementAutomationPeer
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

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Tab;
		}

		private bool CanSelectMultiple()
		{
			return false;
		}

		private bool IsSelectionRequired()
		{
			return true;
		}

		//TODO:MZ: This method is weird.
		private IRawElementProviderSimple GetSelection()
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
						return ProviderFromPeer(peer);
					}
				}
			}
			return null;
		}
	}
}
