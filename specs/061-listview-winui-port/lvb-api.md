# ListViewBase: C# signature map

Single source of truth for every file ported against `ListViewBase_Partial.h`. Write these signatures exactly. Accessibility comes from the C++ section: `public` becomes `internal`, `protected` becomes `private protected`, `private` stays `private`. The exceptions are public API, base-class overrides, and members reached through C++ `friend`, and they are spelled out below.

Members under "Already in the header port" must not be written again. Their code lives in `ListViewBase.partial.h.mux.cs`, which also holds the `ListViewBase.g.h` part: the events, the non-mixin DPs and the explicit interface forwarders. That whole file sits inside `#if LISTVIEW_PORT_CUTOVER`, so wrap each .cpp port in the same guard until integration.

Sources are under `D:\Throwaway\winui251\src\dxaml\xcp\dxaml\lib`, except `LiveReorderHelper` (see below).

## Rules for the .cpp ports

**Two `ElementType`s.** In C++, a bare `ElementType` is the nested enum (`Other`, `Header`, `Footer`, `Item`, `GroupHeader`, `ItemOrGroupHeader`) and `xaml_controls::ElementType` is the panel's (`ItemContainer`, `GroupHeader`). In C#, inside the class, `ElementType` always binds to the nested one, so the panel's must be written `Controls.ElementType`. Both have a `GroupHeader` member, so a wrong one can compile. C++ uses the nested members unscoped (`Other`); C# writes `ElementType.Other`.

**Enum member names stay verbatim:** `ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_None`, `KeyboardReorderDirection.KeyboardReorderDirection_ToLowerIndex`, `ListSerializationFormatVersions.ListSerializationFormatVersions_PhoneBlue`.

**Friend access.** `ListViewBaseItem` and `LoadMoreItemsOperation` are C++ friends, so everything they call is `internal` here regardless of its C++ section: `OnItemPrimaryInteractionGesture`, `OnItemSecondaryInteractionGesture`, `OnDragGesture`, `OnKeyboardReorder` (and its `KeyboardReorderDirection` enum), `FocusItem`, `IsInExclusiveInteraction`, `SetHoldingItem`, `ClearHoldingItem`, `ClearHoldingState`, `GetIsDragEnabled`, `GetPanelOrientation`, `SetDragOverItem`, `IsInLiveReorder`, `m_itemSelectionGestureId`, `LoadMoreItemsAsyncInternal`. `ListViewBaseHeaderItem` and `GroupItem` only call members from C++ `public` sections. The exception is `GetDropOffsetToRoot`, an override.

**`Impl` suffix.** It goes away, except on these, where dropping it would collide:
- `FocusImpl`: Uno's `UIElement` already uses that name.
- `OnSelectionModeChangedImpl` (C++ `OnSelectionModeChanged`): the mixin generates `protected virtual void OnSelectionModeChanged(ListViewSelectionMode, ListViewSelectionMode)` with this exact signature, and it is public surface. The C++ method runs from `OnPropertyChanged2`, so use the suffix there and leave the mixin's method alone.

**Not ported.**
- `QueryInterfaceImpl`, `EventAddHandlerByIndex`, `EventRemoveHandlerByIndex`, `Get*EventSourceNoRef`, and the event source typedefs have no C# form.
- `OnUnloaded` is declared in the header and defined nowhere.
- `~ListViewBase`: no C# finalizer. Keep the body in `#if !HAS_UNO` with a `// TODO Uno`. The live reorder timer and the `DetachScrollViewerPropertyChanged` handler must still be released; the safe place is the control's unload path.

**Constructor and `Initialize`.** `ListViewBase()` assigns the initializer list as written and calls `Initialize()` last.
- Field initializers already cover `m_movedItems`, `m_itemSelectionGestureId`, every `SerialDisposable`, `m_toBeClearedContainers` and `m_toBeClearedItems`. `Initialize()` skips the two `ctl::make` calls for those last two.
- Still needs the constructor: `m_focusedGroupIndex = -1`, `m_budget = 40` (C++ `BUDGET_MANAGER_DEFAULT_LIMIT`; add a private const, as `CalendarViewGeneratorMonthViewHost` does), `m_itemsAreTabStops`/`m_groupsAreTabStops = true`, `m_firstAvailableItemForFocus`/`m_firstAvailableGroupHeaderForFocus`/`m_itemIndexHintForHeaderNavigation = -1`, `m_lowestPhaseInQueue = -1`, `m_semanticZoomCompletedFocusState = FocusState.Programmatic`, `m_showsScrollingPlaceholdersLocal = true`, `m_liveReorderIndices = new(-1, -1, -1)`, `m_dragAcceptedOperation = DataPackageOperation.None`.
- The old constructor's `SelectedItems` setup has to survive, or be replaced by the Selector selection port (see "Base-class changes").
- `Initialize()` is `private`. `ItemsControl` has its own private one, which C# treats as unrelated.
- `m_gettingFocusHandler` and the `Loaded` subscription go through `SerialDisposable` and `Disposable.Create`.

**Value types.**
- `PanVelocity` is a `struct`, and C++ passes it by value. `m_pendingAutoPanVelocity.Clear()` only works on the field, so never copy it to a local to clear it. C++ `PanVelocity()` calls `Clear`, which `default` already does.
- `LiveReorderIndices` is the named form of the anonymous struct. `m_liveReorderIndices = { -1, -1, -1 }` is `= new(-1, -1, -1)`.
- `ListSerializationData` is a `struct`. `ParseSerializationBuffer` writes it through `out`.
- `LiveReorderHelper.MovedItem` should stay a `struct`: C++ copies it into vectors by value.
- `m_pAnchorIndex` is `uint?`. `if (!m_pAnchorIndex)` is `.HasValue`, `*m_pAnchorIndex` is `.Value`.

**`_Out_` that is not always written.** These use `ref`, not a return value, because the callee can leave the caller's seed in place:
- `QueryPanelForItemNavigation`: `pNewFocusedType` is written only when the panel handles the action. The other three outs are always written.
- `QueryGroupFallbackForItemNavigation`: `pIsHandled` is written only when the target stays inside the group, and `OnKeyboardNavigation` reuses its `isHandled` local.
- `ReorderItemsTo`: `_Inout_opt_` on both tracked indexes, and `OnKeyboardReorder` passes `nullptr` for the second. Use `ref int`, and have that caller pass a local set to `-1`. `UpdateTrackedIndex` ignores negative values, so that has the same effect as the null check.

Others look risky but are fine as plain `out` or return:
- `OnGroupHeaderKeyDown`, `OnGroupHeaderKeyUp`: `pHandled` is conditional, but the only caller seeds a fresh `false`. Return `bool` with a local defaulting to `false`.
- `GetNavigationSourcePoint`: writes `X` and `Y` from branches. Start from `Point point = default`.
- `ProcessDragOverAt`: `_Out_opt_`, always written when given. Return `bool`.
- `GetDraggedItems`: both outs are optional and the body branches on them (`if (ppItems != nullptr)`). `pItemIndices` is `List<uint>?` passed in and filled by the callee, as C++ passes a caller-owned vector. `wantItems` replaces the null check on `ppItems`, so callers that only need indexes do not build the item vector.

