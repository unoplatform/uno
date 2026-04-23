// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GroupItem_Partial.cpp, commit 5f9e85113

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class GroupItem
{
	public GroupItem()
		: base()
	{
		m_bWasHidden = false;
		// memset(&m_itemsChangedToken, 0, sizeof(EventRegistrationToken));
		m_itemsChangedToken = null;
		m_previousVisibility = Visibility.Collapsed;
	}

#if HAS_UNO
	// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
	// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.

	// Original destructor logic (not executed):
	// GroupItem::~GroupItem()
	// {
	//     auto spGenerator = m_tpGenerator.GetSafeReference();
	//     if (spGenerator &&
	//         m_itemsChangedToken.value != 0)
	//     {
	//         IGNOREHR(spGenerator->remove_ItemsChanged(m_itemsChangedToken));
	//     }
	//
	//     auto spHeaderControl = m_tpHeaderControl.GetSafeReference();
	//     if (spHeaderControl)
	//     {
	//         IGNOREHR(spHeaderControl.Cast<Control>()->remove_KeyDown(m_headerKeyDownToken));
	//     }
	// }
#endif

	// Handler for KeyDown on our HeaderContent part.
	private void OnHeaderKeyDown(
		object pSender,
		KeyRoutedEventArgs pArgs)
	{
		ListViewBase? spIParentListViewBase = null;
		bool isEnabled = false;
		bool isHandled = false;

		if (m_tpHeaderControl is null)
		{
			return;
		}

		isHandled = pArgs.Handled;
		if (isHandled)
		{
			return;
		}

		isEnabled = m_tpHeaderControl.IsEnabled;
		if (!isEnabled)
		{
			return;
		}

		m_wrParentListViewBaseWeakRef?.TryGetTarget(out spIParentListViewBase);
		if (spIParentListViewBase is not null)
		{
			ListViewBase pParentListViewBase = spIParentListViewBase;
			VirtualKey originalKey = VirtualKey.None;
			VirtualKey key = VirtualKey.None;

			originalKey = pArgs.OriginalKey;
			key = pArgs.Key;

#if false // TODO Uno: ListViewBase.OnGroupHeaderKeyDown is not implemented in Uno.
			pParentListViewBase.OnGroupHeaderKeyDown(this, originalKey, key, out isHandled);

			if (isHandled)
			{
				pArgs.Handled = true;
			}
#endif
		}
	}

	// Helper to get the HeaderContent corresponding to this group.
	internal Control? GetHeaderContent()
	{
		Control? ppHeaderContent = null;

		if (m_tpHeaderControl is not null)
		{
			ppHeaderContent = m_tpHeaderControl;
		}

		return ppHeaderContent;
	}

	// Helper to update m_tpHeaderControl. Attaches and/or detaches OnHeaderKeyDown.
	private void SetHeaderContentReference(Control? pHeaderContent)
	{
		if (m_tpHeaderControl is not null && m_headerKeyDownToken is not null)
		{
			m_tpHeaderControl.KeyDown -= m_headerKeyDownToken;
			m_headerKeyDownToken = null;
		}

		if (pHeaderContent is not null)
		{
			KeyEventHandler spHandler = new KeyEventHandler(OnHeaderKeyDown);

			pHeaderContent.KeyDown += spHandler;
			m_headerKeyDownToken = spHandler;
		}

		// SetPtrValue(m_tpHeaderControl, pHeaderContent);
		m_tpHeaderControl = pHeaderContent;
	}

	// Invoked whenever application code or internal processes call
	// ApplyTemplate.
	protected override void OnApplyTemplate()
	{
		DependencyObject? spElementItemsControlAsDO;
		DependencyObject? spHeaderControlAsDO;
		Control? spHeaderControl;

		base.OnApplyTemplate();

		m_tpItemsControl = null;

		// Get the parts
		spElementItemsControlAsDO = GetTemplateChild("ItemsControl");
		// SetPtrValueWithQIOrNull(m_tpItemsControl, spElementItemsControlAsDO.Get());
		m_tpItemsControl = spElementItemsControlAsDO as ItemsControl;

		spHeaderControlAsDO = GetTemplateChild("HeaderContent");
		spHeaderControl = spHeaderControlAsDO as Control;
		SetHeaderContentReference(spHeaderControl);

		if (m_tpItemsControl is not null && m_tpGenerator is not null)
		{
#if false // TODO Uno: ItemContainerGenerator.m_wrParent and m_tpGroupStyle are internal WinUI state not exposed by Uno's ItemContainerGenerator stub.
			ItemContainerGenerator? spParentGenerator;
			GroupStyle? pGroupStyleNoRef = null;

			spParentGenerator = ((ItemContainerGenerator)m_tpGenerator).m_wrParent as ItemContainerGenerator;
			pGroupStyleNoRef = (spParentGenerator as ItemContainerGenerator)?.m_tpGroupStyle as GroupStyle;
			if (pGroupStyleNoRef is not null)
			{
				ItemsPanelTemplate? spItemsPanel;

				spItemsPanel = pGroupStyleNoRef.Panel;
				m_tpItemsControl.ItemsPanel = spItemsPanel;
			}
#endif
		}
	}

	// determines if this element should be transitioned using the passed in transition
	internal /* override */ void GetCurrentTransitionContext(
		int layoutTickId,
		out ThemeTransitionContext pReturnValue)
	{
		pReturnValue = ThemeTransitionContext.None;
		// disabling the Group transitions for now, there are multiple issues needs to address:
		// 1, 469521 MoCo Grouping : Nested Transitions doesn't work
		// 2, Defining the Transition for groups
		// once both are fixed, this will be enabled back
		//if (m_pParentListViewBase)
		//{
		//    IFC_RETURN(m_pParentListViewBase->GetCurrentTransitionContext(layoutTickId, pReturnValue));
		//}
		//else
		//{
		//    // In case of Reset, by default we will return ContentTransition
		//    // There is bug that in case of reset, the parent ListView is null, and we can get correct transition from
		//    // the Parent
		//    // bug: 103077
		//    *pReturnValue = ThemeTransitionContext::ContentTransition;
		//}
	}


	// determines if mutations are going fast
	internal /* override */ void IsCollectionMutatingFast(
		out bool pReturnValue)
	{
		pReturnValue = false;
	}

	internal /* override */ void GetDropOffsetToRoot(
		out Point pReturnValue)
	{
		ListViewBase? spParentListViewBase = null;

		pReturnValue = new Point();
		pReturnValue.X = 0;
		pReturnValue.Y = 0;

		m_wrParentListViewBaseWeakRef?.TryGetTarget(out spParentListViewBase);
		if (spParentListViewBase is not null)
		{
#if false // TODO Uno: ListViewBase.GetDropOffsetToRoot is not implemented in Uno.
			spParentListViewBase.GetDropOffsetToRoot(out pReturnValue);
#endif
		}
	}

	internal void PrepareItemContainer(object pGroupItem, ItemsControl pItemsControl)
	{
#if false // TODO Uno: Requires ItemContainerGenerator internals (m_wrParent, m_tpGroupStyle) and IsPropertyLocal API that are not exposed by Uno.
		ItemContainerGenerator? spParentGenerator;
		ICollectionViewGroup? spGroup;
		object? spGroupData;
		GroupStyle? pGroupStyleNoRef = null;
		bool bIsPropertyLocal = false;

		var ResetOnExit = Uno.Disposables.Disposable.Create(() => {
			SetCollectionViewGroup(null);
		});

		// IFCPTR_RETURN(pItemsControl);
		if (pItemsControl is null)
		{
			ResetOnExit.Dispose();
			return;
		}

		if (m_tpGenerator is null)
		{
			ResetOnExit.Dispose();
			return; // user-declared GroupItem
		}

		spGroup = pGroupItem as ICollectionViewGroup;
		spGroupData = spGroup?.Group;

		m_wrParentListViewBaseWeakRef = null;
		if (pItemsControl is ListViewBase lvb)
		{
			m_wrParentListViewBaseWeakRef = new WeakReference<ListViewBase>(lvb);
		}

		spParentGenerator = ((ItemContainerGenerator)m_tpGenerator).m_wrParent as ItemContainerGenerator;

		// apply the container style
		pGroupStyleNoRef = (spParentGenerator as ItemContainerGenerator)?.m_tpGroupStyle as GroupStyle;
		if (pGroupStyleNoRef is not null)
		{
			Style? spStyle;

			spStyle = pGroupStyleNoRef.ContainerStyle;

			// no ContainerStyle set, try ContainerStyleSelector
			if (spStyle is null)
			{
				StyleSelector? spStyleSelector;

				spStyleSelector = pGroupStyleNoRef.ContainerStyleSelector;
				if (spStyleSelector is not null)
				{
					spStyle = spStyleSelector.SelectStyle(pGroupItem, this);
				}
			}

			// apply the style, if found
			if (spStyle is not null)
			{
				// TODO: Set flag StyleSetFromGenerator
				Style = spStyle;
			}
		}

		// forward the header template information

		bIsPropertyLocal = IsPropertyLocal(ContentControl.ContentProperty);

		if (!bIsPropertyLocal)
		{
			Content = spGroupData;
			SetCollectionViewGroup(spGroup);
		}

		bIsPropertyLocal = IsPropertyLocal(ContentControl.ContentTemplateProperty);

		if (!bIsPropertyLocal && pGroupStyleNoRef is not null)
		{
			DataTemplate? spHeaderTemplate;

			spHeaderTemplate = pGroupStyleNoRef.HeaderTemplate;
			ContentTemplate = spHeaderTemplate;
		}

		bIsPropertyLocal = IsPropertyLocal(ContentControl.ContentTemplateSelectorProperty);

		if (!bIsPropertyLocal && pGroupStyleNoRef is not null)
		{
			DataTemplateSelector? spHeaderTemplateSelector;

			spHeaderTemplateSelector = pGroupStyleNoRef.HeaderTemplateSelector;
			ContentTemplateSelector = spHeaderTemplateSelector;
		}

		// ResetOnExit.release();
		// Indicate that ResetOnExit should not run.
#endif
	}

	internal void ClearContainerForItem(object pGroupItem)
	{
#if false // TODO Uno: Requires ItemContainerGenerator internals (m_wrParent, m_tpGroupStyle) not exposed by Uno.
		ItemContainerGenerator? spParentGenerator;
		object? spContent;
		ICollectionViewGroup? spGroup;
		object? spGroupData;
		GroupStyle? pGroupStyleNoRef = null;

		bool areEqual = false;

		if (m_tpGenerator is null)
		{
			// user-declared GroupItem
			return;
		}

		spGroup = pGroupItem as ICollectionViewGroup;
		spGroupData = spGroup?.Group;

		spParentGenerator = ((ItemContainerGenerator)m_tpGenerator).m_wrParent as ItemContainerGenerator;

		spContent = Content;
		areEqual = PropertyValue.AreEqual(spGroupData, spContent);
		if (areEqual)
		{
			ClearValue(ContentControl.ContentProperty);
			SetCollectionViewGroup(null);
		}

		pGroupStyleNoRef = (spParentGenerator as ItemContainerGenerator)?.m_tpGroupStyle as GroupStyle;
		if (pGroupStyleNoRef is not null)
		{
			DataTemplate? spContentTemplate;
			DataTemplate? spHeaderTemplate;
			DataTemplateSelector? spContentTemplateSelector;
			DataTemplateSelector? spHeaderTemplateSelector;

			spContentTemplate = ContentTemplate;
			spHeaderTemplate = pGroupStyleNoRef.HeaderTemplate;
			if (spContentTemplate == spHeaderTemplate)
			{
				ClearValue(ContentControl.ContentTemplateProperty);
			}

			spContentTemplateSelector = ContentTemplateSelector;
			spHeaderTemplateSelector = pGroupStyleNoRef.HeaderTemplateSelector;
			if (spContentTemplateSelector == spHeaderTemplateSelector)
			{
				ClearValue(ContentControl.ContentTemplateSelectorProperty);
			}
		}
		m_tpGenerator.RemoveAll();
#endif
	}

	// Create GroupItemAutomationPeer to represent the GroupItem.
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		GroupItemAutomationPeer spGroupItemAutomationPeer;

		spGroupItemAutomationPeer = new GroupItemAutomationPeer(this);

		return spGroupItemAutomationPeer;
	}

	// Call ChangeSelectorItemsVisualState on our ItemsControl template part.
	// This results in a call to ChangeVisualState on all child SelectorItems (including items inside another GroupItem),
	// with optimizations for the virtualization provided by IOrientedVirtualizingPanel.
	internal void ChangeSelectorItemsVisualState(bool bUseTransitions)
	{
#if false // TODO Uno: ItemsControl.ChangeSelectorItemsVisualState is 'private protected' in Uno; not callable from GroupItem until visibility is widened or GroupItem is granted access.
		m_tpItemsControl?.ChangeSelectorItemsVisualState(bUseTransitions);
#endif
	}

	internal void SetCollectionViewGroup(ICollectionViewGroup? pGroup)
	{
		// SetPtrValue(m_tpCVG, pGroup);
		m_tpCVG = pGroup;
	}

	internal ICollectionViewGroup? GetCollectionViewGroup()
	{
		return m_tpCVG;
	}

	// Obtains the index of the group this GroupItem is representing.
	internal void GetGroupIndex(out int pGroupIndex)
	{
		ListViewBase? spIParentListViewBase = null;

		pGroupIndex = -1;

		m_wrParentListViewBaseWeakRef?.TryGetTarget(out spIParentListViewBase);
		if (spIParentListViewBase is not null)
		{
#if false // TODO Uno: ListViewBase.CollectionView is not exposed publicly in Uno.
			ICollectionView? spCollectionView;

			spCollectionView = spIParentListViewBase.CollectionView;
			if (spCollectionView is not null)
			{
				global::System.Collections.Generic.IList<object>? spCollectionGroups;

				spCollectionGroups = spCollectionView.CollectionGroups;
				if (spCollectionGroups is not null)
				{
					global::System.Collections.Generic.IList<object> spCollectionGroupsAsV;
					uint groupCount = 0;

					spCollectionGroupsAsV = spCollectionGroups;
					groupCount = (uint)spCollectionGroupsAsV.Count;

					for (uint i = 0; i < groupCount; i++)
					{
						object? spCurrent;
						bool areEqual = false;

						spCurrent = spCollectionGroupsAsV[(int)i];
						areEqual = object.Equals(m_tpCVG, spCurrent);
						if (areEqual)
						{
							pGroupIndex = (int)i;
							break;
						}

					}
				}
			}
#endif
		}
	}

	internal ItemsControl? GetTemplatedItemsControl()
	{
		return m_tpItemsControl;
	}

	// GroupItem will be asked to hide if it is empty and GroupStyle.HidesIfEmpty is TRUE. In this case
	// set Visibility to collapsed and add a listener to the generator to track if this group becomes non-empty.
	private void Hide()
	{
#if false // TODO Uno: ItemContainerGenerator.ItemsChanged is a NotImplemented stub in Uno; cannot subscribe.
		ItemsChangedEventHandler? spItemsChangedHandler;

		// memset(&m_itemsChangedToken, 0, sizeof(EventRegistrationToken));
		m_itemsChangedToken = null;

		spItemsChangedHandler = new ItemsChangedEventHandler(OnItemsChangedHandler);
		m_tpGenerator!.ItemsChanged += spItemsChangedHandler;
		m_itemsChangedToken = spItemsChangedHandler;

		m_previousVisibility = Visibility;
		Visibility = Visibility.Collapsed;
		m_bWasHidden = true;
#endif
	}

	private void OnItemsChangedHandler(
		object sender,
		ItemsChangedEventArgs args)
	{
#if false // TODO Uno: Requires ItemContainerGenerator.ItemForItemContainer property and ItemsChanged event infrastructure not implemented in Uno.
		ICollectionViewGroup? spGroup;
		object? spItem;
		uint nGroupSize = 0;

		// IFC_RETURN(GetValueByKnownIndex(KnownPropertyIndex::ItemContainerGenerator_ItemForItemContainer, spItem.GetAddressOf()));
		spItem = null; // TODO Uno: KnownPropertyIndex::ItemContainerGenerator_ItemForItemContainer is not available.
		spGroup = spItem as ICollectionViewGroup;

		// Group may be NULL if this container was unlinked e.g. OnRefresh.
		if (spGroup is not null)
		{
			global::System.Collections.Generic.IList<object>? spGroupItems;
			global::System.Collections.Generic.IList<object> spGroupView;

			spGroupItems = spGroup.GroupItems;
			spGroupView = spGroupItems;
			nGroupSize = (uint)spGroupView.Count;
		}

		// If the group becomes non-empty, un-hide the UI by removing the placeholder.
		if (nGroupSize > 0)
		{
			if (m_itemsChangedToken is not null)
			{
				if (m_tpGenerator is not null)
				{
					// Detach the generator's event handler and reset the generator.
					m_tpGenerator.ItemsChanged -= m_itemsChangedToken;
				}
				// memset(&m_itemsChangedToken, 0, sizeof(EventRegistrationToken));
				m_itemsChangedToken = null;
			}

			// Restore the previous visibility.
			Visibility = m_previousVisibility;
			m_bWasHidden = false;
		}
#endif
	}

	// Places focus on the group header, if possible. Note the default GroupItem style has a non-focusable HeaderContent
	// part, meaning that there needs to be focusable content within HeaderContent for this to work.
	internal void FocusHeader(FocusState focusState, out bool pDidSetFocus)
	{
		pDidSetFocus = false;

		if (m_tpHeaderControl is not null)
		{
			pDidSetFocus = (m_tpHeaderControl as UIElement)?.Focus(focusState) ?? false;
		}
	}

	// Called when the GroupItem receives focus.
	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		object? spOriginalSource;
		bool hasFocus = false;
		FocusState focusState = FocusState.Programmatic;

		base.OnGotFocus(pArgs);

		spOriginalSource = pArgs.OriginalSource;
		if (spOriginalSource is not null)
		{
			UIElement? spFocusedElement;

			spFocusedElement = spOriginalSource as UIElement;
			if (spFocusedElement is not null)
			{
				focusState = spFocusedElement.FocusState;
			}
		}

