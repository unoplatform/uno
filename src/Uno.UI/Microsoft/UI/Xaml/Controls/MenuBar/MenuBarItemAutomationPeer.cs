// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MenuBarItemAutomationPeer.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls;
using Windows.UI.Xaml.Automation.Peers;
using Uno.Extensions;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers
{
	public partial class MenuBarItemAutomationPeer
	{
		public MenuBarItemAutomationPeer(MenuBarItem owner) : base(owner)
		{
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.MenuItem;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Invoke || patternInterface == PatternInterface.ExpandCollapse)
			{
				return this;
			}
			return base.GetPatternCore(patternInterface);
		}

		protected override string GetNameCore()
		{
			//Check to see if the item has a defined AutomationProperties.Name
			string name = base.GetNameCore();

			if (name.IsNullOrEmpty())
			{
				var owner = Owner as MenuBarItem;
				name = owner.Title;
			}

			return name;
		}

		protected override string GetClassNameCore()
		{
			return nameof(MenuBarItem);
		}

		public void Invoke()
		{
			var owner = Owner as MenuBarItem;
			owner?.Invoke();
		}

		public ExpandCollapseState ExpandCollapseState
		{
			get
			{
				UIElement owner = Owner;
				var menuBarItem = owner as MenuBarItem;
				if (menuBarItem.IsFlyoutOpen())
				{
					return ExpandCollapseState.Expanded;
				}
				else
				{
					return ExpandCollapseState.Collapsed;
				}

			}
		}

		public void Collapse()
		{
			var owner = Owner as MenuBarItem;
			owner?.CloseMenuFlyout();
		}

		public void Expand()
		{
			var owner = Owner as MenuBarItem;
			owner?.ShowMenuFlyout();
		}
	}
}
