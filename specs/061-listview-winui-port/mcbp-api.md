# ModernCollectionBasePanel: C# signature map

Single source of truth for every file ported against `ModernCollectionBasePanel_Partial.h`. Write these signatures exactly. Accessibility comes from the C++ section: `public` becomes `internal`, `protected` becomes `private protected`, `private` stays `private`.

Members under "Already in the header port" must not be written again. Their code lives in `ModernCollectionBasePanel.partial.h.mux.cs`, and the fields, properties, events and explicit interface forwarders from `ModernCollectionBasePanel.g.h.mux.cs` are also done.

## Rules for the .cpp ports

**Interfaces.** Don't write interface implementations. `ModernCollectionBasePanel.g.h.mux.cs` has all the explicit forwarders. Write the plain `internal` member listed here, with the `Impl` suffix removed.

**`Impl` suffix.** It stays on three members:
- `GetGroupInformationFromItemIndexImpl`, `GetGroupInformationFromGroupIndexImpl`: dropping it would collide with the public forwarders of the same signature.
- `GetItemsPerPageImpl`: the virtual helper behind `GetItemsPerPage`.

**Not ported.** `QueryInterfaceImpl` (all classes) and destructors have no C# form, with these exceptions:
- `Initialize()` runs at the end of the panel constructor.
- `~ModernCollectionBasePanel`: no C# finalizer. It unhooks the items source and group style handlers and calls `Uninitialize`, so keep the body in `#if !HAS_UNO` with a `// TODO Uno` note. An items source outliving the panel would pin it through that handler, which is why the subscription needs a weak handler or an unhook in `DisconnectItemsHost`.
- `CacheManager::~CacheManager`, `ContainerManager::~ContainerManager`: drop them.

**Constructors** (defined in the .cpp, not the header). Assign the initializer list as written.
- Field initializers already cover: `m_windowState`, `m_snapPointState`, `m_viewportBehavior`, recycle queues, every `ContainerManager` array and `m_unlinkedFillers`, `m_focusedContainer`, `m_focusedHeaderContainer`, `m_preparationHoldingArea`. `m_validElements`-style arrays are indexed `[(int)type]`.
- Still needs the constructor: `m_cacheManager`, `m_containerManager`, `m_transitionContextManager` (they take `this`), `m_icg2`, `m_unloadingElements`, `m_tpUnloadedElements`, `m_spLayoutDataInfoProvider`, and `m_indicesOfFirstValidElement`/`m_lastFoundIndexElements` filled with -1.
- `new GroupCache()` runs the -1 constructor. `default` and `List.Capacity` growth do not, so `resize(n)` means adding `new GroupCache()` n times.

**Value types.**
- `CollectionIterator` is a `struct` even though C++ declares a `class`: `Generate` and `RemoveMeasureLeftOvers` take it by value and mutate their copy, and `RunGenerate` then calls `MovePrevious` from the original position. A class would break that silently.
- `PinnedElementInfo` stays a `class`. Every C++ copy is either a read or is followed by erasing the original, and `for_each` mutates in place. `m_focusedContainer = PinnedElementInfo()` is `= new()`.
- `GroupCache`, `ElementInfo`, `ViewportBehaviorInfo`, `StickyHeaderInfo`, `PreparationHoldingArea` are structs, so a `List<T>` element needs copy, edit, write back. Never copy `m_viewportBehavior` to a local to mutate it.
- `Primitives.ElementInfo` (`GetClosestElementInfo`) and `CollectionIterator.ElementInfo` are different types.

**`_Out_` that is not always written.** Some callers seed a sentinel and rely on the callee leaving it alone, so these use `ref`, not a return value:
- `WindowState.WindowCommand` is `delegate void (ref Rect pNewVisibleWindow)`. `DetermineWindowAndAnchors` seeds `{-1,-1,-1,-1}`, then calls the command or `BeginTrackingOnRefresh`.
- `GetGenerationAnchorInformation` takes `ref CollectionIterator` because it calls `Init` on the caller's iterator.

**Pointer-to-member helpers.** `float wf::Rect::*` has no C# form. Use a getter and setter pair, as `LayoutStrategyBase` does:
- `x.*PointFromRectInVirtualizingDirection()` is `PointFromRectInVirtualizingDirection(x)`.
- `x.*Foo() = v` is `SetFoo(ref x, v)`, and `x.*Foo() += v` is `SetFoo(ref x, Foo(x) + v)`.
- `LayoutReference` and `EstimationReference` members are properties, so copy to a local before `ref`.

**Virtualization info.**
- C++ `GetVirtualizationInformation()` returns null if none was created; Uno's `UIElement.GetVirtualizationInformation()` allocates. In ported code use `GetVirtualizationInformationOrNull()`, or the panel helpers, and `InitVirtualizationInformation()`.
- Accessor names: `GetBounds`/`SetBounds` is `Bounds`; `GetItem`/`SetItem` is `Item`; `Get/SetIsRealized` is `IsRealized`; `Get/SetIsGenerated` is `IsGeneratedContainer`; `Get/SetIsHeader` is `IsHeader`; `Get/SetWasRejectedAsAContainerByApp` is `WasRejectedAsAContainerByApp`; `Get/SetMeasureSize` is `MeasureSize`; `Get/SetSelectedTemplate` is `SelectedTemplate`; `GetIsSticky` is `IsSticky`; `Get/SetWantsToSkipContainerPreparation` is `WantsToSkipContainerPreparation`; `GetMaySkipPreparation` is `MaySkipPreparation`; `Get/SetIsPrepared` is `IsPrepared`; `Get/SetIsContainerFromTemplateRoot` is `IsContainerFromTemplateRoot`. `GetBuildTreeArgs()` and `m_stickyHeaderWrapper` keep their names.
- `StickyHeaderWrapper` (`StickyHeaderWrapper.h/.cpp`, namespace `DirectUI`) has to be ported by the sticky headers work; `VirtualizationInformation` already references it.