**Scroll entry points recurse if left as they are.** Uno's `Selector.ScrollIntoViewInternal` has `case ListViewBase: listViewBase.ScrollIntoView(item, alignment)`, and the C++ `ScrollIntoView` path calls `ScrollIntoViewInternal`. Ported as written, that is an infinite loop on the first `ScrollIntoView`. The Selector case has to go at cut-over and the method must call `ModernCollectionBasePanel.ScrollItemIntoView` the way `Selector_Partial.cpp` does. `Selector.cs:214-216` also calls `lvb.ScrollIntoView`, which needs a look at the same time.

**`OnItemsChanged` has three shapes.** C++ has two virtuals: `OnItemsChangedImpl(IInspectable*)` (public API) and `OnItemsChanged(IVectorChangedEventArgs*)` (the one `ListViewBase` overrides). Uno has `ItemsControl.OnItemsChanged(object)` and a private `Selector.OnItemsChanged(NotifyCollectionChangedEventArgs)`. Port the override as `protected override void OnItemsChanged(object e)`, with `e` cast to `IVectorChangedEventArgs`.
Trap: `ItemsControl.OnItemsVectorChanged` is the only caller, and it raises this only for `Items` changes. Changes in an `ItemsSource` reach `OnItemsSourceSingleCollectionChanged` and never call it, while C++ raises it for both because `Items` mirrors the source. So `ListViewBase` must also override `OnItemsSourceSingleCollectionChanged`, or the `m_pAnchorIndex` adjustment and drag cancel miss every `ItemsSource` change.

**`OnPropertyChanged2`.** `internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)`: call `base` first, then compare `args.Property`.
- `KnownPropertyIndex::ItemsControl_ItemsHost` has no DP in Uno. Run that case's body from `OnItemsHostAvailable` too, and do not lose its fallthrough from `ItemsSource`.
- `UIElement_Visibility` compares against `VisibilityProperty`.
- `InitializeDataSourceSelectionInfo` keeps the Uno fix from `ListViewBase_Partial.cs`: `DataSourceAsSelectionInfo = ItemsSource as ISelectionInfo;`. Otherwise setting a new `ItemsSource` keeps the previous source's selection info. Keep the C++ form under `#if false`, as that file does.
- `ItemsControl::TraceVirtualizationEnabledByModernPanel` stays in `#if !HAS_UNO`.

**Scroll host handler.** `IDPChangedEventHandler` has no Uno form. `m_tpOffsetChangedHandler` is an `IDisposable?`: register `RegisterPropertyChangedCallback` for `ScrollViewer.HorizontalOffsetProperty` and `VerticalOffsetProperty` and compose the two with `CompositeDisposable`. `OnScrollHostPropertyChanged(DependencyObject, DependencyProperty)` keeps its shape and its internal check on the DP. `Clear()` is `Dispose()` then `null`.

**Pending porting work nobody owns yet.**
- `LiveReorderHelper` (`inc/LiveReorderHelper.h`, `lib/LiveReorderHelper.cpp`, 215 lines) is not in the winui251 drop. It is at `D:\Work\microsoft-ui-xaml2\dxaml\xcp\components\livereorderhelper`. The Reorder port needs it as `src/Uno.UI/DirectUI/Components/LiveReorderHelper.mux.cs`, namespace `DirectUI.Components.LiveReorderHelper`. The header port aliases `DirectUI.Components.LiveReorderHelper.MovedItems`. The MUX Reference line must name that source, since it is not the 2.5.1 drop. `std::vector` iteration through `begin()`/`end()` becomes `GetEnumerator()`.
- `LoadMoreItemsOperation` (`incrementalloading.h/.cpp`) does not exist in Uno. The DataVirtualization port needs it.
- `DragDropVisual` does not exist in Uno, so its parameters and `m_tpDragDropVisual` are `object`. Its code paths stay in `#if !HAS_UNO`. Uno's drag currently flows through `DragDrop.cs` and `DragStartingEventArgs`, so `OnDragGesture` needs a decision from the Reorder porter: keep `OnItemContainerDragStarting` as Uno's trigger, or map `RaiseDragDropEventAsyncOperation` onto it.

**Inert paths**, behind `#if !HAS_UNO` with `// TODO Uno`, keeping the logic:
- `ISemanticZoomInformation` members and `SemanticZoom` calls. They must exist with the public signatures below, since the generated class declares the interface.
- `PrepareConnectedAnimation`, `TryStartConnectedAnimationAsync`, and `ConnectedAnimationService`.
- DManip: `NotifyStateChange` stays a real method, called through the forwarder. `ScrollViewer.SetDirectManipulationStateChangeHandler` and `IDirectManipulationStateChangeHandler` already exist in Uno, so `OnApplyTemplate` could hook it for real and `ClearHoldingState` would run on a DManip start, which is what `ListViewBase.DragDrop.mux.cs:380` has a TODO for. Decide at integration; the default is inert.
- `ToggleBackKeyListenerOnPhone` and `OnBackButtonPressed` are phone paths: keep them ported, using `BackButtonIntegration` if it fits.

**Other mappings.**
- `get_Header(&x)` is the mixin `Header`; `get_ItemsHost` is `ItemsPanelRoot`; `get_ItemsPresenter` is `ItemsPresenter`. `ItemsPresenter.LoadFooter(bool)` and `DelayLoadFooter` exist.
- `wsy::VirtualKey` is `Windows.System.VirtualKey`; `ixp::IPointerPoint` is `Microsoft.UI.Input.PointerPoint`; `xaml_input::IPointer` is `Pointer`; `IDragEventArgs` is `DragEventArgs`; `ISemanticZoomLocation` is `SemanticZoomLocation`; `IGroupItem` is `GroupItem`; `IPanel` is `Panel`.
- `std::function<HRESULT(IInspectable*)>` is `Action<object?>`. `std::function<HRESULT(UINT, ElementType, BOOLEAN*)>` is `Func<uint, Controls.ElementType, bool>`. `std::function<HRESULT()>` is `Action`.
- `TrackerCollection<T>` exists in `DirectUI` (`JoltCollections.h.cs`). `TabStopProcessingResult` and `InputActivationBehavior` are internal types in `Uno.UI.Xaml.Input`.
- `DirectUI::LayoutInformation` has no instance form: Uno's `LayoutInformation.GetLayoutSlot` is static, so that parameter goes away.
- `get_ShowsScrollingPlaceholders` and `get_CanSemanticZoomGroup` are properties. The first is already in the header port; read the DP with `(bool)GetValue(ShowsScrollingPlaceholdersProperty)` in `OnPropertyChanged2`, because `ShowsScrollingPlaceholders` returns the local field.
- `IsMultiSelectCheckBoxEnabled` and `SelectionMode` changes go through `OnPropertyChanged2` with no DP callback. Do not port `UpdateItemsMultiSelectVisualState`.
- `CanDragItems` has no DP callback in the header port. The old `OnCanDragItemsChanged` sets each container up for Uno's own drag gesture; the Reorder porter must either keep it (add `new FrameworkPropertyMetadata(BoolBoxes.False, OnCanDragItemsChanged)`) or replace that mechanism.
- `DBG` is `DEBUG`. `LVB_DEBUG` traces and telemetry stay in `#if !HAS_UNO`. Assertions use `global::System.Diagnostics.Debug.Assert`.
- `RRETURN` on `IFC` cleanup with a real `Cleanup:` body (for example `ExecuteDeferredScrollCommand`) is `try`/`finally`.

