// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ModernCollectionBasePanel_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//#define MCBP_DEBUG

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DirectUI;
using Uno.Disposables;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public abstract partial class ModernCollectionBasePanel
{
	private protected ModernCollectionBasePanel()
	{
		m_cacheManager = new CacheManager(this);
		m_containerManager = new ContainerManager(this);
		m_transitionContextManager = new TransitionContextManager(this);
		m_estimatedSize = default;
		m_elementCountAtLastMeasure = 0;
		m_groupHeaderPlacement = GroupHeaderPlacement.Top;
		m_originFromItemsPresenter = default;
		m_bUseStickyHeaders = false;
		m_bLastItemRealized = false;
		m_currentHeaderHeight = 0.0;
		m_lastVisibleWindowClippingHeight = 0.0f;
		m_cachedPannableExtent = 0.0;
		m_inCollectionChange = false;
		m_layoutInProgress = false;
		m_refreshPendingLayout = true;

#if DEBUG
		m_debugIsInTemporaryInvalidState = false;
#endif

		m_firstCacheIndexBase = -1;
		m_firstVisibleIndexBase = -1;
		m_lastVisibleIndexBase = -1;
		m_lastCacheIndexBase = -1;
		m_firstCacheGroupIndexBase = -1;
		m_firstVisibleGroupIndexBase = -1;
		m_lastVisibleGroupIndexBase = -1;
		m_lastCacheGroupIndexBase = -1;
		m_typeOfFirstVisibleElementBeforeOrientationChange = ElementType.ItemContainer;
		m_indexOfFirstVisibleElementBeforeOrientationChange = -1;
		m_containerRequestedCount = 0;
		m_containerCreatedCount = 0;

		m_cacheLength = GetDefaultCacheLength();

		Initialize();
	}

#if !HAS_UNO // TODO Uno: no finalizer, DisconnectItemsHost unhooks the items source and the group style handler holds the panel weakly.
	~ModernCollectionBasePanel()
	{
		// Trace telemetry for container recycling effeciency metrics
		TraceLoggingWrite(
			g_hTraceProvider,
			"ContainerRecyclingLifetimeStats",
			TraceLoggingValue(m_containerRequestedCount, "ContainerRequestedCount"),
			TraceLoggingValue(m_containerCreatedCount, "ContainerCreatedCount"),
			TraceLoggingLevel(WINEVENT_LEVEL_LOG_ALWAYS),
			TelemetryPrivacyDataTag(PDT_ProductAndServicePerformance),
			TraceLoggingKeyword(MICROSOFT_KEYWORD_TELEMETRY));

		m_observableItemsSourceChangedToken.Disposable = null;

		m_epGroupStyleChangedHandler.Disposable = null;

		((LayoutDataInfoProvider)m_spLayoutDataInfoProvider).Uninitialize();
	}
#endif

	[MemberNotNull(nameof(m_icg2), nameof(m_tpUnloadedElements), nameof(m_unloadingElements), nameof(m_spLayoutDataInfoProvider))]
	private void Initialize()
	{
		m_icg2 = this;

		// Bind event handlers to these methods, using a pass-through functor
		Unloaded += OnPanelUnloaded;
		m_epUnloadedHandler.Disposable = Disposable.Create(() => Unloaded -= OnPanelUnloaded);

		{
			m_tpUnloadedElements = new TrackerCollection<UIElement>();
			m_unloadingElements = new TrackerCollection<UIElement>();
		}

		// Provides information about the data to the layout strategy.
		{
			LayoutDataInfoProvider spLayoutDataInfoProvider = new();
			spLayoutDataInfoProvider.Initialize(this);
			m_spLayoutDataInfoProvider = spLayoutDataInfoProvider;
		}
	}

	// Gets the default cache length
	internal static double GetDefaultCacheLength() => WindowsDefaultCacheLength;

	private protected void SetLayoutStrategyBase(ILayoutStrategy spLayoutStrategy)
	{
		m_spLayoutStrategy = spLayoutStrategy;
		m_spLayoutStrategy.SetLayoutDataInfoProvider(m_spLayoutDataInfoProvider);
	}

	internal ILayoutStrategy GetLayoutStrategy() => m_spLayoutStrategy;

	internal void RegisterItemsHost(IGeneratorHost pHost)
	{
		IObservableVector<object>? spCollectionGroups = null;

		IGeneratorHost spHost = pHost;

#if MCBP_DEBUG
		global::System.Diagnostics.Debug.WriteLine("Registering itemshost");
#endif

		// register with the cachemanager for speeding up lookups
		m_cacheManager.RegisterItemsHost(spHost);

		var spView = spHost.View;

		if (IncrementalLoadingAdapter.SupportsIncrementalLoading(spView))
		{
			m_transitionContextManager.RegisterForIncrementalLoading();
		}

		// is this a grouped collection by any chance?
		var spCollectionView = spHost.CollectionView;

		if (m_tpObservableItemsSource is not null)
		{
			m_observableItemsSourceChangedToken.Disposable = null;
		}

		if (spCollectionView is not null)
		{
			spCollectionGroups = spCollectionView.CollectionGroups;
			if (spCollectionGroups is not null)
			{
				VectorChangedEventHandler<object> spItemCollectionVectorChangedHandler = NotifyOfGroupsChanged;

#if MCBP_DEBUG
				global::System.Diagnostics.Debug.WriteLine("MCBP: Hooked up to the GroupChanged events");
#endif

				m_tpObservableItemsSource = spCollectionGroups;

				spCollectionGroups.VectorChanged += spItemCollectionVectorChangedHandler;
				m_observableItemsSourceChangedToken.Disposable = Disposable.Create(() => spCollectionGroups.VectorChanged -= spItemCollectionVectorChangedHandler);
			}
		}

		UpdateGroupStyle();

		// it's time to get some caching setup and letting our strategy know about grouping
		{
			// we might have switched sources. discard our cache and rebuild from scratch.
			m_cacheManager.ResetGroupCache();

			// re-evaluate
			using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of block

			// let our strategy know what is going on
			ReevaluateGroupHeaderStrategy();
		}

		InvalidateMeasure();
	}

	// Pick up a new group style from the items host and update its event handlers
	private void UpdateGroupStyle()
	{
		GroupStyle? spGroupStyle = null;

		var spItemsHost = m_cacheManager.GetItemsHost();
		if (spItemsHost is not null)
		{
			bool hidesIfEmpty = false;

			spGroupStyle = spItemsHost.GetGroupStyle(null, 0);
			if (spGroupStyle is not null)
			{
				hidesIfEmpty = spGroupStyle.HidesIfEmpty;
			}
			m_cacheManager.PutHidesIfEmpty(hidesIfEmpty);
		}

		if (m_tpGroupStyle != spGroupStyle)
		{
			m_epGroupStyleChangedHandler.Disposable = null;

			m_tpGroupStyle = spGroupStyle;

			if (m_tpGroupStyle is not null)
			{
				// TODO Uno: WinUI's GroupStyle is a core DependencyObject; here it raises INotifyPropertyChanged.
				// The weak handler stops an outliving GroupStyle from pinning a retemplated panel.
				var groupStyle = m_tpGroupStyle;
				WeakReference<ModernCollectionBasePanel> wrThis = new(this);
				global::System.ComponentModel.PropertyChangedEventHandler handler = (s, e) =>
				{
					if (wrThis.TryGetTarget(out var panel))
					{
						panel.OnGroupStylePropertyChanged(s, e);
					}
				};
				groupStyle.PropertyChanged += handler;
				m_epGroupStyleChangedHandler.Disposable = Disposable.Create(() => groupStyle.PropertyChanged -= handler);
			}

			// We'll reset the caches, because if the group style has gone from null to valid, or vice versa, we need to start/stop
			// displaying group headers
			if ((spGroupStyle is null && m_tpGroupStyle is not null) || (spGroupStyle is not null && m_tpGroupStyle is null))
			{
				m_cacheManager.ResetGroupCache();
			}
			InvalidateMeasure();
		}
	}

	private void OnGroupStyleChanged(string? propertyName)
	{
		if (propertyName == nameof(GroupStyle.HidesIfEmpty))
		{
			// All we need here is to update the flag and invalidate if the state has changed
			bool hidesIfEmpty = false;
			if (m_tpGroupStyle is not null)
			{
				hidesIfEmpty = m_tpGroupStyle.HidesIfEmpty;
			}

			if (m_cacheManager.GetHidesIfEmpty() != hidesIfEmpty)
			{
				InvalidateMeasure();
			}

			m_cacheManager.PutHidesIfEmpty(hidesIfEmpty);
		}
		else
		{
			// If any other property has changed, we should start over from scratch
			Refresh();
		}
	}

	internal void DisconnectItemsHost()
	{
		var spObservableItemsSource = m_tpObservableItemsSource;
		if (spObservableItemsSource is not null)
		{
			m_observableItemsSourceChangedToken.Disposable = null;
		}

		// We call Refresh() here in particular for the case where Hub is retemplated to use a new panel.
		// We depend on Refresh() to unparent UIElement children, so they can be added to the new panel.
		// Refresh() cleans up other state as well to prepare the ItemsHost for using a new panel.
		Refresh();

		m_cacheManager.DisconnectItemsHost();
	}

	private bool IsItemsHostRegistered() => m_cacheManager.IsItemsHostRegistered();

	private protected void SetGroupHeaderPlacement(GroupHeaderPlacement placement)
	{
		m_groupHeaderPlacement = placement;
		ReevaluateGroupHeaderStrategy();
	}

	// Returns the group header strategy enum value based on virtualization direction and group header placement.
	private GroupHeaderStrategy GetGroupHeaderStrategy()
	{
		GroupHeaderStrategy headerStrategy = GroupHeaderStrategy.None;

		if (m_cacheManager.IsGrouping())
		{
			var layoutStrategy = GetLayoutStrategy();
			var orientation = layoutStrategy.VirtualizationDirection;

			switch (orientation)
			{
				case Orientation.Horizontal:
					if (GroupHeaderPlacement.Left == m_groupHeaderPlacement)
					{
						headerStrategy = GroupHeaderStrategy.Inline;
					}
					else
					{
						headerStrategy = GroupHeaderStrategy.Parallel;
					}
					break;

				case Orientation.Vertical:
					if (GroupHeaderPlacement.Left == m_groupHeaderPlacement)
					{
						headerStrategy = GroupHeaderStrategy.Parallel;
					}
					else
					{
						headerStrategy = GroupHeaderStrategy.Inline;
					}
					break;

				default:
					global::System.Diagnostics.Debug.Assert(false);
					break;
			}
		}

		return headerStrategy;
	}

	// Set the strategy's header strategy based on whether or not we're grouping.
	private protected void ReevaluateGroupHeaderStrategy()
	{
		var spStrategy = GetLayoutStrategy();
		var orientation = spStrategy.VirtualizationDirection;

		// Only apiset + vertical Orientation + Headerplacement!=Left can set it to TRUE
		bool bOldUseStickyHeaders = m_bUseStickyHeaders;
		m_bUseStickyHeaders = false;
		m_lastVisibleWindowClippingHeight = 0.0f;

		GroupHeaderStrategy headerStrategy = GetGroupHeaderStrategy();

		if (m_cacheManager.IsGrouping() &&
			GroupHeaderPlacement.Left != m_groupHeaderPlacement &&
			orientation == Orientation.Vertical)
		{
			// get the BOOLEAN value from the public property AreStickyGroupHeadersEnabled
			m_bUseStickyHeaders = AreStickyGroupHeadersEnabledBase;
		}

		SetGroupHeaderStrategy(headerStrategy);

		// Only apiset + vertical Orientation + Headerplacement!=Left can set bOldUseStickyHeaders to TRUE
		if (bOldUseStickyHeaders && !m_bUseStickyHeaders)
		{
			RemoveStickyHeaders();
		}
		else if (!bOldUseStickyHeaders && m_bUseStickyHeaders)
		{
			double width = ActualWidth;
			double height = ActualHeight;
			Size size = default;

			size.Width = (float)width;
			size.Height = (float)height;

			UpdateStickyHeaders(size);
		}
	}

	// determines the panning direction. This returns the stored panning direction that we were able to determine during arrange
	// or none (if a new tick)
	internal PanelScrollingDirection PanningDirectionBase
	{
		get
		{
			PanelScrollingDirection value = PanelScrollingDirection.None;

			int tick = CoreImports.LayoutManager_GetLayoutTickForTransition(this);

			// the stored panning direction, calculated during arrange, is only valid when being queried in the same tick
			if (tick == m_windowState.currentTickNumber)
			{
				value = m_windowState.m_panningDirectionBase;
			}

			return value;
		}
		set => m_windowState.m_panningDirectionBase = value;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		m_layoutInProgress = true;

		try
		{
			if (!IsItemsHostRegistered())
			{
				return default;
			}

			// store the availablesize
			if (m_windowState.lastAvailableSize.Width != availableSize.Width ||
				m_windowState.lastAvailableSize.Height != availableSize.Height)
			{
				m_windowState.lastAvailableSize = availableSize;
				// available size has changed, we should recalculate
				m_windowState.validWindowCalculation = false;
			}

			// Run our virtualization pass to generate, measure, and pre-arrange everything
			const int maxVirtualizationPasses = 100;
			int virtualizationPassCount = 0;
			do
			{
				++virtualizationPassCount;
				RunVirtualization();
			} while (m_windowState.m_command is not null && virtualizationPassCount < maxVirtualizationPasses);

			// A pass may be restarted if focus work is occurring, but in general, this should only take one or two tries.
			// The bailout should never be needed, but it's there in case somebody breaks something horribly, so that we can fail more obviously
			// instead of making a developer diagnose this infinite loop.
			global::System.Diagnostics.Debug.Assert(m_windowState.m_command is null);
			m_windowState.m_command = null;

			// Get the estimated panel extent and setup our return value
			m_estimatedSize = EstimatePanelExtent();
			return m_estimatedSize;
		}
		finally
		{
			m_layoutInProgress = false;
		}
	}

	// This method will detect the visible window, generate/recycle, measure, and pre-arrange
	private void RunVirtualization()
	{
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

#if DEBUG
		global::System.Diagnostics.Debug.Assert(!m_debugIsInTemporaryInvalidState);
#endif

		try
		{
			// sets up caches to make iterating over groups much faster, caches children collection, host, etc.
			using var strongCache = m_cacheManager.CacheStrongRefs(); // Releases when it goes out of scope at the end of block

			// When an initial measure comes in, we should ensure that we have a reasonable visible window set up, based on the available size given
			// If not, we'll ask for too little space in the nonvirtualizing direction, and may never get it back.
			// Window commands (ScrollIntoView) may come in before we've had a chance to get a valid window, so this is valid to run even if we have
			// a pending command.
			if (spScrollViewer is null)
			{
				AttachPanelComponents();
			}
			else if (!m_windowState.validWindowCalculation)
			{
				PrimeVisibleWindow();
				SetRealizationWindowFromVisibleWindow(false);
			}

			// Protected by Apiset
			// Sticky headers : TRUE only for Vertical Orientation + Headerplacement!=Left
			// MaintainViewprt : needs ItemsStackPanel AND m_itemsUpdatingScrollMode != ItemsUpdatingScrollMode::KeepScrollOffset

			if (m_bUseStickyHeaders || IsMaintainViewportSupportedAndEnabled())
			{
				if (spScrollViewer is not null)
				{
					// Update the size of the header once to that it will be correct for all header's creations
					m_currentHeaderHeight = spScrollViewer.HeaderHeight;
				}
			}

			// make sure we are not in a state where we cannot perform layout.
			// we are not doing a FailFast if !IsLockedForLayout because
			// of bug 2863553 which breaks compat.
			if (IsLockedForLayout())
			{
				TraceGuardFailure("LayoutWhenLocked");
			}

			if (m_containerManager.m_unlinkedFillers.Count != 0)
			{
				// If we are in the middle of a group change,
				// we should stop right now instead of failing later
				throw new InvalidOperationException();
			}

			// move unloaded elements from unloaded queue to the visual tree.
			ReloadUnloadedElements();

			// make sure that the focused container is still focused, otherwise move it to recyclequeue
			VerifyStoredFocusedContainer();

			// Ensure that any pinned containers still have a reason to be pinned. If not, unpin them.
			PrunePinnedContainers();

			// Always measure the special container (used for fixed size panels) if the strategy
			// asks for it
			MeasureSpecialElements();

			// Determine the window to show, based on either the user scrolling, or responding to a specific request
			// such as key navigation or ScrollIntoView, and then generate the appropriate anchors for the new view
			DetermineWindowAndAnchors();

			// measure all containers, regardless of window.
			// this allows us to understand how much space we currently take up, and adhere to the measure contract
			// of calling measure on all owned children
			RunGenerate();

			// No matter how hard we try, estimates are going to be wrong. That's why they're called estimates.
			// Here, we detect when they've caused us to deviate from a known constraint. Typically,
			// this means that if item/header 0 is not at pixel (0,0), or if some other element is at (0,0)
			// we'll shift our elements and window over to bring us back to where we belong.
			CorrectForEstimationErrors();

			// If ItemsUpdatingScrollMode is set to KeepLastItemInView and the viewport is at the very end
			// of the list, we need to shift the viewport to the last element which might have been recently inserted.
			TrackLastElement();

			// Measure elements in garbage section to avoid measuring during arrange pass, which can cause a layout cycle.
			MeasureElementsInGarbageSection();
		}
		finally
		{
			m_refreshPendingLayout = false;
		}
	}

	// Measure elements in garbage section.  Since there is no API for getting a non-special item sizes, it uses bogus index (-1) which is not special.
	private void MeasureElementsInGarbageSection()
	{
		Rect realizationWindow = m_windowState.GetRealizationWindow();

		var spChildren = m_cacheManager.GetChildren();
		int childrenCount = spChildren.Count;

		Size headerMeasureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, -1, realizationWindow);
		Size containerMeasureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, -1, realizationWindow);

		for (int garbageIndex = m_containerManager.StartOfGarbageSection(); garbageIndex < childrenCount; ++garbageIndex)
		{
			Size measureSize;
			Size desiredSize;

			var spCurrentElement = spChildren[garbageIndex];

			ElementType elementType = GetElementIsHeader(spCurrentElement) ? ElementType.GroupHeader : ElementType.ItemContainer;

			if (m_containerManager.GetIsElementPinned(elementType, spCurrentElement, out int elementIndex))
			{
				global::System.Diagnostics.Debug.Assert(elementIndex >= 0);

				if (elementType == ElementType.GroupHeader)
				{
					measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, elementIndex, realizationWindow);
				}
				else
				{
					measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, elementIndex, realizationWindow);
				}
			}
			else
			{
				measureSize = (elementType == ElementType.GroupHeader) ? headerMeasureSize : containerMeasureSize;
			}

			spCurrentElement.Measure(measureSize);

			desiredSize = spCurrentElement.DesiredSize;

			SetElementSizeInGarbageSection(spCurrentElement, desiredSize);

			// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
			if (m_bUseStickyHeaders)
			{
				if (elementType == ElementType.GroupHeader)
				{
					// Remove the parametric curves as the header reusing the container will have different curves
					RemoveStickyHeader(spCurrentElement);
				}
			}
		}
	}

	private struct TypeIndexPair
	{
		internal ElementType type;
		internal int index;
		internal bool requestedByStrategy;
	}

	// If the layout specifies a need for a special group and/or item,
	// ensure these items are generated, measured, and pinned.
	private void MeasureSpecialElements()
	{
		bool measureNeeded;

		List<TypeIndexPair> specialElements = new();

		measureNeeded = NeedsSpecialGroup();
		if (measureNeeded)
		{
			int specialGroupIndex = GetSpecialGroupIndex();
			if (specialGroupIndex >= 0 && specialGroupIndex < m_cacheManager.GetTotalGroupCount())
			{
				// C++11 uniform initialization not supported yet, so make a temp
				TypeIndexPair temp = new() { type = ElementType.GroupHeader, index = specialGroupIndex, requestedByStrategy = true };
				specialElements.Add(temp);
			}
		}

		measureNeeded = NeedsSpecialItem();
		if (measureNeeded)
		{
			int specialItemIndex = GetSpecialItemIndex();
			if (specialItemIndex >= 0 && specialItemIndex < m_cacheManager.GetTotalItemCount())
			{
				// C++11 uniform initialization not supported yet, so make a temp
				TypeIndexPair temp = new() { type = ElementType.ItemContainer, index = specialItemIndex, requestedByStrategy = true };
				specialElements.Add(temp);
			}
		}

		var spChildren = m_cacheManager.GetChildren();

		foreach (var specialElementInfo in specialElements)
		{
			bool shouldEnter = false;
			Size childDesiredSize;
			bool newElementGenerated = false;

			Size measureSize = m_spLayoutStrategy.GetElementMeasureSize(specialElementInfo.type, specialElementInfo.index, m_windowState.GetRealizationWindow());

			var spSpecialElement = m_containerManager.GetElementFromPinnedElements(specialElementInfo.type, specialElementInfo.index);

			if (spSpecialElement is null)
			{
				spSpecialElement = m_containerManager.GetElementAtDataIndex(specialElementInfo.type, specialElementInfo.index);
				if (spSpecialElement is null)
				{
					// Generate, because this element hasn't been created yet
					spSpecialElement = GenerateElementAtDataIndex(specialElementInfo.type, specialElementInfo.index)!;
					newElementGenerated = true;

					// items need to be in the visual tree to be measured correctly
					// however, this does not mean they have to be in the valid part.
					int foundIndex = spChildren.IndexOf(spSpecialElement);
					bool found = foundIndex >= 0;
					if (!found)
					{
						spChildren.Add(spSpecialElement);
					}
					else
					{
						// if element is in the visual tree (comes from recycling headers) we should setup Loading transition.
						shouldEnter = true;
					}

					PrepareElementViaItemsHost(specialElementInfo.type, specialElementInfo.index, measureSize, spSpecialElement);
				}

				m_containerManager.RegisterPinnedElement(specialElementInfo.type, specialElementInfo.index, spSpecialElement);
			}

			// items need to be in the visual tree to be measured correctly
			// however, this does not mean they have to be in the valid part.
			if (shouldEnter)
			{
#if !HAS_UNO // TODO Uno: no core layout transition state on UIElement.
				CoreImports.UIElement_SetIsEntering(spSpecialElement, true);
#endif
			}

			spSpecialElement.Measure(measureSize);
			childDesiredSize = spSpecialElement.DesiredSize;

			if (specialElementInfo.requestedByStrategy)
			{
				RegisterSpecialElementSize(specialElementInfo.type, specialElementInfo.index, childDesiredSize);
			}

			// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
			if (m_bUseStickyHeaders && newElementGenerated && specialElementInfo.type == ElementType.GroupHeader)
			{
				// We don't have the positioning, only the size... does it really make sense here ?
				ConfigureStickyHeader(spSpecialElement, specialElementInfo.index, RectUtil.CreateRect(new Point(), childDesiredSize));
			}
		}
	}


	// Measure all valid containers, regardless of window.
	// This is not only required for the measure contract
	// of calling measure on all owned children, but also
	// allows us to understand how much space we take up.
	private void RunGenerate()
	{
		// If there is no existing element to measure, we get out.
		if ((m_cacheManager.IsGrouping() && m_cacheManager.GetTotalLayoutGroupCount() == 0)
			|| (!m_cacheManager.IsGrouping() && m_cacheManager.GetTotalItemCount() == 0))
		{
			return;
		}

		m_spLayoutStrategy.BeginMeasure();

		// Initializes the reference location.
		{
			LayoutReference referenceInformation = CreateDefaultLayoutReference();
			CollectionIterator iterator = new(m_cacheManager);

			// Set the starting location.
			if (m_viewportBehavior.isTracking)
			{
				// We need to shift all the elements to go under the realization window that accounts
				// for the virtualization shift.
				ApplyTrackedElementShift();

				// New behavior, starts from the tracked element.
				iterator.Init(m_viewportBehavior.index, m_viewportBehavior.type);

				// the idea is that the very first measure is going to be relative to itself
				referenceInformation.RelativeLocation = ReferenceIdentity.Myself;
				referenceInformation.ReferenceBounds = m_viewportBehavior.elementBounds;
				referenceInformation.ReferenceIsHeader = (m_viewportBehavior.type == ElementType.GroupHeader);

				Generate(iterator, referenceInformation, true /* goForward */);

				// If we arrived here tracking an element, we'll generate backwards off that same point
				// This should be safe because even if tracking a header, it shouldn't have become floating
				// because the header is visible and not a candidate for recycling off the front. Akso, all of its items come after it in the order,
				// so they're also not candidates for recycling off the front
				if (iterator.MovePrevious())
				{
					referenceInformation.RelativeLocation = ReferenceIdentity.AfterMe;
					Generate(iterator, referenceInformation, false /* goForward */);
				}
			}
			else
			{
				// Old behavior, just start from the first non-floating element
				GetGenerationAnchorInformation(
					out referenceInformation,
					ref iterator);
				referenceInformation.RelativeLocation = ReferenceIdentity.Myself;

				Generate(iterator, referenceInformation, true /* goForward */);

				// We didn't start off tracking an element, so there's no guarantee that the original reference is still alive
				// To avoid those problems, we'll just grab a new reference from the new beginning of the valid range

				GetGenerationAnchorInformation(
					out referenceInformation,
					ref iterator);
				if (iterator.MovePrevious())
				{
					referenceInformation.RelativeLocation = ReferenceIdentity.AfterMe;
					Generate(iterator, referenceInformation, false /* goForward */);
				}

			}
		}

		m_spLayoutStrategy.EndMeasure();
	}

	// Given a start location and a direction, remove/recycle all valid containers and headers.
	private void RemoveMeasureLeftOvers(CollectionIterator iterator, bool goForward)
	{
		CollectionIterator.ElementInfo current = iterator.GetCurrent();

		// We will start from these indices when removing sentinels and leftovers.
		int lastValidContainerIndex = m_containerManager.GetValidContainerIndexFromItemIndex(current.itemIndex);
		int lastValidHeaderIndex = m_containerManager.GetValidHeaderIndexFromGroupIndex(current.groupIndex);

		// This is the number of elements we need to remove either at the beginning or at the end depending
		// on the measure direction (forward / backward).
		int leftOverContainerCount = 0;
		int leftOverHeaderCount = 0;

		if (goForward)
		{
			// We stopped at a sentinel header. lastValidHeaderIndex is already correct.
			if (current.isHeader())
			{
			}
			// We stopped at a sentinel container. lastValidContainerIndex is already correct.
			else
			{
				++lastValidHeaderIndex;     // Skip the current's container group header.
			}

			int validContainerCount = m_containerManager.GetValidContainerCount();
			if (validContainerCount > 0)
			{
				global::System.Diagnostics.Debug.Assert(lastValidContainerIndex >= 0);
				leftOverContainerCount = validContainerCount - lastValidContainerIndex;
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(lastValidContainerIndex == -1);
			}

			if (m_cacheManager.IsGrouping())
			{
				leftOverHeaderCount = m_containerManager.GetValidHeaderCount() - lastValidHeaderIndex;
			}
		}
		// Going backward is not as straightforward as going forward.
		// We should not recycle/remove a header if there is at least one container realized in that group.
		else
		{
			// We stopped at a sentinel header.
			if (current.isHeader())
			{
				// We always generate the floating header while going backwards
				// So we always lay it out if we successfully iterated through all of its items
				// Therefore, the only reason to stop on a header is if it's for an empty group
				global::System.Diagnostics.Debug.Assert(current.itemCountInGroup == 0);

				if (lastValidContainerIndex >= 0)
				{
					// The container index currently points at the first item of the next nonempty group
					// Decrement so this valid item doesn't get caught in the cleanup
					--lastValidContainerIndex;
				}
			}
			// We stopped at a sentinel container.
			else
			{
				// Only remove the current group if we stopped at the last container of that group.
				// Otherwise, if we haven't stopped on the group's last container, there should be
				// a floating header waiting for us. We'll lay it out after the cleanup.
				// In the meantime, let's not recycle it in the cleanup, so decrement the index
				if (m_cacheManager.IsGrouping() && current.IndexInGroup() != (current.itemCountInGroup - 1))
				{
#if DEBUG
					var spHeader = m_containerManager.GetHeaderAtValidIndex(lastValidHeaderIndex);
					global::System.Diagnostics.Debug.Assert(spHeader is not null);
#endif
					--lastValidHeaderIndex;
				}
			}

			// +1 to convert from an index to a count.
			leftOverContainerCount = lastValidContainerIndex + 1;
			if (m_cacheManager.IsGrouping())
			{
				leftOverHeaderCount = lastValidHeaderIndex + 1;
			}
		}

		global::System.Diagnostics.Debug.Assert(leftOverContainerCount >= 0 && leftOverContainerCount <= m_containerManager.GetValidContainerCount());
		global::System.Diagnostics.Debug.Assert(!m_cacheManager.IsGrouping() || (leftOverHeaderCount >= 0 && leftOverHeaderCount <= m_containerManager.GetValidHeaderCount()));

		// We start with containers leftovers and sentinels.
		int validContainerCountAfterRemovingLeftOvers = m_containerManager.GetValidContainerCount() - leftOverContainerCount;
		while (m_containerManager.GetValidContainerCount() > validContainerCountAfterRemovingLeftOvers)
		{
			int validIndex = goForward ? m_containerManager.GetValidContainerCount() - 1 : 0;

			var spCurrentElement = m_containerManager.GetContainerAtValidIndex(validIndex);

			if (!GetElementIsSentinel(spCurrentElement))
			{
				m_icg2.RecycleContainer(spCurrentElement!);
			}

			m_containerManager.RemoveFromValidContainers(validIndex, false /* isForItemRemoval */);
		}

		// And then we do the same for header leftovers and sentinels.
		int validHeaderCountAfterRemovingLeftOvers = m_containerManager.GetValidHeaderCount() - leftOverHeaderCount;
		while (m_containerManager.GetValidHeaderCount() > validHeaderCountAfterRemovingLeftOvers)
		{
			int validIndex = goForward ? m_containerManager.GetValidHeaderCount() - 1 : 0;

			var spCurrentElement = m_containerManager.GetHeaderAtValidIndex(validIndex);

			if (!GetElementIsSentinel(spCurrentElement))
			{
				m_icg2.RecycleHeader(spCurrentElement!);
			}

			m_containerManager.RemoveFromValidHeaders(validIndex, false /* isForGroupRemoval */);
		}
	}

	// grabs the bounds of the supplied starting element and uses that as the anchor into the strategy.
	// 1. asks strategy whether we should continue generating
	//    boolean Strategy.ContinueGenerating(int itemIX, int siblingIX, rect siblingBound, window)
	// 2. generate or recycle container and measure
	// 3. container.VirtualizationInformation.Bounds = Strategy.GetBounds(int itemIX, Size desiredSize, int siblingIX, rect siblingBounds)
	// These bounds will be either desiredsize based or fixed size based (wrapgrid) or vswg like (new strategy that knows sizes upfront)
	private void Generate(CollectionIterator iterator, LayoutReference referenceInformation, bool goForward)
	{
#if !HAS_UNO // TODO Uno: no ETW provider.
		TraceGenerateItemsBegin();

		using var guard = Disposable.Create(() => TraceGenerateItemsEnd());
#endif

		// needed for Sticky Headers
		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		Rect windowToFill;

		// This flag tell us if we were tracking before Generate is called.
		// It's possible that we start tracking during the generate pass.
		// In that case, we will have to update the realization window after
		// the tracked element is placed. This will make sure it doesn't get recycled.
		bool wereTrackingBefore = m_viewportBehavior.isTracking;

		// Keep track of whether we've generated a header that we have yet to place
		UIElement? spFloatingHeader = null;

		var referenceDirection = goForward ? ReferenceIdentity.BeforeMe : ReferenceIdentity.AfterMe;

		if ((m_cacheManager.GetTotalItemCount() == 0 && m_cacheManager.GetTotalLayoutGroupCount() == 0) || !m_windowState.validWindowCalculation)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidContainerCount() > 0 || (m_cacheManager.IsGrouping() && m_containerManager.GetValidHeaderCount() > 0));

		// When tracking, we should care about the future realization window
		// (when EndTrackingFirstVisbleElement gets called).
		if (m_viewportBehavior.isTracking)
		{
			windowToFill = GetRealizationWindowAfterViewportShift();
		}
		else
		{
			windowToFill = m_windowState.GetRealizationWindow();
		}

		// Let's make sure we have a reference rect for the previous header
		if (m_cacheManager.IsGrouping())
		{
			if (referenceInformation.ReferenceIsHeader)
			{
				// If our layout reference is already a header, just make sure it's also reflected here
				referenceInformation.HeaderBounds = referenceInformation.ReferenceBounds;
			}
			else
			{
				var current = iterator.GetCurrent();
				int groupIndexOfReferenceHeader = -1;

				// We're trying to layout from a container
				if (goForward)
				{
					if (current.isHeader())
					{
						// We're trying to generate a header, so the interesting header is the one for groupIndex-1
						groupIndexOfReferenceHeader = current.groupIndex - 1;
					}
					else
					{
						// Generating a container, so keep the header of the current group
						groupIndexOfReferenceHeader = current.groupIndex;
					}
				}
				else
				{
					// Need to look at the header for group+1, to ensure that if the current header is big,
					// we place it far enough left to not push group+1 to the right
					groupIndexOfReferenceHeader = current.groupIndex + 1;
				}

				UIElement? spReferenceHeader = null;
				if (m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(groupIndexOfReferenceHeader)))
				{
					spReferenceHeader = m_containerManager.GetHeaderAtGroupIndex(groupIndexOfReferenceHeader);
				}

				if (spReferenceHeader is not null)
				{
					referenceInformation.HeaderBounds = GetBoundsFromElement(spReferenceHeader);
				}
				else
				{
					// Since our reference rect belongs to a container, that container should have a header.
					// So the only way this can be hit is if we are generating backwards
					global::System.Diagnostics.Debug.Assert(!goForward);
					referenceInformation.HeaderBounds = RectUtil.CreateEmptyRect();
				}
			}
		}
		else
		{
			// Not grouping, let's just clear this out to avoid any misunderstanding
			referenceInformation.HeaderBounds = RectUtil.CreateEmptyRect();
		}

		bool continueGenerating = true;
		bool inCollection = true;
		while (continueGenerating && inCollection)
		{
			CollectionIterator.ElementInfo current = iterator.GetCurrent();
			// TODO: the blocks for headers and containers are nearly identical, refactor support methods to accept enum as type
			// allowing us to remove lots of duplicate code
			if (current.isHeader())
			{
				global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() == 0 ||
					(m_containerManager.GetGroupIndexFromValidIndex(-1) <= current.groupIndex &&
					current.groupIndex <= m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount())));

				UIElement? spCurrentHeader;

				// Let's compare what we have here, with what we *should* have
				spCurrentHeader = m_containerManager.GetHeaderAtGroupIndex(current.groupIndex);

				if (ShouldElementBeVisible(current.type, current.groupIndex))
				{
					bool newHeaderGenerated = false;

					// Only check against the bounds of the window if we need to create a new element
					// Or...
					// If we are going backwards and are looking to finish off a nonempty group by placing its header,
					// we should always do that work even if the header is going to end up outside the window.
					// So, we should only entertain the notion of stopping if we're looking at a "solo" header for an empty group
					// Reasoning: We've already done the expensive work of generating this "floating" header,
					//            so we might as well place it in the valid range and lay it out.
					if (spCurrentHeader is not null ||
						(!goForward && current.itemCountInGroup > 0))
					{
						continueGenerating = true;
					}
					else
					{
						continueGenerating = m_spLayoutStrategy.ShouldContinueFillingUpSpace(
							ElementType.GroupHeader,
							m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, current.groupIndex),
							referenceInformation,
							windowToFill);

						// If we've got a floating header, we should *always* lay it out
						global::System.Diagnostics.Debug.Assert(spFloatingHeader is null);
					}

					// Let's measure this thing
					if (continueGenerating)
					{
						bool oldBoundsAreValid = false;
						Size measureSize;
						Size desiredSize;
						Rect bounds;
						Rect oldBounds = default;

						// If we had a floating header coming in, this should be it
						global::System.Diagnostics.Debug.Assert(spFloatingHeader is null || spFloatingHeader == spCurrentHeader);

						measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, current.groupIndex, windowToFill);

						// If we don't have one yet, let's go ahead and generate it now
						if (spCurrentHeader is null)
						{
							// now generate. Hopefully we have been able to fill the recyclequeue, but if not
							// ICG will just create a new container for us.
							EnsureRecycleHeaderCandidate(goForward /* recycle candidates are at the front */, false /* alwaysRecycle */);
							if (m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(current.groupIndex)))
							{
								// If we're still filling out sentinels, go ahead and recycle future elements from the end we are generating to
								EnsureRecycleHeaderCandidate(!goForward, false /* alwaysRecycle */);
							}
							spCurrentHeader = m_icg2.GenerateHeaderAtGroupIndex(current.groupIndex)!;
							m_containerManager.PlaceInValidHeaders(current.groupIndex, spCurrentHeader);
							PrepareHeaderViaItemsHost(current.groupIndex, measureSize, spCurrentHeader);
							newHeaderGenerated = true;
						}
						else
						{
							oldBoundsAreValid = true;
							oldBounds = GetBoundsFromElement(spCurrentHeader);
						}

						spCurrentHeader.Measure(measureSize);
						desiredSize = spCurrentHeader.DesiredSize;
						bounds = m_spLayoutStrategy.GetElementBounds(
							ElementType.GroupHeader,
							m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, current.groupIndex),
							desiredSize,
							referenceInformation,
							windowToFill);

						// When elements change size, we need to start tracking the first visible element if the maintain
						// viewport behavior is enabled.
						if (oldBoundsAreValid &&
							(!DoubleUtil.AreClose(bounds.Width, oldBounds.Width) || !DoubleUtil.AreClose(bounds.Height, oldBounds.Height)))
						{
							BeginTrackingOnMeasureChange(true /* isGroupChange */, current.groupIndex);
						}

						// If we are tracking due to a measure change and we are about to set
						// new bounds for the tracked element, we will need to update the local
						// realization window.
						if (m_viewportBehavior.isTracking &&
							!wereTrackingBefore &&
							m_viewportBehavior.type == current.type &&
							m_viewportBehavior.index == current.groupIndex)
						{
							m_viewportBehavior.elementBounds = bounds;
							windowToFill = GetRealizationWindowAfterViewportShift();
						}

						SetBoundsForElement(spCurrentHeader, bounds);

						referenceInformation.ReferenceIsHeader = true;
						referenceInformation.RelativeLocation = referenceDirection;
						referenceInformation.ReferenceBounds = bounds;
						referenceInformation.HeaderBounds = bounds;

						// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
						if (m_bUseStickyHeaders && newHeaderGenerated && spScrollViewer is not null)
						{
							ConfigureStickyHeader(spCurrentHeader, current.groupIndex, bounds);
						}

						// We've placed the header, so no more floating header
						spFloatingHeader = null;
					}
				}
				else // endif ShouldElementBeVisible
				{
					if (spCurrentHeader is not null)
					{
						// Looks like we have an element there, but shouldn't.
						// Recycle it and replace it with a sentinel

						// If somebody is trying to hide a non-empty group, we've got a problem here...
						global::System.Diagnostics.Debug.Assert(current.itemCountInGroup == 0);

						// If we are tracking this header, we should elect a new one now.
						if (m_viewportBehavior.isTracking)
						{
							var spTrackedElement = GetTrackedElement();
							if (spCurrentHeader == spTrackedElement)
							{
								ElectNewTrackedElement();
							}
						}

						m_icg2.RecycleHeader(spCurrentHeader);
						spCurrentHeader = null;
					}
					// Before continuing, place a sentinel in the range so it's accounted for
					m_containerManager.PlaceInValidHeaders(current.groupIndex, null);
				}
			}
			else //container case
			{
				global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidContainerCount() == 0 ||
					(m_containerManager.GetItemIndexFromValidIndex(-1) <= current.itemIndex &&
					current.itemIndex <= m_containerManager.GetItemIndexFromValidIndex(m_containerManager.GetValidContainerCount())));

				UIElement? spCurrentContainer;

				// Let's compare what we have here, with what we *should* have
				spCurrentContainer = m_containerManager.GetContainerAtItemIndex(current.itemIndex);

				if (ShouldElementBeVisible(current.type, current.itemIndex))
				{
					// Only check against the bounds of the window if we need to create a new element
					if (spCurrentContainer is not null)
					{
						continueGenerating = true;
					}
					else
					{
						continueGenerating = m_spLayoutStrategy.ShouldContinueFillingUpSpace(
							ElementType.ItemContainer,
							current.IndexInGroup(),
							referenceInformation,
							windowToFill);
					}

					// Let's measure it
					if (continueGenerating)
					{
						bool oldBoundsAreValid = false;
						Size desiredSize;
						Size measureSize;
						Rect bounds;
						Rect oldBounds = default;

						measureSize = m_spLayoutStrategy.GetElementMeasureSize(ElementType.ItemContainer, current.itemIndex, windowToFill);

						// If we don't have one yet, let's go ahead and generate it now
						if (spCurrentContainer is null)
						{
							// now generate. Hopefully we have been able to fill the recyclequeue, but if not
							// ICG will just create a new container for us.
							EnsureRecycleContainerCandidate(current.itemIndex, goForward /* recycle candidates are at the front */);
							if (m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(current.itemIndex)))
							{
								// If we're still filling out sentinels, go ahead and recycle future elements from the end we are generating to
								EnsureRecycleContainerCandidate(current.itemIndex, !goForward);
							}

							spCurrentContainer = m_icg2.GenerateContainerAtIndex(current.itemIndex)!;
							m_containerManager.PlaceInValidContainers(current.itemIndex, spCurrentContainer);
							PrepareContainerViaItemsHost(current.itemIndex, measureSize, spCurrentContainer);
						}
						else
						{
							oldBoundsAreValid = true;
							oldBounds = GetBoundsFromElement(spCurrentContainer);
						}

						spCurrentContainer.Measure(measureSize);
						desiredSize = spCurrentContainer.DesiredSize;
						bounds = m_spLayoutStrategy.GetElementBounds(
							ElementType.ItemContainer,
							current.itemIndex,
							desiredSize,
							referenceInformation,
							windowToFill);

						// When elements change size, we need to start tracking the first visible element if the maintain
						// viewport behavior is enabled.
						if (oldBoundsAreValid &&
							(!DoubleUtil.AreClose(bounds.Width, oldBounds.Width) || !DoubleUtil.AreClose(bounds.Height, oldBounds.Height)))
						{
							BeginTrackingOnMeasureChange(false /* isGroupChange */, current.itemIndex);
						}

						// If we are tracking due to a measure change and we are about to set
						// new bounds for the tracked element, we will need to update the local
						// realization window.
						if (m_viewportBehavior.isTracking &&
							!wereTrackingBefore &&
							m_viewportBehavior.type == current.type &&
							m_viewportBehavior.index == current.itemIndex)
						{
							m_viewportBehavior.elementBounds = bounds;
							windowToFill = GetRealizationWindowAfterViewportShift();
						}

						SetBoundsForElement(spCurrentContainer, bounds);

						// Defer updating the layout reference until we've generated a floating header

						// Ensure we have a header to place at the head of this group
						if (m_cacheManager.IsGrouping() && !goForward && spFloatingHeader is null)
						{
							spFloatingHeader = m_containerManager.GetHeaderAtGroupIndex(current.groupIndex);

							if (spFloatingHeader is null)
							{
								Size measureSizeForFloatingHeader;
								Size desiredSizeForFloatingHeader;
								Rect boundsForFloatingHeader;

								// We need to generate this header to satisfy invariants, but if we somehow think
								// this header shouldn't be generated, somebody is mistaken
								global::System.Diagnostics.Debug.Assert(ShouldElementBeVisible(ElementType.GroupHeader, current.groupIndex));

								measureSizeForFloatingHeader = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, current.groupIndex, windowToFill);

								// We must have just started creating this group. Let's preemptively generate the header
								EnsureRecycleHeaderCandidate(goForward /* recycle candidates are at the front */, false /* always recycle */);
								if (m_containerManager.IsValidHeaderIndexWithinBounds(m_containerManager.GetValidHeaderIndexFromGroupIndex(current.groupIndex)))
								{
									// If we're still filling out sentinels, go ahead and recycle future elements from the end we are generating to
									EnsureRecycleHeaderCandidate(!goForward, false /* alwaysRecycle */);
								}

								spFloatingHeader = m_icg2.GenerateHeaderAtGroupIndex(current.groupIndex)!;
								m_containerManager.PlaceInValidHeaders(current.groupIndex, spFloatingHeader);
								PrepareHeaderViaItemsHost(current.groupIndex, measureSizeForFloatingHeader, spFloatingHeader);

								spFloatingHeader.Measure(measureSizeForFloatingHeader);
								desiredSizeForFloatingHeader = spFloatingHeader.DesiredSize;

								// TODO: We should be able to streamline this a bit with generalized estimations in the future
								// but for now, we need to give it some sensible bounds
								boundsForFloatingHeader = m_spLayoutStrategy.GetElementBounds(
									ElementType.GroupHeader,
									m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, current.groupIndex),
									desiredSizeForFloatingHeader,
									referenceInformation,
									windowToFill);

								SetBoundsForElement(spFloatingHeader, boundsForFloatingHeader);

								// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
								if (m_bUseStickyHeaders && spScrollViewer is not null)
								{
									ConfigureStickyHeader(spFloatingHeader, current.groupIndex, boundsForFloatingHeader);
								}
							}
						}

						referenceInformation.ReferenceIsHeader = false;
						referenceInformation.RelativeLocation = referenceDirection;
						referenceInformation.ReferenceBounds = bounds;
					}
					else // endif ContinueGenerating
					{
						// We are not going to generate anymore, however we could be in a situation where
						// we start off with the realization window at item n of a group, and dont generate
						// even a single item because the reference's (first item) point starts before the
						// window. In this case, we end up not measuring the floating header. Refer Bug: 2798854
						// If there is a floating header, make sure it is always measured in the backward's pass
						if (m_cacheManager.IsGrouping() && !goForward)
						{
							spFloatingHeader = m_containerManager.GetHeaderAtGroupIndex(current.groupIndex);
							if (spFloatingHeader is not null && spFloatingHeader.IsMeasureDirty)
							{
								// we have a floating header that has not been measured, make sure it is

								Size measureSizeForFloatingHeader;
								Size desiredSizeForFloatingHeader;
								Rect boundsForFloatingHeader;

								measureSizeForFloatingHeader = m_spLayoutStrategy.GetElementMeasureSize(ElementType.GroupHeader, current.groupIndex, windowToFill);
								spFloatingHeader.Measure(measureSizeForFloatingHeader);
								desiredSizeForFloatingHeader = spFloatingHeader.DesiredSize;

								boundsForFloatingHeader = m_spLayoutStrategy.GetElementBounds(
									ElementType.GroupHeader,
									m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, current.groupIndex),
									desiredSizeForFloatingHeader,
									referenceInformation,
									windowToFill);

								SetBoundsForElement(spFloatingHeader, boundsForFloatingHeader);

								// Configure the sticky header now that the header is measured
								if (m_bUseStickyHeaders && spScrollViewer is not null)
								{
									ConfigureStickyHeader(spFloatingHeader, current.groupIndex, boundsForFloatingHeader);
								}
							}
						}
					}
				}
				else // endif ShouldElementBeVisible
				{
					if (spCurrentContainer is not null)
					{
						// Looks like we have an element there, but shouldn't.
						// Recycle it and replace it with a sentinel

						m_icg2.RecycleContainer(spCurrentContainer);
						spCurrentContainer = null;
					}

					// Before continuing, place a sentinel in the range so it's accounted for
					m_containerManager.PlaceInValidContainers(current.itemIndex, null);
				}
			}

			// Let's move to the next one and attempt to continue
			if (continueGenerating)
			{
				if (goForward)
				{
					inCollection = iterator.MoveNext();
				}
				else
				{
					inCollection = iterator.MovePrevious();
				}
			}
		}

		// Now that we've measured and/or instantiated all the sentinels, let's clean out the sentinels and leftovers
		if (inCollection)
		{
			RemoveMeasureLeftOvers(iterator, goForward);
		}

		if (spFloatingHeader is not null)
		{
			// Consistency check that the floating header is what we think it is
			global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() > 0 &&
				iterator.GetCurrent().groupIndex == m_containerManager.GetGroupIndexFromValidIndex(0));
			global::System.Diagnostics.Debug.Assert(!goForward);

			EstimateFloatingHeaderLocation(spFloatingHeader);
		}

		m_containerManager.TrimEdgeSentinels();

		// Protected by Apiset : TRUE only for Vertical Orientation + Headerplacement!=Left
		if (m_bUseStickyHeaders && goForward)
		{
			// If we are realizing forward (which we do in each measure pass),
			// we store the fact that we have hit the end of the list
			m_bLastItemRealized = !inCollection;
		}
	}

	// Ensure that any pinned containers still have a reason to be pinned. If not, unpin them.
	private void PrunePinnedContainers()
	{
		// We currently have 2 reasons for keeping a container pinned:
		// 1- The container is the special item.
		// 2- The GeneratorHost tells us we can't recycle that container.
		// Verify that either still hold. If not, unpin.

		int specialItemIndex = GetSpecialItemIndex();

		var spIHost = m_cacheManager.GetItemsHost();

		m_containerManager.FilterPinnedContainers(
			(index, pElement) =>
			{
				bool keep;

				if (specialItemIndex == index)
				{
					keep = true;
				}
				else
				{
					bool canRecycle = spIHost!.CanRecycleContainer(pElement);
					keep = !canRecycle;
				}

				return keep;
			});
	}

	// Recycle containers if our recycle queue is empty and we have containers to recycle.
	private void EnsureRecycleContainerCandidate(int index, bool fromFront)
	{
		bool recycleQueueHasCandidate = false;

		int lastValidContainerIndex = m_containerManager.GetValidContainerCount() - 1;

		// is the recycle queue filled?
		var spRecyclingContext = m_cacheManager.GetContainerRecyclingContext((uint)index);

		recycleQueueHasCandidate = m_icg2.FindRecyclingCandidate(index);

		if (!recycleQueueHasCandidate && lastValidContainerIndex >= 0)
		{
			UIElement? spCandidate = null;
			UIElement? spFocusedContainer = null;

			// Index are valid in collections and therefore are navigated either incrementing from 0 or decrementing from lastValidContainerIndex
			int indexFirstRecycleCandidateInValidContainers = fromFront ? 0 : lastValidContainerIndex;
			int indexRecycleCandidateInValidContainers = indexFirstRecycleCandidateInValidContainers;
			int increment = fromFront ? 1 : -1;
			// In order to handle index changes when we remove a container, we will keep track of positions in the iteration
			// Positions are always counted incrementing from 0
			int focusedPosition = -1;
			int foundPosition = -1;
			int currentPosition = 0;
			int fallbackPosition = -1;

			bool found = false;
			bool areDisjoint = true;
			// ah, there is no candidate for recycling. let's make it happen

			var spIHost = m_cacheManager.GetItemsHost();

			while (areDisjoint && (!found) && (currentPosition < ValidContainersBeforeFallback) && m_containerManager.IsValidContainerIndexWithinBounds(indexRecycleCandidateInValidContainers))
			{
				spCandidate = m_containerManager.GetContainerAtValidIndex(indexRecycleCandidateInValidContainers);

				areDisjoint =
					GetElementIsSentinel(spCandidate) == false && // If the container hasn't been realized yet, it's considered non disjoint.
					RectUtil.AreDisjoint(GetVirtualizationInformationFromElement(spCandidate!)!.Bounds, m_viewportBehavior.isTracking ? GetRealizationWindowAfterViewportShift() : m_windowState.GetRealizationWindow());

				// if !areDisjoint, we reached the start of the virtualization window and will not find a candidate anymore
				if (areDisjoint)
				{
					// focused logic here
					// we have to check whether this container is focused. if so, we move it to some separate storage
					// and try the next container
					bool hasFocus = false;
					bool isRecyclable = false;

					hasFocus = m_cacheManager.IsFocusedChild(spCandidate!);

					isRecyclable = spIHost!.CanRecycleContainer(spCandidate!);

					var isPinned = m_containerManager.GetIsContainerPinned(spCandidate);

					if (hasFocus)
					{
						// yep, this one has focus. let's not use it.
						// potentially tough situation: what if the focused container is the only item in a group
						// and we move it out of the validcontainers, and then find that the next container (in the next)
						// group won't be recycled? Now we're left with a header that has not been cleaned up.
						// So we store the focused container and _only_ move it if we find that we want to cleanup
						// the next container

						spFocusedContainer = spCandidate;
						focusedPosition = currentPosition;
					}
					else if (!isRecyclable &&
							 !isPinned)
					{
						// pin containers which are not pinned yet but should
						m_containerManager.RegisterPinnedContainer(
							m_containerManager.GetItemIndexFromValidIndex(indexRecycleCandidateInValidContainers),
							spCandidate!);
						isPinned = true;
					}

					if (isPinned ||
						hasFocus)
					{
						// for non-eligible candidates try the next one.
						++currentPosition;
						indexRecycleCandidateInValidContainers += increment;
					}
					else
					{
						// The container at the current position is not in the realization window and does not have focus
						// Therefore it is a real candidate

						if (spRecyclingContext is not null)
						{
							// We have to check that the candidate is compatible with the context (typically its DataTemplate)
							bool isCompatible = true;

							isCompatible = spRecyclingContext.IsCompatible(spCandidate!);
							if (isCompatible)
							{
								found = true;
								foundPosition = currentPosition;
							}
							else
							{
								// Just store the current position in case we need to fallback to a container with a wrong template
								if (fallbackPosition == -1)
								{
									fallbackPosition = currentPosition;
								}

								// And try next container
								indexRecycleCandidateInValidContainers += increment;
								currentPosition++;
							}
						}
						else
						{
							// No other constraint, let's take the current candidate
							foundPosition = currentPosition;
							found = true;
						}
					}
				}
			}

			if ((!found) && spRecyclingContext is not null)
			{
				// The queue has no valid container and we don't have one either
				uint queueLength = m_icg2.GetQueueLength();
				// We won't take a "wrong DataTemplate" container from the queue in order to keep chances
				// for next search to succeed
				// But if we have many containers outside the virtualization Windows, let's recycle one
				if ((queueLength < QueueLengthBeforeFallback) && (fallbackPosition != -1) && (currentPosition >= ValidContainersBeforeFallback))
				{
					found = true;
					foundPosition = fallbackPosition;
				}
				// Else we will create a new container or it will be taken from the queue (but with a template change)
			}

			if (found)
			{
				// We need to recycle containers beetween positions 0 and foundPosition (included)
				// except the possible pinned/focused element which is handled differenty

				// If we are starting from front, then we won't have to increment the
				// current index because removing an item will slide others
				// But if we are starting from end, then removing an item will not change the indexes
				int increment2 = fromFront ? 0 : -1;
				int focusedIndexInValidContainers = -1;
				bool recycled = false;

				indexRecycleCandidateInValidContainers = indexFirstRecycleCandidateInValidContainers;

				for (int i = 0; i <= foundPosition; i++, indexRecycleCandidateInValidContainers += increment2)
				{
					// If this is the focused element, just skip it
					if (i == focusedPosition)
					{
						focusedIndexInValidContainers = indexRecycleCandidateInValidContainers;

						if (fromFront)
						{
							// We don't remove the focused container yet, therefore we have to
							// increment the index
							indexRecycleCandidateInValidContainers++;
						}

						continue;
					}
					spCandidate = m_containerManager.GetContainerAtValidIndex(indexRecycleCandidateInValidContainers);

					// we want to guarantee that there are no headers after this container, since
					// that would break 'edge' assumptions: we need to be able to trust that when we
					// get the last header, it can be treated as the 'edge' and generation can start
					// from that edge. So there should not be a header after a group that has a few
					// containers left to be recycled.
					int groupIndex = 0;
					int indexInGroup = 0;
					int countInGroup = 0;

					bool groupsAreRecycled = false;

					if (m_cacheManager.IsGrouping())
					{
						// do this before recycling the container
						m_cacheManager.GetGroupInformationFromItemIndex(
							m_containerManager.GetItemIndexFromValidIndex(indexRecycleCandidateInValidContainers),
							out groupIndex,
							out indexInGroup,
							out countInGroup);
					}

					// If we have just passed the focused container, this is the time to move out that container as well.
					// Note that "positions" are always counted up
					if ((spFocusedContainer is not null) && (i == focusedPosition + 1))
					{
						// this assumes we have cleaned up before
						// this will make sure that the focused container is 'pinned' and won't be recycled, but still
						// is not in the valid container range
						m_containerManager.SetFocusedContainer(
							m_containerManager.GetItemIndexFromValidIndex(focusedIndexInValidContainers),
							spFocusedContainer);

						m_containerManager.RemoveFromValidContainers(focusedIndexInValidContainers, false /* isForItemRemoval */, index);

						// If we are moving forward, we need to adjust the current index as we have just removed an item
						if (fromFront)
						{
							indexRecycleCandidateInValidContainers--;
						}
					}

					// A header won't recycle if it has containers, so we need to recycle the container first;
					// this means we need to be absolutely sure that we recycle the headers after this.
					// Note that TryRecycleContainer will no-op if the container is pinned.
					recycled = m_icg2.TryRecycleContainer(spCandidate!);
					m_containerManager.RemoveFromValidContainers(indexRecycleCandidateInValidContainers, false /* isForItemRemoval */, index);

					if (m_cacheManager.IsGrouping())
					{
						// now make sure that headers are cleaned up appropriately
						if (fromFront)
						{
							// recycling any container means we need to make sure
							// that any previous groups are recycled.
							// if we removed the last container in a group, we will also recycle that header
							int firstGroupIndexGoal = indexInGroup == countInGroup - 1 ? groupIndex + 1 : groupIndex;
							int firstCurrentGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(0);
							groupsAreRecycled = firstCurrentGroupIndex >= firstGroupIndexGoal;

							while (!groupsAreRecycled)
							{
								// we specify that we always want to recycle, whether there is a recycle queue or not
								// since this is about keeping our contiguousness promises
#if DEBUG
								int validHeaderCount = m_containerManager.GetValidHeaderCount();
#endif
								EnsureRecycleHeaderCandidate(true, true /* always recycle */);

#if DEBUG
								// Make sure we recycled
								global::System.Diagnostics.Debug.Assert(validHeaderCount > m_containerManager.GetValidHeaderCount());
#endif

								if (firstCurrentGroupIndex < m_containerManager.GetGroupIndexFromValidIndex(0))
								{
									// we actually were able to recycle that header
									firstCurrentGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(0);
									groupsAreRecycled = firstCurrentGroupIndex >= firstGroupIndexGoal;
								}
								else
								{
									break;
								}
							}
						}
						else
						{
							int lastGroupIndexGoal = indexInGroup == 0 ? groupIndex - 1 : groupIndex;
							int lastCurrentGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);
							groupsAreRecycled = lastCurrentGroupIndex <= lastGroupIndexGoal;
							while (!groupsAreRecycled)
							{
								// we specify that we always want to recycle, whether there is a recycle queue or not
								// since this is about keeping our contiguousness promises
#if DEBUG
								int validHeaderCount = m_containerManager.GetValidHeaderCount();
#endif
								EnsureRecycleHeaderCandidate(false, true /* always recycle */);

#if DEBUG
								// Make sure we recycled
								global::System.Diagnostics.Debug.Assert(validHeaderCount > m_containerManager.GetValidHeaderCount());
#endif

								if (lastCurrentGroupIndex > m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1))
								{
									// we actually were able to recycle that header
									lastCurrentGroupIndex = m_containerManager.GetGroupIndexFromValidIndex(m_containerManager.GetValidHeaderCount() - 1);
									groupsAreRecycled = lastCurrentGroupIndex <= lastGroupIndexGoal;
								}
								else
								{
									break;
								}
							}
						}

						global::System.Diagnostics.Debug.Assert(groupsAreRecycled);  // This should _never_ fire. if it does, we're in trouble
					}
				}
				if (recycled && spRecyclingContext is not null)
				{
					// The last recycled container is the one we were looking for, and we have been able to recycle it
					spRecyclingContext.SelectedContainer = spCandidate;
				}
			}
		}
	}

	// Recycle headers if our recycle queue is empty and we have headers to recycle.
	// alwaysRecycle: even if we have elements in the recyclequeue, still recycle.
	private void EnsureRecycleHeaderCandidate(bool fromFront, bool alwaysRecycle)
	{
		// only ever recycle empty headers (no containers left)

		bool doRecycle = true;

		// is the recycle queue filled? we might not care
		if (!alwaysRecycle)
		{
			// if we are not forced to recycle, then only recycle if the queue is empty.
			// otherwise during generation we will just take one out of the queue
			doRecycle = m_icg2.GetHeaderRecycleQueueEmpty();
		}

		if (doRecycle && m_containerManager.GetValidHeaderCount() > 0)
		{
			int indexRecycleCandidateInValidHeaders = fromFront ? 0 : m_containerManager.GetValidHeaderCount() - 1;

			// ah, there is no candidate for recycling. let's make it happen

			// can only recycle a header that is representing a group that has no valid containers
			// so we check the container at the edge and determine that it is not in this group.
			// that basically means that this group is empty and thus can be recycled.
			int groupIndex = -1;

			if (m_containerManager.GetValidContainerCount() > 0)
			{
				m_cacheManager.GetGroupInformationFromItemIndex(
					m_containerManager.GetItemIndexFromValidIndex(fromFront ? 0 : m_containerManager.GetValidContainerCount() - 1),
					out groupIndex,
					out _,
					out _);
			}

			if (groupIndex != m_containerManager.GetGroupIndexFromValidIndex(indexRecycleCandidateInValidHeaders))
			{
				// the first (or last) container is not in the group that we are considering recycling.
				var spCandidate = m_containerManager.GetHeaderAtValidIndex(indexRecycleCandidateInValidHeaders);

				// UGLY! We really need to generalize this notion of generalized pinning and recycling
				// But for now, this special case stays...
				while (spCandidate is null)
				{
					m_containerManager.RemoveFromValidHeaders(indexRecycleCandidateInValidHeaders, false /* isForGroupRemoval */);
					indexRecycleCandidateInValidHeaders = fromFront ? 0 : m_containerManager.GetValidHeaderCount() - 1;
					spCandidate = m_containerManager.GetHeaderAtValidIndex(indexRecycleCandidateInValidHeaders);
				}

				if (alwaysRecycle || RectUtil.AreDisjoint(
					GetBoundsFromElement(spCandidate),
					m_viewportBehavior.isTracking ? GetRealizationWindowAfterViewportShift() : m_windowState.GetRealizationWindow()))
				{
					if (m_viewportBehavior.isTracking &&
						m_viewportBehavior.type == ElementType.GroupHeader &&
						m_containerManager.GetValidHeaderIndexFromGroupIndex(m_viewportBehavior.index) == indexRecycleCandidateInValidHeaders)
					{
						// We are about to recycle a tracked header.
						// Let's cancel tracking because we don't want GetTrackedElement to later fail.
						m_viewportBehavior.isTracking = false;
					}

					// no intersection, so free to recycle
					m_icg2.RecycleHeader(spCandidate);
					m_containerManager.RemoveFromValidHeaders(indexRecycleCandidateInValidHeaders, false /* isForGroupRemoval */);
				}
			}
		}
	}

	// When generating or measuring backward, this method is used when we need to set the bounds for
	// a header of a non-empty group and the first container of that group haven't been realized yet.
	private void EstimateFloatingHeaderLocation(UIElement spFloatingHeader)
	{
		if (spFloatingHeader is not null)
		{
			// We feel it is important to first generate a header for a group and then generate containers.
			// But this means that when we generated the header, we did not yet have container for itemInGroup 0.
			// So we have placed it wrongly. When generating container for itemInGroup 0, we will have known exactly how to place and
			// there is code that will do that. If we did not end up resetting spFloatingHeader, it means that the realizationwindow
			// stopped us from generating that container, and thus we end up here.

			// Let's estimate where to place this header for now, based on the closest available item
			Rect headerBounds;
			VirtualizationInformation p_containerVirtualizationInfo;
			VirtualizationInformation p_headerVirtualizationInfo;

			global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidHeaderCount() > 0 && m_containerManager.GetValidContainerCount() > 0);

			p_headerVirtualizationInfo = GetVirtualizationInformationFromElement(spFloatingHeader)!;
			global::System.Diagnostics.Debug.Assert(p_headerVirtualizationInfo.IsRealized);
			headerBounds = p_headerVirtualizationInfo.Bounds;

			var spReferenceContainer = m_containerManager.GetContainerAtValidIndex(0);
			p_containerVirtualizationInfo = GetVirtualizationInformationFromElement(spReferenceContainer!)!;

			int itemIndex = m_containerManager.GetItemIndexFromValidIndex(0);
			m_cacheManager.GetGroupInformationFromItemIndex(itemIndex, out int groupIndexOfItem, out int indexInGroup, out int itemCountInGroup);

			global::System.Diagnostics.Debug.Assert(groupIndexOfItem == m_containerManager.GetGroupIndexFromValidIndex(0)); // Make sure we are placing the header we think we should be
			global::System.Diagnostics.Debug.Assert(indexInGroup != 0);              // And that we didn't already have our chance to do so

			// we need to find the distance the header would have to the first container that we _are_ showing
			// TODO: this should probably take into account the item's location in the group. For now, we just go with average group sizes
			EstimationReference headerRef = CreateDefaultEstimationReference(ElementType.GroupHeader);
			headerRef.ElementIndex = m_cacheManager.DataIndexToLayoutIndex(ElementType.GroupHeader, groupIndexOfItem);
			headerRef.ElementBounds = p_headerVirtualizationInfo.Bounds;

			EstimationReference containerRef = CreateDefaultEstimationReference(ElementType.ItemContainer);
			containerRef.ElementIndex = itemIndex;
			containerRef.ElementBounds = p_containerVirtualizationInfo.Bounds;

			headerBounds = m_spLayoutStrategy.EstimateElementBounds(
				ElementType.GroupHeader,
				headerRef.ElementIndex,
				headerRef,
				containerRef,
				m_windowState.GetRealizationWindow());

			// since we called a grouping method, the non-virtualizingdirection has been nulled out probably.
			p_headerVirtualizationInfo.Bounds = headerBounds;
		}
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		Size finalSize = arrangeSize;
		Rect adjustedVisibleWindow;
		bool isIgnoring = false;
		bool forceScrollViewerUpdate = false; // Header's size change may require the SV to be updated

		ScrollViewer? spScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out spScrollViewer);

		try
		{
			if (!IsItemsHostRegistered())
			{
				return default;
			}

			if (spScrollViewer is not null)
			{
				isIgnoring = IsIgnoringTransitions;
				if (!isIgnoring && spScrollViewer.IsInManipulation)
				{
					// make sure not to setup transitions during DM
					IsIgnoringTransitions = true;
					isIgnoring = true;
				}

				if (IsMaintainViewportSupportedAndEnabled())
				{
					double newHeaderHeight = spScrollViewer.HeaderHeight;
					if (!DoubleUtil.AreClose(m_currentHeaderHeight, newHeaderHeight))
					{
						// Store the new value (which will be used to update sticky headers curves)
						m_currentHeaderHeight = newHeaderHeight;
						// And force the update of the ScrollViewer if we are not at the top
						// of the list
						forceScrollViewerUpdate = (m_windowState.GetVisibleWindow().Y > 0.0);
					}
				}
			}

			// At this point we won't have gotten any ViewChanging or ViewChanged events, so we need to get a window ourselves.
			PrimeVisibleWindow(true /* allowTrackingOnViewportSizeChange */);
			SetRealizationWindowFromVisibleWindow(false);

			int oldFirstVisibleIndexBase = FirstVisibleIndexBase;
			int oldLastVisibleIndexBase = LastVisibleIndexBase;

			EndTrackingFirstVisibleElement();

			// Allow us to avoid ambiguity due to edge overlay between elements.
			adjustedVisibleWindow = m_windowState.GetVisibleWindow();
			CoerceWindowToExtent(ref adjustedVisibleWindow);

			SetPointFromRectInVirtualizingDirection(ref adjustedVisibleWindow, PointFromRectInVirtualizingDirection(adjustedVisibleWindow) + EdgeOverlayDisambiguationDelta);
			SetSizeFromRectInVirtualizingDirection(ref adjustedVisibleWindow, SizeFromRectInVirtualizingDirection(adjustedVisibleWindow) - EdgeOverlayDisambiguationDelta * 2);
			if (SizeFromRectInVirtualizingDirection(adjustedVisibleWindow) < 0)
			{
				SetSizeFromRectInVirtualizingDirection(ref adjustedVisibleWindow, 0);
			}
			// make sure that the non-virtualizing direction is not taken into account
			SetPointFromRectInNonVirtualizingDirection(ref adjustedVisibleWindow, 0);

			// going to do 3 walks: headers, containers and garbage
			// not checking for sentinels because they should have been removed during measure
			ArrangeElements(ElementType.GroupHeader, finalSize, adjustedVisibleWindow);

			// Protected by Apiset : TRUE only for Vertical Orientation
			if (m_bUseStickyHeaders)
			{
				adjustedVisibleWindow.Y += m_lastVisibleWindowClippingHeight;
				adjustedVisibleWindow.Height -= m_lastVisibleWindowClippingHeight;
			}

			ArrangeElements(ElementType.ItemContainer, finalSize, adjustedVisibleWindow);

			var spChildren = m_cacheManager.GetChildren();
			int childrenCount = spChildren.Count;

			for (int garbageIndex = m_containerManager.StartOfGarbageSection(); garbageIndex < childrenCount; ++garbageIndex)
			{
				var spCurrentElement = spChildren[garbageIndex];

				// Gets the arrange bounds for the element.
				Rect arrangedBounds = m_spLayoutStrategy.GetElementArrangeBounds(
					GetElementIsHeader(spCurrentElement) ?
						ElementType.GroupHeader :
						ElementType.ItemContainer,
					-1,
					GetBoundsFromElement(spCurrentElement),
					adjustedVisibleWindow,
					finalSize);

#if !HAS_UNO // TODO Uno: no core layout transition state on UIElement.
				// make sure transitions do not play.
				CoreImports.UIElement_SetIsEntering(spCurrentElement, false);

				CoreImports.UIElement_SetCurrentTransitionLocation(
					spCurrentElement,
					arrangedBounds.X,
					arrangedBounds.Y,
					arrangedBounds.Width,
					arrangedBounds.Height);
#endif

				spCurrentElement.Arrange(arrangedBounds);
			}

			// use these values to update the panning direction
			int tick = CoreImports.LayoutManager_GetLayoutTickForTransition(this);
			if (tick != m_windowState.currentTickNumber)
			{
				// aha, the tick has increased, we can now set values
				int previousVisibleIndex = FirstVisibleIndexBase;
				m_windowState.firstVisibleItemIndexOfPreviousTick = previousVisibleIndex;
				m_windowState.currentTickNumber = tick;

				PanningDirectionBase = PanelScrollingDirection.None;
			}

			int newFirstVisibleIndex = FirstVisibleIndexBase;
			int newLastVisibleIndex = LastVisibleIndexBase;

			// compare them
			if (m_windowState.firstVisibleItemIndexOfPreviousTick > -1 && m_windowState.firstVisibleItemIndexOfPreviousTick != newFirstVisibleIndex)
			{
				PanningDirectionBase =
					m_windowState.firstVisibleItemIndexOfPreviousTick < newFirstVisibleIndex ?
					PanelScrollingDirection.Forward : PanelScrollingDirection.Backward;
			}

			if (oldFirstVisibleIndexBase != newFirstVisibleIndex ||
				oldLastVisibleIndexBase != newLastVisibleIndex)
			{
				VisibleIndicesUpdated?.Invoke(this, null!);
			}


			if (m_bUseStickyHeaders)
			{
				int firstVisibleGroupIndex = FirstVisibleGroupIndexBase;
				if (firstVisibleGroupIndex != -1)
				{
					int firstVisibleHeaderValidIndex = m_containerManager.GetValidHeaderIndexFromGroupIndex(firstVisibleGroupIndex);

					// walk backwards from first visible group index.
					for (var validHeaderIndex = firstVisibleHeaderValidIndex;
						validHeaderIndex >= 0;
						--validHeaderIndex)
					{

						global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidHeaderIndexWithinBounds(validHeaderIndex));
						int groupDataIndex = m_containerManager.GetGroupIndexFromValidIndex(validHeaderIndex);
						m_cacheManager.GetGroupInformationFromGroupIndex(groupDataIndex, out int groupStartItemIndex, out int groupItemCount);

						bool isGamepadFocusCandidate = true;
						// empty group headers are always candidates
						if (groupItemCount != 0)
						{
							if (groupDataIndex == firstVisibleGroupIndex)
							{
								// if the group header and its first item are in the visible viewport. The
								// header is a candidate. If the first item is not visible, we should not be able
								// to focus on the header.
								isGamepadFocusCandidate = (groupStartItemIndex >= newFirstVisibleIndex);
							}
							else
							{
								// non-empty headers above the first visible group are not candidates
								isGamepadFocusCandidate = false;
							}
						}

						if (!isGamepadFocusCandidate)
						{
							// not a focus candidate
							var spHeader = m_containerManager.GetHeaderAtValidIndex(validHeaderIndex);
							spHeader!.IsGamepadFocusCandidate = isGamepadFocusCandidate;
						}
					}
				}
			}

			UpdateSnapPoints();

			// We should have processed our command by now.
			global::System.Diagnostics.Debug.Assert(m_windowState.m_command is null);

			// At this point, we have executed our window management command, and should be safe to remove it.
			m_windowState.m_command = null;

			// If we have a coerced window, let's send the new offsets to the ScrollViewer
			// This will trigger viewChanged/Changing events. If the new window coincides
			// with the desired coerced window, no measure invalidation should happen in our handlers.
			// If for some reason, it is different because the ScrollViewer couldn't honor our request,
			// the measure invalidate will occur and will get another chance to perform layout at
			// the final window location.
			if (m_windowState.HasCoercedWindow())
			{
				bool issued = SetScrollViewerOffsetsTo(m_windowState.GetCoercedVisibleWindow(), false /*animate*/);

				// Clear out the coerced window so our next layout pass can start from the raw scroll state
				// only clear out the coerced window if we were succesful scrolling to the offset
				if (issued)
				{
					m_windowState.ClearCoercedWindow();
				}
			}
			else if (forceScrollViewerUpdate)
			{
				// Force the ScrollViewer to display the correct window
				SetScrollViewerOffsetsTo(m_windowState.GetVisibleWindow(), false /*animate*/);
			}

			m_windowState.CorrectVisibleWindowForMouseLargeClick(this);

			TraceVirtualizationData();

			// Alignment is being taken into account on the core. That means that the DesiredSize that you
			// returned in Measure is taken as an input and compared to the AlignmentProperties. If you have
			// alignment of stretch in the non-virtualizing direction, you get that final size and you have to
			// deal with it. If you have any other alignment, you will get your desiredsize.
			// In our case, we just happen to start our elements from an imaginary (0,0) point. So in the case of
			// stretch, where we get more than needed, we could place the elements somewhere in between. However,
			// we do not do that, since we feel adding childrenalignment is not needed to position children:
			// you can tweak the alignment of the panel itself. PM felt that background on the panel is not too
			// widely used.

			// Returning a bigger size than actually used is absolutely possible and is needed to trigger the core
			// to setup layout clipping. So we should never lie to layout.

			// the desiredsize is the output of GetPanelExtent. Instead of rerunning that command (which does a loop), just read
			// it back.
			Size result = DesiredSize;

			// and then account for stretch: if we need more size than stretch can give us, happily return that. Otherwise act as though
			// we took up that space and be happy with our implicit children alignment of top-left.
			result.Width = Math.Max(result.Width, finalSize.Width);
			result.Height = Math.Max(result.Height, finalSize.Height);

			// inform the host that virtualization is finished
			if (IsItemsHostRegistered())
			{
				var spItemsHost = m_cacheManager.GetItemsHost();
				spItemsHost!.VirtualizationFinished();
			}

			// We track the unloading elements for the duration of one layout pass only.
			// The reason for that is that we currently only need them for viewport tracking
			// in order to adjust theme transitions.
			m_unloadingElements.Clear();

			return result;
		}
		finally
		{
			IsIgnoringTransitions = false;
		}
	}

	// returns an array of indices of all pinned elements by type
	internal List<uint> GetPinnedElementsIndexVector(ElementType type)
		=> m_containerManager.GetPinnedElementsIndexVector(type);

	// Arrange all elements of a given type
	private void ArrangeElements(ElementType type, Size finalSize, Rect adjustedVisibleWindow)
	{
		int firstCacheIndex = -1;
		int firstVisibleIndex = -1;
		int lastVisibleIndex = -1;
		int lastCacheIndex = -1;

		// While we arrange group headers, we need to account for sticky headers
		// when deciding what group header is visible first. We also calculate
		// how much height sticky headers are currently taking away from the
		// visible window. Using this value, we make sure the first visible item
		// we pick is not completely clipped away by sticky headers.
		// This will allow us to get an accurate value for the FirstVisibleIndex and
		// FirstVisibleGroupIndex properties.
		float heightOfActiveSickyHeader = 0.0f;
		float offsetOfFirstHeaderAfterStickyHeader = float.PositiveInfinity;

		var elementCount = m_containerManager.GetValidElementCount(type);

		// for item containers (where LiveReorder is only supported), we get the items host
		// to query for the bounds if an arrange is happening during a LiveReorder
		IGeneratorHost? spItemsHost = null;
		if (type == ElementType.ItemContainer &&
			IsItemsHostRegistered())
		{
			spItemsHost = m_cacheManager.GetItemsHost();
		}

		// Clear out old arrange rects
		bool needsElementRects = m_spLayoutStrategy.HasIrregularSnapPoints(type);

		m_arrangeRects[(int)type].Clear();
		if (needsElementRects)
		{
			m_arrangeRects[(int)type].EnsureCapacity(elementCount);
		}

		for (int validElementIndex = 0; validElementIndex < elementCount; ++validElementIndex)
		{
			var spCurrentElement = m_containerManager.GetElementAtValidIndex(type, validElementIndex);
			if (spCurrentElement is not null)
			{
				var dataIndex = m_containerManager.GetDataIndexFromValidIndex(type, validElementIndex);
				var layoutIndex = m_cacheManager.DataIndexToLayoutIndex(type, dataIndex);

				Rect arrangedBounds = m_spLayoutStrategy.GetElementArrangeBounds(
					type,
					layoutIndex,
					GetBoundsFromElement(spCurrentElement),
					adjustedVisibleWindow,
					finalSize);

				var insideVisibleWindow = !RectUtil.AreDisjoint(arrangedBounds, adjustedVisibleWindow);

				// in the case of a first arrange, our window will have dimensions of 0. In this case though,
				// insideVisibileWindow will be false for the first container that comes after a header. We'll
				// come back to it soon when a next arrange comes in with actual window dimensions, but transitions won't
				// be able to be setup any more after that since arrange won't be dirty anymore for that container.
				// So when we see a window without proper dimensions, we are just going to assume we are inside the Visible Window
				if (SizeFromRectInVirtualizingDirection(adjustedVisibleWindow) < 5.0)
				{
					insideVisibleWindow = true;
				}

				if (firstCacheIndex == -1)
				{
					firstCacheIndex = dataIndex;
				}
				if (firstVisibleIndex == -1 && insideVisibleWindow)
				{
					firstVisibleIndex = dataIndex;
				}
				if (lastVisibleIndex < dataIndex && insideVisibleWindow)
				{
					lastVisibleIndex = dataIndex;
				}
				if (lastCacheIndex < dataIndex)
				{
					lastCacheIndex = dataIndex;
				}

#if !HAS_UNO // TODO Uno: no core layout transition state on UIElement.
				if (!insideVisibleWindow)
				{
					// We don't play transitions only if the container isn't in the visible window
					// and didn't use to in the previous layout pass either.
					var container = spCurrentElement;
					var currentOffset = container.GetLayoutStorage().m_offset;
					var currentSize = container.GetLayoutStorage().m_size;
					bool wasInsideVisibleWindow =
						currentSize.width != 0.0f &&
						currentSize.height != 0.0f &&
						!RectUtil.AreDisjoint(
							adjustedVisibleWindow,
							new Rect(currentOffset.x, currentOffset.y, currentSize.width, currentSize.height));

					if (wasInsideVisibleWindow == false)
					{
						// Make sure transitions do not play
						CoreImports.UIElement_SetIsEntering(spCurrentElement, false);
						CoreImports.UIElement_CancelTransition(spCurrentElement);

						CoreImports.UIElement_SetCurrentTransitionLocation(
							spCurrentElement,
							arrangedBounds.X,
							arrangedBounds.Y,
							arrangedBounds.Width,
							arrangedBounds.Height);
					}
				}
#endif

				// query the host for arrange bounds
				// during LiveReorder, containers already have a bound to go to
				if (spItemsHost is not null)
				{
					arrangedBounds = spItemsHost.OverrideContainerArrangeBounds(dataIndex, arrangedBounds);
				}
				spCurrentElement.Arrange(arrangedBounds);

				// After arranging the element, if it is a sticky header go ahead and set bounds on the
				// sticky header wrapper - which will position the LTE if required. Note that this has to be done
				// after arrange since SetDesiredBounds looks up the ActualOffsetX from the element which is only valid
				// after arrange.
				if (type == ElementType.GroupHeader && m_bUseStickyHeaders)
				{
					if (arrangedBounds.Y <= adjustedVisibleWindow.Y)
					{
						firstVisibleIndex = dataIndex;
						heightOfActiveSickyHeader = (float)arrangedBounds.Height;
					}
					else if (offsetOfFirstHeaderAfterStickyHeader == float.PositiveInfinity)
					{
						offsetOfFirstHeaderAfterStickyHeader = (float)arrangedBounds.Y;
					}

					if (GetVirtualizationInformationFromElement(spCurrentElement)!.IsSticky)
					{
						GetVirtualizationInformationFromElement(spCurrentElement)!.m_stickyHeaderWrapper.SetDesiredBounds(arrangedBounds);

						// Start by considering all sticky headers as valid candidates for auto-focus. At the end of arrange
						// we look at the first visible header and decide if we want to set the property to FALSE.
						UIElement stickyHeader = spCurrentElement;
						stickyHeader.IsGamepadFocusCandidate = true;
					}
				}

				if (needsElementRects)
				{
					bool hasSnapPointOnElement = m_spLayoutStrategy.HasSnapPointOnElement(type, layoutIndex);
					if (hasSnapPointOnElement)
					{
						global::System.Diagnostics.Debug.Assert(m_arrangeRects[(int)type].Count < m_arrangeRects[(int)type].Capacity);
						m_arrangeRects[(int)type].Add(arrangedBounds);
					}
				}
			}
		}

		// Now that all the headers have been arranged, let's make sure their sticky curves get updated
		if (type == ElementType.GroupHeader && m_bUseStickyHeaders)
		{
			UpdateStickyHeaders(finalSize);

			// A stickied header may affect the set of visible group headers
			// May happen when, for example, looking at the middle of a large group.
			// The sticky header will be the first and last visible group header in that case.
			if (lastVisibleIndex == -1 && firstVisibleIndex != -1)
			{
				lastVisibleIndex = firstVisibleIndex;
			}

			// The sticky header may obscure an item, so we need to account for that when determining which items are in the visible window
			m_lastVisibleWindowClippingHeight = Math.Min(heightOfActiveSickyHeader, offsetOfFirstHeaderAfterStickyHeader - (float)m_windowState.GetVisibleWindow().Y);
		}

		switch (type)
		{
			case ElementType.GroupHeader:
				FirstCacheGroupIndexBase = firstCacheIndex;
				FirstVisibleGroupIndexBase = firstVisibleIndex;
				LastVisibleGroupIndexBase = lastVisibleIndex;
				LastCacheGroupIndexBase = lastCacheIndex;
				break;

			case ElementType.ItemContainer:
				FirstCacheIndexBase = firstCacheIndex;
				FirstVisibleIndexBase = firstVisibleIndex;
				LastVisibleIndexBase = lastVisibleIndex;
				LastCacheIndexBase = lastCacheIndex;
				break;
		}
	}

	// Output data desired by ETW tracing
	private void TraceVirtualizationData()
	{
#if !HAS_UNO // TODO Uno: no ETW provider.
		// Put these checks into separate variables to deal with a prefast error for
		// warning 6287: "redundant code: the left and right sub-expressions are identical"
		bool eventEnabledVirtualizedCollectionUpdatedInfo = EventEnabledVirtualizedCollectionUpdatedInfo();
		bool eventEnabledVirtualizedCollectionBoundsInfo = EventEnabledVirtualizedCollectionBoundsInfo();

		if (eventEnabledVirtualizedCollectionUpdatedInfo || eventEnabledVirtualizedCollectionBoundsInfo)
		{
			ScrollViewer? spScrollViewer = null;
			m_wrScrollViewer?.TryGetTarget(out spScrollViewer);
			if (spScrollViewer is not null)
			{
				Rect contentBounds = new Rect { X = float.PositiveInfinity, Y = float.PositiveInfinity, Width = 0, Height = 0 };
				Rect realizedItemBounds = new Rect { X = float.PositiveInfinity, Y = float.PositiveInfinity, Width = 0, Height = 0 };
				bool hasPlaceholders = false;

				double viewportWidth = spScrollViewer.ViewportWidth;
				double viewportHeight = spScrollViewer.ViewportHeight;
				double extentWidth = spScrollViewer.ExtentWidth;
				double extentHeight = spScrollViewer.ExtentHeight;

				for (int typeIndex = 0; typeIndex < ElementType_Count; ++typeIndex)
				{
					ElementType type = (ElementType)typeIndex;
					for (int validIndex = 0; validIndex < m_containerManager.GetValidElementCount(type); ++validIndex)
					{
						var spElement = m_containerManager.GetElementAtValidIndex(type, validIndex);
						if (spElement is not null)
						{
							var bounds = GetBoundsFromElement(spElement);

							if (eventEnabledVirtualizedCollectionUpdatedInfo)
							{
								contentBounds.X = Math.Min(bounds.X, contentBounds.X);
								contentBounds.Y = Math.Min(bounds.Y, contentBounds.Y);
								contentBounds.Width = Math.Max(bounds.X + bounds.Width - contentBounds.X, contentBounds.Width);
								contentBounds.Height = Math.Max(bounds.Y + bounds.Height - contentBounds.Y, contentBounds.Height);
							}

							if (type == ElementType.ItemContainer && eventEnabledVirtualizedCollectionBoundsInfo)
							{
								var spSelectorItem = spElement as SelectorItem;
								bool isPlaceholder = spSelectorItem is not null && spSelectorItem.GetIsUIPlaceholder();

								hasPlaceholders = isPlaceholder || hasPlaceholders;
								if (!isPlaceholder)
								{
									realizedItemBounds.X = Math.Min(bounds.X, realizedItemBounds.X);
									realizedItemBounds.Y = Math.Min(bounds.Y, realizedItemBounds.Y);
									realizedItemBounds.Width = Math.Max(bounds.X + bounds.Width - realizedItemBounds.X, realizedItemBounds.Width);
									realizedItemBounds.Height = Math.Max(bounds.Y + bounds.Height - realizedItemBounds.Y, realizedItemBounds.Height);
								}
							}
						}
					}
				}

				TraceVirtualizedCollectionUpdatedInfo(
					spScrollViewer,
					DoubleUtil.IsPositiveInfinity(contentBounds.X) ? 0 : contentBounds.X,
					DoubleUtil.IsPositiveInfinity(contentBounds.Y) ? 0 : contentBounds.Y,
					contentBounds.Width,
					contentBounds.Height,
					(float)viewportWidth,
					(float)viewportHeight,
					(float)extentWidth,
					(float)extentHeight,
					hasPlaceholders
					);

				if (hasPlaceholders)
				{
					TraceVirtualizedCollectionBoundsInfo(
						spScrollViewer,
						DoubleUtil.IsPositiveInfinity(realizedItemBounds.X) ? 0 : realizedItemBounds.X,
						DoubleUtil.IsPositiveInfinity(realizedItemBounds.Y) ? 0 : realizedItemBounds.Y,
						realizedItemBounds.Width,
						realizedItemBounds.Height
						);
				}
			}
		}
#endif
	}

	// Creates a default layout reference.
	private LayoutReference CreateDefaultLayoutReference() => default;

	// Creates a default (aka invalid) estimation reference.
	private EstimationReference CreateDefaultEstimationReference(ElementType elementType)
	{
		EstimationReference reference = default;
		reference.ElementType = elementType;
		reference.ElementIndex = -1;
		reference.ElementBounds = default;
		return reference;
	}

	private void OnPanelUnloaded(object pSender, RoutedEventArgs pArgs)
	{
		DetachPanelComponents();

		// invalidate, so that we are assured of getting a measure when we are put back into the tree
		InvalidateMeasure();
	}

	// Scroll by one visible viewport length,
	// forcing virtualization to run.
	// toIncreasingIndex - Governs the direction of scroll.
	private void SynchronousScrollByPage(bool toIncreasingIndex)
	{
		Rect targetRect = m_windowState.GetVisibleWindow();
		float pageMovementFactor = toIncreasingIndex ? 1.0f : -1.0f;

		var orientation = m_spLayoutStrategy.VirtualizationDirection;
		if (orientation == Orientation.Horizontal)
		{
			targetRect.X += pageMovementFactor * targetRect.Width;
		}
		else
		{
			targetRect.Y += pageMovementFactor * targetRect.Height;
		}

		ScrollRectIntoView(targetRect, true /* forceSynchronous */);

		// When using sticky headers, adjust for the sticky header
		// overlapping over items, but moving the rect by the amount the sticky header
		// overlaps the content. This amount is the same as the amount by which the real
		// header is outside the visible window. Notice that in the case of sticky headers
		// we perfor two scroll rect into view calls because in order to calculate the adjustment
		// amount, we first need to scroll by a page, then find the sticky header and then
		// calculate.
		// true only for vertical
		if (m_bUseStickyHeaders)
		{
			float actualHeaderOutsideVisibleWindow = GetRealHeaderExtentOutsideVisibleWindow();
			targetRect.Y -= actualHeaderOutsideVisibleWindow * pageMovementFactor;
			ScrollRectIntoView(targetRect, true /* forceSynchronous */);
		}
	}

	// Calculates the last visible index, from either direction.
	// This is usually the last fully visible index, but if
	// there is no fully visible index, this returns the last
	// partly visible index.
	// fromLowerIndex - direction the search starts from. I.E., with a horizontal orientation,
	// fromLowerIndex = TRUE means the rightmost visible container.
	private void GetLastVisibleContainer(bool fromLowerIndex, bool searchForItems, out uint lastVisibleContainerIndex, out bool found)
	{
		// Find the last/first visible index by walking each realized item from the appropriate end of the realized containers.
		int idxOfFirstContainer = 0;
		int idxOfLastContainer = 0;
		int startingIndex = 0;
		int step = fromLowerIndex ? -1 : 1;
		int firstPartlyVisibleValidIndex = 0;
		int targetValidIndex = 0;
		bool foundSomething = false;
		bool foundAtLeastOnePartlyVisibleContainer = false;
		Rect visibleWindow = m_windowState.GetVisibleWindow();

		// adjust the visible window for sticky headers
		// this makes sure we have a more accurate index when
		// scrolling back up.
		if (m_bUseStickyHeaders)
		{
			visibleWindow.Y += m_lastVisibleWindowClippingHeight;
			visibleWindow.Height -= m_lastVisibleWindowClippingHeight;
		}

		lastVisibleContainerIndex = 0;

		found = false;

		var virtDirection = m_spLayoutStrategy.VirtualizationDirection;

		idxOfLastContainer = ((searchForItems) ? m_containerManager.GetValidContainerCount() : m_containerManager.GetValidHeaderCount()) - 1;
		startingIndex = fromLowerIndex ? idxOfLastContainer : idxOfFirstContainer;

		for (int currentValidIndex = startingIndex;
			 currentValidIndex >= idxOfFirstContainer && currentValidIndex <= idxOfLastContainer;
			 currentValidIndex += step)
		{
			UIElement? spChild;
			Rect childBounds;

			if (searchForItems)
			{
				spChild = m_containerManager.GetContainerAtValidIndex(currentValidIndex);
			}
			else
			{
				spChild = m_containerManager.GetHeaderAtValidIndex(currentValidIndex);
			}

			childBounds = GetBoundsFromElement(spChild!);

			if (RectUtil.AreDisjoint(childBounds, visibleWindow))
			{
				// Container is completely disjoint from the visible window- go to the next container.
				continue;
			}

			// To handle containers taller than the nonvirtualizing direction, we constrain
			// the nonvirtualizing container bounds to the nonvirtualizing extent.
			if (virtDirection == Orientation.Horizontal)
			{
				childBounds.Y = Math.Max(visibleWindow.Y, Math.Min(childBounds.Y, 0.0f));
				childBounds.Height = Math.Min(childBounds.Height, visibleWindow.Height - childBounds.Y);
			}
			else
			{
				childBounds.X = Math.Max(visibleWindow.X, Math.Min(childBounds.X, 0.0f));
				childBounds.Width = Math.Min(childBounds.Width, visibleWindow.Width - childBounds.X);
			}

			if (RectUtil.ContainsFully(visibleWindow, childBounds))
			{
				targetValidIndex = currentValidIndex;
				foundSomething = true;
				break;
			}
			else if (foundAtLeastOnePartlyVisibleContainer)
			{
				if (RectUtil.AreDisjoint(visibleWindow, childBounds))
				{
					// We've been seeing partly visible containers, but no full intersections.
					// However, we've seen a completely invisible container. This means we're
					// at a set of two consecutive items, both of which are partly visible.
					// So, return the last partly visible container.
					foundSomething = true;
					targetValidIndex = currentValidIndex;
					break;
				}
				else
				{
					// Continue searching.
				}
			}
			else
			{
				// Haven't found any partly visible containers yet.
				if (!RectUtil.AreDisjoint(visibleWindow, childBounds))
				{
					foundAtLeastOnePartlyVisibleContainer = true;
					firstPartlyVisibleValidIndex = currentValidIndex;
				}
				else
				{
					// Continue searching.
				}
			}
		}

		if (foundSomething ||
			foundAtLeastOnePartlyVisibleContainer)
		{
			int lastVisibleIndex = (foundSomething) ? targetValidIndex : firstPartlyVisibleValidIndex;

			if (searchForItems)
			{
				global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidContainerIndexWithinBounds(lastVisibleIndex));
				lastVisibleContainerIndex = (uint)m_containerManager.GetItemIndexFromValidIndex(lastVisibleIndex);
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(m_containerManager.IsValidHeaderIndexWithinBounds(lastVisibleIndex));
				lastVisibleContainerIndex = (uint)m_containerManager.GetGroupIndexFromValidIndex(lastVisibleIndex);
			}

			found = true;
		}
		else
		{
			// Nothing was found in the view, so default to either first or last container in the entire collection.
			if (fromLowerIndex)
			{
				if (searchForItems)
				{
					lastVisibleContainerIndex = (uint)(m_cacheManager.GetTotalItemCount() - 1);
				}
				else
				{
					lastVisibleContainerIndex = (uint)(m_cacheManager.GetTotalGroupCount() - 1);
				}
			}
		}
	}

	// Get the closest element information to the point.
	internal ElementInfo GetClosestElementInfo(Point position)
	{
		ElementInfo returnValue = default;

		// Basic idea:
		// 1- Find a reference container so we can anchor the search.
		// 1a- If grouping, try to find group containing point by walking group headers. Then, grab the first realized container within the group.
		// 1b- If not grouping, grab the first realized container
		// TODO: Optimize which realized container we use, invoking estimation APIs.
		// -- jump to layout strategy -- Given: Index of item within group + point relative to either first item in group or to header.
		// 2- Do math to figure out which column the point is in.
		// 3- Knowing column and row height, figure out closest index.

		int targetItemIndex = -1;
		IndexSearchHint searchHint = IndexSearchHint.NoHint;
		IndexSearchHint previousHint = IndexSearchHint.NoHint;

		returnValue.m_childIndex = -1;

		FindValidContainerInGroupForPoint(position, out bool groupWasEmpty, out int referenceGroupIndex, out int referenceItemIndex, out int referenceItemIndexInGroup, out Rect referenceItemBounds);

		if (!groupWasEmpty)
		{
			int sizeOfGroup = 0;

			if (m_cacheManager.IsGrouping())
			{
				m_cacheManager.GetGroupInformationFromGroupIndex(referenceGroupIndex, out _ /* pStartItemIndex */, out sizeOfGroup);
			}
			else
			{
				sizeOfGroup = m_cacheManager.GetTotalItemCount();
			}

			do
			{
				UIElement? spContainer = null;
				int validContainerIndex = m_containerManager.GetValidContainerIndexFromItemIndex(referenceItemIndex);

				if (m_containerManager.IsValidContainerIndexWithinBounds(validContainerIndex))
				{
					// it was within the bounds. We can try to get a container
					spContainer = m_containerManager.GetContainerAtValidIndex(validContainerIndex);
				}
				else
				{
					// that is not within the bounds.
					// this can happen when we have just inserted a couple of elements at the beginning of the collection. There wouldn't have been
					// sentinels, and we wouldn't be getting a valid range.
				}

				// if spcontainer is null, we could be dealing with a sentinel or we've fallen outside of the valid range
				if (spContainer is not null)
				{
					EstimationReference containerReference = CreateDefaultEstimationReference(ElementType.ItemContainer);
					containerReference.ElementIndex = referenceItemIndex;
					containerReference.ElementBounds = GetBoundsFromElement(spContainer);

					m_spLayoutStrategy.EstimateIndexFromPoint(
						false /* requestingInsertionIndex */,
						position,
						containerReference,
						m_windowState.GetRealizationWindow(),
						out searchHint,
						out ElementType targetElementType,
						out targetItemIndex);

					global::System.Diagnostics.Debug.Assert(targetElementType == ElementType.ItemContainer);

					if (previousHint == IndexSearchHint.NoHint)
					{
						// initialize
						previousHint = searchHint;
					}
				}
				else
				{
					// if we hit a sentinel or we're outside of the valid range, give up.

					// set back previous values based on the direction we are going.
					if (searchHint == IndexSearchHint.SearchBackwards)
					{
						referenceItemIndex++;
						referenceItemIndexInGroup++;
					}
					else
					{
						referenceItemIndex--;
						referenceItemIndexInGroup--;
					}
					searchHint = IndexSearchHint.Exact;
				}

				if (searchHint == IndexSearchHint.SearchBackwards)
				{
					global::System.Diagnostics.Debug.Assert(referenceItemIndex == targetItemIndex);

					if (referenceItemIndex == 0 ||
						referenceItemIndexInGroup == 0 ||
						!m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(referenceItemIndex - 1)))
					{
						searchHint = IndexSearchHint.Exact;
					}
					else
					{
						referenceItemIndex--;
						referenceItemIndexInGroup--;
					}

					if (previousHint == IndexSearchHint.SearchForwards)
					{
						// we hit a gap. This can happen when items are being deleted.
						// Let's give up
						searchHint = IndexSearchHint.Exact;
					}
				}

				if (searchHint == IndexSearchHint.SearchForwards)
				{
					global::System.Diagnostics.Debug.Assert(referenceItemIndex == targetItemIndex);

					if (referenceItemIndexInGroup == sizeOfGroup - 1 ||
						!m_containerManager.IsValidContainerIndexWithinBounds(m_containerManager.GetValidContainerIndexFromItemIndex(referenceItemIndex + 1)))
					{
						searchHint = IndexSearchHint.Exact;
					}
					else if (previousHint == IndexSearchHint.SearchBackwards)
					{
						// we hit a gap. This can happen when items are being deleted.
						// Let's give up
						searchHint = IndexSearchHint.Exact;
					}
					else
					{
						referenceItemIndex++;
						referenceItemIndexInGroup++;
					}
				}

			} while (searchHint == IndexSearchHint.SearchBackwards || searchHint == IndexSearchHint.SearchForwards);

			// We don't support inexact EstimateIndexFromPoint calls yet.
			global::System.Diagnostics.Debug.Assert(searchHint == IndexSearchHint.Exact);

			// We get the target as a group index. Add the group start index to convert to global index.
			returnValue.m_childIndex = targetItemIndex;
		}
		else
		{
			if (m_cacheManager.IsGrouping())
			{
				returnValue.m_childIndex = referenceGroupIndex;
				returnValue.m_childIsHeader = true;
			}
			else
			{
				// Means our collection was empty. Return -1.
				//Item with 0 index kind of valid and will be returned when we have count of 1.
				returnValue.m_childIndex = -1;
			}
		}

		return returnValue;
	}

	// Get the index where an item should be inserted if it were dropped at
	// the given position. This will be used by live reordering.
	internal int GetInsertionIndex(Point position)
	{
		int returnValue = 0;

		// Basic idea:
		// 1- Find a reference container so we can anchor the search.
		// 1a- If grouping, try to find group containing point by walking group headers. Then, grab the first realized container within the group.
		// 1b- If not grouping, grab the first realized container
		// TODO: Optimize which realized container we use, invoking estimation APIs.
		// -- jump to layout strategy -- Given: Index of item within group + point relative to either first item in group or to header.
		// 2- Do math to figure out which column the point is in.
		// 3- Knowing column and row height, figure out insertion index.

		int targetItemIndex = -1;
		IndexSearchHint searchHint = IndexSearchHint.NoHint;
		IndexSearchHint previousHint = IndexSearchHint.NoHint;

		FindValidContainerInGroupForPoint(position, out bool groupWasEmpty, out int groupIndex, out int referenceItemIndex, out int referenceItemIndexInGroup, out Rect referenceItemBounds);

		if (!groupWasEmpty)
		{
			int sizeOfGroup = 0;

			if (m_cacheManager.IsGrouping())
			{
				m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out _, out sizeOfGroup);
			}
			else
			{
				sizeOfGroup = m_cacheManager.GetTotalItemCount();
			}

			do
			{
				UIElement? spContainer = null;
				int validContainerIndex = m_containerManager.GetValidContainerIndexFromItemIndex(referenceItemIndex);

				if (m_containerManager.IsValidContainerIndexWithinBounds(validContainerIndex))
				{
					// it was within the bounds. We can try to get a container
					spContainer = m_containerManager.GetContainerAtValidIndex(validContainerIndex);
				}
				else
				{
					// that is not within the bounds.
					// this can happen when we have just inserted a couple of elements at the beginning of the collection. There wouldn't have been
					// sentinels, and we wouldn't be getting a valid range.
				}

				// if spcontainer is null, we could be dealing with a sentinel or we've fallen outside of the valid range
				if (spContainer is not null)
				{
					EstimationReference containerReference = CreateDefaultEstimationReference(ElementType.ItemContainer);
					containerReference.ElementIndex = referenceItemIndex;
					containerReference.ElementBounds = GetBoundsFromElement(spContainer);

					m_spLayoutStrategy.EstimateIndexFromPoint(
						true /* requestingInsertionIndex */,
						position,
						containerReference,
						m_windowState.GetRealizationWindow(),
						out searchHint,
						out ElementType targetElementType,
						out targetItemIndex);

					global::System.Diagnostics.Debug.Assert(targetElementType == ElementType.ItemContainer);

					if (previousHint == IndexSearchHint.NoHint)
					{
						// initialize
						previousHint = searchHint;
					}
				}
				else
				{
					// if we hit a sentinel or we're outside of the valid range, give up.

					// set back previous values based on the direction we are going.
					if (searchHint == IndexSearchHint.SearchBackwards)
					{
						referenceItemIndex++;
						referenceItemIndexInGroup++;
					}
					else
					{
						referenceItemIndex--;
						referenceItemIndexInGroup--;
					}
					searchHint = IndexSearchHint.Exact;
				}

				if (searchHint == IndexSearchHint.SearchBackwards)
				{
					if (referenceItemIndex == 0)
					{
						searchHint = IndexSearchHint.Exact;
					}
					else
					{
						referenceItemIndex--;
						referenceItemIndexInGroup--;
					}

					if (previousHint == IndexSearchHint.SearchForwards)
					{
						// we hit a gap. This can happen when items are being deleted.
						// Let's give up
						searchHint = IndexSearchHint.Exact;
					}
				}
				else if (searchHint == IndexSearchHint.SearchForwards)
				{
					if (referenceItemIndex == m_cacheManager.GetTotalItemCount() - 1)
					{
						searchHint = IndexSearchHint.Exact;
						// we allow insertion at the end of the list
						++targetItemIndex;
					}
					else if (previousHint == IndexSearchHint.SearchBackwards)
					{
						// we hit a gap. This can happen when items are being deleted.
						// Let's give up
						searchHint = IndexSearchHint.Exact;
					}
					else
					{
						referenceItemIndex++;
						referenceItemIndexInGroup++;
					}
				}

			} while (searchHint == IndexSearchHint.SearchBackwards || searchHint == IndexSearchHint.SearchForwards);

			// Note: allow index to be == size of collection, because it's valid to insert at end of collection!
			targetItemIndex = Math.Min(m_cacheManager.GetTotalItemCount(), targetItemIndex);

			// We don't support inexact EstimateInsertIndexFromPoint calls yet.
			global::System.Diagnostics.Debug.Assert(searchHint == IndexSearchHint.Exact);

			// The target item index is already a global index.
			returnValue = targetItemIndex;
		}
		else
		{
			if (m_cacheManager.IsGrouping())
			{
				// We shouldn't get here since we don't currently try to get insertion indexes with grouping enabled.
				// A possible algorithm would be to try to find the nearest group with a non-zero count, and determine
				// insertion index based on that.
				throw new NotSupportedException();
			}
			else
			{
				// Means our collection was empty. Return 0.
				returnValue = 0;
			}
		}

		return returnValue;
	}

	// Get the indexes where an item should be inserted if it were dropped at
	// the given position
	internal void GetInsertionIndexes(Point position, out int first, out int second)
	{
		bool firstCheck = false;
		bool secondCheck = false;

		first = -1;
		second = -1;

		int insertionIndex = GetInsertionIndex(position);
		IsLayoutBoundary(insertionIndex, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary);

		first = insertionIndex - 1;
		second = insertionIndex;

		var orientation = m_spLayoutStrategy.VirtualizationDirection;
		if (orientation == Orientation.Vertical)
		{
			firstCheck = isTopBoundary;
			secondCheck = isBottomBoundary;
		}
		else
		{
			firstCheck = isLeftBoundary;
			secondCheck = isRightBoundary;
		}

		// make sure we're not at the edges of the panel
		if (firstCheck)
		{
			first = -1;
		}
		else if (secondCheck)
		{
			second = -1;
		}
	}

	// Finds an arbitrary realized item from within the group under the given point.
	// Non-grouped scenarios are treated as one large group.
	private void FindValidContainerInGroupForPoint(Point point, out bool groupWasEmpty, out int groupIndex, out int referenceItemIndex, out int referenceItemIndexInGroup, out Rect referenceItemBounds)
	{
		referenceItemBounds = default;

		UIElement? spReferenceContainer = null;
		referenceItemIndex = -1;
		referenceItemIndexInGroup = -1;
		groupIndex = -1;
		groupWasEmpty = false;

		if (m_cacheManager.IsGrouping())
		{
			// Find the group we're in. Walk from left to right until we notice a group header to the right of/below our point.
			int foundHeaderIndex = 0;
			int validHeaderCount = m_containerManager.GetValidHeaderCount();

			global::System.Diagnostics.Debug.Assert(validHeaderCount > 0);

			var orientation = m_spLayoutStrategy.VirtualizationDirection;

			// we start iterating from 2nd group if it exist. If not the loop will be ended right away.
			for (int headerIndex = 1; headerIndex < validHeaderCount; headerIndex++)
			{
				var spHeader = m_containerManager.GetHeaderAtValidIndex(headerIndex);

				// It's possible that a header doesn't exist, for example if it's hidden (HidesIfEmpty==true).
				// Skip over such headers.
				if (spHeader is not null)
				{
					Rect headerBounds = GetBoundsFromElement(spHeader);
					if ((orientation == Orientation.Horizontal) ? (point.X < headerBounds.X) : (point.Y < headerBounds.Y))
					{
						break;
					}
					// store current candidate. We will use this value in case all following groups are all hidden or if the next
					// candidate boundary is beyond the manipulation point.
					foundHeaderIndex = headerIndex;
				}
			}

			// by this point we should have found the index of valid group.
			groupIndex = m_containerManager.GetGroupIndexFromValidIndex(foundHeaderIndex);

			// Get any realized container in the group. There has to be at least one, unless the group is empty.
			m_cacheManager.GetGroupInformationFromGroupIndex(groupIndex, out int startItemIndex, out int itemCountInGroup);

			if (itemCountInGroup > 0)
			{
				int validContainerSearchStart = m_containerManager.GetValidContainerIndexFromItemIndex(startItemIndex);
				int validContainerSearchEnd = m_containerManager.GetValidContainerIndexFromItemIndex(startItemIndex + itemCountInGroup);

				groupWasEmpty = false;

				validContainerSearchStart = Math.Max(0, validContainerSearchStart);
				validContainerSearchEnd = Math.Min(m_containerManager.GetValidContainerCount(), validContainerSearchEnd);

				// We had a header, we should have at least one valid container in that header's group.
				global::System.Diagnostics.Debug.Assert(validContainerSearchStart < validContainerSearchEnd);
				spReferenceContainer = m_containerManager.GetContainerAtValidIndex(validContainerSearchStart);
				referenceItemIndex = m_containerManager.GetItemIndexFromValidIndex(validContainerSearchStart);
				referenceItemIndexInGroup = referenceItemIndex - startItemIndex;
			}
			else
			{
				// Our group was empty! Let caller know.
				groupWasEmpty = true;
			}

		}
		else if (m_containerManager.GetValidContainerCount() > 0)
		{
			// Not grouping. Grab any old element for now (could optimize to try to get closer to input point).
			referenceItemIndex = m_containerManager.GetItemIndexFromValidIndex(0);
			referenceItemIndexInGroup = referenceItemIndex;
			groupIndex = 0;
			groupWasEmpty = m_cacheManager.GetTotalItemCount() == 0;
			if (!groupWasEmpty)
			{
				spReferenceContainer = m_containerManager.GetContainerAtValidIndex(0);
			}
		}

		if (spReferenceContainer is not null)
		{
			referenceItemBounds = GetBoundsFromElement(spReferenceContainer);
		}
	}

	// Gets a series of BOOLEAN values indicating whether a given index is
	// positioned on the leftmost, topmost, rightmost, or bottommost
	// edges of the layout.  This can be useful for both determining whether
	// to tilt items at the edges of rows or columns as well as providing
	// data for portal animations.
	internal void IsLayoutBoundary(int index, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary)
	{
		// 1- Find group index, index in group, etc.
		// 2- Ask strategy if it's a boundary.

		// Don't support grouping yet. At present, we don't call into this private API if we're grouping.
		global::System.Diagnostics.Debug.Assert(!m_cacheManager.IsGrouping());

		m_spLayoutStrategy.IsIndexLayoutBoundary(
			ElementType.ItemContainer,
			index,
			m_windowState.GetRealizationWindow(),
			out isLeftBoundary,
			out isTopBoundary,
			out isRightBoundary,
			out isBottomBoundary);
	}

	internal Rect GetItemsBounds() => m_windowState.GetVisibleWindow();

	// is used by GenerateForward and Backward to determine how to generate.
	// forward generation: needs an anchor at the left side of the valid ranges
	// backward generation: needs an anchor on the left side of the valid ranges
	//  Will always be the first container, unless there are no containers at all,
	//  in which case we will fallback to using headers.
	private void GetGenerationAnchorInformation(out LayoutReference referenceInformation, ref CollectionIterator iterator)
	{
		// determine a reference
		// this can work because we have the following recycle rules:
		// 1. a header that has a container in it is not recycled
		// 2. when we recycle the last container from a group, we also recycle the header
		// 3. an empty header is not recycled, unless a container in a previous group needs
		//    to be recycled.
		// However, in the meantime inserts might have occurred on a group that have not resulted
		// in sentinels because they were at the edge. In this scenario we are in quite the pickle:
		// if that group lies outside of the realization window, then adding sentinels to it will result
		// in generation of containers that are outside the realization window which means that they will
		// be eligible for recycling again. The approach thus is to recognize that situation and special casing it.

		referenceInformation = CreateDefaultLayoutReference();
		UIElement? spEdgeElement;
		UIElement? spEdgeHeader = null;

		int groupIndexOfEdgeItem = -1;
		int indexInGroupOfEdgeItem = -1;
		int itemIndexOfEdgeItem = -1;
		bool useContainerAsEdge = false;

		if (m_containerManager.GetValidContainerCount() > 0)
		{
			itemIndexOfEdgeItem = m_containerManager.GetItemIndexFromValidIndex(0);
			if (m_cacheManager.IsGrouping())
			{
				m_cacheManager.GetGroupInformationFromItemIndex(
					itemIndexOfEdgeItem,
					out groupIndexOfEdgeItem,
					out indexInGroupOfEdgeItem,
					out _);

				spEdgeHeader = m_containerManager.GetHeaderAtGroupIndex(groupIndexOfEdgeItem);
			}
		}

		useContainerAsEdge =
			// if we are not grouping, use a container
			!m_cacheManager.IsGrouping()
			// if the first valid container is not representing the first item in the group, we have more containers to generate
			|| indexInGroupOfEdgeItem > 0;

		if (useContainerAsEdge)
		{
			// we use a container if the container we found is floating. Whether we inserted new items or not, that
			// is still the best approach.
			spEdgeElement = m_containerManager.GetContainerAtValidIndex(0);
			referenceInformation.ReferenceIsHeader = false;
			iterator.Init(itemIndexOfEdgeItem, ElementType.ItemContainer);

			// get the headerbounds of this container
			if (spEdgeHeader is not null)
			{
				referenceInformation.HeaderBounds = GetBoundsFromElement(spEdgeHeader);
			}
		}
		else
		{
			// we might have to use a header. We should do that when:
			// 1. there are no containers at all
			// 2. the index in group is 0, use its header (it is not floating)

			// but what if items have been inserted before the first container. Those sentinels will not have been
			// created, and creating them on the fly is fraught with danger.

			// reaching this else block means that:
			// a. we are grouping
			// b. we may have an edge item that is at index 0

			if (spEdgeHeader is not null)
			{
				// if we already determined a header, we know it mcatches the item
				spEdgeElement = spEdgeHeader;
				iterator.Init(groupIndexOfEdgeItem, ElementType.GroupHeader);
			}
			else
			{
				// we have no header, per definition this means that we have no containers
				global::System.Diagnostics.Debug.Assert(m_containerManager.GetValidContainerCount() == 0);

				// in that case, we can just take the first header
				spEdgeElement = m_containerManager.GetHeaderAtValidIndex(0);
				iterator.Init(m_containerManager.GetGroupIndexFromValidIndex(0), ElementType.GroupHeader);
			}

			referenceInformation.ReferenceIsHeader = true;

		}

		referenceInformation.ReferenceBounds = GetBoundsFromElement(spEdgeElement!);
		referenceInformation.RelativeLocation = ReferenceIdentity.AfterMe;
	}

	// Return whether an element should be generated/placed/shown. Normally, this is always true
	// but can return false if we're hiding empty groups or collapsing groups
	private bool ShouldElementBeVisible(ElementType type, int index)
	{
		bool shouldBeVisible = false;

		switch (type)
		{
			case ElementType.GroupHeader:
				{
					if (m_cacheManager.IsGrouping())
					{
						if (m_cacheManager.GetHidesIfEmpty())
						{
							m_cacheManager.GetGroupInformationFromGroupIndex(index, out _, out int itemsInGroup);

							shouldBeVisible = (itemsInGroup > 0);
						}
						else
						{
							shouldBeVisible = true;
						}
					}
					else
					{
						// Why would we even call this? If we're not grouping, we shouldn't be displaying headers
						global::System.Diagnostics.Debug.Assert(false);
						shouldBeVisible = false;
					}
				}
				break;

			case ElementType.ItemContainer:
				shouldBeVisible = true;
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}

		return shouldBeVisible;
	}

	private float PointFromPointInVirtualizingDirection(Point point)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)point.X : (float)point.Y;

	private void SetPointFromPointInVirtualizingDirection(ref Point point, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			point.X = value;
		}
		else
		{
			point.Y = value;
		}
	}

	private float PointFromSizeInVirtualizingDirection(Size size)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)size.Width : (float)size.Height;

	private void SetPointFromSizeInVirtualizingDirection(ref Size size, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			size.Width = value;
		}
		else
		{
			size.Height = value;
		}
	}

	private float SizeInVirtualizingDirection(Size size)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)size.Width : (float)size.Height;

	private void SetSizeInVirtualizingDirection(ref Size size, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			size.Width = value;
		}
		else
		{
			size.Height = value;
		}
	}

	private float SizeInNonVirtualizingDirection(Size size)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)size.Height : (float)size.Width;

	private void SetSizeInNonVirtualizingDirection(ref Size size, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			size.Height = value;
		}
		else
		{
			size.Width = value;
		}
	}


	private float PointFromRectInVirtualizingDirection(Rect rect)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)rect.X : (float)rect.Y;

	private void SetPointFromRectInVirtualizingDirection(ref Rect rect, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			rect.X = value;
		}
		else
		{
			rect.Y = value;
		}
	}

	private float PointFromRectInNonVirtualizingDirection(Rect rect)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)rect.Y : (float)rect.X;

	private void SetPointFromRectInNonVirtualizingDirection(ref Rect rect, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			rect.Y = value;
		}
		else
		{
			rect.X = value;
		}
	}

	private float SizeFromRectInVirtualizingDirection(Rect rect)
		=> m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal ? (float)rect.Width : (float)rect.Height;

	private void SetSizeFromRectInVirtualizingDirection(ref Rect rect, float value)
	{
		if (m_spLayoutStrategy.VirtualizationDirection == Orientation.Horizontal)
		{
			rect.Width = value;
		}
		else
		{
			rect.Height = value;
		}
	}

	// Returns the thickess size in the virtualizing direction.
	private protected double SizeFromThicknessInVirtualizingDirection(Thickness thickness)
	{
		switch (m_spLayoutStrategy.VirtualizationDirection)
		{
			case Orientation.Horizontal:
				return thickness.Left + thickness.Right;
			case Orientation.Vertical:
				return thickness.Top + thickness.Bottom;
		}

		return 0.0;
	}

	internal double CacheLengthBase
	{
		// Gets the cache length.
		get => m_cacheLength;
		// Sets the cache length.
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value));
			}

			m_cacheLength = value;

			ResetCacheBuffers();
		}
	}

	internal static bool ElementIsPositionedInGarbageSection(UIElement spElement)
	{
		Point offScreenPosition = GetOffScreenPosition();
		Rect bounds = GetBoundsFromElement(spElement);
		bool hasOffScreenPosition = bounds.X == offScreenPosition.X && bounds.Y == offScreenPosition.Y;

		return hasOffScreenPosition;
	}

	private static void SetElementEmptySizeInGarbageSection(UIElement spElement)
		=> SetElementSizeInGarbageSection(spElement, new Size());

	private static void SetElementSizeInGarbageSection(UIElement spElement, Size size)
	{
		UIElement uielement = spElement;
		global::System.Diagnostics.Debug.Assert(uielement is not null);

		uielement.SetSkipFocusSubtree(true /*skipFocusSubtree*/, true /*forOffScreenPosition*/);
		uielement.GetVirtualizationInformationOrNull()!.Bounds = RectUtil.CreateRect(GetOffScreenPosition(), size);
	}

	private static void SetBoundsForElement(UIElement spElement, Rect bounds)
	{
#if DEBUG
		Point offScreenPositionDbg = GetOffScreenPosition();
		bool hasOffScreenPositionDbg = bounds.X == offScreenPositionDbg.X && bounds.Y == offScreenPositionDbg.Y;
		global::System.Diagnostics.Debug.Assert(!hasOffScreenPositionDbg);
#endif

		UIElement uielement = spElement;
		global::System.Diagnostics.Debug.Assert(uielement is not null);

		uielement.SetSkipFocusSubtree(false /*skipFocusSubtree*/, true /*forOffScreenPosition*/);
		uielement.GetVirtualizationInformationOrNull()!.Bounds = bounds;
	}

	private protected Size GetViewportSize()
	{
		Size size = default;

		size.Width = 0.0;
		size.Height = 0.0;
		ScrollViewer? pScrollViewer = null;
		m_wrScrollViewer?.TryGetTarget(out pScrollViewer);
		if (pScrollViewer is not null)
		{
			double viewportHeight = pScrollViewer.ViewportHeight;
			double viewportWidth = pScrollViewer.ViewportWidth;

			size.Width = (float)viewportWidth;
			size.Height = (float)viewportHeight;
		}

		return size;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == AreStickyGroupHeadersEnabledBaseProperty)
		{
			// Call ReevaluateGroupHeaderStrategy when AreStickyGroupHeadersEnabled changes
			ReevaluateGroupHeaderStrategy();
		}
	}
}
