// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButtonAutomationPeer.cpp, tag winui3/release/1.8.4

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
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

#if HAS_UNO
		// Uno-specific: WinUI's DXAML core auto-parents popup content to its anchor in the UIA tree.
		// Uno only wires the inverse direction (popup content -> anchor via GetPopupAssociatedAutomationPeer),
		// so flyout children are not discoverable from the SplitButton peer without this override.
		protected override IList<AutomationPeer> GetChildrenCore()
		{
			var baseChildren = base.GetChildrenCore();

			if (GetImpl() is not { IsFlyoutOpen: true } splitButton)
			{
				return baseChildren;
			}

			var presenter = splitButton.Flyout?.GetPresenter();
			if (presenter?.GetOrCreateAutomationPeer() is not { } flyoutPeer)
			{
				return baseChildren;
			}

			var result = baseChildren is null
				? new List<AutomationPeer>(1)
				: new List<AutomationPeer>(baseChildren);
			result.Add(flyoutPeer);
			return result;
		}
#endif

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