**Other mappings.**
- Events: `ctl::EventPtr` and `EventRegistrationToken` are `SerialDisposable` (`m_epX.Disposable = Disposable.Create(...)`). Raise with `VisibleIndicesUpdated?.Invoke(this, null!)`, and the same for the two snap point events.
- Containers: `IVector<UIElement*>` is `IList<UIElement>`, `IVectorView<FLOAT>` is `IReadOnlyList<float>`, `IVectorView<UINT>` is `IReadOnlyList<uint>`, and `m_spGroupSizes`/`m_spNonEmptyGroupSizes` are `List<uint>?`.
- `std::function` filters are `Func<int, UIElement, bool>`.
- Assertions: `global::System.Diagnostics.Debug.Assert`, matching the strategy ports. `IFCEXPECT` throws `InvalidOperationException`.
- `DBG` is `DEBUG`. Telemetry `TraceLoggingWrite` stays in `#if !HAS_UNO`.
- `AreStickyGroupHeadersEnabledBase` is a real DP. Its default of `true` is inferred, because `ItemsStackPanel_Partial.cpp` only pushes changes and the public default is `true`.

## Callers that must keep working

`ItemsControlAutomationPeer` casts to the panel and must not be edited. C++ `get_X(&v)` is property access.

| Peer call | C++ | C# member | Where |
|---|---|---|---|
| `FirstCacheIndexBase` | `get_FirstCacheIndexBase` | `internal int FirstCacheIndexBase { get; set; }` | `.g.h.mux.cs` |
| `LastCacheIndexBase` | `get_LastCacheIndexBase` | `internal int LastCacheIndexBase { get; set; }` | `.g.h.mux.cs` |
| `FirstCacheGroupIndexBase` | `get_FirstCacheGroupIndexBase` | `internal int FirstCacheGroupIndexBase { get; set; }` | `.g.h.mux.cs` |
| `LastCacheGroupIndexBase` | `get_LastCacheGroupIndexBase` | `internal int LastCacheGroupIndexBase { get; set; }` | `.g.h.mux.cs` |
| `HeaderFromIndex(i)` | `HeaderFromIndex` / `HeaderFromIndexImpl` | `internal DependencyObject? HeaderFromIndex(int index)` | IICM port |
| `ContainerFromIndex(i)` | `ContainerFromIndex` / `ContainerFromIndexImpl` | `internal DependencyObject? ContainerFromIndex(int index)` | IICM port |
| `IndexFromContainer(c)` | `IndexFromContainer` / `IndexFromContainerImpl` | `internal int IndexFromContainer(DependencyObject container)` | IICM port |
| `GetGroupInformationFromItemIndex(i, out, out, out)` | same | `internal void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)` | header, inline |

## Defined in the .cpp files

Target file names follow the plan: `ModernCollectionBasePanel_<Part>.partial.mux.cs`.

### ModernCollectionBasePanel_Partial.cpp (`ModernCollectionBasePanel.partial.mux.cs`)

- `private protected ModernCollectionBasePanel()`
- `internal static double GetDefaultCacheLength()`
- `private protected void SetLayoutStrategyBase(ILayoutStrategy spLayoutStrategy)`
- `internal ILayoutStrategy GetLayoutStrategy()`
- `internal void RegisterItemsHost(IGeneratorHost pHost)`
- `private void UpdateGroupStyle()`
- `private void OnGroupStyleChanged(DependencyObject pSender, DependencyProperty pDP)`
- `internal void DisconnectItemsHost()`
- `private bool IsItemsHostRegistered()`
- `private protected void SetGroupHeaderPlacement(GroupHeaderPlacement placement)`
- `private GroupHeaderStrategy GetGroupHeaderStrategy()`
- `private protected void ReevaluateGroupHeaderStrategy()`
- `internal PanelScrollingDirection PanningDirectionBase { get; set; }` (`get_`/`put_PanningDirectionBaseImpl`)
- `protected override Size MeasureOverride(Size availableSize)`
- `private void RunVirtualization()`
- `private void MeasureElementsInGarbageSection()`
- `private void MeasureSpecialElements()`
- `private void RunGenerate()`
- `private void RemoveMeasureLeftOvers(CollectionIterator iterator, bool goForward)`
- `private void Generate(CollectionIterator iterator, LayoutReference referenceInformation, bool goForward)`
- `private void PrunePinnedContainers()`
- `private void EnsureRecycleContainerCandidate(int index, bool fromFront)`
- `private void EnsureRecycleHeaderCandidate(bool fromFront, bool alwaysRecycle)`
- `private void EstimateFloatingHeaderLocation(UIElement spFloatingHeader)`
- `protected override Size ArrangeOverride(Size arrangeSize)`
- `internal List<uint> GetPinnedElementsIndexVector(ElementType type)`
- `private void ArrangeElements(ElementType type, Size finalSize, Rect adjustedVisibleWindow)`
- `private void TraceVirtualizationData()`
- `private LayoutReference CreateDefaultLayoutReference()`
- `private EstimationReference CreateDefaultEstimationReference(ElementType elementType)`
- `private void OnPanelUnloaded(object pSender, RoutedEventArgs pArgs)`
- `private void SynchronousScrollByPage(bool toIncreasingIndex)`
- `private void GetLastVisibleContainer(bool fromLowerIndex, bool searchForItems, out uint lastVisibleContainerIndex, out bool found)`
- `internal ElementInfo GetClosestElementInfo(Point position)`
- `internal int GetInsertionIndex(Point position)`
- `internal void GetInsertionIndexes(Point position, out int first, out int second)`
- `private void FindValidContainerInGroupForPoint(Point point, out bool groupWasEmpty, out int groupIndex, out int referenceItemIndex, out int referenceItemIndexInGroup, out Rect referenceItemBounds)`
- `internal void IsLayoutBoundary(int index, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary)`
- `internal Rect GetItemsBounds()`
- `private void GetGenerationAnchorInformation(out LayoutReference referenceInformation, ref CollectionIterator iterator)`
- `private bool ShouldElementBeVisible(ElementType type, int index)`
- `private float PointFromPointInVirtualizingDirection(Point point)` + `private void SetPointFromPointInVirtualizingDirection(ref Point point, float value)`
- `private float PointFromSizeInVirtualizingDirection(Size size)` + `private void SetPointFromSizeInVirtualizingDirection(ref Size size, float value)`
- `private float SizeInVirtualizingDirection(Size size)` + `private void SetSizeInVirtualizingDirection(ref Size size, float value)`
- `private float SizeInNonVirtualizingDirection(Size size)` + `private void SetSizeInNonVirtualizingDirection(ref Size size, float value)`
- `private float PointFromRectInVirtualizingDirection(Rect rect)` + `private void SetPointFromRectInVirtualizingDirection(ref Rect rect, float value)`
- `private float PointFromRectInNonVirtualizingDirection(Rect rect)` + `private void SetPointFromRectInNonVirtualizingDirection(ref Rect rect, float value)`
- `private float SizeFromRectInVirtualizingDirection(Rect rect)` + `private void SetSizeFromRectInVirtualizingDirection(ref Rect rect, float value)`
- `private protected double SizeFromThicknessInVirtualizingDirection(Thickness thickness)`
- `internal double CacheLengthBase { get; set; }` (`get_`/`put_CacheLengthBase`)
- `internal static bool ElementIsPositionedInGarbageSection(UIElement spElement)`
- `private static void SetElementEmptySizeInGarbageSection(UIElement spElement)`
- `private static void SetElementSizeInGarbageSection(UIElement spElement, Size size)`
- `private static void SetBoundsForElement(UIElement spElement, Rect bounds)`
- `private protected Size GetViewportSize()`
- `internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)`: call `base` first.