## Base-class changes

Today's state of `Selector`, `ItemsControl` and `UIElement`. The port won't compile until these exist. The hosts phase or integration owns them.

| Needed | C++ | Uno today |
|---|---|---|
| `internal virtual void MakeSingleSelection(int index, ...)` | `virtual`, `UINT index` | `Selector_Partial.cs:332`, `internal void`, `int index`. Keep `int`. |
| `SetFocusedItem` (both overloads) `virtual` | `virtual` | `Selector.cs:847,872`, `private protected`, not virtual. The second has no default for `animateIfBringIntoView`. |
| `OnSelectionChanged(int, int, object, object, bool, FocusNavigationDirection)` | `virtual` | Present and `virtual`: `Selector.partial.mux.cs:325`. |
| `internal virtual bool IsSelectionPatternApplicable()` | `virtual`, out param | `Selector_Partial.cs:464`, not virtual. |
| `private protected virtual bool CanSelectMultiple { get; }` | `virtual get_CanSelectMultiple` | Missing. |
| `internal virtual void ItemFocused(SelectorItem)`, `internal virtual void ItemUnfocused(SelectorItem)` | `virtual` | Missing. `ListViewBaseItem` calls both. |
| `private protected` for `UpdateVisibleAndCachedItemsSelectionAndVisualState`, `SetLastFocusedIndex` | `protected` | `private`. |
| `GetLastFocusedIndex()` | `protected` | Missing, 11 uses. |
| `SelectRangeInternal`, `DeselectRangeInternal`, `SelectAllInternal`, `SelectOneItemInternal`, `ClearSelection`, `get_SelectedItemsInternal`, `GetSelectedIndexes`, `IsIndexSelected`, `m_selection` | `Selector` selection model | All missing. `Selector.cs` runs its own model. The Selection and Interaction ports depend on this, so it needs an owner before they start. |
| `private protected virtual void OnItemsHostAvailable()` | `ItemsControl_Partial.cpp:660` | Missing. `OnItemsPanelRootPrepared` is the nearest hook. |
| `private protected virtual bool IsPrepareContainerForItemDeferred(DependencyObject)` | `ItemsControl_Partial.cpp:908` | Missing, and `IGeneratorHost.PrepareItemContainer` does not check it. Without the check, `ContainerContentChanging` phasing runs wrongly. |
| `private protected virtual void PrepareGroupContainer` and `ClearGroupContainerForGroup` | `virtual` | `ItemsControl.partial.mux.cs:171,206`, explicit interface bodies only. |
| `internal virtual Point GetDropOffsetToRoot()` | `ItemsControl_Partial.h:281` | Missing. |
| `internal virtual bool FocusImpl(FocusState)` | `override` | `UIElement.mux.cs:606`, not virtual. |
| `Selector.IsSingleSelection` | none | Keep `internal override bool IsSingleSelection`. See "Existing Uno members". |

## Callers that must keep working

C++ callers outside the ListViewBase files, and how each resolves. Everything below is in the signature lists.

| Caller | Calls |
|---|---|
| `ListViewBaseItem_Partial.cpp` | `FocusItem`, `GetDropOffsetToRoot`, `GetDragGrabbedItems`, `DragItemsCount`, `GetIsDragEnabled`, `GetIsHolding`, `GetPanelOrientation`, `IsContainerDragDropOwner`, `IsDragOverItem`, `IsInDragDrop`, `IsInExclusiveInteraction`, `IsInLiveReorder`, `ItemFocused`, `ItemUnfocused`, `OnDragGesture`, `OnItemPrimaryInteractionGesture`, `OnItemSecondaryInteractionGesture`, `OnKeyboardReorder`, `SetDragOverItem`, `SetHandleKeyDownArgsFromItem`, `SetHoldingItem`, `ClearHoldingItem`, `SetIsHolding`, `m_itemSelectionGestureId` |
| `ListViewBaseHeaderItem_Partial.cpp` | `GroupHeaderItemFocused`, `GroupHeaderItemUnfocused`, `OnGroupHeaderKeyDown`, `OnGroupHeaderKeyUp`, `SetHandleKeyDownArgsFromItem`, `OnHeaderItemTap` |
| `GroupItem_Partial.cpp` | `GetDropOffsetToRoot`, `HasFocusedGroup`, `OnGroupFocusChanged`, `GroupItemFocused`, `GroupItemUnfocused` |
| `ItemInvokeAdapter_Partial.cpp` | `AutomationItemClick`, `AutomationHeaderItemClick` |
| `ListViewBaseAutomationPeer_Partial.cpp` | `GetDropTargetDropEffect`, `GetDropTargetDropEffects` |
| `ListViewBaseItemAutomationPeer_Partial.cpp`, `ListView`/`GridView` data peers | `GetDragGrabbedItems`, `IsScrollable` |
| `Panel_Partial.cpp` | `GetFocusableElement` |
| `DropOperationTarget.cpp` | `UpdateDragPositionOnCapturedTarget` |
| `incrementalloading.cpp` | `LoadMoreItemsAsyncInternal` |
| `ListViewPersistenceHelper_Partial.cpp` | `GetRelativeScrollPosition`, `SetRelativeScrollPositionAsync` |
| `VirtualizingPanel_Partial.cpp` | `UpdateClip` |

## Defined in the .cpp files

Target file names follow the plan: `ListViewBase_<Part>.partial.mux.cs`.

### ListViewBase_Partial.cpp (`ListViewBase.partial.mux.cs`)

