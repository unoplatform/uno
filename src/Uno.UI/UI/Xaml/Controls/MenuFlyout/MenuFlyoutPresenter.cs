using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class MenuFlyoutPresenter : ItemsControl, IMenuPresenter
	{
		// Can be negative. (-1) means nothing focused.
		internal int m_iFocusedIndex;

		// Weak reference to the menu that ultimately owns this MenuFlyoutPresenter.
		private WeakReference m_wrOwningMenu;

		// Weak reference to the parent MenuFlyout.
		private WeakReference m_wrParentMenuFlyout;

		// Weak reference to the owner of this menu.
		// Only populated if this is the presenter for an ISubMenuOwner.
		private WeakReference m_wrOwner;

		// Weak reference to the sub-presenter that was created by a child menu owner.
		private WeakReference m_wrSubPresenter;

		// Whether ItemsSource contains at least one ToggleMenuFlyoutItem.
		private bool m_containsToggleItems;

		// Whether ItemsSource contains at least one MenuFlyoutItem with an Icon.
		private bool m_containsIconItems;

		// Whether ItemsSource contains at least one MenuFlyoutItem or ToggleMenuFlyoutItem with KeyboardAcceleratorText.
		private bool m_containsItemsWithKeyboardAcceleratorText;

		// UNO TODO
		// private bool m_animationInProgress;

		private bool m_isSubPresenter;

		private int m_depth;

		//private FlyoutBase.MajorPlacementMode m_mostRecentPlacement;

		private ScrollViewer m_tpScrollViewer;

		public MenuFlyoutPresenterTemplateSettings TemplateSettings { get; } = new MenuFlyoutPresenterTemplateSettings();

		public MenuFlyoutPresenter()
		{
			m_iFocusedIndex = -1;
			m_containsToggleItems = false;
			m_containsIconItems = false;
			m_containsItemsWithKeyboardAcceleratorText = false;
			// UNO TODO
			// m_animationInProgress = false;
			m_isSubPresenter = false;
			//m_mostRecentPlacement = FlyoutBase.MajorPlacementMode.Bottom;

			DefaultStyleKey = typeof(MenuFlyoutPresenter);
		}

		internal bool IsSubPresenter { get => m_isSubPresenter; set => m_isSubPresenter = value; }

		// Responds to the KeyDown event.

		protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;

			if (!handled)
			{
				var key = pArgs.Key;
				pArgs.Handled = KeyPressMenuFlyoutPresenter.KeyDown(key, this);
			}
		}

		internal void HandleUpOrDownKey(bool isDownKey)
		{
			CycleFocus(isDownKey, FocusState.Keyboard);
		}

		void CycleFocus(bool shouldCycleDown, FocusState focusState)
		{

			// Ensure the initial focus index to validate m_iFocusedIndex when the focused item
			// is set by application's code like as MenuFlyout Opened event.
			EnsureInitialFocusIndex();

			var originalFocusedIndex = m_iFocusedIndex;

			var parentFlyout = GetParentMenuFlyout();

			// We should wrap around at the bottom or the top of the presenter if the user isn't using a gamepad or remote.
			var shouldWrap = parentFlyout != null ? (parentFlyout.InputDeviceTypeUsedToOpen != FocusInputDeviceKind.GameController) : true;

			var nCount = Items.Size;

			// Determine direction of index movement based on the Up/Down key.
			var deltaIndex = shouldCycleDown ? 1 : -1;

			// Set index by moving deltaIndex amount from the current focused item.
			var index = m_iFocusedIndex + deltaIndex;

			// We have two locations where we want to wrap, so we'll encapsulate the wrapping behavior in a function object that we can call.
			int wrapIndexIfNeeded(int indexToWrap)
			{
				if (shouldWrap)
				{
					if (indexToWrap < 0)
					{
						indexToWrap = (int)(nCount) - 1;
					}
					else if (indexToWrap >= (int)(nCount))
					{
						indexToWrap = 0;
					}
				}

				return indexToWrap;
			}

			// If there is no item focused right now, then set index to 0 for Down key or to n-1 for Up key.
			// Otherwise, if we should be wrapping, we'll do an initial check for whether we should wrap before we enter the loop.
			if (m_iFocusedIndex == -1)
			{
				index = shouldCycleDown ? 0 : (int)(nCount) - 1;

				// If the focused index is -1, then our value of -1 for originalFocusedIndex will not successfully stop the loop.
				// In this case, we'll make originalFocusedIndex one step in the opposite direction from the initial index,
				// so that way the loop will go all the way through the list of items before stopping.
				originalFocusedIndex = wrapIndexIfNeeded(index - deltaIndex);
			}
			else
			{
				index = wrapIndexIfNeeded(index);
			}

			// We need to examine all items with indices [0, m_iFocusedIndex) or (m_iFocusedIndex, nCount-1] for Down/Up keys.
			// While index is within the range, we keep going through the item list until we are successfully able to focus an item,
			// at which point we update the m_iFocusedIndex and break out of the loop.
			while (0 <= index && index < (int)(nCount))
			{
				var spItemAsDependencyObject = Items[index] as DependencyObject;

				var isFocusable = false;
				// We determine whether the item is a focusable MenuFlyoutItem or MenuFlyoutSubItem because we want to exclude MenuSeparators here.
				var spItem = spItemAsDependencyObject as MenuFlyoutItem;
				if (spItem != null)
				{
					isFocusable = spItem.IsFocusable;
				}
				else
				{
					MenuFlyoutSubItem spSubItem;
					spSubItem = spItemAsDependencyObject as MenuFlyoutSubItem;
					if (spSubItem != null)
					{
						isFocusable = spSubItem.IsFocusable;
					}
				}

				// If the item is focusable, move the focus to it, update the m_iFocusedIndex and break out of the loop.
				if (isFocusable)
				{
					var spSubItem = spItemAsDependencyObject as Control;
					if (spSubItem != null)
					{
						spSubItem.Focus(focusState);
						m_iFocusedIndex = index;
						break;
					}
				}

				// If we've gone all the way around the list of items and still have not found a suitable focus candidate,
				// then we'll stop - there's nothing else for us to do.
				if (index == originalFocusedIndex)
				{
					break;
				}

				index += deltaIndex;

				// If we should be wrapping, then we'll perform the wrap at this point.
				index = wrapIndexIfNeeded(index);
			}

		}

		internal void HandleKeyDownLeftOrEscape()
		{
			(this as IMenuPresenter).CloseSubMenu();
		}


		protected override void PrepareContainerForItemOverride(
			 DependencyObject pElement,
			 object pItem)
		{
			base.PrepareContainerForItemOverride(pElement, pItem);

			var spMenuFlyoutItemBase = pElement as MenuFlyoutItemBase;

			spMenuFlyoutItemBase.SetParentMenuFlyoutPresenter(this);

			SynchronizeTemplatedParent(spMenuFlyoutItemBase);
		}

		private void SynchronizeTemplatedParent(MenuFlyoutItemBase spMenuFlyoutItemBase)
		{
			// Manual propagation of the templated parent to the content properly
			// until we get the propagation running properly
			if (spMenuFlyoutItemBase is FrameworkElement content)
			{
				content.TemplatedParent = TemplatedParent;
			}
		}

		protected override void ClearContainerForItemOverride(
			 DependencyObject pElement,
			 object pItem)
		{
			base.ClearContainerForItemOverride(pElement, pItem);

			(pElement as MenuFlyoutItemBase).SetParentMenuFlyoutPresenter(null);
		}

		// Get the parent MenuFlyout.
		internal MenuFlyout GetParentMenuFlyout()
		{
			return m_wrParentMenuFlyout?.Target as MenuFlyout;
		}

		// Sets the parent MenuFlyout.
		internal void SetParentMenuFlyout(MenuFlyout pParentMenuFlyout)
		{
			m_wrParentMenuFlyout = new WeakReference(pParentMenuFlyout);
		}

		// Called when the ItemsSource property changes.
		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs args)
		{
			base.OnItemsSourceChanged(args);
			var pNewValue = args.NewValue;

			m_iFocusedIndex = -1;
			m_containsToggleItems = false;
			m_containsIconItems = false;
			m_containsItemsWithKeyboardAcceleratorText = false;

			if (pNewValue != null)
			{
				IList<MenuFlyoutItemBase> spItems;
				spItems = pNewValue as IList<MenuFlyoutItemBase>;
				if (spItems == null)
				{
					// MenuFlyoutPresenter could be used outside of MenuFlyout, but at this time
					// we don't support that usage.  If a customer is using MenuFlyoutPresenter
					// independently and uses an ItemsSource that is not an IVector<MenuFlyoutItemBase>,
					// we throw E_INVALIDARG to indicate that this usage is invalid.
					// If we decide to allow this usage, we need to override and implement
					// IsItemItsOwnContainerOverride() and GetContainerForItemOverride().
					//
					// GetContainerForItemOverride() is tricky since MenuFlyoutPresenter supports 3 different
					// kinds of children (MenuFlyoutSeparator, MenuFlyoutItem, ToggleMenuFlyoutItem),
					// so we are punting on that scenario for now.
					throw new InvalidOperationException("Cannot use MenuFlyoutPresenter outside of a MenuFlyout");
				}

				// MenuFlyoutItem's alignment changes based on the layout of other MenuFlyoutItems.
				// This check looks through all MenuFlyoutItems in our items source and checks for
				// ToggleMenuFlyoutItems and the presence of Icons, which can change the layout of
				// all MenuFlyoutItems in the presenter.
				var nCount = spItems.Count;
				for (var i = 0; i < nCount; ++i)
				{
					MenuFlyoutItemBase item;
					MenuFlyoutItem itemAsMenuItem;
					MenuFlyoutSubItem itemAsMenuSubItem;
					IconElement iconElement;
					string keyboardAcceleratorText;

					item = spItems[i] as MenuFlyoutItemBase;

					// To prevent casting the same item more than we need to, each cast is conditional
					// on the previous one failing. This way we only cast each item as many times as we
					// need to.
					itemAsMenuItem = item as MenuFlyoutItem;
					if (itemAsMenuItem != null)
					{
						m_containsToggleItems = m_containsToggleItems || (itemAsMenuItem as MenuFlyoutItem).HasToggle();

						iconElement = (itemAsMenuItem as MenuFlyoutItem).Icon;
						m_containsIconItems = m_containsIconItems || iconElement != null;

						keyboardAcceleratorText = (itemAsMenuItem as MenuFlyoutItem).KeyboardAcceleratorTextOverride;
						m_containsItemsWithKeyboardAcceleratorText = m_containsItemsWithKeyboardAcceleratorText || !string.IsNullOrEmpty(keyboardAcceleratorText);
					}
					else
					{
						itemAsMenuSubItem = item as MenuFlyoutSubItem;
						if (itemAsMenuSubItem != null)
						{
							iconElement = (itemAsMenuSubItem as MenuFlyoutSubItem).Icon;
							m_containsIconItems = m_containsIconItems || iconElement != null;
						}
					}

					if (m_containsIconItems && m_containsToggleItems && m_containsItemsWithKeyboardAcceleratorText)
					{
						break;
					}
				}

				UpdateTemplateSettings();
			}
		}

		// Create MenuFlyoutPresenterAutomationPeer to represent the
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new MenuFlyoutPresenterAutomationPeer(this);
		}