### ModernCollectionBasePanel_CacheManager_Partial.cpp, nested `CacheManager`

- `internal CacheManager(ModernCollectionBasePanel owner)`
- `CacheManager.GroupCache`: `public GroupCache()`
- `internal StrongRefLifetime CacheStrongRefs()`: `using var strongCache = ...`; the `HRESULT*` out goes away because `GuaranteeCache` throws.
- `internal IGeneratorHost? GetItemsHost()`
- `internal IList<UIElement> GetChildren()`
- `internal void GuaranteeCache()`
- `internal void UnguaranteeCache()`
- `internal void ResetGroupCache()`
- `internal void ResetGroupCacheIfGroupCountsMismatch()`
- `internal void RenewCacheAfterMutation(int itemIndexAdded, int itemIndexRemoved, int groupIndexAdded, int groupIndexRemoved)`
- `internal void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)`
- `internal void GetGroupHistoryInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)`
- `internal void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)`
- `internal int DataIndexToLayoutIndex(ElementType type, int index)`
- `internal int LayoutIndexToDataIndex(ElementType type, int index)`
- `internal int GetTotalLayoutGroupCount()`
- `internal bool IsFocusedChild(UIElement pCandidate)`
- `internal IReadOnlyList<uint>? GetLayoutGroupSizes()`
- `private void InitCollectionsCache()`
- `private void ClearCollectionsCache()`
- `private void CacheGroup(int groupIndex, bool cacheItems)`
- `internal ICollectionViewGroup? GetGroup(int groupIndex)`
- `internal object? GetItem(int indexInItemCollection)`
- `internal void GetIndex(object? pItem, out uint index, out bool isGroupIndex, out bool itemFound)`
- `internal IContainerRecyclingContext? GetContainerRecyclingContext(uint itemIndex)`
- `internal void GetLastElementInLayout(out int elementIndex, out ElementType elementType)`

### ModernCollectionBasePanel_CollectionIterator_Partial.cpp, nested `struct CollectionIterator`

- `internal CollectionIterator(CacheManager cache)`
- `internal CollectionIterator(CacheManager cache, int index, ElementType type)`
- `internal void Init(int index, ElementType type)`
- `internal bool MoveNext()`
- `internal bool MovePrevious()`

### ModernCollectionBasePanel_ContainerContentChangingIterator.cpp

Nested `ContainerContentChangingIterator`:

- `public ContainerContentChangingIterator()`
- `internal void Initialize(ModernCollectionBasePanel pPanel)`
- `public uint Size { get; }`
- `public DependencyObject? Current { get; }`
- `public bool MoveNext()`
- `public void Reset()`

Panel:

- `internal IContainerContentChangingIterator GetContainersForIncrementalVisualization()`

### ModernCollectionBasePanel_ContainerMapper_Partial.cpp, nested `ContainerManager`

