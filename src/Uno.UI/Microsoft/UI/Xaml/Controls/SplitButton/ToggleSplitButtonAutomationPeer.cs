using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	public partial class ToggleSplitButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IToggleProvider
	{
		private readonly ToggleSplitButton _owner;

		public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner) : base(owner)
		{
			_owner = owner;
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse ||
				patternInterface == PatternInterface.Toggle)
			{
				return this;
			}
			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(ToggleSplitButton);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.SplitButton;
		}

		private ToggleSplitButton GetImpl() => _owner;

		// IExpandCollapseProvider 
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				ExpandCollapseState currentState = ExpandCollapseState.Collapsed;

				var splitButton = GetImpl();
				if (splitButton != null)
				{
					if (splitButton.IsFlyoutOpen)
					{
						currentState = ExpandCollapseState.Expanded;
					}
				}

				return currentState;
			}
		}

		public void Expand() => GetImpl()?.OpenFlyout();

		public void Collapse() => GetImpl()?.CloseFlyout();

		// IToggleProvider
		public ToggleState ToggleState
		{
			get
			{
				ToggleState state = ToggleState.Off;

				var splitButton = GetImpl();
				if (splitButton != null)
				{
					if (splitButton.IsChecked)
					{
						state = ToggleState.On;
					}
				}

				return state;
			}
		}

		public void Toggle() => GetImpl()?.Toggle();
	}
}