#if false
		void UpdateVisualStateForPlacement(FlyoutBase.MajorPlacementMode placement)
		{
			m_mostRecentPlacement = placement;

			UpdateVisualState(false);
		}

		void ResetVisualState()
		{
			VisualStateManager.GoToState(this, "None", false);
		}
#endif
		private protected override void ChangeVisualState(
		   // true to use transitions when updating the visual state, false
		   // to snap directly to the new visual state.
		   bool bUseTransitions)
		{
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			ScrollViewer spScrollViewer;

			// Get the ScrollViewer template part
			spScrollViewer = this.GetTemplateChild("MenuFlyoutPresenterScrollViewer") as ScrollViewer;
			m_tpScrollViewer = spScrollViewer;

			// Apply a shadow

			// UNO TODO
			//bool isDefaultShadowEnabled = IsDefaultShadowEnabled;
			//if (isDefaultShadowEnabled)
			//{
			//	DependencyObject spChild;
			//	VisualTreeHelper.GetChildStatic(this, 0, &spChild);
			//	var spChildAsUIE = spChild as UIElement;

			//	if (spChildAsUIE != null)
			//	{
			//		ApplyElevationEffect(spChildAsUIE, GetDepth());
			//	}
			//}
		}

		// UNO TODO
		//
		//Timeline AttachEntranceAnimationCompleted(
		//	string pszStateName,
		//	uint nStateNameLength,
		//	TimelineCompletedEventCallback pCompletedEvent)
		//{
		//	bool found = false;
		//	VisualState spState;

		//	VisualStateManager.TryGetState(this, pszStateName, null, &spState, &found);

		//	if (found && spState)
		//	{
		//		Storyboard spStoryboard;

		//		spStoryboard = (spState.Storyboard);
		//		if (spStoryboard != null)
		//		{
		//			Timeline spTimeline;
		//			(spStoryboard.As<ITimeline>(&spTimeline));

		//			(pCompletedEvent.AttachEventHandler(spTimeline,
		//				std.bind(&OnEntranceAnimationCompleted, this, _1, _2)));

		//			(spTimeline.CopyTo(ppTimeline));
		//		}
		//	}
		//}

