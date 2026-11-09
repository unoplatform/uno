// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_IICG2_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
#if !HAS_UNO // TODO Uno: only used by the containment gates in RecycleLinkedContainer and RecycleHeader.
	// Bug 62434240: [2.0 Servicing] Re-elect tracked element on recycle to prevent FAIL_FAST_ASSERT in PlaceInValidElements (RCC: ModernCollectionBasePanel_StaleViewportAnchorRecycle)
	private const int WINAPPSDK_CHANGEID_62434240 = 62434240;
#endif

	//#define MCBP_DEBUG

	private void ReloadUnloadedElements()
	{
		uint nSize = (uint)m_tpUnloadedElements.Count;

		if (nSize == 0)
		{
			return;
		}

		var spChildren = m_cacheManager.GetChildren();

		// we have 2 pass intentionally.
		// First pass will check existing children and if any exist in unloaded elements we will just remove it from unloaded
		for (int i = (int)nSize - 1; i >= 0; --i)
		{
			UIElement spContainer = m_tpUnloadedElements[i];
			bool found = spChildren.IndexOf(spContainer) >= 0;
			if (found)
			{
				m_tpUnloadedElements.RemoveAt(i);
			}
		}

		nSize = (uint)m_tpUnloadedElements.Count;

		// Second pass we move all elements which are not in visual tree to the visual tree
		// It allows us to not increase size of children collection for IndexOf operation.
		for (uint i = 0; i < nSize; ++i)
		{
			UIElement spContainer = m_tpUnloadedElements[0];
			spChildren.Add(spContainer);
			m_tpUnloadedElements.RemoveAt(0);
		}
	}

	internal void GetRootOfItemTemplateAsContainer(IGeneratorHost spHost, object? item, bool queryForInspecting, ref UIElement? container)
	{
		container = null;

		// Scope this feature only to ListViewBase.
		// This bug tracks making it work correctly in the ItemsControl general case:
		// Bug 17815731: Using DataTemplate as Container feature does not work for ItemsControl (breaks binding)
		ListViewBase? listViewBase = spHost as ListViewBase;
		if (listViewBase is not null)
		{
			// Check if the ItemTemplate includes a container. If so use that. This allows us to provide
			// a container as part of the ItemTemplate.
			ItemsControl? itemsControl = spHost as ItemsControl;
			if (itemsControl is not null)
			{
				// Get the template from either ItemsControl.ItemTemplate or ItemsControl.ItemTemplateSelector
				DataTemplate? selectedTemplate = itemsControl.ItemTemplate;
				if (selectedTemplate is null)
				{
					// Check TemplateSelector
					DataTemplateSelector? itemTemplateSelector = itemsControl.ItemTemplateSelector;
					if (itemTemplateSelector is not null)
					{
						selectedTemplate = itemTemplateSelector.SelectTemplate(item!);
					}
				}

				if (selectedTemplate is not null)
				{
					DependencyObject? expandedTemplateCoreNoRef;
					if (queryForInspecting)
					{
						// Query for an instance of the template. We may or may not use it, so the QueryContentNoRef call caches the instance in CDataTemplate to be used later.
						expandedTemplateCoreNoRef = selectedTemplate.QueryContentNoRef();
					}
					else
					{
						// We are going to use the instance, so call LoadContent so that the
						// cached instance in CDataTemplate will be cleared.
						// LoadContent returns a CDependencyObject that we must release, so we store it in a smart pointer.
						// The QueryContentNoRef call above returns a raw pointer to a cached object that the template owns, so we do not want to release it.
						expandedTemplateCoreNoRef = selectedTemplate.LoadContent();
					}

					if (expandedTemplateCoreNoRef is not null)
					{
						DependencyObject expandedTemplate = expandedTemplateCoreNoRef;
						bool isOwnContainer = spHost.IsItemItsOwnContainer(expandedTemplate);
						if (isOwnContainer)
						{
							container = (UIElement)expandedTemplate;
						}
						else
						{
							// not its own container.
							container = null;
						}
					}
				}
			}
		}
	}

	// Creates or recycles a container for the given index.
	internal UIElement? GenerateContainerAtIndex(int indexInItemCollection)
	{
#if !HAS_UNO // TODO Uno: no ETW tracing.
		TraceGenerateMCContainerBegin(indexInItemCollection);
		using var traceScope = Uno.Disposables.Disposable.Create(() => TraceGenerateMCContainerEnd());
#endif

		UIElement? spContainer = null;
		UIElement? spContainerFromItemTemplate = null;
		bool shouldSkipContainerPreparation = false;
		bool isOwnContainer = false;

		// Increment container requested count for telemetry purposes - only done if cache potential reached so we are counting use during panning and not initial setup
		if (m_windowState.cachePotentialReached)
		{
			++m_containerRequestedCount;
		}

		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		// get content for the container
		global::System.Diagnostics.Debug.Assert(0 <= indexInItemCollection && indexInItemCollection < m_cacheManager.GetTotalItemCount());
		object? spItem = m_cacheManager.GetItem(indexInItemCollection);

		// We need to know if this item/container is pinned.
		UIElement? spPinnedContainer = m_containerManager.GetContainerFromPinnedContainers(indexInItemCollection);
		bool foundPinnedContainer = spPinnedContainer is not null;

		// Try several approaches to getting a container for this item.
		// Different approaches yield varying degrees of preparedness
		// for a container.

		// Query to see if we need to skip container preparation
		GetRootOfItemTemplateAsContainer(spIHost, spItem, true /* queryOnly */, ref spContainerFromItemTemplate);

		if (foundPinnedContainer) // Do we have the container in the pinned collection?
		{
			// The container was pinned. We're done, just return it.
			// Pinned containers are already linked and prepared, unless we're in the process
			// of preparing them.
			spContainer = spPinnedContainer;
		}
		else
		{
#if HAS_UNO
			ReclaimUnloadingContainerForItem(spItem);
#endif

			isOwnContainer = spIHost.IsItemItsOwnContainer(spItem);
			if (isOwnContainer) // Is the item a container in its own right?
			{
				spContainer = (UIElement)spItem!;
#if HAS_UNO
				// TODO Uno: ItemsControlFromItemContainer reads the owner from a property the host sets here instead of walking the parents.
				spIHost.GetContainerForItem(spItem!, null);
#endif
				spContainer.InitVirtualizationInformation();
				SetElementIsGenerated(spContainer, false);
			}
			else
			{
				// Container wasn't pinned. Get a container some other way.
				// We need to link and prepare it as well, since it was previously
				// unlinked and cleared (or was never prepared and linked at all).
				UIElement? spSuggestedContainer = null;
				SelectorItem? spContainerAsSelectorItem = null;
				bool shouldRaiseChoosingItemContainer = false;
				bool choosingItemContainerIsContainerPrepared = false;

				IContainerRecyclingContext? spRecyclingContext = m_cacheManager.GetContainerRecyclingContext((uint)indexInItemCollection);

				if (spRecyclingContext is not null)
				{
					spSuggestedContainer = spRecyclingContext.SelectedContainer;
					if (spSuggestedContainer is not null)
					{
						global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spSuggestedContainer));
						global::System.Diagnostics.Debug.Assert(!GetElementIsHeader(spSuggestedContainer));

						RemoveFromQueue(spSuggestedContainer);
						spRecyclingContext.ConfigureSelectedContainer(spSuggestedContainer);
					}
					else
					{
						// Check the queue Length
						if (m_recycleQueue.Count >= QueueLengthBeforeFallback)
						{
							// Lets recycle a container even if it does not have the right template
							spSuggestedContainer = ResolveWeakElement(m_recycleQueue[m_recycleQueue.Count - 1]);
							m_recycleQueue.RemoveAt(m_recycleQueue.Count - 1);
							global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spSuggestedContainer!));
							global::System.Diagnostics.Debug.Assert(!GetElementIsHeader(spSuggestedContainer!));
							spRecyclingContext.ConfigureSelectedContainer(spSuggestedContainer!);
							spRecyclingContext.SelectedContainer = null;
						}
					}
				}
				else if (m_recycleQueue.Count != 0) // Do we have containers ready in the recycle queue?
				{
					spSuggestedContainer = ResolveWeakElement(m_recycleQueue[m_recycleQueue.Count - 1]);
					m_recycleQueue.RemoveAt(m_recycleQueue.Count - 1);
					global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spSuggestedContainer!));
					global::System.Diagnostics.Debug.Assert(!GetElementIsHeader(spSuggestedContainer!));
				}

				if (spSuggestedContainer is not null)
				{
					spContainerAsSelectorItem = spSuggestedContainer as SelectorItem;
				}

				shouldRaiseChoosingItemContainer = spIHost.ShouldRaiseChoosingItemContainer();

				if (shouldRaiseChoosingItemContainer)
				{
					if (m_trChoosingItemContainerEventArgs is null)
					{
						m_trChoosingItemContainerEventArgs = new ChoosingItemContainerEventArgs();
					}

					m_trChoosingItemContainerEventArgs.ItemIndex = indexInItemCollection;
					m_trChoosingItemContainerEventArgs.Item = spItem;
					m_trChoosingItemContainerEventArgs.ItemContainer = spContainerAsSelectorItem;
					m_trChoosingItemContainerEventArgs.IsContainerPrepared = false;

					// First prompt the application to provide a container for this item via ChoosingItemContainer.
					spIHost.RaiseChoosingItemContainer(m_trChoosingItemContainerEventArgs);

					spContainerAsSelectorItem = m_trChoosingItemContainerEventArgs.ItemContainer;
					choosingItemContainerIsContainerPrepared = m_trChoosingItemContainerEventArgs.IsContainerPrepared;

					// NOTE: We still need to prepare this container via the items host (see PrepareContainerViaItemsHost),
					// unless ChoosingItemContainer was marked as handled, in which case we can just reuse it as-is.
					shouldSkipContainerPreparation = choosingItemContainerIsContainerPrepared;
				}

				if (spContainerAsSelectorItem is not null)
				{
					spContainer = spContainerAsSelectorItem;
				}

				// If we suggested a container and if we're not going to use it,
				// then we should put that container back into the recycle queue.
				if (spContainerAsSelectorItem is not null && spContainerAsSelectorItem != spSuggestedContainer)
				{
					spContainer!.InitVirtualizationInformation();

					// put back the original one
					if (spSuggestedContainer is not null)
					{
						// and make a note that the app did not like this container
						// this will make sure that we keep trying to recycle if the recycle queue only has elements that are
						// rejected
						var p_virtualizationinfo = GetVirtualizationInformationFromElement(spSuggestedContainer);
						p_virtualizationinfo!.WasRejectedAsAContainerByApp = true;

						m_recycleQueue.Add(new WeakReference<UIElement>(spSuggestedContainer));
					}

					// ofcourse, the container that we got could have been from the recycle queue itself
					// in fact, we really hope it is!
					RemoveFromQueue(spContainer, false /*expectContainerToBePresent*/);

					SetElementIsGenerated(spContainer, true);

					// Set a local value of null to block data context propagation.
					((FrameworkElement)spContainer).DataContext = null;

					if (spRecyclingContext is not null)
					{
						spRecyclingContext.ConfigureSelectedContainer(spContainer);
						spRecyclingContext.SelectedContainer = null;
					}
				}
				// Otherwise, if ChoosingItemContainer didn't give us a container, then we'll need to get one a different way,
				// either by directly using the suggested container or by creating a new container if no container was suggested.
				else if (spContainerAsSelectorItem is null)
				{
					if (spSuggestedContainer is not null)
					{
						spContainer = spSuggestedContainer;
					}
					else
					{
						// Increment new container count for telemetry purposes - only done if cache potential reached so we are counting use during panning and not initial setup
						if (m_windowState.cachePotentialReached)
						{
							++m_containerCreatedCount;
						}

						// If root of ItemTemplate is a container. use that.
						if (spContainerFromItemTemplate is not null)
						{
							// false to queryOnly argument will call LoadContent to clear the cached entry in CDataTemplate since we are
							// going to consume that instance now.
							GetRootOfItemTemplateAsContainer(spIHost, spItem, false /* queryOnly */, ref spContainer);
						}
						else
						{
							// have to actually create the container from scratch. This is an expensive operation
							DependencyObject spContainerAsDO = spIHost.GetContainerForItem(spItem, null);
							spContainer = (UIElement)spContainerAsDO;
						}

						spContainer!.InitVirtualizationInformation();
						SetElementIsGenerated(spContainer, true);

						// set a local value of null to block datacontext propagation
						((FrameworkElement)spContainer).DataContext = null;

						if (spRecyclingContext is not null)
						{
							spRecyclingContext.ConfigureSelectedContainer(spContainer);
							spRecyclingContext.SelectedContainer = null;
						}

					}
				}
			}

			// to avoid re-parenting we should mark the recycled container that it did't leave the tree in current layout cycle.
			CoreImports.UIElement_SetIsLeaving(spContainer!, false);

			// make sure it has a valid virtualizationinformation section.
			spContainer!.InitVirtualizationInformation();

			SetElementIsHeader(spContainer, false);

			LinkContainerToItem(spContainer, spItem);
		}

		SetElementEmptySizeInGarbageSection(spContainer!);

		if (spContainerFromItemTemplate is not null && !isOwnContainer)
		{
			// If root of ItemTemplate is a container, and the item itself is not a container,
			// Skip container preparation. Otherwise we will end up setting the ContentTemplate
			// on the container which we do not want here. This is tracked by a flag in VirtualizationInformation.
			SetIsContainerFromTemplateRoot(spContainer!, true /* isFromTemplateRoot */);
		}

		SetElementWantsToSkipContainerPreparation(spContainer!, shouldSkipContainerPreparation);