- `internal ContainerManager(ModernCollectionBasePanel owner)`
- `ContainerManager.PinnedElementInfo`: `internal PinnedElementInfo(WeakReference<UIElement>? wrElement, int index)`, both C++ overloads collapse into this one.
- `internal void Refresh()`
- `internal void ClearPinAndFocus()`
- `internal void Reset()`
- `internal UIElement? GetElementAtValidIndex(ElementType type, int indexInValidElements)`
- `internal UIElement? GetElementAtDataIndex(ElementType type, int dataIndex)`
- `internal bool IsItemConnected(uint index)`
- `internal bool IsGroupHeaderConnected(uint groupIndex, uint neighboringItemIndex)`
- `internal bool IsGroupHeaderRealized(uint groupIndex)`
- `internal void UpdateDataIndexForFirstValidElement(ElementType type, int dataIndexOfFirstValidElement)`
- `internal void PlaceInValidElements(ElementType type, int dataIndex, UIElement? spElement)`
- `internal void InsertSentinelElement(ElementType type, int groupIndex)`
- `internal void RemoveFromValidElements(ElementType type, int indexInValidElements, bool isForDataRemoval, int gateIndex)`
- `internal void RemoveAllValidElements(ElementType type)`
- `internal void PlaceInGarbageRegionIfNotInChildren(UIElement spElement)`
- `internal DependencyObject? Interface_ElementFromDataItem(ElementType type, object? dataItem)`
- `internal int Interface_IndexFromElement(ElementType type, DependencyObject element, bool excludeHiddenEmptyGroups)`
- `internal DependencyObject? Interface_ElementFromDataIndex(ElementType type, int index)`
- `internal object? Interface_DataItemFromElement(ElementType type, DependencyObject element)`
- `internal DependencyObject? Interface_GroupHeaderContainerFromItemContainerImpl(DependencyObject pItemContainer)`
- `internal List<uint> GetPinnedElementsIndexVector(ElementType type)`
- `internal UIElement? GetElementFromPinnedElements(ElementType type, int index)`
- `internal void RegisterPinnedElement(ElementType type, int index, UIElement spElement)`
- `internal void UnregisterPinnedElement(ElementType type, int index)`
- `internal void FilterPinnedContainers(Func<int, UIElement, bool> filterFunction)`
- `internal bool GetIsElementPinned(ElementType type, UIElement? spElement, out int pinnedIndex)`
- `internal bool GetIsIndexPinned(ElementType type, int index)`
- `internal void HoldForPrepare(UIElement spElement, ElementType type, int index)`
- `internal void ResetAfterPrepare()`
- `internal void SetFocusedContainer(int itemIndex, UIElement spContainer)`
- `internal void GetFocusedContainer(out int itemIndex, out UIElement? container)`
- `internal void ResetFocusedContainer(bool removeNonGeneratedContainer)`
- `internal void SetFocusedHeaderContainer(int groupIndex, UIElement spContainer)`
- `internal void GetFocusedHeaderContainer(out int groupIndex, out UIElement? container)`
- `internal void ResetFocusedHeaderContainer(bool removeNonGeneratedContainer)`
- `internal void OnDataItemAdded(ElementType type, int index)`
- `internal void OnDataItemRemoved(ElementType type, int index)`
- `internal void OnDataItemReplaced(ElementType type, int index, UIElement pNewElement)`
- `internal UIElement? GetVisualChild(int visualChildIndex)`
- `internal void TrimEdgeSentinels()`
- `private void TrimEdgeSentinels(ElementType type)`
- `private void RemoveFromVisualChildren(ElementType type, int index, UIElement spElement, bool isForElementRemoval)`
- `private void AddToVisualChildren(ElementType type, int index, UIElement spElement)`
- `private void ElementUnpinned(ElementType type, int index, UIElement spElement, bool removeNonGeneratedContainer)`

### ModernCollectionBasePanel_ICTCP_Partial.cpp

Panel:

- `internal ThemeTransitionContext GetChildTransitionContext(UIElement element, int layoutTickId)`
- `internal bool IsCollectionMutatingFast()`
- `internal Rect GetChildTransitionBounds(UIElement element)`
- `internal void NotifyOfItemsReordered(uint nCount)`

Nested `TransitionContextManager`:

- `internal TransitionContextManager(ModernCollectionBasePanel owner)`
- `internal void NotifyOfItemsChanging(CollectionChange action)`
- `internal void NotifyOfItemsChanged(CollectionChange action)`
- `internal void NotifyOfGroupsChanged(CollectionChange action)`
- `internal void NotifyOfItemsReordered(uint nCount)`
- `internal bool IsCollectionMutatingFast()`
- `private void UpdateContextCounters(bool isUpdateDueToMutation)`
- `internal ThemeTransitionContext GetTransitionContext(int layoutTickId, bool isWrapping)`

### ModernCollectionBasePanel_IICG2_Partial.cpp

Panel:

- `private void ReloadUnloadedElements()`
- `internal void GetRootOfItemTemplateAsContainer(IGeneratorHost spHost, object? item, bool queryForInspecting, ref UIElement? container)`
- `internal UIElement? GenerateContainerAtIndex(int index)`
- `private void PrepareContainerViaItemsHost(int indexInItemCollection, Size measureSize, UIElement spContainer)`
- `private void PrepareHeaderViaItemsHost(int indexInGroupCollection, Size measureSize, UIElement spHeader)`
- `internal UIElement? GenerateHeaderAtGroupIndex(int index)`
- `internal bool GetContainerRecycleQueueEmpty()`
- `private void VerifyStoredFocusedContainer()`
- `private bool StoredFocusedContainerStillHasFocus()`
- `private void ReleaseStoredFocusedContainer()`
- `internal void RecycleContainer(UIElement container)`
- `internal bool TryRecycleContainer(UIElement container)`
- `internal void RecycleLinkedContainer(UIElement container)`
- `internal void RecycleAllContainers()`
- `internal bool GetHeaderRecycleQueueEmpty()`
- `internal void RecycleHeader(UIElement header)`
- `internal void RecycleAllHeaders()`
- `internal void LinkContainerToItem(UIElement spContainer, object? spItem)`
- `internal void LinkHeaderToGroup(UIElement spHeader, object? spItem)`
- `internal object? UnlinkContainerFromItem(UIElement spContainer)`
- `internal object? UnlinkHeaderFromGroup(UIElement spHeader)`
- `internal void Refresh()`
- `internal void NotifyOfItemsChanging(IVectorChangedEventArgs e)`
- `internal void NotifyOfItemsChanged(IVectorChangedEventArgs e)`
- `internal void NotifyOfGroupsChanged(IObservableVector<object> pSender, IVectorChangedEventArgs e)`
- `private void OnItemAdded(int nIndex)`
- `private void OnItemRemoved(int nIndex, bool itemRemovedForGroupReset = false)`
- `private void OnGroupAdded(int nIndex)`
- `private void OnGroupRemoved(int nIndex)`
- `private void OnGroupReset(int nIndex)`
- `private void OnDataElementReplaced(ElementType type, int index)`
- `private void OnCollectionReset()`
- `private void CreateUnlinkedFillersForGroup(int nIndex)`
- `internal void FinishUnlinkingContainer(UIElement pContainer, bool useUnloadingQueue)`
- `internal bool FindRecyclingCandidate(int index)`
- `internal uint GetQueueLength()`
- `internal void RemoveFromQueue(UIElement pContainer, bool expectContainerToBePresent = true)`
- `internal bool RemoveFromHeaderRecycleQueue(UIElement pContainer)`

