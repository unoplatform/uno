// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ToggleSplitButtonAutomationPeer.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class ToggleSplitButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IToggleProvider
	{
		public ToggleSplitButtonAutomationPeer(ToggleSplitButton owner) : base(owner)
		{
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

		private ToggleSplitButton GetImpl()
		{
			ToggleSplitButton impl = null;

			if (Owner is ToggleSplitButton splitButton)
			{
				impl = splitButton;
			}

			return impl;
		}

		// IExpandCollapseProvider 
		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				ExpandCollapseState currentState = ExpandCollapseState.Collapsed;

				if (GetImpl() is { } splitButton)
				{
					if (splitButton.IsFlyoutOpen)
					{
						currentState = ExpandCollapseState.Expanded;
					}
				}

				return currentState;
			}
		}

		public void Expand()
		{
			if (GetImpl() is { } splitButton)
			{
				splitButton.OpenFlyout();
			}
		}

		public void Collapse()
		{
			if (GetImpl() is { } splitButton)
			{
				splitButton.CloseFlyout();
			}
		}

		// IToggleProvider
		public ToggleState ToggleState
		{
			get
			{
				ToggleState state = ToggleState.Off;

				if (GetImpl() is { } splitButton)
				{
					if (splitButton.IsChecked)
					{
						state = ToggleState.On;
					}
				}

				return state;
			}
		}

		public void Toggle()
		{
			if (GetImpl() is { } splitButton)
			{
				splitButton.Toggle();
			}
		}
	}
}