#if false
		void DetachEntranceAnimationCompletedHandlers()
		{
			// Marked as no longer used
			//
			//if (m_epTopPortraitCompletedHandler)
			//{
			//	ASSERT(m_tpTopPortraitTimeline);
			//	(m_epTopPortraitCompletedHandler.DetachEventHandler(m_tpTopPortraitTimeline));
			//	m_tpTopPortraitTimeline.Clear();
			//}

			//if (m_epBottomPortraitCompletedHandler)
			//{
			//	ASSERT(m_tpBottomPortraitTimeline);
			//	(m_epBottomPortraitCompletedHandler.DetachEventHandler(m_tpBottomPortraitTimeline));
			//	m_tpBottomPortraitTimeline.Clear();
			//}

			//if (m_epLeftLandscapeCompletedHandler)
			//{
			//	ASSERT(m_tpLeftLandscapeTimeline);
			//	(m_epLeftLandscapeCompletedHandler.DetachEventHandler(m_tpLeftLandscapeTimeline));
			//	m_tpLeftLandscapeTimeline.Clear();
			//}

			//if (m_epRightLandscapeCompletedHandler)
			//{
			//	ASSERT(m_tpRightLandscapeTimeline);
			//	(m_epRightLandscapeCompletedHandler.DetachEventHandler(m_tpRightLandscapeTimeline));
			//	m_tpRightLandscapeTimeline.Clear();
			//}
		}

		void OnEntranceAnimationCompleted(
			DependencyObject pSender,
			DependencyObject pArgs)
		{
			// UNO TODO
			// m_animationInProgress = false;

			Focus(FocusState.Programmatic);
		}