- `protected internal ListViewBase()`: keeps the old accessibility.
- `private void Initialize()`
- `internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)`
- `protected override void PrepareContainerForItemOverride(DependencyObject element, object item)`
- `protected override void ClearContainerForItemOverride(DependencyObject element, object item)`
- `protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)`
- `protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)`
- `protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)`
- `private protected override void OnItemsHostAvailable()`
- `internal bool IsScrollable()`
- `public void ScrollIntoView(object item)` (`ScrollIntoViewImpl`)
- `public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)` (`ScrollIntoViewWithAlignmentImpl`)
- `internal void ScrollIntoViewWithOptionalAnimation(object item, ScrollIntoViewAlignment alignment, bool disableAnimation)`
- `protected override void OnApplyTemplate()`
- `protected override Size ArrangeOverride(Size arrangeSize)`
- `protected override Size MeasureOverride(Size availableSize)`
- `private void NotifyStateChange(DMManipulationState state, float xCumulativeTranslation, float yCumulativeTranslation, float zCumulativeFactor, float xCenter, float yCenter, bool isInertial, bool isTouchConfigurationActivated, bool isBringIntoViewportConfigurationActivated)`
- `internal override string GetPlainText()`
- `internal override DependencyObject? GetFirstFocusableElementOverride()`
- `internal override DependencyObject? GetLastFocusableElementOverride()`
- `internal DependencyObject? GetFocusableElement(bool isBackward)`
- `private protected override bool CanRecycleContainer(DependencyObject pContainer)`
- `private protected override DependencyObject? SuggestContainerForContainerFromItemLookup()`
- `private protected override void PrepareGroupContainer(DependencyObject pContainer, ICollectionViewGroup? pGroup)`
- `private protected override void ClearGroupContainerForGroup(DependencyObject pContainer, ICollectionViewGroup? pItem)`
- `internal override bool IsDraggableOrPannable()`
- `public ConnectedAnimation PrepareConnectedAnimation(string key, object item, string elementName)`
- `public IAsyncOperation<bool> TryStartConnectedAnimationAsync(ConnectedAnimation animation, object item, string elementName)`
- `private protected override void PreProcessContentPreparation(DependencyObject pContainer, object? pItem)`
- `private void ScheduleDeferredScrollCommand(Action<object?> command, object? pScrollToItem, DeferPoint deferUntil)`
- `private void ExecuteDeferredScrollCommand()`
- `private void EnsureScrollHostOffsetChangeAction()`
- `private void AttachScrollViewerPropertyChanged()`
- `private void DetachScrollViewerPropertyChanged()`
- `private void OnScrollHostPropertyChanged(DependencyObject pSender, DependencyProperty pDP)`
- `private void OnScrollHostOffsetsChanged()`
- `private void LoadMoreItemsIfNeeded(Size finalSize)`
- `private DependencyObject? GetFocusableElementForModernPanel(bool isBackward)`
- `private void UpdateTabStopFlags()`
- `private DependencyObject? GetNextFocusablePeer(bool isBackward)`
- `private uint ValidateItemIndexForGroup(uint itemIndex, uint groupIndex, bool atBeginning, bool forceSet)`
- `private bool GroupHasElements(uint groupIndex)`
- `private bool HasBoundaryEmptyGroup(bool atBeginning)`
- `private void IdentifyParentElement(DependencyObject? pChild, out ElementType pParentElementType, out DependencyObject? pParent)`
- `private DependencyObject? GetNextTabStopForElementType(ElementType elementType, bool isBackward)`
- `private DependencyObject? GetNextTabStop(ElementType elementType, bool isBackward)`
- `private DependencyObject? GetFocusCandidateFromPanel()`
- `private void TryNextElementType(ElementType initialElementType, ElementType candidateElementType, bool isBackward, out ElementType pNewElementType, out DependencyObject? ppNextElement)`
- `private void ProcessTabStopInternal(DependencyObject? pFocusedElement, DependencyObject? pCandidateElement, bool isBackward, bool didCycleFocusAtRootVisualScope, out bool pHandled, out DependencyObject? ppNewTabStop)`
- `private void FindNextTabStopCandidateFromItem(int referenceItemIndex, bool isBackwards, bool supportGroupHeaders, out int focusCandidateIndex, out bool isCandidateAGroupHeader)`
- `private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args)`
- `private static ElementType GetElementTypeTransition(ElementType current, bool isBackward)`
- `internal Point GetDropOffsetToRoot()`: `internal override` once the base exists.
- `internal void UpdateClip()`
- `internal override bool FocusImpl(FocusState value)`
- `internal override TabStopProcessingResult ProcessTabStopOverride(DependencyObject? pFocusedElement, DependencyObject? pCandidateTabStopElement, bool isBackward, bool didCycleFocusAtRootVisualScope)`
- `internal override TabStopProcessingResult ProcessCandidateTabStopOverride(DependencyObject? pFocusedElement, DependencyObject pCandidateTabStopElement, DependencyObject? pOverriddenCandidateTabStopElement, bool isBackward)`: map `pIsCandidateTabStopOverridden`/`ppNewTabStop` to `new TabStopProcessingResult(isOverriden, newTabStop)`.
- `private void OnLoaded(object pSender, RoutedEventArgs pArgs)`: a separate overload from `FrameworkElement.OnLoaded()`.

### ListViewBase_Partial_Selection.cpp (`ListViewBase_Selection.partial.mux.cs`)

- `public IList<object> SelectedItems { get; }`
- `public IReadOnlyList<ItemIndexRange> SelectedRanges { get; }`
- `private void OnSelectionModeChangedImpl(ListViewSelectionMode oldMode, ListViewSelectionMode newMode)`
- `private protected override bool CanSelectMultiple { get; }`
- `private protected override void OnSelectionChanged(int oldSelectedIndex, int newSelectedIndex, object? pOldSelectedItem, object? pNewSelectedItem, bool animateIfBringIntoView = false, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)`
- `protected override void OnItemsChanged(object e)`
- `internal override void MakeSingleSelection(int index, bool animateIfBringIntoView, object? pSelectedItem, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)`
- `private void MakeSingleSelection(ListViewBaseItem pItem, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None)`
- `private void MakeToggleSelection(ListViewBaseItem pItem)`
- `private void MakeRangeSelection(uint index, bool clearOldSelection)`
- `private void MakeRangeSelection(ListViewBaseItem pItem, bool clearOldSelection)`
- `public void SelectAll()`
- `public void SelectRange(ItemIndexRange itemIndexRange)`
- `public void DeselectRange(ItemIndexRange itemIndexRange)`
- `internal void DeselectAll()`
- `private void InitializeDataSourceSelectionInfo()`
- `internal override bool IsSelectionPatternApplicable()`

### ListViewBase_Partial_Interaction.cpp (`ListViewBase_Interaction.partial.mux.cs`)