#if !HAS_UNO // TODO Uno: no CUIElement item container flag for implicit show/hide animations.
		// Item Containers have special rules regarding implicit show/hide animations.
		// Set a flag so that this case can be properly detected.
		spContainer.SetIsItemContainer(true);
#endif

		return spContainer;
	}

	// Delegate the preparation of an container to the items host.
	// Note that caller has to guarantee that the container is already in the visual tree since this is going to do a measure
	private void PrepareContainerViaItemsHost(
		int indexInItemCollection,
		Size measureSize,
		UIElement spContainer)
	{
		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		if (!GetElementIsPrepared(spContainer))
		{
			object? spItem = GetItemFromElement(spContainer);

			// Temporarily hold it so that lookups will work during during preparation.
			m_containerManager.HoldForPrepare(spContainer, ElementType.ItemContainer, indexInItemCollection);

#if HAS_UNO
			spContainer.SetValue(ItemsControl.IndexForItemContainerProperty, Uno.UI.Helpers.Boxes.Boxer.Box(indexInItemCollection));
#endif
			spIHost.PrepareItemContainer(spContainer, spItem);

			// after a prepare we are guaranteeing that we are going to raise the ContainerContentChanging event
			// when setting content, we need to guarantee the contenttemplateroot has been applied, and thus we need to trigger
			// a measure. For that to not incur overhead because of later measures, we need to be accurate in the size that
			// we measure with (if you are not invalid and the size you get measured with hasn't changed, it should no-op)
			spIHost.SetupContainerContentChangingAfterPrepare(spContainer, spItem, indexInItemCollection, measureSize);

			// make the container a candidate for gamedpad/2D focus.
			spContainer.IsGamepadFocusCandidate = true;

			m_containerManager.ResetAfterPrepare();
		}
		else
		{
			// We don't want to prepare and re-setup CCC/CIC events for pinned container, but we should at least ensure that our items host
			// will be registered for work on the next BuildTree pass to pick up what might be a changed visible/cache range

			// MCBP doesn't need to know about CCC/CIC events, also the non treebuild controls don't need to create the CCC/CIC event args
			// let the Host decide if it needs the CCC/CIC args.
			spIHost.RegisterWorkForContainer(spContainer);
		}
	}

	// Delegate the preparation of an container to the items host.
	// Note that caller has to guarantee that the container is already in the visual tree since this is going to set the style
	private void PrepareHeaderViaItemsHost(
		int indexInGroupCollection,
		Size measureSize, // Not using this parameter for now.
		UIElement spHeader)
	{
		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		global::System.Diagnostics.Debug.Assert(0 <= indexInGroupCollection && indexInGroupCollection < m_cacheManager.GetTotalGroupCount());
		ICollectionViewGroup? spCurrentGroup = m_cacheManager.GetGroup(indexInGroupCollection);

		// Temporarily hold it so that lookups will work during during preparation.
		m_containerManager.HoldForPrepare(spHeader, ElementType.GroupHeader, indexInGroupCollection);

		spIHost.PrepareGroupContainer(spHeader, spCurrentGroup);

		m_containerManager.ResetAfterPrepare();
	}

	// Creates or recycles a header for the given index.
	internal UIElement? GenerateHeaderAtGroupIndex(int index)
	{
		UIElement? spHeader;
		bool foundPinnedHeader = false;
		bool shouldRaiseChoosingGroupHeaderContainer = false;

		// this implementation works with our own panels

		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		global::System.Diagnostics.Debug.Assert(0 <= index && index < m_cacheManager.GetTotalGroupCount());
		ICollectionViewGroup? spCurrentGroup = m_cacheManager.GetGroup(index);

		// do we have it in the pinned collection?
		spHeader = m_containerManager.GetHeaderFromPinnedHeaders(index);
		foundPinnedHeader = spHeader is not null;

		// Use the pinned header if one was found, like GenerateContainerAtIndexImpl uses pinned item containers when found.
		if (spHeader is null)
		{
			if (m_recycleHeaderQueue.Count != 0)
			{
				// great, I can get it from the queue
				spHeader = ResolveWeakElement(m_recycleHeaderQueue[m_recycleHeaderQueue.Count - 1]);
				global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spHeader!));
				m_recycleHeaderQueue.RemoveAt(m_recycleHeaderQueue.Count - 1);
			}

			shouldRaiseChoosingGroupHeaderContainer = spIHost.ShouldRaiseChoosingGroupHeaderContainer();

			if (shouldRaiseChoosingGroupHeaderContainer)
			{
				ListViewBaseHeaderItem? spHeaderObtained;
				UIElement? spHeaderObtainedAsUIElement;
				object? spGroupData;

				// Provide a chance for the app to give us a container
				// For improved performance, we just create on event args and resuse it every time we raise the event
				if (m_trChoosingGroupHeaderContainerEventArgs is null)
				{
					m_trChoosingGroupHeaderContainerEventArgs = new ChoosingGroupHeaderContainerEventArgs();
				}

				spGroupData = spCurrentGroup!.Group;
				m_trChoosingGroupHeaderContainerEventArgs.Group = spGroupData;
				m_trChoosingGroupHeaderContainerEventArgs.GroupIndex = index;
				m_trChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer = spHeader as ListViewBaseHeaderItem;

				// prompt the application to provide a header container through ChoosingGroupHeaderContainer
				spIHost.RaiseChoosingGroupHeaderContainer(m_trChoosingGroupHeaderContainerEventArgs);
				spHeaderObtained = m_trChoosingGroupHeaderContainerEventArgs.GroupHeaderContainer;

				if (spHeader != null && spHeaderObtained == null)
				{
					// We suggested a container but got back null from ChoosingGroupHeaderContainer
					throw new InvalidOperationException();
				}

				// If ChoosingGroupHeaderContainer gave us a container (that is not what we suggested), use that
				// If it gave back null, create a new one
				// If it gave back what we suggested, then just do what we would normally do (like the event didn't exist)
				if (spHeaderObtained != null && spHeader != spHeaderObtained)
				{
					// ChoosingGroupHeaderContainer gave us a container that was not the one we gave it.
					// If we suggested a header container put that back into the recycle queue
					if (spHeader is not null)
					{
						m_recycleHeaderQueue.Add(new WeakReference<UIElement>(spHeader));
					}

					// Check if the header returned is in the recycle queue. If so, remove it
					spHeaderObtainedAsUIElement = spHeaderObtained;
					bool wasInRecycleQueue = RemoveFromHeaderRecycleQueue(spHeaderObtainedAsUIElement);

					if (!wasInRecycleQueue)
					{
						// Check if the header is already realized. If so, throw an exception
						bool hasParent = VisualTreeHelper.GetParent(spHeaderObtainedAsUIElement) is not null;
						if (hasParent)
						{
							throw new InvalidOperationException("ERROR_LISTVIEWBASE_GROUPHEADERCONTAINER_ALREADY_IN_USE");
						}
					}

					// Got back a valid container, use that
					spHeader = spHeaderObtainedAsUIElement;
					spHeader.InitVirtualizationInformation();
					SetElementIsGenerated(spHeader, true);
				}
			}

			if (spHeader is null)
			{
				// create a new group header
				DependencyObject spHeaderAsDO = spIHost.GetHeaderForGroup(spCurrentGroup);
				spHeader = (UIElement)spHeaderAsDO;
				spHeader.InitVirtualizationInformation();
				SetElementIsGenerated(spHeader, true);

				((FrameworkElement)spHeader).DataContext = null;
			}
		}

		SetElementIsHeader(spHeader, true);
		SetElementEmptySizeInGarbageSection(spHeader);

		// put it in the pinned containers, so that lookups will work during the Link and Prepare phases
		if (!foundPinnedHeader)
		{
			m_containerManager.RegisterPinnedHeader(index, spHeader);

			// if we are pinned, we've already been linked and prepared
			// per definition we have a matched group
			// todo: assert this?
			LinkHeaderToGroup(spHeader, spCurrentGroup);

			// possibly remove it from the pinned containers
			m_containerManager.UnregisterPinnedHeader(index);
		}