#endif
		protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
		{
			var handled = pArgs.Handled;

			if (!handled)
			{
				Pointer spPointer;
				spPointer = (pArgs.Pointer);
				var pointerDeviceType = (spPointer.PointerDeviceType);
				if (PointerDeviceType.Mouse == (PointerDeviceType)pointerDeviceType && !m_isSubPresenter)
				{
					var isHitVerticalScrollBarOrSubPresenter = false;
					IMenuPresenter subPresenter;

					// Hit test the current position for the vertical ScrollBar and the sub presenter.
					// Close the existing sub presenter if the current mouse position isn't hit
					// the vertical ScrollBar nor sub presenter.

					subPresenter = (this as IMenuPresenter).SubPresenter;

					if (subPresenter != null)
					{
						PointerPoint spPointerPoint;
						UIElement spSubPresenterAsUIE;

						spSubPresenterAsUIE = subPresenter as UIElement;

						spPointerPoint = pArgs.GetCurrentPoint(null /* relativeTo*/);
						var clientLogicalPointerPosition = spPointerPoint.Position;

						if (m_tpScrollViewer != null)
						{
							UIElement spVerticalScrollBarAsUE;
							Control spScrollViewerAsControl;
							DependencyObject spVerticalScrollBarAsDO;

							spScrollViewerAsControl = m_tpScrollViewer as Control;
							spVerticalScrollBarAsDO = spScrollViewerAsControl.GetTemplateChild("VerticalScrollBar");
							spVerticalScrollBarAsUE = spVerticalScrollBarAsDO as UIElement;

							if (spSubPresenterAsUIE != null || spVerticalScrollBarAsUE != null)
							{
								var spElements = VisualTreeHelper.FindElementsInHostCoordinates(clientLogicalPointerPosition, spVerticalScrollBarAsUE, true /* includeAllElements */);

								foreach (var spElement in spElements)
								{
									DependencyObject pElementAsCDO = spElement;

									if ((pElementAsCDO is ScrollBar && spVerticalScrollBarAsUE == spElement) ||
										(spSubPresenterAsUIE == spElement))
									{
										isHitVerticalScrollBarOrSubPresenter = true;
										break;
									}
								}

								if (!isHitVerticalScrollBarOrSubPresenter)
								{
									spElements = VisualTreeHelper.FindElementsInHostCoordinates(clientLogicalPointerPosition, spSubPresenterAsUIE, true /* includeAllElements */);

									foreach (var spElement in spElements)
									{
										DependencyObject pElementAsCDO = spElement;

										if ((pElementAsCDO is ScrollBar && spVerticalScrollBarAsUE == spElement) ||
											(spSubPresenterAsUIE == spElement))
										{
											isHitVerticalScrollBarOrSubPresenter = true;
											break;
										}
									}
								}
							}
						}

						// The opened MenuFlyoutSubItem won't to be closed if the mouse position is
						// on the vertical ScrollBar or sub presenter.
						if (!isHitVerticalScrollBarOrSubPresenter)
						{
							var subPresenterBoundsLogical = GetSubPresenterBounds(spSubPresenterAsUIE);
							var containsSubPresenter = subPresenterBoundsLogical.Contains(clientLogicalPointerPosition);

							if (!containsSubPresenter)
							{
								DelayCloseMenuFlyoutSubItem();
							}
						}
					}
				}

				pArgs.Handled = true;
			}
		}

		internal bool GetContainsToggleItems()
		{
			return m_containsToggleItems;
		}

		// Returns true if the ItemsSource contains at least one MenuFlyoutItem with an Icon; false otherwise.
		internal bool GetContainsIconItems()
		{
			return m_containsIconItems;
		}

		// Returns true if the ItemsSource contains at least one MenuFlyoutItem with an Icon; false otherwise.
		internal bool GetContainsItemsWithKeyboardAcceleratorText()
		{
			return m_containsItemsWithKeyboardAcceleratorText;
		}

		private Rect GetSubPresenterBounds(UIElement pSubPresenterAsUIE)
		{
			var t = pSubPresenterAsUIE.TransformToVisual(null);
			var r = t.TransformBounds(pSubPresenterAsUIE.LayoutSlotWithMarginsAndAlignments);

			return r;
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
		{
			base.OnPointerEntered(pArgs);

			var handled = pArgs.Handled;

			if (!handled)
			{
				Pointer spPointer;
				spPointer = pArgs.Pointer;
				var pointerDeviceType = spPointer.PointerDeviceType;
				if (PointerDeviceType.Mouse == (PointerDeviceType)pointerDeviceType && m_isSubPresenter)
				{
					CancelCloseMenuFlyoutSubItem();
					var owner = (this as IMenuPresenter).Owner;

					if (owner != null)
					{
						var parentSubItem = owner as MenuFlyoutSubItem;
						if (parentSubItem != null)
						{
							var presenter = parentSubItem.GetParentMenuFlyoutPresenter();
							if (presenter != null)
							{
								// When the mouse enters a MenuFlyoutPresenter that is a sub menu
								// we have to tell the parent presenter to cancel any plans it had
								// to close this sub
								presenter.CancelCloseMenuFlyoutSubItem();
							}
						}
					}
				}

				pArgs.Handled = true;
			}
		}

		private protected override string GetPlainText()
		{
			string automationName = null;

			var ownerFlyout = m_wrParentMenuFlyout.Target as DependencyObject;

			if (ownerFlyout != null)
			{
				// If an automation name is set on the parent flyout, we'll use that as our plain text.
				// Otherwise, we'll report the default plain text.
				automationName = AutomationProperties.GetName(ownerFlyout);
			}

			if (automationName != null)
			{
				return automationName;
			}
			else
			{
				// UNO TODO

				// If we have no title, we'll fall back to the default implementation,
				// which retrieves our content as plain text (e.g., if our content is a string,
				// it returns that; if our content is a TextBlock, it returns its Text value, etc.)
				// MenuFlyoutPresenterGenerated.GetPlainText(strPlainText);

				// If we get the plain text from the content, then we want to truncate it,
				// in case the resulting automation name is very long.
				// Popup.TruncateAutomationName(strPlainText);
			}

			return automationName;
		}

		void IMenuPresenter.CloseSubMenu()
		{
			var subPresenter = m_wrSubPresenter?.Target as IMenuPresenter;

			if (subPresenter != null)
			{
				subPresenter.CloseSubMenu();
			}

			ISubMenuOwner owner;
			owner = m_wrOwner?.Target as ISubMenuOwner;

			if (owner != null)
			{
				owner.CloseSubMenu();
			}

			// Reset the focused index not to cached the previous focused index when
			// the sub menu is opened in the next time
			m_iFocusedIndex = -1;
		}

		void DelayCloseMenuFlyoutSubItem()
		{
			var subMenuPresenter = m_wrSubPresenter?.Target as IMenuPresenter;

			if (subMenuPresenter != null)
			{
				ISubMenuOwner subMenuOwner;
				subMenuOwner = (subMenuPresenter.Owner);

				if (subMenuOwner != null)
				{
					subMenuOwner.DelayCloseSubMenu();
				}
			}
		}

		void CancelCloseMenuFlyoutSubItem()
		{
			var subMenuPresenter = m_wrSubPresenter?.Target as IMenuPresenter;

			if (subMenuPresenter != null)
			{
				ISubMenuOwner subMenuOwner;
				subMenuOwner = (subMenuPresenter.Owner);

				if (subMenuOwner != null)
				{
					subMenuOwner.CancelCloseSubMenu();
				}
			}
		}

#if false
		DependencyObject GetParentMenuFlyoutSubItem(DependencyObject nativeDO)
		{
			var spThis = nativeDO as MenuFlyoutPresenter;
			ISubMenuOwner spResult;
			spResult = (spThis as IMenuPresenter).Owner;

			var spResultAsMenuFlyoutSubItem = spResult as MenuFlyoutSubItem;

			if (spResultAsMenuFlyoutSubItem != null)
			{
				return spResultAsMenuFlyoutSubItem as DependencyObject;
			}
			else
			{
				return null;
			}
		}
#endif

		internal void UpdateTemplateSettings()
		{
			var templateSettings = TemplateSettings;
			var templateSettingsConcrete = templateSettings as MenuFlyoutPresenterTemplateSettings;

			var ownerFlyout = GetParentMenuFlyout();

			if (ownerFlyout != null && templateSettingsConcrete != null)
			{
				// Query MenuFlyout Content MinWidth, given the input mode, from resource dictionary.
				var flyoutContentMinWidth = ResourceResolver.ResolveTopLevelResourceDouble(
					(ownerFlyout.InputDeviceTypeUsedToOpen == FocusInputDeviceKind.Touch || ownerFlyout.InputDeviceTypeUsedToOpen == FocusInputDeviceKind.GameController)
						? "FlyoutThemeTouchMinWidth" : "FlyoutThemeMinWidth"
				);
				var visibleBounds = this.LayoutSlotWithMarginsAndAlignments;
				// DXamlCore.GetCurrent().GetVisibleContentBoundsForElement(GetHandle(), &visibleBounds);

				templateSettingsConcrete.FlyoutContentMinWidth = Math.Min(visibleBounds.Width, flyoutContentMinWidth);
			}

			double maxItemKeyboardAcceleratorTextWidth = 0;

			IObservableVector<object> menuItems = Items;

			// MenuFlyoutItem's alignment changes based on the layout of other MenuFlyoutItems.
			// This check looks through all MenuFlyoutItems in our items source and finds the max
			// keyboard accelerator label width, which affects the look of other items.
			var nCount = (menuItems as ItemCollection).Size;
			for (var i = 0; i < nCount; ++i)
			{
				MenuFlyoutItemBase item;
				MenuFlyoutItem itemAsMenuItem;

				item = Items[i] as MenuFlyoutItemBase;

				itemAsMenuItem = item as MenuFlyoutItem;
				if (itemAsMenuItem != null)
				{
					var desiredSize = (itemAsMenuItem as MenuFlyoutItem).GetKeyboardAcceleratorTextDesiredSize();
					var desiredWidth = desiredSize.Width;
					if (desiredWidth > maxItemKeyboardAcceleratorTextWidth)
					{
						maxItemKeyboardAcceleratorTextWidth = desiredWidth;
					}
				}
			}

			for (var i = 0; i < nCount; ++i)
			{
				MenuFlyoutItemBase item;
				MenuFlyoutItem itemAsMenuItem;

				item = Items[i] as MenuFlyoutItemBase;

				itemAsMenuItem = item as MenuFlyoutItem;
				if (itemAsMenuItem != null)
				{
					(itemAsMenuItem as MenuFlyoutItem).UpdateTemplateSettings(maxItemKeyboardAcceleratorTextWidth);
				}
			}
		}

		protected override void OnGotFocus(RoutedEventArgs pArgs)
		{
			var focusState = FocusState;
			if (m_iFocusedIndex == -1 &&
				focusState != FocusState.Unfocused)
			{
				// The MenuFlyoutPresenter gets focused for the first time right after it is opened.
				// In this case we want to send focus to the first focusable item.
				CycleFocus(true /* shouldCycleDown */, focusState);
			}
			else if (focusState == FocusState.Unfocused)
			{
				// A child element got focus, so make sure we keep m_iFocusedIndex in sync
				// with it.
				var focusedElement = (XamlRoot is null ?
					FocusManager.GetFocusedElement() :
					FocusManager.GetFocusedElement(XamlRoot)) as DependencyObject;

				// Since GotFocus is an async event, the focused element could be null if we got it
				// after the popup closes, which clears focus.
				if (focusedElement != null)
				{
					var focusedElementIndex = IndexFromContainer(focusedElement);
					if (focusedElementIndex != -1)
					{
						m_iFocusedIndex = focusedElementIndex;
					}
				}
			}


		}

		void EnsureInitialFocusIndex()
		{
			if (m_iFocusedIndex == -1)
			{
				var focusedElement = XamlRoot is null ?
					FocusManager.GetFocusedElement() :
					FocusManager.GetFocusedElement(XamlRoot);

				if (this != focusedElement)
				{
					var menuItemsCount = Items.Count;

					for (var i = 0; i < menuItemsCount; ++i)
					{
						var itemAsDependencyObject = Items[i] as DependencyObject;
						MenuFlyoutItem menuItem;

						menuItem = itemAsDependencyObject as MenuFlyoutItem;

						if (menuItem == focusedElement)
						{
							m_iFocusedIndex = i;
							break;
						}
					}

					global::System.Diagnostics.Debug.Assert(m_iFocusedIndex != -1);
				}
			}
		}

#if false
		int GetPositionInSetHelper(MenuFlyoutItemBase item)
		{
			var returnValue = -1;

			var presenter = item.GetParentMenuFlyoutPresenter();

			if (presenter != null)
			{
				var indexOfItem = Items.IndexOf(item);

				if (indexOfItem != -1)
				{
					// Iterate through the items preceding this item and subtract the number
					// of separaters and collapsed items to get its position in the set.
					var positionInSet = (int)(indexOfItem);

					for (var i = 0; i < indexOfItem; ++i)
					{
						var child = Items[i] as DependencyObject;

						if (child != null)
						{
							if (child is MenuFlyoutSeparator)
							{
								--positionInSet;
							}
							else
							{
								if (child is UIElement itemAsUI)
								{
									var visibility = itemAsUI.Visibility;

									if (Visibility.Visible != visibility)
									{
										--positionInSet;
									}
								}
							}
						}
					}

					global::System.Diagnostics.Debug.Assert(positionInSet >= 0);

					returnValue = positionInSet + 1;  // Add 1 to convert from a 0-based index to a 1-based index.
				}
			}

			return returnValue;
		}

		int GetSizeOfSetHelper(MenuFlyoutItemBase item)
		{
			var returnValue = -1;

			var presenter = item.GetParentMenuFlyoutPresenter();

			if (presenter != null)
			{
				var itemsCount = Items.Count;

				// Iterate through the parent presenters items and subtract the
				// number of separaters and collapsed items from the total count
				// to get the size of the set.
				var sizeOfSet = (int)(itemsCount);

				for (var i = 0; i < itemsCount; ++i)
				{
					var child = Items[i] as DependencyObject;

					if (child != null)
					{
						if (child is MenuFlyoutSeparator)
						{
							--sizeOfSet;
						}
						else
						{
							var itemAsUI = child as UIElement;
							if (itemAsUI != null)
							{
								var visibility = itemAsUI.Visibility;

								if (Visibility.Visible != visibility)
								{
									--sizeOfSet;
								}
							}
						}
					}
				}

				global::System.Diagnostics.Debug.Assert(sizeOfSet >= 0);

				returnValue = sizeOfSet;
			}

			return returnValue;
		}
#endif
		ISubMenuOwner IMenuPresenter.Owner
		{
			get => m_wrOwner?.Target as ISubMenuOwner;
			set => m_wrOwner = new WeakReference(value);
		}

		IMenu IMenuPresenter.OwningMenu
		{
			get => m_wrOwningMenu?.Target as IMenu;
			set => m_wrOwningMenu = new WeakReference(value);
		}

		IMenuPresenter IMenuPresenter.SubPresenter
		{
			get => m_wrSubPresenter?.Target as IMenuPresenter;
			set => m_wrSubPresenter = new WeakReference(value);
		}

		internal bool IsTextScaleFactorEnabledInternal { get; set; }

		internal void SetDepth(int depth)
		{
			m_depth = depth;
		}

		internal int GetDepth()
		{
			return m_depth;
		}
	}
}
