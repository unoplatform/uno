// MUX reference InfoBarAutomationPeer.cpp, commit 3125489

using Microsoft.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class InfoBarAutomationPeer : FrameworkElementAutomationPeer
	{
		public InfoBarAutomationPeer(InfoBar owner) : base(owner)
		{
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.StatusBar;
		}

		protected override string GetClassNameCore()
		{
			return nameof(InfoBar);
		}

		internal void RaiseOpenedEvent(InfoBarSeverity severity, string displayString)
		{
			//if (IAutomationPeer7 automationPeer7 = this)
			{
				this.RaiseNotificationEvent(
					AutomationNotificationKind.Other,
					GetProcessingForSeverity(severity),
					displayString,
					"InfoBarOpenedActivityId");
			}
		}

		internal void RaiseClosedEvent(InfoBarSeverity severity, string displayString)
		{
			//AutomationNotificationProcessing processing = AutomationNotificationProcessing.CurrentThenMostRecent;

			//if (IAutomationPeer7 automationPeer7 = this)
			{
				this.RaiseNotificationEvent(
					AutomationNotificationKind.Other,
					GetProcessingForSeverity(severity),
					displayString,
					"InfoBarClosedActivityId");
			}
		}


		private AutomationNotificationProcessing GetProcessingForSeverity(InfoBarSeverity severity)
		{
			AutomationNotificationProcessing processing = AutomationNotificationProcessing.CurrentThenMostRecent;

			if (severity == InfoBarSeverity.Error
				|| severity == InfoBarSeverity.Warning)
			{
				processing = AutomationNotificationProcessing.ImportantAll;
			}

			return processing;
		}

		private InfoBar GetInfoBar()
		{
			UIElement owner = Owner;
			return (InfoBar)owner;
		}
	}
}