Nested `DeferredModernPanelUnlinkActionPayload`:

- `internal void InitializePayload(ModernCollectionBasePanel pOwner)`
- `internal override void Execute(UIElement pContainer)`

### ModernCollectionBasePanel_IICM_Partial.cpp

- `internal IItemContainerMapping GetItemContainerMapping()`
- `internal IGroupHeaderMapping GetGroupHeaderMapping()`
- `internal object? ItemFromContainer(DependencyObject container)`
- `internal DependencyObject? ContainerFromItem(object? item)`
- `internal int IndexFromContainer(DependencyObject container)`
- `internal DependencyObject? ContainerFromIndex(int index)`
- `internal DependencyObject? GroupHeaderContainerFromItemContainer(DependencyObject pItemContainer)`
- `internal object? GroupFromHeader(DependencyObject header)`
- `internal DependencyObject? HeaderFromGroup(object? group)`
- `internal int IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups)`
- `internal DependencyObject? HeaderFromIndex(int index)`

### ModernCollectionBasePanel_IItemsLookupAndIPaginatedPanel.cpp

- `internal int GetLastItemIndexInViewport(ScrollViewer scrollViewer)`
- `internal double GetItemsPerPage(ScrollViewer scrollViewer)`
- `private protected virtual double GetItemsPerPageImpl(Rect window)`

### ModernCollectionBasePanel_Interaction_Partial.cpp

- `internal bool SupportsKeyNavigationAction(KeyNavigationAction action)`
- `internal void GetTargetIndexFromNavigationAction(uint sourceIndex, ElementType sourceType, KeyNavigationAction action, bool allowWrap, uint itemIndexHintForHeaderNavigation, out uint computedTargetIndex, out ElementType computedTargetElementType, out bool actionValidForSourceIndex)`
- `internal void GetTargetHeaderIndexFromNavigationAction(uint sourceIndex, KeyNavigationAction action, out uint computedTargetIndex, out bool actionHandled)`
- `private uint GetTargetIndexFromNavigationActionByPage(uint sourceIndex, bool isPageDown, bool allowWrap)`
- `private uint GetTargetHeaderIndexFromNavigationActionByItem(uint sourceIndex, KeyNavigationAction action)`
- `private uint GetPreviousHeaderIndex(uint sourceIndex)`
- `internal uint GetNeighboringItemIndex(uint currentGroupIndex, uint newGroupIndex)`

### ModernCollectionBasePanel_LayoutDataInfoProvider.cpp

Nested `LayoutDataInfoProvider`:

- `public LayoutDataInfoProvider()`
- `internal void Initialize(ModernCollectionBasePanel pPanel)`
- `internal void Uninitialize()`
- `public int GetTotalItemCount()`
- `public int GetTotalGroupCount()`
- `public void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)`
- `public void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)`
- `private ModernCollectionBasePanel? GetPanelRef()`

Panel:

- `private int GetTotalItemCount()`
- `internal int GetTotalGroupCount()`
- `private void GetGroupInformationFromItemIndexImpl(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)`
- `private void GetGroupInformationFromGroupIndexImpl(int groupIndex, out int startItemIndex, out int itemCountInGroup)`

### ModernCollectionBasePanel_SnapPoints.cpp

Panel:

- `internal bool AreHorizontalSnapPointsRegular { get; }`
- `internal bool AreVerticalSnapPointsRegular { get; }`
- `internal IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)`
- `internal void GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset, out float spacing)`
- `private void UpdateSnapPoints()`

Nested `SnapPointState`:

- `internal SnapPointState()`
- `internal bool SetIrregularSnapPoints(Orientation orientation, List<float> keys)`: takes ownership, as the C++ rvalue does.
- `internal IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)`
- `internal bool SetRegularSnapPoints(Orientation orientation, float spacing, float nearOffset, float farOffset)`
- `internal void GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset, out float spacing)`

### ModernCollectionBasePanel_StickyHeaders.cpp

- `private bool ConfigureStickyHeader(UIElement spHeader, ScrollViewer pScrollViewer, UIElement pLTEParent, double groupBottomY, Rect desiredBounds, bool manuallyUpdatePosition)`
- `private void ConfigureStickyHeader(UIElement spHeader, int groupIndex, Rect desiredBounds)`
- `private void UpdateStickyHeaders(Size finalSize)`
- `private void UpdateItemClippingForStickyHeaders(Size finalSize, bool manuallyUpdatePosition, bool updateClip)`
- `private void RemoveStickyHeader(UIElement spHeader)`
- `private void RemoveStickyHeaders()`
- `private static double GetStickyHeaderPositionY(double groupTopY, double groupInnerHeight, double visibleTopY)`
- `private float GetRealHeaderExtentOutsideVisibleWindow()`
- `internal float GetVerticalViewportPadding()`
- `internal void CoerceStickyHeaderOffsets(ListViewBaseHeaderItem pHeaderItem, int cOffsets, double[] pOffsets)`
- `internal void NotifyLayoutTransitionStart(ListViewBaseHeaderItem pHeaderItem)`
- `internal void NotifyLayoutTransitionEnd(ListViewBaseHeaderItem pHeaderItem)`

### ModernCollectionBasePanel_ViewportManagement_Partial.cpp