#if !HAS_UNO // TODO Uno: no CUIElement item container flag for implicit show/hide animations.
		// Item Containers/Headers have special rules regarding implicit show/hide animations.
		// Set a flag so that this case can be properly detected.
		spHeader.SetIsItemContainer(true);
#endif

		return spHeader;
	}

	// Is our container recycle queue empty?
	internal bool GetContainerRecycleQueueEmpty() => m_recycleQueue.Count == 0;

	// Makes sure that whatever container we think is focused is still focused.
	// Otherwise, recycle it.
	private void VerifyStoredFocusedContainer()
	{
		bool stillHasFocus = StoredFocusedContainerStillHasFocus();
		if (!stillHasFocus && !m_containerManager.m_isScrollIntoViewPending)
		{
			ReleaseStoredFocusedContainer();
		}
	}

	// Does the previously stored focused container still have focus?
	private bool StoredFocusedContainerStillHasFocus()
	{
		bool hasFocus = false;

		m_containerManager.GetFocusedContainer(out _, out UIElement? spFocusedContainer);

		if (spFocusedContainer is not null)
		{
			FrameworkElement? spFE;
			spFE = spFocusedContainer as FrameworkElement;

			if (spFE is not null)
			{
				hasFocus = spFE.HasFocus();
			}
		}

		return hasFocus;
	}

	// If we have a stored focused container, release it back into the recycle queue.
	private void ReleaseStoredFocusedContainer()
	{
		m_containerManager.GetFocusedContainer(out int itemIndex, out UIElement? spFocusedContainer);

		if (spFocusedContainer is not null)
		{
			// We can potentially re-use this container
			// Do not remove yet, just reset the reference to focused element, the removal will happen later.
			m_containerManager.ResetFocusedContainer(false);

			// would not have made it into the recycle queue if it was recycled, but won't
			// be in valid range either
			if (!m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(itemIndex)))
			{
				// container is not in the valid range, so we can now really put it in the recyclequeue. Before this it was being
				// held onto by direct storage.

				// ICG2 will not actually recycle if an element is pinned. This makes sense, since the concept of pinning
				// is that something should not be recycled.
				// However, something being focused also counts as being pinned.
				// So we have to do the recycling ourselves. I'd rather do that than add an ugly parameter to recyclecontainer.

				// find out if it is still pinned, even after resetting the focused container
				if (!m_containerManager.GetIsContainerPinned(spFocusedContainer))
				{
					// now temporarily pin it, so that Clears know how to map index to container
					m_containerManager.SetFocusedContainer(itemIndex, spFocusedContainer);

					RecycleLinkedContainer(spFocusedContainer);

					// and finally clear it out again
					// This time remove the non-generated container from the tree
					m_containerManager.ResetFocusedContainer(true);
				}
			}
		}
	}

	// Call UnlinkContainerFromItem and ClearContainerForItem on the given container. If the item isn't
	// its own container, return the container to the recycle queue.
	// For pinned items, this is a no-op.
	internal void RecycleContainer(UIElement container)
	{
		TryRecycleContainer(container);
	}

	// Call UnlinkContainerFromItem and ClearContainerForItem on the given container. If the item isn't
	// its own container, return the container to the recycle queue.
	// For pinned items, this is a no-op.
	internal bool TryRecycleContainer(UIElement container)
	{
		bool recycled = false;
		UIElement spContainer = container;

		// pinned containers do not get recycled
		if (!m_containerManager.GetIsContainerPinned(spContainer))
		{
			RecycleLinkedContainer(spContainer);
			recycled = true;
		}

		return recycled;
	}

	// Prepare the container for recycling by unlinking and clearing, then recycle the container.
	// (recycling means different things depending on if the container is generated or not).
	// Generated containers are left in the tree and reused for another item, while
	// nongenerated containers are removed from the tree entirely (and will be re-added when the
	// virtualization window is appropriate for them).
	// Linked containers are containers which have had LinkContainerToItem and PrepareContainerForItem
	// called on them.
	internal void RecycleLinkedContainer(UIElement container)
	{
		UIElement spContainer = container;
		var p_virtualizationInformation = GetVirtualizationInformationFromElement(spContainer);
		// make a note that the user has not yet dismissed this container perse
		p_virtualizationInformation!.WasRejectedAsAContainerByApp = false;
		// rejecting happens after we use this container as a suggestion to the CIC event.

		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		object? spItem = UnlinkContainerFromItem(spContainer);

		// Items which are their own containers are not eligible for the recycle queue.
		bool isOwnContainer = spIHost.IsItemItsOwnContainer(spItem);

		// this calls clear on the container, this is expensive
		// However, we 'defer' the actual Clear to idle time when this panel is in a listview.
		// In fact, if a prepare comes along before we've had time to perform the clear, we will
		// skip the whole clear logic.
		spIHost.ClearContainerForItem(spContainer, spItem);

		// make the container not focusable by gamepad/2D auto focus. We do not
		// want a recycled container to get focused. This usually ends up causing
		// focus to jump when the container gets reused.
		spContainer.IsGamepadFocusCandidate = false;

		// Declare the container not focusable in general so that it cannot be selected as
		// a focus candidate. This is to handle the rare case where a focus selection is
		// imminently made before the next measure pass (i.e. before the next execution of
		// ModernCollectionBasePanel::MeasureElementsInGarbageSection() where recycled items are
		// marked unfocusable in the common cases via a SetElementSizeInGarbageSection call).
		SetElementEmptySizeInGarbageSection(spContainer);

#if !HAS_UNO // TODO Uno: no ETW tracing.
		TraceVirtualizedItemRemovedInfo((ulong)spContainer.GetHandle());
#endif

		// Only containers that are not items are eligible for recycling.
		// Ideally the item should specify what "recycling" means for itself,
		// (i.e., generated containers are pushed into the recycle queue and left in-tree,
		// while nongenerated containers are no-opped here).
		// For now this knowledge is rolled in here.
		if (!isOwnContainer)
		{
			global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spContainer));

			CoreImports.UIElement_CancelTransition(spContainer);

			m_recycleQueue.Add(new WeakReference<UIElement>(spContainer));

			// Make sure we are not recycling a tracked element. If we are then
			// leave a trace breadcrumb and keep going. We are likely to hit a crash
			// shortly, but atleast the dump will have this callstack in the stowed
			// exceptions to figure out what caused this situation.
			if (m_viewportBehavior.isTracking &&
				m_viewportBehavior.type == ElementType.ItemContainer)
			{
				UIElement? trackedElement = GetTrackedElement();
				if (trackedElement == container)
				{
#if !HAS_UNO // TODO Uno: WinAppSDK containment gate has no equivalent, the change is always on.
					if (WinAppSdk.Containment.IsChangeEnabled<WINAPPSDK_CHANGEID_62434240>())
#endif
					{
						// Re-elect a visually-adjacent realized element so the next measure pass doesn't seed from a recycled container.
						TraceGuardFailure("TrackedContainerRecycled - NewTrackedElementElected");
						ElectNewTrackedElement();
					}
#if !HAS_UNO
					else
					{
						TraceGuardFailure("TrackedContainerRecycled");
					}
#endif
				}
			}
		}
	}

	// Calls RecycleContainer on all realized containers.
	internal void RecycleAllContainers()
	{
		for (int validContainerIndex = 0; validContainerIndex < m_containerManager.GetValidContainerCount(); ++validContainerIndex)
		{
			UIElement? spContainer = m_containerManager.GetContainerAtValidIndex(validContainerIndex);

			if (spContainer is not null)
			{
				RecycleContainer(spContainer);
			}
		}
	}

	// Is our header recycle queue empty?
	internal bool GetHeaderRecycleQueueEmpty() => m_recycleHeaderQueue.Count == 0;

	// Call UnlinkHeaderFromGroup and ClearContainerForGroup on the given header and return it to
	// the recycle queue (except for pinned headers, for which this is a no-op).
	internal void RecycleHeader(UIElement header)
	{
		// move this container into the recycle queue
		UIElement spHeader = header;

		IGeneratorHost spIHost = m_cacheManager.GetItemsHost()!;

		// pinned headers do not get recycled
		if (!m_containerManager.GetIsHeaderPinned(spHeader))
		{
			object? spItem = UnlinkHeaderFromGroup(spHeader);
			spIHost.ClearGroupContainerForGroup(spHeader, spItem as ICollectionViewGroup);

			global::System.Diagnostics.Debug.Assert(GetElementIsGenerated(spHeader));
			m_recycleHeaderQueue.Add(new WeakReference<UIElement>(spHeader));

			// Make sure we are not recycling a tracked element. If we are then
			// leave a trace breadcrumb and keep going. We are likely to hit a crash
			// shortly, but at least the dump will have this callstack in the stowed
			// exceptions to figure out what caused this situation.
			if (m_viewportBehavior.isTracking &&
				m_viewportBehavior.type == ElementType.GroupHeader)
			{
				UIElement? trackedElement = GetTrackedElement();
				if (trackedElement == header)
				{
#if !HAS_UNO // TODO Uno: WinAppSDK containment gate has no equivalent, the change is always on.
					if (WinAppSdk.Containment.IsChangeEnabled<WINAPPSDK_CHANGEID_62434240>())
#endif
					{
						// Re-elect a visually-adjacent realized element so the next measure pass doesn't seed from a recycled header.
						TraceGuardFailure("TrackedHeaderRecycled - NewTrackedElementElected");
						ElectNewTrackedElement();
					}
#if !HAS_UNO
					else
					{
						TraceGuardFailure("TrackedHeaderRecycled");
					}
#endif
				}
			}

			// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
			if (m_bUseStickyHeaders)
			{
				// Remove the parametric curves as the header reusing the container will have different curves
				RemoveStickyHeader(spHeader);
			}
		}
	}

	// Calls RecycleHeaderImpl on all realized headers.
	internal void RecycleAllHeaders()
	{
		for (int validHeaderIndex = 0; validHeaderIndex < m_containerManager.GetValidHeaderCount(); ++validHeaderIndex)
		{
			UIElement? spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
			if (spHeader is not null)
			{
				RecycleHeader(spHeader);
			}
		}
	}

