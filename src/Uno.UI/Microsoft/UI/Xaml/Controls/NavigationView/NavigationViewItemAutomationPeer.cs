// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference NavigationViewItemAutomationPeer.cpp, commit 2ec9b1c

using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class NavigationViewItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
	{
		private enum AutomationOutput
		{
			Position,
			Size,
		}

		public NavigationViewItemAutomationPeer(NavigationViewItem navigationViewItem) : base(navigationViewItem)
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
			int positionInSet = 0;

			if (IsOnTopNavigation() && !IsOnFooterNavigation())
			{
				positionInSet = GetPositionOrSetCountInTopNavHelper(AutomationOutput.Position);
			}
			else
			{
				positionInSet = GetPositionOrSetCountInLeftNavHelper(AutomationOutput.Position);
			}

			return positionInSet;
		}

		protected override int GetSizeOfSetCore()
		{
			int sizeOfSet = 0;

			if (IsOnTopNavigation() && !IsOnFooterNavigation())
			{
				var navview = GetParentNavigationView();
				if (navview != null)
				{
					sizeOfSet = GetPositionOrSetCountInTopNavHelper(AutomationOutput.Size);

				}
			}
			else
			{
				sizeOfSet = GetPositionOrSetCountInLeftNavHelper(AutomationOutput.Size);
			}

			return sizeOfSet;
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

		int GetNavigationViewItemCountInPrimaryList()
		{
			int count = 0;
			var navigationView = GetParentNavigationView();
			if (navigationView != null)
			{
				count = navigationView.GetNavigationViewItemCountInPrimaryList();
			}
			return count;
		}

		int GetNavigationViewItemCountInTopNav()
		{
			int count = 0;
			var navigationView = GetParentNavigationView();
			if (navigationView != null)
			{
				count = navigationView.GetNavigationViewItemCountInTopNav();
			}
			return count;
		}

		bool IsSettingsItem()
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

		private bool IsOnTopNavigation()
		{
			var position = GetNavigationViewRepeaterPosition();
			return position != NavigationViewRepeaterPosition.LeftNav && position != NavigationViewRepeaterPosition.LeftFooter;
		}

		private bool IsOnTopNavigationOverflow()
		{
			return GetNavigationViewRepeaterPosition() == NavigationViewRepeaterPosition.TopOverflow;
		}

		private bool IsOnFooterNavigation()
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


		// Get either the position or the size of the set for this particular item in the case of left nav. 
		// We go through all the items and then we determine if the listviewitem from the left listview can be a navigation view item header
		// or a navigation view item. If it's the former, we just reset the count. If it's the latter, we increment the counter.
		// In case of calculating the position, if this is the NavigationViewItemAutomationPeer we're iterating through we break the loop.
		private int GetPositionOrSetCountInLeftNavHelper(AutomationOutput automationOutput)
		{
			int returnValue = 0;

			var repeater = GetParentItemsRepeater();
			if (repeater != null)
			{
				var parent = FrameworkElementAutomationPeer.CreatePeerForElement(repeater) as AutomationPeer;
				if (parent != null)
				{
					var children = parent.GetChildren();
					if (children != null)
					{
						int index = 0;
						bool itemFound = false;

						foreach (var child in children)
						{
							var dependencyObject = repeater.TryGetElement(index);
							if (dependencyObject != null)
							{
								if (dependencyObject is NavigationViewItemHeader)
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

								else if (dependencyObject is NavigationViewItem navviewItem)
								{
									if (navviewItem.Visibility == Visibility.Visible)
									{
										returnValue++;

										if (FrameworkElementAutomationPeer.FromElement(navviewItem) == (NavigationViewItemAutomationPeer)(this))
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
							index++;
						}
					}
				}
			}

			return returnValue;
		}

		// Get either the position or the size of the set for this particular item in the case of top nav (primary/overflow items). 
		// Basically, we do the same here as GetPositionOrSetCountInLeftNavHelper without dealing with the listview directly, because 
		// TopDataProvider provcides two methods: GetOverflowItems() and GetPrimaryItems(), so we can break the loop (in case of position) by 
		// comparing the value of the FrameworkElementAutomationPeer we can get from the item we're iterating through to this object.
		private int GetPositionOrSetCountInTopNavHelper(AutomationOutput automationOutput)
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

		bool IsSelected()
		{
			var nvi = Owner as NavigationViewItem;
			if (nvi != null)
			{
				return nvi.IsSelected;
			}
			return false;
		}

		IRawElementProviderSimple SelectionContainer()
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

		void AddToSelection()
		{
			ChangeSelection(true);
		}

		void Select()
		{
			ChangeSelection(true);
		}

		void RemoveFromSelection()
		{
			ChangeSelection(false);
		}

		void ChangeSelection(bool isSelected)
		{
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
}