- `private void BeginTrackingFirstVisibleElement(bool ignoreIsTrackingExtentEnd = false)`
- `private void BeginTrackingFirstVisibleElement(bool isCollectionChange, bool isGroupChange, int index, CollectionChange action = CollectionChange.Reset)`
- `private protected void CacheFirstVisibleElementBeforeOrientationChange()`
- `private Rect BeginTrackingOnOrientationChange()`
- `private void BeginTrackingOnRefresh(ref Rect newVisibleWindow)`: leaves the seed untouched on its early exits.
- `private void BeginTrackingOnCollectionChange(bool isGroupChange, int index, CollectionChange action)`
- `private void BeginTrackingOnMeasureChange(bool isGroupChange, int index)`
- `private void BeginTrackingOnViewportSizeChange(Rect previousWindow, Rect currentWindow)`
- `private void ApplyTrackedElementShift()`
- `private void IsChangeBeforeTrackedElement(bool isCollectionChange, bool isGroupChange, int index, CollectionChange action, out bool isChangeBeforeTrackedElement, out bool isChangeGoingToHideTrackedGroupHeader)`
- `private void TrackLastElement()`
- `private void EndTrackingFirstVisibleElement()`
- `private Rect GetRealizationWindowAfterViewportShift()`
- `private void AdjustLayoutTransitions(float viewportShift)`
- `private void AdjustLayoutTransitionsForElement(UIElement element, float viewportShift, Orientation layoutOrientation)`
- `internal void GetFirstVisibleElementForSerialization(out ElementType type, out double offset, out UIElement? uiElement)`
- `internal double AdjustViewportOffsetForDeserialization(double offset)`
- `private void GetFirstVisibleElement(out ElementType type, out int index, out UIElement? uiElement)`
- `private void GetFirstElementInWindow(Rect window, out ElementType elementType, out int elementIndex, out UIElement? uiElement, out Rect adjustedWindowForStickyHeaders)`
- `private void SetTrackedElementOffsetRelativeToViewport()`
- `private UIElement? GetTrackedElement()`
- `private void ElectNewTrackedElement()`
- `private float GetNewViewportEdge()`
- `private bool IsBottomOrRightAligned(FrameworkElement element, Orientation orientation)`
- `private void UpdateViewportOffsetDelta(Rect oldVisibleWindow, Rect newVisibleWindow)`

### ModernCollectionBasePanel_WindowManagement_Partial.cpp

Panel:

- `private RelativePosition GetReferenceDirectionFromWindow(Rect referenceRect, Rect window)`
- `private Rect CalculateVisibleWindowFromScrollPoint(Point newScrollPoint, float zoomFactor)`
- `private Rect CalculateVisibleWindowFromScrollData(Rect scrollViewport, float zoomFactor)`
- `private void SetRealizationWindowFromVisibleWindow(bool allowCache)`
- `internal override bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation orientation)`
- `private void DetachPanelComponents()`
- `private void OnScrollViewChanged(object pSender, ScrollViewerViewChangedEventArgs pArgs)`
- `private void OnScrollViewChanging(object pSender, ScrollViewerViewChangingEventArgs pArgs)`
- `private void OnScrollViewerSizeChanged(object pSender, SizeChangedEventArgs pArgs)`
- `private void AttachPanelComponents()`
- `private void PrimeVisibleWindow(bool allowTrackingOnViewportSizeChange = false)`
- `private protected void ResetCacheBuffers()`
- `private void DetectAndHandleDisconnectedView()`
- `private void GenerateAnchorsForWindow(Rect windowToFill, EstimationReference? headerReference, EstimationReference? containerReference)`
- `private void GenerateAnchorForGroup(int groupIndex, EstimationReference? headerReference, EstimationReference? containerReference)`
- `private void GenerateAnchorForItem(int itemIndex, EstimationReference? headerReference, EstimationReference? containerReference)`
- `private void GenerateAnchorAndSetFocus(int index, bool isHeader)`
- `private void IsLastElementRealized(out bool result, out float panelExtent)`
- `private Size EstimatePanelExtent()`
- `private void CorrectForEstimationErrors(bool adjustWindow = true)`
- `private bool SetScrollViewerOffsetsTo(Rect targetRect, bool animate)`
- `private protected void ProcessOrientationChange()`
- `private void FlushWindowStateCommand()`
- `internal void ScrollRectIntoView(Rect targetRect, bool forceSynchronous)`
- `internal void ScrollItemIntoView(uint index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous, bool animate = false)`
- `internal void GetFocusCandidate(out int index, out int groupIndex, out UIElement? container)`
- `internal void ScrollGroupHeaderIntoView(uint index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous, bool animate = false, uint neighboringItemIndex = 0)`
- `private void DetermineWindowAndAnchors()`
- `private Rect DoProcessOrientation()`
- `private void DoScrollItemIntoView(int itemIndex, ScrollIntoViewAlignment alignment, double offset, bool animate, out Rect computedWindow)`
- `private void DoScrollGroupHeaderIntoView(int groupIndex, ScrollIntoViewAlignment alignment, double offset, bool animate, out Rect computedWindow)`
- `private Rect PlaceWindowAroundElementRect(Rect sourceWindowRect, Rect elementRectToAlignTo, ScrollIntoViewAlignment alignment, double offset)`
- `private void RecycleAllContainersAndHeaders()`
- `private bool CanIncreaseCacheLength()`
- `internal bool IsBuildTreeSuspended { get; }`
- `internal bool BuildTree()`
- `private void CoerceWindowToExtent(ref Rect visibleWindow)`

Nested `WindowState`:

- `internal WindowState()`: the panel field initializer calls it.
- `internal double GetCurrentAdjustmentY()`
- `internal void ApplyAdjustment(Point correction)`
- `internal Rect GetVisibleWindow()`
- `internal void SetScrollStateVisibleWindow(Rect newWindowFromScrollState, out bool shouldInvalidate)`
- `internal bool HasCoercedWindow()`
- `internal void ClearCoercedWindow()`
- `internal void CorrectVisibleWindowForMouseLargeClick(ModernCollectionBasePanel panel)`

## Already in the header port

Do not reimplement these. They appear here so the map covers every header method.

