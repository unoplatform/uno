#nullable enable

using System;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class AutomationPeer
{
	private static IAutomationPeerListener? _automationPeerListener;

	internal static IAutomationPeerListener? AutomationPeerListener
	{
		get => TestAutomationPeerListener ?? _automationPeerListener;
		set
		{
			if (_automationPeerListener is not null)
			{
				throw new InvalidOperationException("AutomationPeerListener should only be set once.");
			}

			_automationPeerListener = value;
		}
	}

	internal static IAutomationPeerListener? TestAutomationPeerListener { get; set; }

	public void RaisePropertyChangedEvent(AutomationProperty automationProperty, object oldValue, object newValue)
	{
		AutomationPeerListener?.NotifyPropertyChangedEvent(this, automationProperty, oldValue, newValue);
	}
}
