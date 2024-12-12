// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButtonAutomationPeer.cpp, tag winui3/release/1.4.2

using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	public partial class SplitButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, IInvokeProvider
	{
		public SplitButtonAutomationPeer(SplitButton owner) : base(owner)
		{
		}

		// IAutomationPeerOverrides
		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.ExpandCollapse ||
				patternInterface == PatternInterface.Invoke)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore()
		{
			return nameof(SplitButton);
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.SplitButton;
		}

		private SplitButton GetImpl()
		{
			SplitButton impl = null;

			if (Owner is SplitButton splitButton)
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

		// IInvokeProvider
		public void Invoke()
		{
			if (GetImpl() is { } splitButton)
			{
				splitButton.Invoke();
			}
		}
	}
}