#if HAS_UNO
	// TODO Uno: WinUI fades an LTE, so it recycles the container right away and the item's return finds it at the
	// back of the queue. Here the container itself fades: ending its unload puts it in the same place.
	private void ReclaimUnloadingContainerForItem(object? spItem)
	{
		if (Children.UnloadingElements is not { Count: > 0 } unloadingElements)
		{
			return;
		}

		for (var i = 0; i < unloadingElements.Count; i++)
		{
			var spContainer = unloadingElements[i];
			if (!GetElementIsHeader(spContainer) && Equals(GetVirtualizationInformationFromElement(spContainer)!.Item, spItem))
			{
				m_unloadingElements.Remove(spContainer);
				CoreImports.UIElement_CancelTransition(spContainer);
				ReloadUnloadedElements();
				return;
			}
		}
	}
#endif

	// Associates the item to the container.
	internal void LinkContainerToItem(UIElement spContainer, object? spItem)
	{
		// todo: validate the old code: it cast pItem to a DO first. Why?

		// linking the item to the container
		SetItemForElement(spContainer, spItem);

		// todo: decide if we still want to set datacontext here
	}

	// Associates the group to the header.
	internal void LinkHeaderToGroup(UIElement spHeader, object? spItem)
	{
		// linking the item to the container
		SetItemForElement(spHeader, spItem);
	}

	// Clears the item from the container.
	internal object? UnlinkContainerFromItem(UIElement spContainer)
	{
		object? pspItem = null;

#if !HAS_UNO // TODO Uno: no peg, the container can't be collected by the XAML GC while we use it.
		// If we're part of a graph that got GC'ed, then don't worry about doing the following
		// cleanup. All of these objects will be removed from memory soon anyway.
		auto peggedContainer = ctl.try_make_autopeg(spContainer);

		if (peggedContainer)
#endif
		{
			VirtualizationInformation p_virtualizationInformation = GetVirtualizationInformationFromElement(spContainer)!;
			pspItem = p_virtualizationInformation.Item;
			p_virtualizationInformation.Item = null;
		}

		return pspItem;
	}

	// Clears the group from the header.
	internal object? UnlinkHeaderFromGroup(UIElement spHeader)
	{
		object? pspItem = null;

#if !HAS_UNO // TODO Uno: no peg, the header can't be collected by the XAML GC while we use it.
		// If we're part of a graph that got GC'ed, then don't worry about doing the following
		// cleanup. All of these objects will be removed from memory soon anyway.
		auto peggedHeader = ctl.try_make_autopeg(spHeader);

		if (peggedHeader)
#endif
		{
			VirtualizationInformation p_virtualizationInformation = GetVirtualizationInformationFromElement(spHeader)!;
			pspItem = p_virtualizationInformation.Item;
			p_virtualizationInformation.Item = null;
		}

		return pspItem;
	}

	// naming confusion: for long we have shipped with a Refresh that did the work of what I would consider
	// a reset. This has seeped into public api, and therefore I can't rectify that. So understand these definitions:
	// *******    Refresh:   destructive, time consuming. We run this too often, but it is the way it is.
	// *******    Reset:     maintain state as much as possible. We can't trust our concept of indices anymore and want to redo them
	internal void Refresh()
	{
		m_inCollectionChange = true;
		m_refreshPendingLayout = true;
		if (m_layoutInProgress)
		{
			TraceGuardFailure("RefreshDuringLayout");
		}

		// We should not be tracking anything at this stage.
		m_viewportBehavior.Reset();

		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		// Note: there is a bit of internal overlap between ICG2 and our panel...


		// since this is a refresh, we really want to get rid of _all_ state, which means we should
		// be left with no focused container and other pinned containers even. Let us make sure they
		// are all gone
		m_containerManager.ClearPinAndFocus();

		// Recycle everything to give them a chance to call ClearContainerForItemOverride
		// (if separated, this code will be under IICG2::Refresh()
		RecycleAllHeaders();
		RecycleAllContainers();

		m_recycleQueue.Clear();
		m_recycleHeaderQueue.Clear();

		// Now we clear out everything regarding our children collection
		{
			m_cacheManager.ResetGroupCache();
			m_containerManager.Refresh();
			Children.Clear();
		}

		ResetCacheBuffers();

		// also clear out any tracked element for orientation
		m_tpFirstVisibleElementBeforeOrientationChange = null;
		m_typeOfFirstVisibleElementBeforeOrientationChange = ElementType.ItemContainer;
		m_indexOfFirstVisibleElementBeforeOrientationChange = -1;


		// reset all the caches that we know for sure are going to be invalid now
		m_firstCacheIndexBase = -1;
		m_firstVisibleIndexBase = -1;
		m_lastVisibleIndexBase = -1;
		m_lastCacheIndexBase = -1;
		m_firstCacheGroupIndexBase = -1;
		m_firstVisibleGroupIndexBase = -1;
		m_lastVisibleGroupIndexBase = -1;
		m_lastCacheGroupIndexBase = -1;

		UpdateGroupStyle();

		// TODO: Decide which element we want to scroll to in the new view

		// We should be done with all collection change related work at this point.
		// The ChangeViewWithOptionalAnimation call below might, in turn, call UpdateLayout
		// if the viewport is in manipulation; and we don't want to trigger the layout
		// guard during layout.
		m_inCollectionChange = false;

		// We either don't have an element to target, or targeting an element is meaningless
		// because we don't yet have a valid window. Just go to 0,0 in the SV so the header is visible.
		if (spScrollViewer is not null)
		{
			// For now, we assume we aren't in a nested SV.
			spScrollViewer.ChangeViewWithOptionalAnimation(
				0.0,
				0.0,
				null /*zoom */,
				true /*disable animation */);
		}

		InvalidateMeasure();
	}

	// being called from ItemsControl when ItemCollection calls into it for a NotifyOfSourceChanged
	// this should only be called for items.
	internal void NotifyOfItemsChanging(IVectorChangedEventArgs e)
	{
		CollectionChange action;
		uint nIndex = 0;

		if (m_layoutInProgress)
		{
			TraceGuardFailure("ItemsChangingDuringLayout");
		}

		try
		{
			if (m_inCollectionChange)
			{
				// We are already in the middle of a collection change.
				// we are headed for a crash. Fail here preemptively
				throw new InvalidOperationException();
			}

			m_inCollectionChange = true;

			action = e.CollectionChange;

			m_transitionContextManager.NotifyOfItemsChanging(action);

			switch (action)
			{
				case CollectionChange.ItemInserted:
					nIndex = e.Index;
					OnItemAdded((int)nIndex);
					break;

				case CollectionChange.Reset:
					{
						if (m_cacheManager.IsGrouping() && m_cacheManager.GetTotalGroupCount() > 0 && !m_cacheManager.ShouldInterpretResetAsFullReset())
						{
							// can be that we are getting this reset but we have already refreshed the collection
							// because we've also had a NotifyOfGroupChanged with a reset

							// in case of grouping the reset event which is comming from CVS implementation has the group index
							// which needs to be used.
							// Note: In custom implementation of ICollectionView customer can decide to use WinRT Vector
							// WinRT vector fail if on Reset action we are reading index. We are workarounding this error by ignoring it.
							// Thus we still get benefit of using index of group where Reset happens provided by build-in implementation of our CVS.
							try
							{
								nIndex = e.Index;
							}
							catch (Exception ex) when (ex.HResult == unchecked((int)0x80004005) /* E_FAIL */)
							{
								nIndex = 0;
							}

							OnGroupReset((int)nIndex);
						}
						else
						{
							OnCollectionReset();
						}
						break;
					}
				case CollectionChange.ItemChanged:
				case CollectionChange.ItemRemoved:
					// we do not react to removed before others have processed the removal
					break;

				default:
					// Impossible to get here:
					throw new InvalidOperationException();
			}
		}
		finally
		{
			m_inCollectionChange = false;
		}
	}

	internal void NotifyOfItemsChanged(IVectorChangedEventArgs e)
	{
		CollectionChange action;
		uint nIndex = 0;

		if (m_layoutInProgress)
		{
			TraceGuardFailure("ItemsChangedDuringLayout");
		}

		try
		{
			if (m_inCollectionChange)
			{
				// We are already in the middle of a collection change.
				// we are headed for a crash. Fail here preemptively
				throw new InvalidOperationException();
			}

			m_inCollectionChange = true;

			action = e.CollectionChange;

			m_transitionContextManager.NotifyOfItemsChanged(action);

			switch (action)
			{
				case CollectionChange.ItemInserted:
				case CollectionChange.Reset:
					// we only respond to removed
					break;
				case CollectionChange.ItemRemoved:
					nIndex = e.Index;
					OnItemRemoved((int)nIndex);
					break;
				case CollectionChange.ItemChanged:
					nIndex = e.Index;
					OnDataElementReplaced(ElementType.ItemContainer, (int)nIndex);
					break;
				default:
					// Impossible to get here:
					throw new InvalidOperationException();
			}

			OnCollectionChangeProcessed();
		}
		finally
		{
			m_inCollectionChange = false;
		}
	}

	// The hookup here is strange. We follow the old ICG's pattern of hooking up ourselves
	// even though I think in the future it is better if IC just calls into us. For now, IC
	// does not get notifications of group changes and we subscribe ourselves in RegisterItemsHost.
	internal void NotifyOfGroupsChanged(
		IObservableVector<object> pSender,
		IVectorChangedEventArgs e)
	{
		CollectionChange action;
		uint nIndex = 0;

		if (m_layoutInProgress)
		{
			TraceGuardFailure("GroupsChangedDuringLayout");
		}

		try
		{
			if (m_inCollectionChange)
			{
				// We are already in the middle of a collection change.
				// we are headed for a crash. Fail here preemptively
				throw new InvalidOperationException();
			}

			m_inCollectionChange = true;

			action = e.CollectionChange;

			m_transitionContextManager.NotifyOfGroupsChanged(action);

			if (m_cacheManager.ShouldInterpretResetAsFullReset())
			{
				// cvs is rebuilding its groups and raising events, we need to ignore until we
				// get a item reset event
				return;
			}

			switch (action)
			{
				case CollectionChange.ItemInserted:
					nIndex = e.Index;
					OnGroupAdded((int)nIndex);
					break;

				case CollectionChange.ItemRemoved:
					nIndex = e.Index;
					OnGroupRemoved((int)nIndex);
					break;

				case CollectionChange.ItemChanged:
					nIndex = e.Index;
					OnDataElementReplaced(ElementType.GroupHeader, (int)nIndex);
					break;

				case CollectionChange.Reset:
					// we are getting a reset on the group collection. In CVS implementations, this is going to be
					// followed by a reset on the (flat) item collection. After discussing with the feature team
					// we have decided it is very hard to do a reset here and now and then somehow know how to interpret the
					// flat reset that is coming in.
					// However, the flat reset has no way of indicating the 'full-ness' of the reset, since the args.index is
					// specified as a uint, so they cannot do a -1. That reset also tends to be raised if individual groups have been
					// reset, in which case the args.index _does_ make sense. So, we're setting a boolean here, that we will clear in
					// OnItemCollectionChanged event handler.
					// This is not perfect, and will fall down if consumers do not implement grouping per guidelines. Tough luck, it is
					// impossible to change the eventing ordering at this point in the release.
					// We should follow up with a drastic amount of samples
					m_cacheManager.RegisterInterpretResetAsFullReset();
					break;

				default:
					// Impossible to get here:
					throw new InvalidOperationException();
			}

			OnCollectionChangeProcessed();
		}
		finally
		{
			m_inCollectionChange = false;
		}
	}

	// item added in the collection
	private void OnItemAdded(int nIndex)
	{
#if MCBP_DEBUG
		Trace($"Item added, index {nIndex}");
#endif

#if DEBUG
		try
#endif
		{
			// todo: fill in bug number. We need to remove this if condition when they fix their code.
			// 5/7/2015. VoiceRecorder app actually gives us the wrong indices when raising events. We do not want to RI
			// a change that will break them, but are working with them to fix their code. A very scoped change to understand
			// their scenario and regress to the old reset code path.
			if (m_cacheManager.GetTotalItemCount() < nIndex)
			{
#if MCBP_DEBUG
				Trace($"Going into regressed OnItemAdded path because index of add {nIndex} is bigger then total item count");
#endif
				m_cacheManager.ResetGroupCache();
			}
			else
			{
				// caches are no longer valid
				// if we are in 'group filling mode', the groupAdded event has already taken care of the correct
				// renewal of the cache
				if (m_containerManager.m_unlinkedFillers.Count == 0)
				{
					// so we only care if we are not filling out a groupAdd
					m_cacheManager.RenewCacheAfterMutation(nIndex, -1, -1, -1);
				}
			}

			{
				// sets up caches to make iterating over groups much faster, caches children collection, host, etc.
				using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of method

				// super special case: if we added a group, we might be in 'group filling mode'.
				// GroupedDataCollection gives us back inserts going backward, so we need to recognize this behavior and correct
				// our perceived index
				if (m_containerManager.m_unlinkedFillers.Count != 0)
				{
					if (m_containerManager.m_unlinkedFillers[m_containerManager.m_unlinkedFillers.Count - 1] == nIndex)
					{
						// yep, we can always expect that it starts from high indices to low
						nIndex = m_containerManager.m_unlinkedFillers[0];
						// and remove it
						m_containerManager.m_unlinkedFillers.RemoveAt(m_containerManager.m_unlinkedFillers.Count - 1);
					}
					else
					{
						// well, that means there should not be any unlinkedfillers
						if (m_containerManager.m_unlinkedFillers.Count != 0)
						{
							throw new InvalidOperationException();
						}
					}
				}

				BeginTrackingOnCollectionChange(false /* isGroupChange */, nIndex, CollectionChange.ItemInserted);

				// while indexes are still correct, let the pinned containers know
				// and update our mapping indices
				m_containerManager.OnItemAdded(nIndex);

				// should we insert a sentinel? Do so if the container would be added inside the valid range
				// If we only have a single valid container, there is no need for a sentinel, as all the mapping will be
				// accurate, as the new item lies outside the realized block. We only care about insertions *inside* the block
				if (m_containerManager.GetValidContainerCount() > 1 &&
					m_containerManager.GetItemIndexFromValidIndex(0) < nIndex &&
					nIndex <= m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount() - 1))
				{
					m_containerManager.InsertSentinelContainer(nIndex);
				}

				// If this insertion happened before our collection, we won't insert a sentinel, but we do need to shift the location
				// of our containers appropriately. Find the first container, and use its size to perform the shift
				// When tracking, we will shift the tracked element instead.
				if (m_containerManager.SizeOfContainerVisualSection() > 0 &&
					nIndex < m_containerManager.GetItemIndexFromValidIndex(0) &&
					!m_viewportBehavior.isTracking)
				{
					Size desiredSize;
					Rect newLocation;
					LayoutReference referenceInformation = CreateDefaultLayoutReference();

					UIElement spFirstContainer = m_containerManager.GetContainerAtValidIndex(0)!;
					if (!ElementHasVirtualizationInformation(spFirstContainer))
					{
						throw new InvalidOperationException();
					}

					desiredSize = spFirstContainer.DesiredSize;
					referenceInformation.ReferenceIsHeader = false;
					referenceInformation.RelativeLocation = ReferenceIdentity.BeforeMe;
					referenceInformation.ReferenceBounds = GetBoundsFromElement(spFirstContainer);

					newLocation = m_spLayoutStrategy.GetElementBounds(
						ElementType.ItemContainer,
						nIndex,
						desiredSize,
						referenceInformation,
						m_windowState.GetRealizationWindow());

					SetBoundsForElement(spFirstContainer, newLocation);
				}

				// always re-do measure even if outside of the valid window (for the extents)
				InvalidateMeasure();
			}
		}
