// MUX Reference: TabViewAutomationPeer.cpp, commit 542e6f9

using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	/// <summary>
	/// Exposes TabView types to Microsoft UI Automation.
	/// </summary>
	public partial class TabViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
	{
		/// <summary>
		/// Initializes a new instance of the TabViewAutomationPeer class.
		/// </summary>
		/// <param name="owner">The TabView control instance to create the peer for.</param>
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