#if false // TODO Uno: DependencyObject.HasFocus() is not exposed publicly in Uno.
		hasFocus = HasFocus();
#endif
		FocusChanged(hasFocus, focusState);
	}

	// Called when the GroupItem loses focus.
	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		object? spOriginalSource;
		bool hasFocus = false;
		FocusState focusState = FocusState.Unfocused;

		base.OnLostFocus(pArgs);

		spOriginalSource = pArgs.OriginalSource;
		if (spOriginalSource is not null)
		{
			UIElement? spFocusedElement;

			spFocusedElement = spOriginalSource as UIElement;
			if (spFocusedElement is not null)
			{
				focusState = spFocusedElement.FocusState;
			}
		}

#if false // TODO Uno: DependencyObject.HasFocus() is not exposed publicly in Uno.
		hasFocus = HasFocus();
#endif
		FocusChanged(hasFocus, focusState);
	}

	// Update our parent ListView when group focus changes so it can manage
	// the currently focused header, if appropriate.
	private void FocusChanged(
		bool hasFocus,
		FocusState howFocusChanged)
	{
		ListViewBase? spIParentListViewBase = null;
		bool isFocusedByPointer = !(FocusState.Keyboard == howFocusChanged ||
									   FocusState.Programmatic == howFocusChanged);

		m_wrParentListViewBaseWeakRef?.TryGetTarget(out spIParentListViewBase);
		if (spIParentListViewBase is not null)
		{
			ListViewBase pParentListViewBaseNoRef = spIParentListViewBase;
#if false // TODO Uno: ListViewBase.HasFocusedGroup/OnGroupFocusChanged/GroupItemFocused/GroupItemUnfocused are not implemented in Uno.
			// If the parent ListView doesn't have a focused group, then focus
			// is coming from outside of the ListView.
			// If this is the case and we're not being focused by pointer, ask our LVB to
			// forward focus to the correct element.
			if (!isFocusedByPointer && !pParentListViewBaseNoRef.HasFocusedGroup())
			{
				bool headerHasFocus = false;
				if (m_tpHeaderControl is not null)
				{
					headerHasFocus = m_tpHeaderControl.HasFocus();
				}
				// If this GroupItem is obtaining focus from outside the
				// ListView, then it's merely the first focusable header and we
				// really want to focus the last focused group header.  Pass this
				// notification along to our parent who will focus the last
				// focused header.  (Note: Our parent will usually have
				// IsTabStop=False so it won't receive its own GotFocus and
				// LostFocus notifications.)
				pParentListViewBaseNoRef.OnGroupFocusChanged(hasFocus, headerHasFocus, howFocusChanged);
			}
			else if (hasFocus)
			{
				pParentListViewBaseNoRef.GroupItemFocused(this);
			}
			else
			{
				pParentListViewBaseNoRef.GroupItemUnfocused(this);
			}
#endif
		}
	}

	// Unloads all containers to main generator container recycling queue
	internal void Recycle()
	{
		if (m_tpItemsControl is not null)
		{
			Panel? spItemsHost;

			// Uno: WinUI uses ItemsControl::get_ItemsHost; Uno exposes the equivalent as ItemsPanelRoot.
			spItemsHost = m_tpItemsControl.ItemsPanelRoot;

			// we should ignore transitions during recycling
			if (spItemsHost is not null)
			{
#if false // TODO Uno: Panel.IsIgnoringTransitions is not implemented in Uno.
				spItemsHost.IsIgnoringTransitions = true;
#endif
			}
		}

		if (m_bWasHidden)
		{
			if (m_itemsChangedToken is not null)
			{
				if (m_tpGenerator is not null)
				{
#if false // TODO Uno: ItemContainerGenerator.ItemsChanged is a NotImplemented stub in Uno.
					// Detach the generator's event handler and reset the generator.
					m_tpGenerator.ItemsChanged -= m_itemsChangedToken;
#endif
				}
				// memset(&m_itemsChangedToken, 0, sizeof(EventRegistrationToken));
				m_itemsChangedToken = null;
			}
			// Restore the previous visibility.
			Visibility = m_previousVisibility;

			// initialize to initial state as c~tor does
			m_bWasHidden = false;
			m_previousVisibility = Visibility.Collapsed;
		}

		// unload containers and clear items host panel
		if (m_tpGenerator is not null)
		{
#if false // TODO Uno: ItemContainerGenerator.Refresh is an internal WinUI method not exposed by Uno's stub.
			((ItemContainerGenerator)m_tpGenerator).Refresh();
#endif
		}
	}

	// Provides the behavior for the Arrange pass of layout.  Classes
	// can override this method to define their own Arrange pass
	// behavior.
	protected override Size ArrangeOverride(
		// The computed size that is used to arrange the content.
		Size arrangeSize)
	{
		Size returnValue;

		returnValue = base.ArrangeOverride(arrangeSize);

		if (m_tpItemsControl is not null)
		{
			Panel? spItemsHost;

			// Uno: WinUI uses ItemsControl::get_ItemsHost; Uno exposes the equivalent as ItemsPanelRoot.
			spItemsHost = m_tpItemsControl.ItemsPanelRoot;

			if (spItemsHost is not null)
			{
#if false // TODO Uno: Panel.IsIgnoringTransitions is not implemented in Uno.
				spItemsHost.IsIgnoringTransitions = false;
#endif
			}
		}

		return returnValue;
	}

	internal void SetGenerator(
		ItemContainerGenerator? pGenerator)
	{
		if (pGenerator is not null)
		{
			// SetPtrValue(m_tpGenerator, pGenerator);
			m_tpGenerator = pGenerator;
		}
		else
		{
			m_tpGenerator = null;
		}
	}
}