#if DEBUG
		finally
		{
			m_debugIsInTemporaryInvalidState = false;
		}
#endif
	}

	// item removed in the collection
	private void OnItemRemoved(int nIndex, bool itemRemovedForGroupReset = false)
	{
#if MCBP_DEBUG
		Trace($"Item removed, index {nIndex}");
#endif

#if DEBUG
		try
#endif
		{
			// caches are no longer valid
			m_cacheManager.RenewCacheAfterMutation(-1, nIndex, -1, -1);

			{
				// sets up caches to make iterating over groups much faster, caches children collection, host, etc.
				using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of method

				BeginTrackingOnCollectionChange(false /* isGroupChange */, nIndex, CollectionChange.ItemRemoved);

				// get the container from the pinned collection
				UIElement? spPinnedContainer = m_containerManager.GetContainerFromPinnedContainers(nIndex);

				// while indexes are still correct, let the pinned containers know
				m_containerManager.OnItemRemoved(nIndex);

				if (m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(nIndex)))
				{
					bool isTransitioning = false;
					Microsoft.UI.Xaml.Media.Animation.TransitionTrigger trigger = Microsoft.UI.Xaml.Media.Animation.TransitionTrigger.Layout;
					int validIndex = m_containerManager.GetValidContainerIndexFromItemIndex(nIndex);
					UIElement? spOldContainer = m_containerManager.GetContainerAtValidIndex(validIndex);

#if DEBUG
					m_debugIsInTemporaryInvalidState = true;
#endif

					// recognize that EnsureRecycleContainer does more work: it will ensure the recycling of headers
					// before a group whose containers are partially virtualized.
					// However, we cannot call Ensure because its core functionality is different: it will try
					// and recycle if something is outside of bounds. (It will potentially recycle not the first container
					// (if there was a focused container in between), but that is not the case since we already removed
					// this container from the pinned containers etc.).

					// so removing a first or last container might leave us with a group that has more than 0 items, but no
					// valid containers. This can happen if this container is the last container of group at the front
					// or the first container of a group at the back. In that case we need to recycle the group as well to
					// ensure our contiguousness

					m_containerManager.RemoveFromValidContainers(validIndex, true /* isForItemRemoval */);
					if (spOldContainer is not null)
					{
						CoreImports.UIElement_GetHasTransition(spOldContainer, out isTransitioning, out trigger);
						if (!isTransitioning || trigger != Microsoft.UI.Xaml.Media.Animation.TransitionTrigger.Unload)
						{
							// unloading queue is needed only for callback from transition completion routine.
							FinishUnlinkingContainer(spOldContainer, false /* useUnloadingQueue */);
						}
						else
						{
							DeferredModernPanelUnlinkActionPayload spPayload = new();

							spPayload.InitializePayload(this);

							CoreImports.UIElement_SetDeferUnlinkContainer(spOldContainer, spPayload);
							m_unloadingElements.Add(spOldContainer);
						}
					}
#if DEBUG
					m_debugIsInTemporaryInvalidState = false;
#endif

					if (m_cacheManager.IsGrouping() && itemRemovedForGroupReset == false)
					{
						int indexOfGroup = -1;
						int indexInsideGroup = -1;
						int itemCountInGroup = -1;

						if (m_cacheManager.WasGrouping())
						{
							m_cacheManager.GetGroupHistoryInformationFromItemIndex(nIndex,
								out indexOfGroup,
								out indexInsideGroup,
								out itemCountInGroup);
						}

						if (validIndex == 0 || validIndex == m_containerManager.GetValidContainerCount())   // notice that since we removed, the current validContainerCount is already subtracted by 1
						{
							// notice how we are using information that was gathered before we reset the caches.
							// The item removed is the first (or) last item in the window. Note that we do not
							// do this for items that are in the middle of the window. So if there is a group in the middle
							// with hides if empty is true and its last item was removed, we do not recycle the header until
							// the next generate.

							if (validIndex == 0 && indexInsideGroup == itemCountInGroup - 1 && itemCountInGroup > 1
								&& m_containerManager.GetValidHeaderIndexFromGroupIndex(indexOfGroup) == 0)
							{
								// we have just removed the last container in a non-empty group
								// that means that we should have a header. Based on contiguous promises, that header
								// should be the first one
								// H - Header
								// C - Container
								// _ - Unrealized
								// X - Deleted
								// R - Header that needs to be recycled
								//  Window  H _ _ C H C C C
								//          R     X
								EnsureRecycleHeaderCandidate(true, true /* always recycle */);
							}

							if (validIndex != 0 && indexInsideGroup == 0 && itemCountInGroup > 1
								&& m_containerManager.GetValidHeaderIndexFromGroupIndex(indexOfGroup) == m_containerManager.GetValidHeaderCount() - 1)
							{
								// we have just removed the first container of a non-empty group
								// Window   H C C C H C _ _ _
								//                  R X
								EnsureRecycleHeaderCandidate(false, true /* always recycle */);
							}

							if (m_cacheManager.GetHidesIfEmpty() && itemCountInGroup == 1)
							{
								// hidesIfEmpty is true and
								// we have removed the only item in the group
								// pre-emptively recycle the header as well
								bool fromFront = validIndex == 0;
								EnsureRecycleHeaderCandidate(fromFront, true /* always recycle */);
							}
						}
					}
				}
				else
				{
					// we removed something in front of the valid range
					if (nIndex < m_containerManager.GetItemIndexFromValidIndex(0))
					{
						m_containerManager.UpdateItemIndexForFirstValidContainer(m_containerManager.GetItemIndexFromValidIndex(0) - 1);
					}

					if (spPinnedContainer is not null)
					{
						// we removed a pinned container that is not in the valid range
						RecycleContainer(spPinnedContainer);
					}
				}

				if (nIndex <= m_containerManager.GetItemIndexFromValidIndex(0) && m_containerManager.GetValidContainerCount() > 0)
				{
					int indexInGroup = -1;

					if (m_cacheManager.IsGrouping())
					{
						m_cacheManager.GetGroupInformationFromItemIndex(
							m_containerManager.GetItemIndexFromValidIndex(0),
							out _,
							out indexInGroup,
							out _);
					}

					// the first container is potentially located incorrectly
					// redo its layout based on himself!
					// you would think we can just take the removed containers location, but we also need to
					// redo work if there was no old container, so this container is really all we can use.
					// When tracking, we will shift the tracked element instead.

					// ungrouped logic:
					// take the (potentially new) first containers location and act as though it is the location
					// of the next container, then 'generateContainer' for this new first container.

					// grouped logic: there is always going to be a header before the item (on which to base forward positions on
					// during measure), so we only care about correcting if we are not at groupInIndex 0

					if ((!m_cacheManager.IsGrouping() || indexInGroup > 0) && !m_viewportBehavior.isTracking)
					{
						LayoutReference referenceInformation = CreateDefaultLayoutReference();
						Size containerDesiredSize;
						Rect newLocation;

						UIElement spFirstContainer = m_containerManager.GetContainerAtValidIndex(0)!;
						if (!ElementHasVirtualizationInformation(spFirstContainer))
						{
							throw new InvalidOperationException();
						}

						containerDesiredSize = spFirstContainer.DesiredSize;

						referenceInformation.ReferenceIsHeader = false;
						referenceInformation.RelativeLocation = ReferenceIdentity.AfterMe;
						referenceInformation.ReferenceBounds = GetBoundsFromElement(spFirstContainer);

						newLocation = m_spLayoutStrategy.GetElementBounds(
							ElementType.ItemContainer,
							m_containerManager.GetItemIndexFromValidIndex(0),
							containerDesiredSize,
							referenceInformation,
							m_windowState.GetRealizationWindow());

						SetBoundsForElement(spFirstContainer, newLocation);
					}

				}

				// always re-do measure even if outside of the valid window (for the extents)
				InvalidateMeasure();
			}
		}