- `internal void SetOriginFromItemsPresenter(Point origin)`
- `private protected virtual double GroupPaddingSizeInVirtualizingDirection()`
- `private protected virtual ItemsUpdatingScrollMode GetItemsUpdatingScrollMode()`
- `private protected bool IsMaintainViewportSupportedAndEnabled()`
- `private protected virtual void OnCollectionChangeProcessed()`
- `private protected virtual bool NeedsSpecialItem()`
- `private protected virtual bool NeedsSpecialGroup()`
- `private protected virtual int GetSpecialItemIndex()`
- `private protected virtual int GetSpecialGroupIndex()`
- `private protected virtual void RegisterSpecialContainerSize(int itemIndex, Size containerDesiredSize)`
- `private protected virtual void RegisterSpecialHeaderSize(int groupIndex, Size headerDesiredSize)`
- `private protected virtual void SetGroupHeaderStrategy(GroupHeaderStrategy strategy)`
- `private protected virtual float GetAverageHeaderSize()`
- `private protected virtual float GetAverageContainerSize()`
- `internal void ShutDownDeferredWork()`
- CacheManager.StrongRefLifetime: `internal StrongRefLifetime(CacheManager pOwner)`
- CacheManager.StrongRefLifetime: `public void Dispose()`
- CacheManager: `internal void DisconnectItemsHost()`
- CacheManager: `internal bool IsItemsHostRegistered()`
- CacheManager: `internal bool GetHidesIfEmpty()`
- CacheManager: `internal void PutHidesIfEmpty(bool hidesIfEmpty)`
- CacheManager: `internal int GetTotalItemCount()`
- CacheManager: `internal int GetTotalGroupCount()`
- CacheManager: `internal int GetNonEmptyGroupCount()`
- CacheManager: `internal bool IsGrouping()`
- CacheManager: `internal bool WasGrouping()`
- CacheManager: `internal void RegisterInterpretResetAsFullReset()`
- CacheManager: `internal bool ShouldInterpretResetAsFullReset()`
- ContainerManager.PinnedElementInfo: `internal PinnedElementInfo()`
- ContainerManager.PinnedElementInfo: `internal int GetIndex()`
- ContainerManager.PinnedElementInfo: `internal UIElement? GetElement()`
- ContainerManager.PinnedElementInfo: `internal void UpdateIndex(int index)`
- ContainerManager.PinnedElementInfo: `internal void UpdateElement(WeakReference<UIElement>? wrElement)`
- ContainerManager: `internal UIElement? GetHeaderAtValidIndex(int indexInValidHeaders)`
- ContainerManager: `internal UIElement? GetContainerAtValidIndex(int indexInValidContainers)`
- ContainerManager: `internal UIElement? GetHeaderAtGroupIndex(int groupIndex)`
- ContainerManager: `internal UIElement? GetContainerAtItemIndex(int itemIndex)`
- ContainerManager: `internal int GetValidHeaderCount()`
- ContainerManager: `internal int GetValidContainerCount()`
- ContainerManager: `internal int GetValidElementCount(ElementType type)`
- ContainerManager: `internal int GetGroupIndexFromValidIndex(int indexInValidHeaders)`
- ContainerManager: `internal int GetItemIndexFromValidIndex(int indexInValidContainers)`
- ContainerManager: `internal int GetDataIndexFromValidIndex(ElementType type, int indexInValidElements)`
- ContainerManager: `internal int GetValidHeaderIndexFromGroupIndex(int groupIndex)`
- ContainerManager: `internal int GetValidContainerIndexFromItemIndex(int indexInItemCollection)`
- ContainerManager: `internal int GetValidElementIndexFromDataIndex(ElementType type, int dataIndexInCollection)`
- ContainerManager: `internal bool IsValidContainerIndexWithinBounds(int indexInValidContainers)`
- ContainerManager: `internal bool IsValidHeaderIndexWithinBounds(int indexInValidHeaders)`
- ContainerManager: `internal bool IsValidElementIndexWithinBounds(ElementType type, int indexInValidElements)`
- ContainerManager: `internal bool IsItemIndexWithinValidRange(int itemIndex)`
- ContainerManager: `internal bool IsGroupIndexWithinValidRange(int groupIndex)`
- ContainerManager: `internal bool IsDataIndexWithinValidRange(ElementType type, int dataIndex)`
- ContainerManager: `internal void UpdateGroupIndexForFirstValidHeader(int groupIndexOfFirstValidHeader)`
- ContainerManager: `internal void UpdateItemIndexForFirstValidContainer(int itemIndexOfFirstValidContainer)`
- ContainerManager: `internal void PlaceInValidHeaders(int groupIndex, UIElement? spHeader)`
- ContainerManager: `internal void PlaceInValidContainers(int itemIndex, UIElement? spContainer)`
- ContainerManager: `internal void InsertSentinelContainer(int itemIndex)`
- ContainerManager: `internal void InsertSentinelHeader(int groupIndex)`
- ContainerManager: `internal void RemoveFromValidHeaders(int indexInValidHeaders, bool isForGroupRemoval, int gateIndex = -1)`
- ContainerManager: `internal void RemoveFromValidContainers(int indexInValidContainers, bool isForItemRemoval, int gateIndex = -1)`
- ContainerManager: `internal void RemoveAllValidHeaders()`
- ContainerManager: `internal void RemoveAllValidContainers()`
- ContainerManager: `internal DependencyObject? IICM_ContainerFromItem(object? item)`
- ContainerManager: `internal DependencyObject? IGHM_HeaderFromGroup(object? group)`
- ContainerManager: `internal int IICM_IndexFromContainer(DependencyObject container)`
- ContainerManager: `internal int IGHM_IndexFromHeader(DependencyObject header, bool excludeHiddenEmptyGroups)`
- ContainerManager: `internal DependencyObject? IICM_ContainerFromIndex(int index)`
- ContainerManager: `internal DependencyObject? IGHM_HeaderFromIndex(int index)`
- ContainerManager: `internal object? IICM_ItemFromContainer(DependencyObject container)`
- ContainerManager: `internal object? IGHM_GroupFromHeader(DependencyObject header)`
- ContainerManager: `internal DependencyObject? IICM_GroupHeaderContainerFromItemContainerImpl(DependencyObject pItemContainer)`
- ContainerManager: `internal List<uint> GetContainerPinnedElementsIndexVector()`
- ContainerManager: `internal List<uint> GetHeaderHeaderPinnedElementsIndexVector()`
- ContainerManager: `internal UIElement? GetHeaderFromPinnedHeaders(int index)`
- ContainerManager: `internal UIElement? GetContainerFromPinnedContainers(int index)`
- ContainerManager: `internal void RegisterPinnedHeader(int index, UIElement spHeader)`
- ContainerManager: `internal void RegisterPinnedContainer(int index, UIElement spContainer)`
- ContainerManager: `internal void UnregisterPinnedHeader(int index)`
- ContainerManager: `internal void UnregisterPinnedContainer(int index)`
- ContainerManager: `internal bool GetIsHeaderPinned(UIElement? spHeader)`
- ContainerManager: `internal bool GetIsContainerPinned(UIElement? spContainer)`
- ContainerManager: `internal bool HasFocusedContainer()`
- ContainerManager: `internal bool HasFocusedHeader()`
- ContainerManager: `internal bool GetIsIndexFocused(ElementType type, int index)`
- ContainerManager: `internal void OnGroupAdded(int index)`
- ContainerManager: `internal void OnItemAdded(int index)`
- ContainerManager: `internal void OnGroupRemoved(int index)`
- ContainerManager: `internal void OnItemRemoved(int index)`
- ContainerManager: `internal int StartOfVisualSection(ElementType type)`
- ContainerManager: `internal int SizeOfVisualSection(ElementType type)`
- ContainerManager: `internal int EndOfVisualSection(ElementType type)`
- ContainerManager: `internal int StartOfHeaderVisualSection()`
- ContainerManager: `internal int StartOfContainerVisualSection()`
- ContainerManager: `internal int SizeOfHeaderVisualSection()`
- ContainerManager: `internal int SizeOfContainerVisualSection()`
- ContainerManager: `internal int EndOfHeaderVisualSection()`
- ContainerManager: `internal int EndOfContainerVisualSection()`
- ContainerManager: `internal int StartOfGarbageSection()`
- ContainerManager: `internal bool IsGroupBeingReset()`
- ContainerManager: `internal int GetIndexOfGroupBeingReset()`
- ContainerManager: `internal void SetIndexOfGroupBeingReset(int index)`
- ContainerManager: `internal void InvalidateIndexOfGroupBeingReset()`
- ContainerManager.PreparationHoldingArea: `public PreparationHoldingArea()`
- ContainerManager.PreparationHoldingArea: `internal void Reset()`
- ContainerManager.PreparationHoldingArea: `internal readonly bool Valid()`
- CollectionIterator.ElementInfo: `internal readonly bool isHeader()`
- CollectionIterator.ElementInfo: `internal readonly int IndexInGroup()`
- CollectionIterator: `internal readonly ElementInfo GetCurrent()`
- CollectionIterator: `private void UpdateCurrentGroupInformation()`
- `internal UIElement? GenerateElementAtDataIndex(ElementType type, int index)`
- `internal bool IsLockedForLayout()`
- `internal bool CanScrollIntoView()`
- `private void PrepareElementViaItemsHost(ElementType type, int indexInDataCollection, Size measureSize, UIElement spElement)`
- TransitionContextManager: `internal void RegisterForIncrementalLoading()`
- `internal void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup)`
- `internal void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup)`
- WindowState: `internal Rect GetRealizationWindow()`
- WindowState: `internal void SetRealizationWindow(Rect arg)`
- WindowState: `internal void SetScrollStateVisibleWindow(Rect newWindowFromScrollState)`
- WindowState: `internal void SetCoercedVisibleWindow(Rect newCoercedWindow)`
- WindowState: `internal Rect GetCoercedVisibleWindow()`
- ViewportBehaviorInfo: `public ViewportBehaviorInfo()`
- ViewportBehaviorInfo: `internal void Reset()`
- ViewportBehaviorInfo: `internal void ResetLastTracedDbg()`
- ViewportBehaviorInfo: `internal void TraceChangesDbg(string context)`
- `internal GroupStyle? GetGroupStyle()`
- `private static VirtualizationInformation? GetVirtualizationInformationFromElement(UIElement spElement)`
- `private static bool ElementHasVirtualizationInformation(UIElement spElement)`
- `private static Rect GetBoundsFromElement(UIElement spElement)`
- `private static void SetElementIsRealized(UIElement spElement, bool isRealized)`
- `private static object? GetItemFromElement(UIElement spElement)`
- `private static void SetItemForElement(UIElement spElement, object? spItem)`
- `private static void SetElementIsGenerated(UIElement spElement, bool isGenerated)`
- `private static bool GetElementIsGenerated(UIElement spElement)`
- `private static void SetElementIsHeader(UIElement spElement, bool isHeader)`
- `private static bool GetElementIsHeader(UIElement spElement)`
- `private static bool GetElementIsSentinel(UIElement? spElement)`
- `private static void SetElementWantsToSkipContainerPreparation(UIElement spElement, bool shouldSkipContainerPreparation)`
- `private static void SetElementIsPrepared(UIElement spElement, bool isPrepared)`
- `private static bool GetElementIsPrepared(UIElement spElement)`
- `private static void SetIsContainerFromTemplateRoot(UIElement spElement, bool isFromRoot)`
- `private static bool GetIsContainerFromTemplateRoot(UIElement spElement)`
- `private void RegisterSpecialElementSize(ElementType type, int index, Size desiredSize)`
- `private static Point GetOffScreenPosition()`
- `private void ThrowEFAIL()`
- `private void TraceGuardFailure(string failureReason)`
- `private protected ContainerManager GetContainerManager()`
- SnapPointState: `internal bool AreSnapPointsRegular(Orientation orientation)`
- SnapPointState: `internal Orientation GetOrientation()`
- StickyHeaderInfo: `internal StickyHeaderInfo(double offset, double size)`