- `internal void GroupItemFocused(GroupItem pItem)`
- `internal void GroupItemUnfocused(GroupItem pItem)`
- `internal void GroupHeaderItemFocused(ListViewBaseHeaderItem pItem)`
- `internal void GroupHeaderItemUnfocused(ListViewBaseHeaderItem pItem)`
- `internal override void ItemFocused(SelectorItem pSelectorItem)`
- `internal void AutomationItemClick(ListViewBaseItem pItem)`
- `internal void AutomationHeaderItemClick(ListViewBaseHeaderItem headerItem)`
- `internal bool OnItemPrimaryInteractionGesture(ListViewBaseItem pItem, bool isKeyboardInput)`
- `internal bool OnItemSecondaryInteractionGesture(ListViewBaseItem pItem, bool isKeyboardInput)`
- `private protected bool OnSelectItemPrimary(ListViewBaseItem pItem, bool isKeyboardInput, bool shouldFocusItem)`
- `private protected bool OnSelectItemSecondary(ListViewBaseItem pItem, bool isKeyboardInput)`
- `private protected void OnItemClick(ListViewBaseItem pItem)`
- `internal void SetHandleKeyDownArgsFromItem(bool fFromItem)`
- `private void GetCacheStartAndEnd(bool isForGroupIndexes, out int cacheIndexStart, out int cacheIndexEnd)`
- `protected override void OnKeyDown(KeyRoutedEventArgs pArgs)`
- `protected override void OnKeyUp(KeyRoutedEventArgs pArgs)`
- `internal bool OnGroupHeaderKeyDown(object? pItem, VirtualKey originalKey, VirtualKey key)`
- `internal bool OnGroupHeaderKeyUp(object? pItem, VirtualKey originalKey, VirtualKey key)`
- `internal bool OnHeaderItemTap(object? pItem)`
- `private protected bool ToggleSemanticZoomActiveView(object? pItem)`
- `private protected bool OnGroupKeyboardNavigation(VirtualKey key, VirtualKey originalKey, bool animateIfBringIntoView)`
- `private protected void OnKeyboardNavigation(VirtualKey key, VirtualKey originalKey, bool animateIfBringIntoView, out int pNewFocusedIndex, out Controls.ElementType pNewFocusedType)`
- `private protected void GetClosestIndexToItemInGroupPanel(uint sourceIndexInCollection, uint targetGroupIndex, KeyNavigationAction action, GroupItem pSourceGroupItem, out int pClosestIndex, out bool pFoundItem, out bool pUseFallback)`
- `private protected Point GetNavigationSourcePoint(FrameworkElement pElementNavigatedFrom, KeyNavigationAction action)`
- `private protected void FindFirstSelectableItemInGroup(uint groupIndex, bool fromBeginning, int startIndexInCollection, out int pFirstSelectableItemIndexInCollection, out bool pFoundSelectableItem)`
- `private protected void GetGroupInformation(uint groupIndex, out uint pGroupStartIndex, out uint pGroupSize, out bool pIsValid)`
- `private protected bool ShouldNavDirectionIgnoreGroups(KeyNavigationAction action)`
- `private protected void TranslateKeyToKeyNavigationAction(VirtualKey key, out KeyNavigationAction pNavAction, out bool pIsValidKey)`
- `private protected void QueryPanelForItemNavigation(Panel pTargetPanel, KeyNavigationAction action, int sourceFocusedIndex, Controls.ElementType sourceFocusedType, Func<uint, Controls.ElementType, bool> verificationFunction, bool allowWrap, uint itemIndexHintForHeaderNavigation, out int pNewFocusedIndex, ref Controls.ElementType pNewFocusedType, out bool pIsHandled, out bool pUseFallback)`
- `private protected void QuerySelectorFallbackForItemNavigation(VirtualKey key, int sourceFocusedIndex, out int pNewFocusedIndex, out bool pIsHandled)`
- `private protected void QueryGroupFallbackForGroupNavigation(VirtualKey key, int sourceGroupIndex, out int pNewGroupIndex, out bool pIsHandled)`
- `private protected int GetGroupCount()`
- `private protected void QueryGroupFallbackForItemNavigation(Panel pGroupPanel, VirtualKey key, int sourceIndexInCollection, int groupLeftEdgeIndex, int groupRightEdgeIndex, out int pNewFocusedIndexInCollection, ref bool pIsHandled)`
- `protected override void OnGotFocus(RoutedEventArgs pArgs)`
- `protected override void OnLostFocus(RoutedEventArgs pArgs)`
- `private protected void OnFocusChanged(bool hasFocus)`
- `internal void OnGroupFocusChanged(bool hasFocus, bool headerHasFocus, FocusState howFocusChanged)`
- `internal void FocusItem(FocusState focusState, ListViewBaseItem pItem)`
- `private protected override void SetFocusedItem(int index, bool shouldScrollIntoView, bool animateIfBringIntoView = false, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None, InputActivationBehavior inputActivationBehavior = InputActivationBehavior.RequestActivation)`
- `private protected override void SetFocusedItem(int index, bool shouldScrollIntoView, bool forceFocus, FocusState focusState, bool animateIfBringIntoView = false, FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None, InputActivationBehavior inputActivationBehavior = InputActivationBehavior.RequestActivation)`
- `private void OnIsSelectionActiveChanged()`
- `internal void SetHoldingItem(ListViewBaseItem pItem)`
- `internal void ClearHoldingItem(ListViewBaseItem pItem)`
- `internal void ClearHoldingState()`
- `internal bool IsInExclusiveInteraction()`
- `private static bool IsGroupItemFocusable(IGroupHeaderMapping pMapping, uint groupIndex)`
- `private static bool IsItemFocusable(IItemContainerMapping pMapping, uint itemIndex, bool unrealizedItemsAssumedFocusable)`
- `private bool CanPerformKeyboardNavigation(VirtualKey key)`

### ListViewBase_Partial_DataVirtualization.cpp (`ListViewBase_DataVirtualization.partial.mux.cs`)

- `public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync()`
- `internal IAsyncInfo? LoadMoreItemsAsyncInternal(LoadMoreItemsOperation? pChainedAsyncOperation)`
- `private void ProcessDataVirtualizationScrollOffsets()`
- `private protected override void VirtualizationFinished()`
- `private void InitializeDataSourceItemsRangeInfo()`
- `private void InvokeDataSourceRangesChanged()`
- `private void InvokeDataSourceRangesChangedHelper(ItemIndexRange visibleRange, TrackerCollection<ItemIndexRange> trackedItems)`
- `private void OnLoadAsyncCompleted(IAsyncOperation<LoadMoreItemsResult> getOperation, AsyncStatus status)`: declared in the SemanticZoom part of the header, defined here.

### ListViewBase_Partial_Reorder.cpp (`ListViewBase_Reorder.partial.mux.cs`)

The `LISTVIEWBASE_EDGE_SCROLL_*` and `*_LIVEREORDER_TIMER` `#define`s become `private const` at the top of this file. `UpdateTrackedIndex` is `private static void UpdateTrackedIndex(int realItemIndex, int insertIndex, ref int pTrackedIndex)`.