#if DEBUG
		finally
		{
			m_debugIsInTemporaryInvalidState = false;
		}
#endif
	}

	// Group added to the collection.
	private void OnGroupAdded(int nIndex)
	{
#if MCBP_DEBUG
		Trace($"Group added, index {nIndex}");
#endif

#if DEBUG
		try
#endif
		{
			// caches are no longer valid
			m_cacheManager.RenewCacheAfterMutation(-1, -1, nIndex, -1);

			{
				// sets up caches to make iterating over groups much faster, caches children collection, host, etc.
				using var strongCache = m_cacheManager.CacheStrongRefs();

				BeginTrackingOnCollectionChange(true /* isGroupChange */, nIndex, CollectionChange.ItemInserted);

				// while indexes are still correct, let the pinned containers know
				m_containerManager.OnGroupAdded(nIndex);

				// should we insert a sentinel? Do so if the header would be added inside the valid range
				// If we only have a single valid header, there is no need for a sentinel, as all the mapping will be
				// accurate, as the new item lies outside the realized block. We only care about insertions *inside* the block
				if (m_containerManager.GetValidHeaderCount() > 1 &&
					m_containerManager.GetGroupIndexFromValidIndex(0) < nIndex &&
					nIndex <= m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1))
				{
					m_containerManager.InsertSentinelHeader(nIndex);
				}

				// If this insertion happened before our collection, we won't insert a sentinel, but we do need to shift the location
				// of our headers appropriately. Find the first header, and use its size to perform the shift
				// When tracking, we will shift the tracked element instead.
				if (m_containerManager.SizeOfHeaderVisualSection() > 0 &&
					nIndex < m_containerManager.GetGroupIndexFromValidIndex(0) &&
					!m_viewportBehavior.isTracking)
				{
					Size desiredSize;
					Rect newLocation;
					LayoutReference referenceInformation = CreateDefaultLayoutReference();

					UIElement spFirstHeader = m_containerManager.GetHeaderAtValidIndex(0)!;
					if (!ElementHasVirtualizationInformation(spFirstHeader))
					{
						throw new InvalidOperationException();
					}

					desiredSize = spFirstHeader.DesiredSize;
					referenceInformation.ReferenceIsHeader = true;
					referenceInformation.RelativeLocation = ReferenceIdentity.BeforeMe;
					referenceInformation.ReferenceBounds = GetBoundsFromElement(spFirstHeader);

					int layoutIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, nIndex);
					newLocation = m_spLayoutStrategy.GetElementBounds(
						ElementType.GroupHeader,
						layoutIndex,
						desiredSize,
						referenceInformation,
						m_windowState.GetRealizationWindow());

					SetBoundsForElement(spFirstHeader, newLocation);
				}

				// always re-do measure even if outside of the valid window (for the extents)
				InvalidateMeasure();
			}

			CreateUnlinkedFillersForGroup(nIndex);
		}
#if DEBUG
		finally
		{
			m_debugIsInTemporaryInvalidState = false;
		}
