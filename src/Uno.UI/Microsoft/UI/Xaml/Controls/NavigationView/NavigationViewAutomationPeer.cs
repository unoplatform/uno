// MUX reference NavigationViewAutomationPeer.cpp, commit 8f1a682

using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;

public partial class NavigationViewAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
{
	public NavigationViewAutomationPeer(NavigationView owner) : base(owner)
	{
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Selection)
		{
			return this;
		}

		return base.GetPatternCore(patternInterface);
	}

	bool ISelectionProvider.CanSelectMultiple => false;

	bool ISelectionProvider.IsSelectionRequired => false;

	IRawElementProviderSimple[] ISelectionProvider.GetSelection()
	{
		var nv = Owner as NavigationView;
		if (nv != null)
		{
			var nvi = nv.GetSelectedContainer() as NavigationViewItem;
			if (nvi != null)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(nvi);
				if (peer != null)
				{
					return new[] { ProviderFromPeer(peer) };
				}
			}
		}
		return Array.Empty<IRawElementProviderSimple>();
	}

	internal void RaiseSelectionChangedEvent(object oldSelection, object newSelecttion)
	{
		if (AutomationPeer.ListenerExists(AutomationEvents.SelectionPatternOnInvalidated))
		{
			var nv = Owner as NavigationView;
			if (nv != null)
			{
				var nvi = nv.GetSelectedContainer() as NavigationViewItem;
				if (nvi != null)
				{
					var peer = FrameworkElementAutomationPeer.CreatePeerForElement(nvi);
					if (peer != null)
					{
						peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
					}
				}
			}
		}
	}
}