- `internal bool IsCurrentlyReordering()`
- `internal bool OnBackButtonPressed()`
- `internal bool IsContainerDragDropOwner(UIElement pContainer)`
- `internal bool IsInDragDrop()`
- `internal int DragItemsCount()`: not `uint`, because `ListViewBaseItemVisualStatesCriteria.dragItemsCount` is `int` and `ListViewBaseItem.mux.cs:964` assigns directly. Body is `(int)m_dragItemsCount`.
- `internal void UpdateDragPositionOnCapturedTarget(DragDropMessageType type, Point dragPoint)`
- `internal bool IsDragOverItem(SelectorItem pItem)`: `Given_GridViewItemPresenter` and `Given_ListViewBaseItem` call it. C++ takes `ListViewBaseItem*`; `SelectorItem` matches the field and accepts every caller.
- `public bool IsDragSource()`
- `private protected bool OnDragItemsStarting(IList<object> pDraggedItems, DataPackage pData)`
- `internal bool OnDragGesture(Pointer pointer, PointerPoint pointerPoint, ListViewBaseItem pOriginatingItem, object pDragVisual, Point dragStartPoint, Action onDragStarting)`
- `private protected bool OnDragMoveGesture(Pointer? pointer, Point dragPoint)`
- `private protected bool OnDropGesture(Pointer? pointer, Point dropPoint)`
- `private protected void CancelDrag()`
- `private protected void OnReorderDrop(Point dropPoint)`
- `protected override void OnDragEnter(DragEventArgs pArgs)`
- `protected override void OnDragLeave(DragEventArgs pArgs)`
- `protected override void OnDragOver(DragEventArgs pArgs)`
- `protected override void OnDrop(DragEventArgs pArgs)`
- `private bool ProcessDragOverAt(Point dragPoint)`
- `private void GetDraggedItems(ListViewBaseItem pOriginatingItem, bool wantItems, out IList<object>? ppItems, List<uint>? pItemIndices)`
- `private void ReorderItemsTo(List<uint> pReorderItemIndexes, int insertIndex, ref int pTrackedIndex1, ref int pTrackedIndex2)`
- `private bool DropCausesReorder()`
- `private bool IsItemReorderingActive()`
- `private bool IsNotGroupingAndHostIsLookupPanel()`
- `private void CompleteDrop()`
- `private void ClearPrimaryDragContainer()`
- `private bool ShouldAcceptDragInput(Pointer? pointer)`
- `private static bool AreSamePointer(Pointer pPointerA, Pointer pPointerB)`
- `private DataPackage GetBlankDataPackage()`
- `internal bool GetIsDragEnabled()`
- `private PanVelocity ComputeEdgeScrollVelocity(Point dragPoint)`
- `private float ComputeEdgeScrollVelocityFromEdgeDistance(double distanceFromEdge)`
- `private void SetPendingAutoPanVelocity(PanVelocity velocity)`
- `private void ScrollWithVelocity(PanVelocity velocity)`
- `private void EnsureStartEdgeScrollTimer()`
- `private void DestroyStartEdgeScrollTimer()`
- `private void StartEdgeScrollTimerTick(object? pUnused1, object? pUnused2)`
- `private void NotifyDragVisualCreated(object pDragVisual)`
- `private void NotifyDragVisualReleased()`
- `internal void OnKeyboardReorder(KeyboardReorderDirection direction, ListViewBaseItem pFocusedItem)`
- `private void ToggleBackKeyListenerOnPhone(bool bIsZoomedOutViewActive)`
- `private void OnDragCompleted(IAsyncOperation<DataPackageOperation> pAsyncOp, AsyncStatus status, IList<object> pItems)`
- `private bool ShouldPlayLiveReorder()`
- `private void StartLiveReorderTimer()`
- `private void EnsureLiveReorderTimer()`
- `private void StopLiveReorderTimer()`
- `private void LiveReorderTimerTickHandler()`
- `private void GetNewMovedItemsForLiveReorder(List<MovedItem> newItems)`
- `private void AddNewItemForLiveReorder(int sourceIndex, int targetIndex, List<MovedItem> newItems)`
- `private void ResetAllItemsForLiveReorder()`
- `private void MoveItemsForLiveReorder(bool areNewItems, List<MovedItem> newItemsToMove)`
- `private Rect GetLayoutSlot(int itemIndex)`
- `private int GetItemsCount()`: distinct from `ItemsControl.GetItemCount()`, which is `private protected`, so both can coexist.
- `internal Orientation GetPanelOrientation()`: `GetPanelOrientations` does not exist in Uno. Keep the old switch (`ItemsStackPanel`, `ItemsWrapGrid`, `WrapPanel`, fallback `GetItemsHostOrientations().LogicalOrientation`) until the panels implement `IOrientedPanel`; the `ItemsWrapGrid` port already does.
- `internal void SetDragOverItem(ListViewBaseItem? dragOverItem)`
- `private protected override Rect OverrideContainerArrangeBounds(int index, Rect suggestedBounds)`

### ListViewBase_Partial_SemanticZoomInformation.cpp (`ListViewBase_SemanticZoomInformation.partial.mux.cs`)

- `public void InitializeViewChange()`
- `public void CompleteViewChange()`
- `public void MakeVisible(SemanticZoomLocation item)`
- `public void StartViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)`
- `public void StartViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)`
- `public void CompleteViewChangeFrom(SemanticZoomLocation source, SemanticZoomLocation destination)`
- `public void CompleteViewChangeTo(SemanticZoomLocation source, SemanticZoomLocation destination)`
- `private bool CanSemanticZoomGroup { get; }` (`get_CanSemanticZoomGroup`)
- `private void FindFirstItem(uint groupIndex, out int pItemIndex, out object? ppItem)`

### ListViewBase_Partial_UIAProperties.cpp (`ListViewBase_UIAProperties.partial.mux.cs`)

- `internal string? GetDropTargetDropEffect()`
- `internal string[] GetDropTargetDropEffects()`
- `internal IRawElementProviderSimple[] GetDragGrabbedItems()`
- `private void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving)`
- `private void UpdateDropTargetDropEffect(bool forceUpdate, bool isLeaving, UIElement? keyboardReorderedContainer)`
- `private void UpdateDragGrabbedItems(bool forceUpdate)`
- `private IRawElementProviderSimple[] CreateCopyOfDragGrabbedItems()`
- `private string? GetAutomationString(DependencyObject item)`

`ListViewBaseAutomationPeer.GetDropEffect` and `GetDropEffects` currently test `ListViewBaseOwner is IDropTargetProvider`, which is never true. Point them at these two methods at cut-over.

### ListViewBase_Partial_ContainerPhase.cpp (`ListViewBase_ContainerPhase.partial.mux.cs`)

- `public void SetDesiredContainerUpdateDuration(TimeSpan duration)`
- `private protected override bool IsPrepareContainerForItemDeferred(DependencyObject element)`
- `private protected override void SetupContainerContentChangingAfterPrepare(DependencyObject container, object? item, int itemIndex, Size measureSize)`
- `private protected override void RegisterWorkForContainer(UIElement pContainer)`
- `private protected override void RegisterWorkFromArgs(ContainerContentChangingEventArgs pArgs)`
- `internal void RegisterIfWorkPending()`
- `internal bool IsBuildTreeSuspended { get; }`
- `internal bool BuildTree()`
- `internal void ShutDownDeferredWork()`
- `private protected override bool ShouldRaiseChoosingItemContainer()`
- `private protected override bool ShouldRaiseChoosingGroupHeaderContainer()`
- `private protected override void RaiseChoosingItemContainer(ChoosingItemContainerEventArgs pArgs)`
- `private protected override void RaiseChoosingGroupHeaderContainer(ChoosingGroupHeaderContainerEventArgs pArgs)`
- `private protected override void RaiseContainerContentChangingOnRecycle(UIElement container, object? item)`

### ListViewBase_Partial_Serialization.cpp (`ListViewBase_Serialization.partial.mux.cs`)

- `internal string GetRelativeScrollPosition(ListViewItemToKeyHandler itemToKeyHandler)`
- `internal IAsyncAction SetRelativeScrollPositionAsync(string relativeScrollPosition, ListViewKeyToItemHandler keyToItemHandler)`
- `private IBuffer CreateSerializationBuffer(ListSerializationData serializationData)`
- `private bool ParseSerializationBuffer(IBuffer pBuffer, out ListSerializationData pSerializationData)`
- `private string EncodeBuffer(IBuffer pBuffer)`
- `private IBuffer DecodeBuffer(string base64String)`
- `private Action<object?> GetDeserializationScrollCommand(bool isHeader, bool isFooter, double offset)`

## Already in the header port

Do not reimplement these. They appear here so the map covers every header method.