#endif
	}

	// Group removed in the collection
	private void OnGroupRemoved(int nIndex)
	{
#if MCBP_DEBUG
		Trace($"Group Removed, index {nIndex}");
#endif

#if DEBUG
		try
#endif
		{
			// caches are no longer valid
			m_cacheManager.RenewCacheAfterMutation(-1, -1, -1, nIndex);

			{
				// sets up caches to make iterating over groups much faster, caches children collection, host, etc.
				using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of method

				BeginTrackingOnCollectionChange(true /* isGroupChange */, nIndex, CollectionChange.ItemRemoved);

				// get the container from the pinned collection
				UIElement? spPinnedHeader = m_containerManager.GetHeaderFromPinnedHeaders(nIndex);

				// while indexes are still correct, let the pinned containers know
				m_containerManager.OnGroupRemoved(nIndex);

				if (m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(nIndex)))
				{
					int validIndex = m_containerManager.GetValidHeaderIndexFromGroupIndex(nIndex);

#if DEBUG
					m_debugIsInTemporaryInvalidState = true;
#endif

					UIElement? spOldHeader = m_containerManager.GetHeaderAtValidIndex(validIndex);

					m_containerManager.RemoveFromValidHeaders(validIndex, true /* isForGroupRemoval */);

					if (spOldHeader is not null)
					{
						RecycleHeader(spOldHeader);
					}

#if DEBUG
					m_debugIsInTemporaryInvalidState = false;
#endif
				}
				else
				{
					if (spPinnedHeader is not null)
					{
						// we removed a pinned container that is not in the valid range
						RecycleHeader(spPinnedHeader);
					}

					// we removed something in front of the valid range
					if (nIndex < m_containerManager.GetGroupIndexFromValidIndex(0))
					{
						m_containerManager.UpdateGroupIndexForFirstValidHeader(m_containerManager.GetGroupIndexFromValidIndex(0) - 1);
					}
				}


				if (nIndex <= m_containerManager.GetGroupIndexFromValidIndex(0) && m_containerManager.GetValidHeaderCount() > 0)
				{
					// When tracking, we will shift the tracked element instead.
					if (!m_viewportBehavior.isTracking)
					{
						// the first container is potentially located incorrectly
						// redo its layout based on himself!
						// you would think we can just take the removed containers location, but we also need to
						// redo work if there was no old container, so this container is really all we have
						// to use.
						LayoutReference referenceInformation = CreateDefaultLayoutReference();
						VirtualizationInformation p_virtualizationInformation;
						Size desiredSize;
						Rect newLocation;

						// we'll use the first container as a reference
						UIElement spFirstHeader = m_containerManager.GetHeaderAtValidIndex(0)!;
						p_virtualizationInformation = GetVirtualizationInformationFromElement(spFirstHeader)!;
						desiredSize = spFirstHeader.DesiredSize;

						referenceInformation.ReferenceIsHeader = true;
						referenceInformation.RelativeLocation = ReferenceIdentity.AfterMe;
						referenceInformation.ReferenceBounds = p_virtualizationInformation.Bounds;

						int layoutIndex = m_cacheManager.DataIndexToLayoutIndex(
							ElementType.GroupHeader,
							m_containerManager.GetGroupIndexFromValidIndex(0));

						newLocation = m_spLayoutStrategy.GetElementBounds(
							ElementType.GroupHeader,
							layoutIndex,
							desiredSize,
							referenceInformation,
							m_windowState.GetRealizationWindow());

						p_virtualizationInformation.Bounds = newLocation;
					}
				}

				// always re-do measure even if outside of the valid window (for the extents)
				InvalidateMeasure();
			}
		}
#if DEBUG
		finally
		{
			m_debugIsInTemporaryInvalidState = false;
		}
