#nullable enable

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Controls;

partial class ProgressRing
{
	// WinUI's ProgressRing raises no range events; Uno's accessibility bridges need them to announce progress.
	private void RaiseRangeValuePropertyChangedEvent(AutomationProperty property, object oldValue, object newValue)
	{
		if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)
			&& GetOrCreateAutomationPeer() is ProgressRingAutomationPeer peer)
		{
			peer.RaisePropertyChangedEvent(property, oldValue, newValue);
		}
	}
}