- `internal const string ListViewBaseName`
- PanVelocity: `internal static PanVelocity Stationary()`, `internal readonly bool IsStationary()`, `internal void Clear()`
- LiveReorderIndices: `internal LiveReorderIndices(int draggedItemIndex, int draggedOverIndex, int itemsCount)`
- `private const uint s_reorderConfirmationDelayInMsec`, `private const float s_reorderConfirmationThresholdInDips`: their values are in `_Reorder.cpp`, moved to the header.
- `private protected int GetFocusedGroupIndex()`
- `private protected void SetFocusedGroupIndex(int focusedGroupIndex)`
- `private protected int GetLastFocusedGroupIndex()`
- `private protected void SetLastFocusedGroupIndex(int lastFocusedGroupIndex)`
- `private static bool IsListViewBaseItem(object? pItem)`
- `private static bool IsListViewBaseHeaderItem(object? pItem)`
- `private void SetAnchorIndex(uint index)`
- `private void ReleaseAnchorIndex()`
- `internal bool HasFocusedGroup()`
- `internal bool GetIsHolding()`
- `internal void SetIsHolding(bool isHolding)`
- `internal bool IsInLiveReorder()`
- `private int GetInsertionIndexForLiveReorder()`
- From `ListViewBase.g.h`: `internal bool IsRegisteredForCallbacks`; events `ChoosingGroupHeaderContainer`, `ChoosingItemContainer`, `ContainerContentChanging`, `DragItemsCompleted`, `DragItemsStarting`, `ItemClick`; DPs `CanDragItems`, `CanReorderItems`, `FooterTransitions`, `HeaderTransitions`, `IsActiveView`, `IsMultiSelectCheckBoxEnabled`, `IsSwipeEnabled`, `IsZoomedInView`, `ReorderMode`, `SemanticZoomOwner`, `ShowsScrollingPlaceholders`, `SingleSelectionFollowsFocus`; forwarders for `ITreeBuilder`, `IBackButtonListener`, `IDirectManipulationStateChangeHandler`.

The nine mixin DPs (`Header`, `HeaderTemplate`, `Footer`, `FooterTemplate`, `SelectionMode`, `IsItemClickEnabled`, `DataFetchSize`, `IncrementalLoadingThreshold`, `IncrementalLoadingTrigger`) stay in `DependencyPropertyMixinGenerator`. Their `protected virtual On<Name>Changed` methods are public surface and stay.

## Existing Uno members

Every public or internal member of the old files (`ListViewBase.cs`, `.managed.cs`, `.Properties.cs`, `_Partial.cs`, `.h.mux.cs`, `.DragDrop.cs`, `.DragDrop.mux.cs`), with its callers outside those files. Grepped under `src/Uno.UI`, `src/Uno.UI.RuntimeTests`, `src/Uno.UI.UnitTests`.

**Same**: the port has this name and a compatible signature. **New**: the port has another name and the caller changes. **Forwarder**: not in C++, so keep a small Uno member. **Drop**: nothing outside uses it.

### Called from outside

| Member | Callers | Port |
|---|---|---|
| `IsCustomReorder` | `TreeViewList.cs:37` | **Forwarder.** Keep `internal bool IsCustomReorder;`. `TreeViewList` overrides drag handling itself, and the reorder path must skip its own handling when set, as `DragDrop.cs:171` does. |
| `AutomationItemClick(SelectorItem)` | `ItemInvokeAdapter.mux.cs:34` | **Same**, parameter narrows to `ListViewBaseItem`. The caller already passes one. |
| `RaiseContainerContentChanging(object, SelectorItem, int)` | `SelectorItem.cs:155` | **Forwarder, or delete with its caller.** `SetupContainerContentChangingAfterPrepare` replaces it. If `PendingContainerContentChanging` and its fallback in `SelectorItem` go at cut-over, drop this. Otherwise forward to the ContainerPhase path. |
| `FocusedIndexContainerItem` | `SelectorItem.cs:564-567` (`OnGotFocus`) | **Drop, removing the caller.** `ListViewBaseItem` reaches `ItemFocused`, which sets `m_focusedIndex`. Remove the block from `SelectorItem.OnGotFocus` and the `// TODO Uno` at `ListViewBaseItem.mux.cs:650`. |
| `IsSelectionMultiple` | `Selector_Partial.cs:409,431,456` | **Forwarder**: `internal bool IsSelectionMultiple => CanSelectMultiple;`. Same value for all four modes. |
| `IsSingleSelection` (`override`) | `Selector.cs:509`, `SelectorAutomationPeer.cs:32` | **Forwarder**: keep the override. See `Uno issues recorded`: the peer reads it wrongly for `SelectionMode.None`. |
| `ScrollIntoView` (both overloads) | `Selector.cs:216`, `Selector_Partial.cs:376,380` | **Same.** The callers are the recursion trap in Rules. |
| `SelectedItems` | `Selector_Partial.cs:411-458`, `SelectorAutomationPeer.cs:70` | **Same.** |
| `IsItemClickEnabled`, `SelectionMode`, `IsMultiSelectCheckBoxEnabled` | `SelectorItem.cs`, peers, `ListViewBaseItem.mux.cs` | **Same.** |
| `SelectedItem` | `SelectorAutomationPeer.cs:82` | **Same**, on `Selector`. |
| `SemanticZoomOwner`, `IsActiveView` | `ListViewBaseAutomationPeer.cs` | **Same**, now real DPs. |
| `ItemsPresenter` | `ListViewBaseAutomationPeer.cs:45` | **Same**, on `ItemsControl`. |
| `CanReorderItems`, `CanDragItems` | `TabView.cs`, `TreeViewList.cs`, `ListViewBaseItem.mux.cs` | **Same.** |
| `IsInExclusiveInteraction` | `ListViewBaseItem.mux.cs:822,877` | **Same.** |
| `IsInDragDrop`, `IsDragOverItem`, `DragItemsCount`, `IsContainerDragDropOwner`, `GetIsHolding` | `ListViewBaseItem.mux.cs:962-970` | **Same.** |
| `SetIsHolding`, `SetHoldingItem`, `ClearHoldingItem` | `ListViewBaseItem.mux.cs:268-282` | **Same.** |
| `GetPanelOrientation`, `SetDragOverItem` | `ListViewBaseItem.mux.cs:1177-1206` | **Same.** |
| `IsLiveReorderPrimaryContainer(UIElement, DragEventArgs)` | `ListViewBaseItem.mux.cs:1170` | **Forwarder while Uno's drag lives on.** `!IsCustomReorder && args.DataView.FindRawData(ReorderOwnerFormatId) == this && IsContainerDragDropOwner(container)`, so `ReorderOwnerFormatId` must stay too. If the Reorder port brings WinUI's live reorder, where the dragged item leaves an empty gap, delete this and the check at `:1170`. |
| `TryLoadMoreItems(int)` | `VirtualizingPanelLayout.managed.cs:315` | **Drop**: the caller goes in phase 5. `ProcessDataVirtualizationScrollOffsets` and `LoadMoreItemsIfNeeded` replace it. |
| `ListViewBaseItem.GetParentListView`, `ChangeSelectorItemsVisualState` | `ListViewBaseItem.mux.cs` | Not on `ListViewBase`; unaffected. |
| `Given_*` runtime tests: `IsDragOverItem`, `GetIsHolding` | `Given_ListViewBaseItem.cs`, `Given_GridViewItemPresenter.cs` | **Same.** |

Other callers, checked:
- `ComboBox`: its `TryHandleKeyDown` is its own overload. `FlipView` has no `ListViewBase` reference.
- `TabView`, `AutoSuggestBox`, `MediaTransportControls`: public API only.
- `NavigationView`, `SwipeControl`, `FocusManager`, `DropUITarget`, `Control`, `ScrollViewer`, `UIElement`: comments only.
- Subclasses only override public or `ItemsControl` members:
  - `ListView`, `GridView`: `GetGroupHeaderContainer`, container overrides. `ListView` also has `OnCreateAutomationPeer`.
  - `TabViewListView`: `OnItemsChanged(object)`, `PrepareContainerForItemOverride`. Its `base.OnItemsChanged(item)` reaches the new override, so that override's cast has to accept what `TabViewListView` passes.
  - `TreeViewList`: `OnDrop`, `OnDragOver`, `OnDragEnter`, `OnDragLeave`, `OnApplyTemplate`, `PrepareContainerForItemOverride`, `OnCreateAutomationPeer`.

