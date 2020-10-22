// TODO comments MUX Reference TeachingTipAutomationPeer.cpp, commit 46f9da3

#nullable enable

using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public class TeachingTipAutomationPeer : FrameworkElementAutomationPeer
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

		protected override string GetClassNameCore()
		{
			return nameof(TeachingTip);
		}

		private WindowInteractionState InteractionState()
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

		private bool IsModal()
		{
			return GetTeachingTip().IsLightDismissEnabled;
		}

		private bool IsTopmost()
		{
			return GetTeachingTip().IsOpen;
		}

		private bool Maximizable()
		{
			return false;
		}

		private bool Minimizable()
		{
			return false;
		}

		private WindowVisualState VisualState()
		{
			return WindowVisualState.Normal;
		}

		private void Close()
		{
			(GetTeachingTip()).IsOpen = false;
		}

		private void SetVisualState(WindowVisualState state)
		{

		}

		private bool WaitForInputIdle(int milliseconds)
		{
			return true;
		}

		private void RaiseWindowClosedEvent()
		{
			// We only report as a window when light dismiss is enabled.
			if (GetTeachingTip().IsLightDismissEnabled &&
				AutomationPeer.ListenerExists(AutomationEvents.WindowClosed))
			{
				RaiseAutomationEvent(AutomationEvents.WindowClosed);
			}
		}

		private void RaiseWindowOpenedEvent(string displayString)
		{
			AutomationPeer automationPeer7 = this;
			if (automationPeer7 != null)
			{
				automationPeer7.RaiseNotificationEvent(
					AutomationNotificationKind.Other,
					AutomationNotificationProcessing.CurrentThenMostRecent,
					displayString,
					"TeachingTipOpenedActivityId");
			}

			// We only report as a window when light dismiss is enabled.
			if ((GetTeachingTip()).IsLightDismissEnabled &&
				AutomationPeer.ListenerExists(AutomationEvents.WindowOpened))
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
}
