// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemAutomationPeer.cpp, commit 65718e2813

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

public partial class NavigationViewItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider, ISelectionItemProvider
{
	private enum AutomationOutput
	{
		Position,
		Size,
	}

	public NavigationViewItemAutomationPeer(NavigationViewItem owner) : base(owner)
	{
	}

	protected override string GetNameCore()
	{
		string returnHString = base.GetNameCore();

		// If a name hasn't been provided by AutomationProperties.Name in markup:
		if (string.IsNullOrEmpty(returnHString))
		{
			var lvi = Owner as NavigationViewItem;
			if (lvi != null)
			{
				returnHString = SharedHelpers.TryGetStringRepresentationFromObject(lvi.Content);
			}
		}

		if (string.IsNullOrEmpty(returnHString))
		{
			// NB: It'll be up to the app to determine the automation label for
			// when they're using a PlaceholderValue vs. Value.

			returnHString = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_NavigationViewItemDefaultControlName);
		}

		return returnHString;
	}

	protected override object GetPatternCore(PatternInterface pattern)
	{
		// Note: We are intentionally not supporting Invoke Pattern, since supporting both SelectionItem and Invoke was
		// causing problems. 
		// See this Issue for more details: https://github.com/microsoft/microsoft-ui-xaml/issues/2702
		if (pattern == PatternInterface.SelectionItem ||
			// Only provide expand collapse pattern if we have children!
			(pattern == PatternInterface.ExpandCollapse && HasChildren()))
		{
			return this;
		}

		return base.GetPatternCore(pattern);
	}

	protected override string GetClassNameCore()
	{
		return nameof(NavigationViewItem);
	}

	protected override AutomationControlType GetAutomationControlTypeCore()
	{
		// To be compliant with MAS 4.1.2, in DisplayMode 'Top',
		//  a NavigationViewItem should report itsself as TabItem
		if (IsOnTopNavigation())
		{
			return AutomationControlType.TabItem;
		}
		else
		{
			// TODO: Should this be ListItem in minimal mode and
			// TreeItem otherwise.
			return AutomationControlType.ListItem;
		}
	}


	protected override int GetPositionInSetCore()
	{
		return GetPositionOrSetCountHelper(AutomationOutput.Position);
	}

	protected override int GetSizeOfSetCore()
	{
		return GetPositionOrSetCountHelper(AutomationOutput.Size);
	}

	protected override int GetLevelCore()
	{
		NavigationViewItemBase nvib = Owner as NavigationViewItemBase;
		if (nvib != null)
		{
			var nvibImpl = nvib;
			if (nvibImpl.IsTopLevelItem)
			{
				return 1;
			}
			else
			{
				var navView = GetParentNavigationView();
				if (navView != null)
				{
					var indexPath = navView.GetIndexPathForContainer(nvib);
					if (indexPath != null)
					{
						// first index in path stands for main or footer menu
						return indexPath.GetSize() - 1;
					}
				}
			}
		}

		return 0;
	}

	private void Invoke()
	{
		var navView = GetParentNavigationView();
		if (navView != null)
		{
			var navigationViewItem = Owner as NavigationViewItem;
			if (navigationViewItem != null)
			{
				if (navigationViewItem == navView.SettingsItem)
				{
					navView.OnSettingsInvoked();
				}
				else
				{
					navView.OnNavigationViewItemInvoked(navigationViewItem);
				}
			}
		}
	}

	// IExpandCollapseProvider
	public ExpandCollapseState ExpandCollapseState
	{
		get
		{
			var state = ExpandCollapseState.LeafNode;
			NavigationViewItem navigationViewItem = Owner as NavigationViewItem;
			if (navigationViewItem != null)
			{
				state = navigationViewItem.IsExpanded ?
					ExpandCollapseState.Expanded :
					ExpandCollapseState.Collapsed;
			}

			return state;
		}
	}

	public void Collapse()
	{
		var navView = GetParentNavigationView();
		if (navView != null)
		{
			NavigationViewItem navigationViewItem = Owner as NavigationViewItem;
			if (navigationViewItem != null)
			{
				navView.Collapse(navigationViewItem);
				RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Collapsed);
			}
		}
	}

	public void Expand()
	{
		var navView = GetParentNavigationView();
		if (navView != null)
		{
			NavigationViewItem navigationViewItem = Owner as NavigationViewItem;
			if (navigationViewItem != null)
			{
				navView.Expand(navigationViewItem);
				RaiseExpandCollapseAutomationEvent(ExpandCollapseState.Expanded);
			}
		}
	}

	internal void RaiseExpandCollapseAutomationEvent(ExpandCollapseState newState)
	{
		if (AutomationPeer.ListenerExists(AutomationEvents.PropertyChanged))
		{
			ExpandCollapseState oldState = (newState == ExpandCollapseState.Expanded) ?
			   ExpandCollapseState.Collapsed :
			   ExpandCollapseState.Expanded;

			//oldState doesn't work here, use ReferenceWithABIRuntimeClassName to make Narrator can unbox it.
			RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
			   oldState,
			   newState);
		}
	}

	private NavigationView GetParentNavigationView()
	{
		NavigationView navigationView = null;

		NavigationViewItemBase navigationViewItem = Owner as NavigationViewItemBase;
		if (navigationViewItem != null)
		{
			navigationView = navigationViewItem.GetNavigationView();
		}
		return navigationView;
	}

	internal int GetNavigationViewItemCountInPrimaryList()
	{
		int count = 0;
		var navigationView = GetParentNavigationView();
		if (navigationView != null)
		{
			count = navigationView.GetNavigationViewItemCountInPrimaryList();
		}
		return count;
	}

	internal int GetNavigationViewItemCountInTopNav()
	{
		int count = 0;
		var navigationView = GetParentNavigationView();
		if (navigationView != null)
		{
			count = navigationView.GetNavigationViewItemCountInTopNav();
		}
		return count;
	}

	internal bool IsSettingsItem()
	{
		var navView = GetParentNavigationView();
		if (navView != null)
		{
			NavigationViewItem item = Owner as NavigationViewItem;
			var settingsItem = navView.SettingsItem;
			if (item != null && settingsItem != null && (item == settingsItem || item.Content == settingsItem))
			{
				return true;
			}
		}
		return false;
	}

	internal bool IsOnTopNavigation()
	{
		var position = GetNavigationViewRepeaterPosition();
		return position != NavigationViewRepeaterPosition.LeftNav && position != NavigationViewRepeaterPosition.LeftFooter;
	}

	internal bool IsOnTopNavigationOverflow()
	{
		return GetNavigationViewRepeaterPosition() == NavigationViewRepeaterPosition.TopOverflow;
	}

	internal bool IsOnFooterNavigation()
	{
		var position = GetNavigationViewRepeaterPosition();
		return position == NavigationViewRepeaterPosition.LeftFooter || position == NavigationViewRepeaterPosition.TopFooter;
	}

	private NavigationViewRepeaterPosition GetNavigationViewRepeaterPosition()
	{
		NavigationViewItemBase navigationViewItem = Owner as NavigationViewItemBase;
		if (navigationViewItem != null)
		{
			return navigationViewItem.Position;
		}
		return NavigationViewRepeaterPosition.LeftNav;
	}

	private ItemsRepeater GetParentItemsRepeater()
	{
		var navview = GetParentNavigationView();
		if (navview != null)
		{
			NavigationViewItemBase navigationViewItem = Owner as NavigationViewItemBase;
			if (navigationViewItem != null)
			{
				return navview.GetParentItemsRepeaterForContainer(navigationViewItem);
			}
		}
		return null;
	}

	// Get either the position or the size of the set for this particular item by iterating through the children of the
	// parent items repeater and comparing the value of the FrameworkElementAutomationPeer we can get from the item
	// we're iterating through to this object.
	private int GetPositionOrSetCountHelper(AutomationOutput automationOutput)
	{
		int returnValue = 0;
		bool itemFound = false;

		var parentRepeater = GetParentItemsRepeater();
		if (parentRepeater != null)
		{
			var itemsSourceView = parentRepeater.ItemsSourceView;
			if (itemsSourceView != null)
			{
				var numberOfElements = itemsSourceView.Count;

				for (int i = 0; i < numberOfElements; i++)
				{
					var child = parentRepeater.TryGetElement(i);
					if (child != null)
					{
						if (child is NavigationViewItemHeader)
						{
							if (automationOutput == AutomationOutput.Size && itemFound)
							{
								break;
							}
							else
							{
								returnValue = 0;
							}
						}

						else if (child is NavigationViewItem navviewitem)
						{
							if (navviewitem.Visibility == Visibility.Visible)
							{
								returnValue++;

								if (FrameworkElementAutomationPeer.FromElement(navviewitem) == (NavigationViewItemAutomationPeer)(this))
								{
									if (automationOutput == AutomationOutput.Position)
									{
										break;
									}
									else
									{
										itemFound = true;
									}
								}
							}
						}
					}
				}
			}
		}

		return returnValue;
	}

	bool ISelectionItemProvider.IsSelected
	{
		get
		{
			var nvi = Owner as NavigationViewItem;
			if (nvi != null)
			{
				return nvi.IsSelected;
			}
			return false;
		}
	}

	IRawElementProviderSimple ISelectionItemProvider.SelectionContainer
	{
		get
		{
			var navview = GetParentNavigationView();
			if (navview != null)
			{
				var peer = FrameworkElementAutomationPeer.CreatePeerForElement(navview);
				if (peer != null)
				{
					return ProviderFromPeer(peer);
				}
			}

			return null;
		}
	}

	void ISelectionItemProvider.AddToSelection()
	{
		ChangeSelection(true);
	}

	void ISelectionItemProvider.Select()
	{
		ChangeSelection(true);
	}

	void ISelectionItemProvider.RemoveFromSelection()
	{
		ChangeSelection(false);
	}

	private void ChangeSelection(bool isSelected)
	{
		// If the item is being selected, we trigger an invoke as if the user had clicked on the item:
		if (isSelected)
		{
			Invoke();
		}
		var nvi = Owner as NavigationViewItem;
		if (nvi != null)
		{
			nvi.IsSelected = isSelected;
		}
	}

	bool HasChildren()
	{
		var navigationViewItem = Owner as NavigationViewItem;
		if (navigationViewItem != null)
		{
			return navigationViewItem.HasChildren();
		}
		return false;
	}
}