### Not called from outside

| Member | Port |
|---|---|
| `RefreshOnCollectionChanged` (`public`) | **Forwarder, by decision.** Uno-only public API with no caller in the repo. Removing it is a breaking change. If kept, `OnItemsSourceSingleCollectionChanged` and `OnItemsSourceGroupsChanged` must still route to a full refresh when set. |
| `OnCreateAutomationPeer` (`protected override`) | **Forwarder.** Not in the C++ header. `GridView` does not override it, so it depends on this one. Keep it in `ListViewBase.partial.mux.cs`. |
| `ContainerContentChanging`, `ItemClick`, `DragItemsStarting`, `DragItemsCompleted` | **Same**, in the header port. |
| `CanReorderItemsProperty`, `CanDragItemsProperty`, `SingleSelectionFollowsFocusProperty`, `IsMultiSelectCheckBoxEnabledProperty` | **Same**, in the header port. |
| `ArrangeOverride`, `OnKeyDown`, `OnApplyTemplate`, `PrepareContainerForItemOverride`, `OnDrop` | **Same**, in the signature lists. |
| `IsDraggableOrPannable`, `GetIsDragEnabled`, `IsContainerDragDropOwner`, `ClearHoldingState` | **Same.** |
| `TryHandleKeyDown(KeyRoutedEventArgs)` | **Drop.** `OnKeyDown` is ported. `ComboBox` has its own overload. |
| `ExtendedShiftSelectionStart` | **Drop.** `m_pAnchorIndex` replaces it. |
| `ShouldShowHeader`, `ShouldShowFooter`, `ResolveHeaderContext`, `ResolveFooterContext` | **Drop.** No caller, the `VirtualizingPanelLayout` era. |
| `ChangeSelectedItem`, `OnSelectedItemChanged`, `OnSelectedIndexChanged`, `IsSelected(int)`, `OnItemClicked(int, VirtualKeyModifiers)` (`internal override`) | **Drop the overrides.** They keep the old model; `OnSelectionChanged` and `OnItemPrimaryInteractionGesture` replace them. Check `SelectorItem`'s click path, which still ends in `Selector.OnItemClicked`: `ListViewBaseItem` must take the WinUI tap path or clicks run twice. |
| `OnItemsSourceSingleCollectionChanged`, `OnItemsSourceGroupsChanged`, `OnGroupPropertyChanged`, `OnItemsSourceChanged` | Base class handles these. Keep a thin `OnItemsSourceSingleCollectionChanged` override for the `OnItemsChanged` trap in Rules. |
| `ContainerPreparedForItem`, `ContainerClearedForItem` | **Drop the overrides.** |
| `OnDataContextChanged` | **Drop.** It only supported the header context. |
| Private `.managed.cs` helpers (`PageSize`, `AddItems`, `RemoveItems`, `MoveItems`, `AddGroup`, `RemoveGroup`, `ReplaceGroup`, `ScrollIntoViewFastPath`), old collection repair helpers | **Drop** with `VirtualizingPanelLayout`. |
| Private `DragDrop.cs` helpers (`OnItemContainerDragStarting`, `OnReorderDragUpdated`, `DoMove` and friends) | **Reorder porter's call.** See "Pending porting work": `DoMove` is the closest Uno equivalent of `ReorderItemsTo`'s vector handling. |

## Public API

Everything in `Generated\3.0.0.0\Microsoft.UI.Xaml.Controls\ListViewBase.cs` marked NotImplemented. Flip those to `#if false` at integration. Rows marked `.g.h` are in the header port.

| Member | Implemented in |
|---|---|
| `FooterTransitionsProperty`, `FooterTransitions` | `.g.h` |
| `HeaderTransitionsProperty`, `HeaderTransitions` | `.g.h` |
| `IsActiveViewProperty`, `IsActiveView` | `.g.h` |
| `IsSwipeEnabledProperty`, `IsSwipeEnabled` | `.g.h`; `_Partial.cpp` `OnPropertyChanged2` has an empty case |
| `IsZoomedInViewProperty`, `IsZoomedInView` | `.g.h` |
| `ReorderModeProperty`, `ReorderMode` | `.g.h`; read in `_Reorder.cpp` |
| `SemanticZoomOwnerProperty`, `SemanticZoomOwner` | `.g.h` |
| `ShowsScrollingPlaceholdersProperty`, `ShowsScrollingPlaceholders` | `.g.h`; getter from `_Partial.cpp:231` |
| `SelectedRanges` | `_Selection.cpp:37` |
| `SelectAll()` | `_Selection.cpp:468` |
| `SelectRange(ItemIndexRange)` | `_Selection.cpp:475` |
| `DeselectRange(ItemIndexRange)` | `_Selection.cpp:491` |
| `LoadMoreItemsAsync()` | `_DataVirtualization.cpp:159` |
| `SetDesiredContainerUpdateDuration(TimeSpan)` | `_ContainerPhase.cpp:28` |
| `IsDragSource()` | `_Reorder.cpp:2066` |
| `PrepareConnectedAnimation(string, object, string)` | `_Partial.cpp:2973`, inert |
| `TryStartConnectedAnimationAsync(ConnectedAnimation, object, string)` | `_Partial.cpp:3020`, inert |
| `InitializeViewChange()` | `_SemanticZoomInformation.cpp:22`, inert |
| `CompleteViewChange()` | `_SemanticZoomInformation.cpp:59`, inert |
| `MakeVisible(SemanticZoomLocation)` | `_SemanticZoomInformation.cpp:75`, inert |
| `StartViewChangeFrom(SemanticZoomLocation, SemanticZoomLocation)` | `_SemanticZoomInformation.cpp:325`, inert |
| `StartViewChangeTo(SemanticZoomLocation, SemanticZoomLocation)` | `_SemanticZoomInformation.cpp:772`, inert |
| `CompleteViewChangeFrom(SemanticZoomLocation, SemanticZoomLocation)` | `_SemanticZoomInformation.cpp:783`, inert |
| `CompleteViewChangeTo(SemanticZoomLocation, SemanticZoomLocation)` | `_SemanticZoomInformation.cpp:791`, inert |
| `ChoosingGroupHeaderContainer` event | `.g.h`; raised in `_ContainerPhase.cpp:1018` |
| `ChoosingItemContainer` event | `.g.h`; raised in `_ContainerPhase.cpp:1000` |

`ListViewPersistenceHelper` (also `NotImplemented` in `Generated`) forwards to `GetRelativeScrollPosition` and `SetRelativeScrollPositionAsync` in `_Serialization.cpp`. Its `ListViewPersistenceHelper_Partial.cpp` has no owner yet.

The class-level interface `ISemanticZoomInformation` stays declared on the generated partial, so `ListViewBase.partial.h.mux.cs` does not repeat it.