#endif
	}

	// Group reset in the collection
	private void OnGroupReset(int groupIndex)
	{
		int startItemIndex = 0;
		int itemCountInGroup = 0;

#if MCBP_DEBUG
		Trace($"Group reset, index {groupIndex}");
#endif

		m_containerManager.SetIndexOfGroupBeingReset(groupIndex);

		try
		{
			m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out startItemIndex, out itemCountInGroup);

			for (int i = 0; i < itemCountInGroup; ++i)
			{
				OnItemRemoved(startItemIndex, true /* itemRemovedForGroupReset */);
			}

			// Ensures that the group header at the end of the valid range gets recycled if it's the header
			// for the current group (i.e. groupIndex).
			if (m_cacheManager.GetHidesIfEmpty() &&
				m_containerManager.GetValidHeaderCount() > 0)
			{
				if (m_containerManager.GetGroupIndexFromValidIndex(0) == groupIndex)
				{
					EnsureRecycleHeaderCandidate(true /* fromFront */, true /* alwaysRecycle */);
				}
				else if (m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1) == groupIndex)
				{
					EnsureRecycleHeaderCandidate(false /* fromFront */, true /* alwaysRecycle */);
				}
			}

			// Group resets don't generate a set of item additions, so we need to clear out any fillers
			m_containerManager.m_unlinkedFillers.Clear();

			// Reset the group cache so that the cache manager is aware
			// of the new items in the group that just got reset.
			m_cacheManager.ResetGroupCache();

			{
				using var strongCache = m_cacheManager.CacheStrongRefs();

				m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out _, out int newItemCountInGroup);

				for (int i = 0; i < newItemCountInGroup; ++i)
				{
					OnItemAdded(startItemIndex);
				}

				InvalidateMeasure();
			}
		}
		finally
		{
			m_containerManager.InvalidateIndexOfGroupBeingReset();
		}
	}

	private void OnDataElementReplaced(ElementType type, int index)
	{
		UIElement? spOldElement;

		// the cache only actually changes when a group has been replaced.
		// tell the cache manager that the groups have really changed
		if (type == ElementType.GroupHeader)
		{
			m_cacheManager.ResetGroupCache();
		}

		{
			// Use the interface mapping method, as it will also retrieve pinned elements
			DependencyObject? spOldElementAsDO = m_containerManager.Interface_ElementFromDataIndex(type, index);
			spOldElement = spOldElementAsDO as UIElement;
		}

		bool wasFocused = m_containerManager.GetIsIndexFocused(type, index);
		bool wasPinned = m_containerManager.GetIsIndexPinned(type, index);

		if (wasFocused)
		{
			m_containerManager.ResetFocusedContainer(true);
		}
		if (wasPinned)
		{
			m_containerManager.UnregisterPinnedElement(type, index);
		}

		if (spOldElement is not null || wasFocused || wasPinned)
		{
			// If we want animations to run, we can't just throw new content into the UI element
			// So, we remove/recycle the old one, and generate a new replacement
			// If this element was realized, let's get the old bounds and generate a new element at the same location
			UIElement? spNewElement = null;

			using var strongCache = m_cacheManager.CacheStrongRefs();
			BeginTrackingOnCollectionChange(type == ElementType.GroupHeader /* isGroupChange */, index, CollectionChange.ItemChanged);

			Rect oldElementBounds = GetBoundsFromElement(spOldElement!);

			switch (type)
			{
				case ElementType.GroupHeader:
					{
						spNewElement = GenerateHeaderAtGroupIndex(index);
					}
					break;

				case ElementType.ItemContainer:
					{
						spNewElement = GenerateContainerAtIndex(index);
					}
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false, "Invalid type!");
					break;
			}

			// This will update the replaced element in both the realized range and the pinned collection
			m_containerManager.OnDataItemReplaced(type, index, spNewElement!);

			SetBoundsForElement(spNewElement!, oldElementBounds);

			switch (type)
			{
				case ElementType.GroupHeader:
					{
						Size measureSize;
						RecycleHeader(spOldElement!);
						measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, index, m_windowState.GetRealizationWindow());
						PrepareHeaderViaItemsHost(index, measureSize, spNewElement!);
					}

					// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
					if (m_bUseStickyHeaders)
					{
						// The best positioning we can do here is the replaced header (it will be updated later if incorrect)
						ConfigureStickyHeader(spNewElement!, index, GetBoundsFromElement(spOldElement!));
					}
					break;

				case ElementType.ItemContainer:
					{
						Size measureSize;
						RecycleContainer(spOldElement!);
						measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, index, m_windowState.GetRealizationWindow());
						PrepareContainerViaItemsHost(index, measureSize, spNewElement!);
					}
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false, "Invalid type!");
					break;
			}

			// Reregister the focus/pin state
			if (wasFocused)
			{
				m_containerManager.SetFocusedContainer(index, spNewElement);
			}
			if (wasPinned)
			{
				// Element could already be pinned by PrepareViaItemsHost, so check again
				if (!m_containerManager.GetIsIndexPinned(type, index))
				{
					m_containerManager.RegisterPinnedElement(type, index, spNewElement!);
				}
			}
		}

		if (type == ElementType.GroupHeader)
		{
			using var strongCache = m_cacheManager.CacheStrongRefs();
			CreateUnlinkedFillersForGroup(index);
		}

		InvalidateMeasure();
	}

	// naming confusion: for long we have shipped with a Refresh that did the work of what I would consider
	// a reset. This has seeped into public api, and therefore I can't rectify that. So understand these definitions:
	// *******    Refresh:   destructive, time consuming
	// *******    Reset:     maintain state as much as possible. We can't trust our concept of indices anymore and want to redo them
	private void OnCollectionReset()
	{
		// we shall call this method from OnItemsReset

		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
		int containerIndexToStartGeneratingFrom = -1;
		int headerIndexToStartGeneratingFrom = -1;
		double offsetFromTop = 0.0;

		m_inCollectionChange = true;
		try
		{
			if (m_layoutInProgress)
			{
				TraceGuardFailure("ResetDuringLayout");
			}

			// logic during refresh:
			// 1. store items from pinnedcontainers and first visible container
			// 2. iterate over them and ask for new indices
			// 3. recycle elements (raising CCC) including pinnedcontainers, but do not clear out visual tree
			// 4. reset our caches
			// 5. start generation, but make sure we use the correct containers for the correct indices


			// we want to keep track of pinned containers, focus container and first visible container

			// re-evaluate

			// no longer trust this
			m_cacheManager.ResetGroupCache();

			if (IsItemsHostRegistered())
			{
				using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of block

				// getting to our first visible container

				// if we ever get a a new elementtype, we probably need to update this code
				global::System.Diagnostics.Debug.Assert(ElementType_Count == 2);
				for (int typeindex = 0; typeindex < ElementType_Count && containerIndexToStartGeneratingFrom == -1; ++typeindex)
				{
					ElementType elementType = (ElementType)typeindex;

					Rect adjustedWindowForStickyHeaders = m_windowState.GetVisibleWindow();
					if (elementType == ElementType.ItemContainer && m_bUseStickyHeaders)
					{
						adjustedWindowForStickyHeaders.Y += m_lastVisibleWindowClippingHeight;
						adjustedWindowForStickyHeaders.Height -= m_lastVisibleWindowClippingHeight;
					}

					// we also care about the first visible container or (if no containers available) header
					// ordering is always correct (since we are sorted by dataindex and our layouts are supposed to be in order)
					// this means that the first container we find is the only one we care about
					for (int validIndex = 0; validIndex < m_containerManager.GetValidElementCount(elementType); ++validIndex)
					{
						Rect bounds = default;

						UIElement? spElement = m_containerManager.GetElementAtValidIndex(elementType, validIndex);

						// Ignores any sentinel.
						if (GetElementIsSentinel(spElement))
						{
							continue;
						}

						bounds = GetBoundsFromElement(spElement!);
						// We don't want elements to overlap over one line.

						SetSizeFromRectInVirtualizingDirection(ref bounds, SizeFromRectInVirtualizingDirection(bounds) - EdgeOverlayDisambiguationDelta);

						if (!RectUtil.AreDisjoint(bounds, adjustedWindowForStickyHeaders))
						{
							bool isGroupHeader = false;
							bool found = false;
							uint newItemIndex = 0;

							object? spDataItem = m_containerManager.Interface_DataItemFromElement(elementType, spElement!);

							m_cacheManager.GetIndex(spDataItem, out newItemIndex, out isGroupHeader, out found);
							if (found)
							{
								// we can only do the scroll if we find a new index. If this did not happen, we're out of luck
								if (isGroupHeader)
								{
									headerIndexToStartGeneratingFrom = (int)newItemIndex;
								}
								else
								{
									containerIndexToStartGeneratingFrom = (int)newItemIndex;
								}
								offsetFromTop = PointFromRectInVirtualizingDirection(adjustedWindowForStickyHeaders) - PointFromRectInVirtualizingDirection(bounds);
								break;
							}
						}
					}
				}

				// We should not be tracking anything at this stage. Note that
				// we need to reset the tracked element before recycling all the containers.
				m_viewportBehavior.Reset();

				// great. we have scribbled away the first visible container index
				// now recycle all, note how this will conveniently save the focused container for us during recycling so the
				// ordering is important here (this comes before m_containerManager.Reset)
				RecycleAllContainersAndHeaders();

				// tell containerManager to reset. This will tell him to update all the indices of both the focused and the pinned elements
				m_containerManager.Reset();

				// also clear out any tracked element for orientation
				m_tpFirstVisibleElementBeforeOrientationChange = null;
				m_typeOfFirstVisibleElementBeforeOrientationChange = ElementType.ItemContainer;
				m_indexOfFirstVisibleElementBeforeOrientationChange = -1;


				// let's start with the right container
				if (spScrollViewer is not null)
				{

					if (containerIndexToStartGeneratingFrom > -1)
					{
						ScrollItemIntoView(
							(uint)containerIndexToStartGeneratingFrom,
							ScrollIntoViewAlignment.Leading,
							offsetFromTop,
							false /* forceSynchronous */);
					}
					else if (headerIndexToStartGeneratingFrom > -1)
					{
						ScrollGroupHeaderIntoView(
							(uint)headerIndexToStartGeneratingFrom,
							ScrollIntoViewAlignment.Leading,
							offsetFromTop,
							false /* forceSynchronous */);
					}
					else
					{
						// For now, we assume we aren't in a nested SV.
						spScrollViewer.ChangeViewWithOptionalAnimation(
							0.0,
							0.0,
							null /*zoom */,
							true /*disable animation */);
					}
				}

				UpdateGroupStyle();
			}

			InvalidateMeasure();
		}
		finally
		{
			m_inCollectionChange = false;
		}
	}

	// When groups are inserted, we get subsequent item insertions, but they arrive in reverse order!
	// This could be ugly if we don't correct for it.
	// For instance, if we are looking at group 0, items 0-10, and a new group comes in with 10 items,
	// we'll see a group header come in, but then we'll see an insertion at index 10, which will look to us
	// like we're inserting something right before the old index 10. Things will get messed up.
	private void CreateUnlinkedFillersForGroup(int nIndex)
	{
		int startItemIndex = -1;
		int countInGroup = -1;        // fill them up again

		// get the groupsize of the group just inserted, this allows us to insert dummies
		m_cacheManager.GetGroupInformationFromGroupIndex(nIndex, out startItemIndex, out countInGroup);

		m_containerManager.m_unlinkedFillers.Clear();
		m_containerManager.m_unlinkedFillers.Capacity = Math.Max(m_containerManager.m_unlinkedFillers.Capacity, countInGroup);

		while (countInGroup > 0)
		{
			m_containerManager.m_unlinkedFillers.Add(startItemIndex);
			++startItemIndex;
			--countInGroup;
		}
	}

	internal partial class DeferredModernPanelUnlinkActionPayload
	{
		internal void InitializePayload(ModernCollectionBasePanel pOwner) => m_tpOwner = pOwner;

		internal override void Execute(UIElement pContainer)
			// unloading queue is needed only for callback from transition completion routine.
			=> m_tpOwner!.FinishUnlinkingContainer(pContainer, true /* useUnloadingQueue */);
	}

	// Callback for finishing unlinking from DeferredModernPanelUnlinkActionPayload when layout transition is complete.
	// Transition completion routine should use unloaded queue to avoid adding elements to the visual tree after the Layout completes.
	internal void FinishUnlinkingContainer(UIElement pContainer, bool useUnloadingQueue)
	{
		RecycleContainer(pContainer);

		// only add back to the tree if we actually put this guy into the recycle queue

		{
			// use the implicit knowledge that we will have pushed to the back
			bool found = false;
			for (int i = m_recycleQueue.Count - 1; i >= 0; --i)
			{
				UIElement? spContainer = ResolveWeakElement(m_recycleQueue[i]);
				if (spContainer == pContainer)
				{
					found = true;
					break;
				}
			}

			if (found)
			{
				if (useUnloadingQueue)
				{
					m_tpUnloadedElements.Add(pContainer);
				}
				else
				{
					var spChildren = m_cacheManager.GetChildren();
					spChildren.Add(pContainer);
				}
			}
		}
	}

	internal bool FindRecyclingCandidate(int index)
	{
		bool hasMatchingCandidate = false;

		// If queue is empty, no need to go further
		if (m_recycleQueue.Count > 0)
		{
			IContainerRecyclingContext? spRecyclingContext = m_cacheManager.GetContainerRecyclingContext((uint)index);

			if (spRecyclingContext is null)
			{
				// if a candidate was previously rejected (suggested in the ChoosingItemContainer event but
				// not used by the app) we should not propose it again. In those cases they just stay alive in the
				// recycle queue. This means we will try to recycle another container. That is great and we will make
				// sure that that is the one that is suggested to the app. However, if we cannot find another candidate to
				// recycle, since the queue is not empty, we will just suggest a container from the queue and that is
				// perfect.
				hasMatchingCandidate = false;

				for (int i = m_recycleQueue.Count - 1; i >= 0; --i)
				{
					bool denied = false;
					UIElement? spContainer = ResolveWeakElement(m_recycleQueue[i]);
					if (spContainer is not null)
					{
						var p_virtualizationInformation = GetVirtualizationInformationFromElement(spContainer);
						denied = p_virtualizationInformation!.WasRejectedAsAContainerByApp;

					}

					if (!denied)
					{
						hasMatchingCandidate = true;
						break;
					}
				}
			}
			else
			{
				for (int i = m_recycleQueue.Count - 1; i >= 0; --i)
				{
					bool compatible = false;
					UIElement? spContainer = ResolveWeakElement(m_recycleQueue[i]);
					if (spContainer is not null)
					{
						compatible = spRecyclingContext.IsCompatible(spContainer);
					}

					if (compatible)
					{
						spRecyclingContext.SelectedContainer = spContainer;
						hasMatchingCandidate = true;
						break;
					}
				}
			}
		}

		return hasMatchingCandidate;
	}

	internal uint GetQueueLength() => (uint)m_recycleQueue.Count;

	internal void RemoveFromQueue(UIElement pContainer, bool expectContainerToBePresent = true)
	{
		bool found = false;
		if (m_recycleQueue.Count > 0)
		{
			for (int i = m_recycleQueue.Count - 1; i >= 0; --i)
			{
				if (ResolveWeakElement(m_recycleQueue[i]) == pContainer)
				{
					// Found it
					m_recycleQueue.RemoveAt(i);
					found = true;
					break;
				}
			}
		}

		global::System.Diagnostics.Debug.Assert(!expectContainerToBePresent || found);
	}

	internal bool RemoveFromHeaderRecycleQueue(UIElement pContainer)
	{
		bool wasInQueue = false;
		if (m_recycleHeaderQueue.Count > 0)
		{
			for (int i = m_recycleHeaderQueue.Count - 1; i >= 0; --i)
			{
				if (ResolveWeakElement(m_recycleHeaderQueue[i]) == pContainer)
				{
					// Found it
					m_recycleHeaderQueue.RemoveAt(i);
					wasInQueue = true;
					break;
				}
			}
		}

		return wasInQueue;
	}

	private static UIElement? ResolveWeakElement(WeakReference<UIElement>? weakRef)
		=> weakRef is not null && weakRef.TryGetTarget(out var element) ? element : null;
}
