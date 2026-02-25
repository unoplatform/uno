// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TeachingTip\TeachingTipAutomationPeer.cpp, tag winui3/release/1.8.4

#nullable enable

using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class TeachingTipAutomationPeer : FrameworkElementAutomationPeer, IWindowProvider
{
	public TeachingTipAutomationPeer(TeachingTip owner) : base(owner)
	{
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		if ((GetTeachingTip()).IsLightDismissEnabled)
		{
			return AutomationControlType.Window;
		}
		else
		{
			return AutomationControlType.Pane;
		}
	}

	protected override string GetClassNameCore() => nameof(TeachingTip);

	WindowInteractionState IWindowProvider.InteractionState
	{
		get
		{
			var teachingTip = GetTeachingTip();
			if (teachingTip.m_isIdle && teachingTip.IsOpen)
			{
				return WindowInteractionState.ReadyForUserInteraction;
			}
			else if (teachingTip.m_isIdle && !teachingTip.IsOpen)
			{
				return WindowInteractionState.BlockedByModalWindow;
			}
			else if (!teachingTip.m_isIdle && !teachingTip.IsOpen)
			{
				return WindowInteractionState.Closing;
			}
			else
			{
				return WindowInteractionState.Running;
			}
		}
	}

	bool IWindowProvider.IsModal => GetTeachingTip().IsLightDismissEnabled;

	bool IWindowProvider.IsTopmost => GetTeachingTip().IsOpen;

	bool IWindowProvider.Maximizable => false;

	bool IWindowProvider.Minimizable => false;

	WindowVisualState IWindowProvider.VisualState => WindowVisualState.Normal;

	void IWindowProvider.Close() => GetTeachingTip().IsOpen = false;

	void IWindowProvider.SetVisualState(WindowVisualState state)
	{
	}

	bool IWindowProvider.WaitForInputIdle(int milliseconds) => true;

	internal void RaiseWindowClosedEvent()
	{
		// We only report as a window when light dismiss is enabled.
		if (GetTeachingTip().IsLightDismissEnabled &&
			ListenerExists(AutomationEvents.WindowClosed))
		{
			RaiseAutomationEvent(AutomationEvents.WindowClosed);
		}
	}

	internal void RaiseWindowOpenedEvent(string displayString)
	{
		AutomationPeer automationPeer = this;
		if (automationPeer != null)
		{
			automationPeer.RaiseNotificationEvent(
				AutomationNotificationKind.Other,
				AutomationNotificationProcessing.CurrentThenMostRecent,
				displayString,
				"TeachingTipOpenedActivityId");
		}

		// We only report as a window when light dismiss is enabled.
		if (GetTeachingTip().IsLightDismissEnabled &&
			ListenerExists(AutomationEvents.WindowOpened))
		{
			RaiseAutomationEvent(AutomationEvents.WindowOpened);
		}
	}

	private TeachingTip GetTeachingTip()
	{
		UIElement owner = Owner;
		return (TeachingTip)owner;
	}
}
