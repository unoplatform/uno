# Selection System Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** SelectionModel, SelectionModelChildrenRequestedEventArgs, SelectionModelSelectionChangedEventArgs, SelectionNode, SelectionTreeHelper, SelectedItems, IndexPath, IndexRange

## Summary

| Type | High | Medium | Low | Total |
|------|------|--------|-----|-------|
| SelectionModel | 0 | 4 | 6 | 10 |
| SelectionModelChildrenRequestedEventArgs | 0 | 2 | 1 | 3 |
| SelectionModelSelectionChangedEventArgs | 0 | 1 | 1 | 2 |
| SelectionNode | 1 | 4 | 4 | 9 |
| SelectionTreeHelper | 0 | 0 | 0 | 0 |
| SelectedItems | 1 | 3 | 2 | 6 |
| IndexPath | 0 | 3 | 3 | 6 |
| IndexRange | 0 | 4 | 3 | 7 |
| **Total** | **2** | **21** | **20** | **43** |

## File layout audit

| WinUI source | Expected Uno split | Actual Uno layout | Status |
|--------------|--------------------|--------------------|--------|
| `SelectionModel.cpp` + `SelectionModel.h` + `SelectionModel.idl` | `SelectionModel.cs` (decl), `SelectionModel.h.mux.cs`, `SelectionModel.mux.cs`, `SelectionModel.Properties.cs` | All four files present | OK |
| `SelectionModelChildrenRequestedEventArgs.cpp` + `.h` | `SelectionModelChildrenRequestedEventArgs.cs` (decl) + `.h.mux.cs` + `.mux.cs` | Single monolithic `SelectionModelChildrenRequestedEventArgs.cs` | **Discrepancy (Medium): split missing, despite .h having 4 fields + .cpp having method bodies.** Also missing MUX Reference header. |
| `SelectionModelSelectionChangedEventArgs.cpp` + `.h` | `SelectionModelSelectionChangedEventArgs.cs` (decl) only acceptable: .h has 0 fields and 0 inline bodies, .cpp has 0 method bodies | Single monolithic `SelectionModelSelectionChangedEventArgs.cs` | OK on layout — but missing MUX Reference header (Medium). |
| `SelectionNode.cpp` + `SelectionNode.h` | `SelectionNode.cs` (decl), `SelectionNode.h.mux.cs`, `SelectionNode.mux.cs` | All three files present | OK |
| `SelectionTreeHelper.cpp` + `SelectionTreeHelper.h` | `SelectionTreeHelper.cs` (decl), `SelectionTreeHelper.h.mux.cs`, `SelectionTreeHelper.mux.cs` | All three present | OK |
| `SelectedItems.h` (template-only, no .cpp) | `SelectedItems.cs` (monolithic acceptable for template-only) | Single monolithic `SelectedItems.cs` | OK on layout — but missing MUX Reference header (Medium). |
| `IndexPath.cpp` + `IndexPath.h` | `IndexPath.cs` (decl) + `.h.mux.cs` + `.mux.cs` | Single monolithic `IndexPath.cs` | **Discrepancy (Medium): split missing. Also references commit `de78834`, not `4b206bce3`.** |
| `IndexRange.cpp` + `IndexRange.h` | `IndexRange.cs` (decl) + `.h.mux.cs` + `.mux.cs` | Single monolithic `IndexRange.cs` | **Discrepancy (Medium): split missing. Also references commit `de78834`, not `4b206bce3`.** |

---

## Per-type sections

### SelectionModel

#### Method order verification (.cpp vs .mux.cs)

| # | C++ method | C# counterpart | Order match |
|---|------------|----------------|-------------|
| 1 | `SelectionModel()` ctor | `public SelectionModel()` | Yes |
| 2 | `~SelectionModel()` dtor | Commented `#if HAS_UNO` block | Yes (per Uno rule 8) |
| 3 | `Source()` get | `Source` get | Yes |
| 4 | `Source(value)` set | `Source` set | Yes |
| 5 | `SingleSelect()` get | `SingleSelect` get | Yes |
| 6 | `SingleSelect(value)` set | `SingleSelect` set | Yes |
| 7 | `AnchorIndex()` get | `AnchorIndex` get | Yes |
| 8 | `AnchorIndex(value)` set | `AnchorIndex` set | Yes |
| 9 | `SelectedIndex()` get | `SelectedIndex` get | Yes |
| 10 | `SelectedIndex(value)` set | `SelectedIndex` set | Yes |
| 11 | `SelectedItem()` | `SelectedItem` | Yes |
| 12 | `SelectedItems()` | `SelectedItems` | Yes |
| 13 | `SelectedIndices()` | `SelectedIndices` | Yes |
| 14 | `SetAnchorIndex(int)` | `SetAnchorIndex(int)` | Yes |
| 15 | `SetAnchorIndex(group, item)` | `SetAnchorIndex(group, item)` | Yes |
| 16 | `Select(int)` | `Select(int)` | Yes |
| 17 | `Select(group, item)` | `Select(group, item)` | Yes |
| 18 | `SelectAt(IndexPath)` | `SelectAt(IndexPath)` | Yes |
| 19 | `Deselect(int)` | `Deselect(int)` | Yes |
| 20 | `Deselect(group, item)` | `Deselect(group, item)` | Yes |
| 21 | `DeselectAt(IndexPath)` | `DeselectAt(IndexPath)` | Yes |
| 22 | `IsSelected(int)` | `IsSelected(int)` | Yes |
| 23 | `IsSelected(group, item)` | `IsSelected(group, item)` | Yes |
| 24 | `IsSelectedAt(IndexPath)` | `IsSelectedAt(IndexPath)` | Yes |
| 25 | `SelectRangeFromAnchor(int)` | `SelectRangeFromAnchor(int)` | Yes |
| 26 | `SelectRangeFromAnchor(g,i)` | `SelectRangeFromAnchor(g,i)` | Yes |
| 27 | `SelectRangeFromAnchorTo` | `SelectRangeFromAnchorTo` | Yes |
| 28 | `DeselectRangeFromAnchor(int)` | `DeselectRangeFromAnchor(int)` | Yes |
| 29 | `DeselectRangeFromAnchor(g,i)` | `DeselectRangeFromAnchor(g,i)` | Yes |
| 30 | `DeselectRangeFromAnchorTo` | `DeselectRangeFromAnchorTo` | Yes |
| 31 | `SelectRange(start, end)` | `SelectRange(start, end)` | Yes |
| 32 | `DeselectRange(start, end)` | `DeselectRange(start, end)` | Yes |
| 33 | `SelectAll()` | `SelectAll()` | Yes |
| 34 | `SelectAllFlat()` | `SelectAllFlat()` | Yes |
| 35 | `ClearSelection()` | `ClearSelection()` | Yes |
| 36 | `Type()` | `((ICustomPropertyProvider)).Type` | Yes |
| 37 | `GetCustomProperty(name)` | `((ICustomPropertyProvider)).GetCustomProperty` | Yes |
| 38 | `GetIndexedProperty(name, type)` | `((ICustomPropertyProvider)).GetIndexedProperty` | Yes |
| 39 | `GetStringRepresentation()` | `((ICustomPropertyProvider)).GetStringRepresentation` | Yes |
| 40 | `PropertyChanged(add)` / `(remove)` | C# `event PropertyChangedEventHandler PropertyChanged` | Yes (idiomatic C#) |
| 41 | `OnPropertyChanged(name)` | `OnPropertyChanged(name)` | Yes |
| 42 | `RaisePropertyChanged(name)` | `RaisePropertyChanged(name)` | Yes |
| 43 | `OnSelectionInvalidatedDueToCollectionChange` | same | Yes |
| 44 | `ResolvePath` | `ResolvePath` | Yes |
| 45 | `ClearSelection(reset, raise)` | `ClearSelection(reset, raise)` | Yes |
| 46 | `OnSelectionChanged` | `OnSelectionChanged` | Yes |
| 47 | `SelectImpl` | `SelectImpl` | Yes |
| 48 | `SelectWithGroupImpl` | `SelectWithGroupImpl` | Yes |
| 49 | `SelectWithPathImpl` | `SelectWithPathImpl` | Yes |
| 50 | `SelectRangeFromAnchorImpl` | `SelectRangeFromAnchorImpl` | Yes |
| 51 | `SelectRangeFromAnchorWithGroupImpl` | same | Yes |
| 52 | `SelectRangeImpl` | `SelectRangeImpl` | Yes |

#### Field verification (.h vs .h.mux.cs)

| C++ field | Decl | C# field | Match |
|-----------|------|----------|-------|
| `std::shared_ptr<SelectionNode> m_rootNode{ nullptr }` | private | `private SelectionNode m_rootNode = null` | OK |
| `bool m_singleSelect{ false }` | private | `private bool m_singleSelect = false` | OK |
| `bool m_selectionInvalidatedDueToCollectionChange{ false }` | private | same | OK |
| `winrt::IVectorView<winrt::IndexPath> m_selectedIndicesCached{ nullptr }` | private | `private IReadOnlyList<IndexPath> m_selectedIndicesCached = null` | OK |
| `winrt::IVectorView<winrt::IInspectable> m_selectedItemsCached{ nullptr }` | private | `private IReadOnlyList<object> m_selectedItemsCached = null` | OK |
| `event_source<PropertyChangedEventHandler> m_propertyChangedEventSource{ this }` | private | C# event field-event (auto) | OK (idiomatic) |
| `tracker_ref<…ChildrenRequestedEventArgs> m_childrenRequestedEventArgs{ this }` | private | `private SelectionModelChildrenRequestedEventArgs m_childrenRequestedEventArgs` | OK |
| `tracker_ref<…SelectionChangedEventArgs> m_selectionChangedEventArgs{ this }` | private | `private SelectionModelSelectionChangedEventArgs m_selectionChangedEventArgs` | OK |
| `std::shared_ptr<SelectionNode> m_leafNode` | private | `private SelectionNode m_leafNode` | OK |
| Inline accessor `SharedLeafNode()` returns m_leafNode | inline | `internal SelectionNode SharedLeafNode => m_leafNode` | OK |
| `SelectionInvalidatedDueToCollectionChange()` inline | inline | `internal bool SelectionInvalidatedDueToCollectionChange()` | OK |
| `struct SelectedItemInfo { std::weak_ptr<SelectionNode> Node; winrt::IndexPath Path; };` | top of .h | `internal struct SelectedItemInfo` in .h.mux.cs | OK |

#### Public API surface verification (.Properties.cs)

WinUI defines `ChildrenRequested` and `SelectionChanged` typed events via the `SelectionModel.idl`/`SelectionModelProperties` base. Uno exposes both as public events in `SelectionModel.Properties.cs`. OK.

#### Findings (SelectionModel)

**Medium**

1. **SelectionModel.cs missing class XML doc consistency**: Uno's decl file `SelectionModel.cs` has a `<summary>` XML doc which is appropriate, but the file header reads `// MUX Reference SelectionModel.idl, commit 4b206bce3`. That is fine. No finding.

2. **`SelectionModel.Properties.cs` style differs from rule 10/9** — the file body is wrapped in `namespace Microsoft.UI.Xaml.Controls { … }` (block) instead of file-scoped namespace used by all other files; and lacks MUX Reference header. Severity: **Medium**.

3. **`ResolvePath` interface check uses `IList<object>` / `IEnumerable<object>` rather than the non-generic `IBindableIterable` / `IIterable<IInspectable>` pair used by WinUI.**

   C++:
   ```cpp
   if (data.try_as<winrt::ItemsSourceView>() ||
       data.try_as<winrt::IBindableVector>() ||
       data.try_as<winrt::IIterable<winrt::IInspectable>>() ||
       data.try_as<winrt::IBindableIterable>())
   ```
   C#:
   ```csharp
   if (data is ItemsSourceView ||
       data is IBindableObservableVector ||
       data is IList<object> ||
       data is IEnumerable<object>)
   ```
   `IBindableObservableVector` is **not** the same as `IBindableVector`; `IBindableVector` is the projected XAML non-generic vector. Also, `IList<object>` is too narrow — a `List<MyType>` is not an `IList<object>`. This is a **behavioural** divergence in the auto-resolution code path. Severity: **Medium**.

4. **`Type` property implementation**:

   C++:
   ```cpp
   winrt::TypeName SelectionModel::Type()
   {
       auto outer = get_strong().as<winrt::IInspectable>();
       winrt::TypeName typeName;
       typeName.Kind = winrt::TypeKind::Metadata;
       typeName.Name = winrt::get_class_name(outer);
       return typeName;
   }
   ```
   C#:
   ```csharp
   Type ICustomPropertyProvider.Type => GetType();
   ```
   C# returns `System.Type` of the runtime class (which may be a subclass), while WinUI returns a `TypeName` built from `get_class_name(outer)` whose Kind is `Metadata`. This isn't strictly a bug but loses the `Kind = Metadata` semantics and the WinRT class name. Severity: **Medium**.

**Low**

5. **`#pragma region` markers** are preserved only as `// #pragma region ...` comments (not `#region`/`#endregion`). Per the spirit of rule 1 they should be preserved verbatim. Severity: **Low**.

6. **Whitespace / formatting**: WinUI uses tabs in some places, but Uno's port uses tabs consistently — OK.

7. **Comment fidelity**: C++ comment "We want to be single select, so make sure there is only" trails with a space; Uno strips trailing whitespace — acceptable.

8. **`Type ICustomPropertyProvider.Type` is explicitly implemented (interface-explicit), while WinUI exposes it implicitly via the IDL.** Acceptable for C# idiom. Severity: **Low**.

9. **Empty line between `DeselectRangeFromAnchorTo` and `SelectRange` is preserved by WinUI as one blank line, Uno port has two blank lines (lines 416-419).** Lossless port rule. Severity: **Low**.

10. **Uno destructor commented-out body block is correct, but uses an explanatory `// TODO Uno:` prologue with phrasing that may differ from standard `TODO Uno:` recommended convention.** Severity: **Low** (informational).

---

### SelectionModelChildrenRequestedEventArgs

#### Findings

**Medium**

1. **File layout split missing** — WinUI has `.cpp` with method bodies and `.h` with 4 fields. Per layout rule, this should be split into `.cs` (decl), `.h.mux.cs` (fields) and `.mux.cs` (methods). Currently monolithic. Severity: **Medium**.

2. **MUX Reference header missing** — file starts with `using System;` with no copyright/license/MUX-reference header. Severity: **Medium**.

**Low**

3. **`Children` property** in C++ has explicit getter and setter (with `m_children.set(value)`); in C# this is an auto-property (`public object Children { get; set; }`). Functionally equivalent. The `Initialize` method sets `Children = null` which matches the C++ `m_children.set(nullptr)`. OK. Severity: **Low** (style note only — no behaviour change).

4. **Constructor visibility**: C++ has public ctor (`SelectionModelChildrenRequestedEventArgs(...)`). C# is `internal`. Acceptable since only `SelectionModel` constructs it. Severity: **Low**.

---

### SelectionModelSelectionChangedEventArgs

#### Findings

**Medium**

1. **MUX Reference header missing** — Uno file has no copyright header and no `// MUX Reference ...` line. Per rule 4 this is required. Severity: **Medium**.

**Low**

2. **Internal default constructor**: WinUI declares the class with no user constructor (defaulted), Uno explicitly defines `internal SelectionModelSelectionChangedEventArgs() {}`. Functionally equivalent. Severity: **Low**.

---

### SelectionNode

#### Method order verification (.cpp vs .mux.cs)

| # | C++ method | C# counterpart | Order match |
|---|------------|----------------|-------------|
| 1 | `SelectionNode(manager, parent)` ctor | `internal SelectionNode(SelectionModel, SelectionNode)` | Yes |
| 2 | `~SelectionNode()` dtor | Commented `#if HAS_UNO` block | Yes |
| 3 | `Source()` get / `Source(value)` set | `Source` property | Yes |
| 4 | `ItemsSourceView()` | `ItemsSourceView` | Yes |
| 5 | `DataCount()` | `DataCount` | Yes |
| 6 | `ChildrenNodeCount()` | `ChildrenNodeCount` | Yes |
| 7 | `RealizedChildrenNodeCount()` | `RealizedChildrenNodeCount()` | Yes |
| 8 | `AnchorIndex()` / `AnchorIndex(value)` | `AnchorIndex { get; set; }` | Yes |
| 9 | `IndexPath()` | `IndexPath` | Yes |
| 10 | `GetAt(index, realize)` | `GetAt(index, realize)` | Yes |
| 11 | `SelectedCount()` | `SelectedCount` | Yes |
| 12 | `IsSelected(int)` | `IsSelected(int)` | Yes |
| 13 | `IsSelectedWithPartial()` | `IsSelectedWithPartial()` | Yes |
| 14 | `IsSelectedWithPartial(int)` | `IsSelectedWithPartial(int)` | Yes |
| 15 | `SelectedIndex()` get / `SelectedIndex(int)` set | `SelectedIndex` property | Yes |
| 16 | `SelectedIndices()` | `SelectedIndices` | Yes |
| 17 | `Select(int, bool)` | `Select(int, bool)` | Yes |
| 18 | `ToggleSelect(int)` | `ToggleSelect(int)` | Yes |
| 19 | `SelectAll()` | `SelectAll()` | Yes |
| 20 | `Clear()` | `Clear()` | Yes |
| 21 | `SelectRange(range, select)` | `SelectRange(range, select)` | Yes |
| 22 | `HookupCollectionChangedHandler()` | `HookupCollectionChangedHandler()` | Yes |
| 23 | `UnhookCollectionChangedHandler()` | `UnhookCollectionChangedHandler()` | Yes |
| 24 | `IsValidIndex(int)` | `IsValidIndex(int)` | Yes |
| 25 | `AddRange(range, raise)` | `AddRange(range, raise)` | Yes |
| 26 | `RemoveRange(range, raise)` | `RemoveRange(range, raise)` | Yes |
| 27 | `ClearSelection()` | `ClearSelection()` | Yes |
| 28 | `Select(int, bool, raise)` | `Select(int, bool, bool)` | Yes |
| 29 | `OnSourceListChanged(src, args)` | `OnSourceListChanged(src, args)` | Yes |
| 30 | `OnItemsAdded(index, count)` | `OnItemsAdded(int, int)` | Yes |
| 31 | `OnItemsRemoved(index, count)` | `OnItemsRemoved(int, int)` | Yes |
| 32 | `OnSelectionChanged()` (private) | `OnSelectionChanged()` (private) | Yes |
| 33 | `ConvertToNullableBool(state)` (static, in public block) | `ConvertToNullableBool` (internal static, after `OnSelectionChanged`) | Order matches .cpp |
| 34 | `EvaluateIsSelectedBasedOnChildrenNodes()` | same | Yes |

#### Field verification (.h vs .h.mux.cs)

| C++ field | Decl | C# field | Match |
|-----------|------|----------|-------|
| `SelectionModel* m_manager` | private | `private SelectionModel m_manager` | OK |
| `std::vector<std::shared_ptr<SelectionNode>> m_childrenNodes` | private | `private List<SelectionNode> m_childrenNodes = new List<SelectionNode>()` | OK |
| `SelectionNode* m_parent { nullptr }` | private | `private SelectionNode m_parent = null` | OK |
| `std::vector<IndexRange> m_selected` | private | `private List<IndexRange> m_selected = new List<IndexRange>()` | OK |
| `tracker_ref<winrt::IInspectable> m_source` | private | `private object m_source` | OK |
| `tracker_ref<winrt::ItemsSourceView> m_dataSource` | private | `private ItemsSourceView m_dataSource` | OK |
| `winrt::ItemsSourceView::CollectionChanged_revoker m_itemsSourceViewChanged{}` | private | **MISSING — replaced by TODO Uno comment about direct subscribe/unsubscribe** | **Discrepancy — see finding #1 below** |
| `int m_selectedCount{ 0 }` | private | `private int m_selectedCount = 0` | OK |
| `std::vector<int> m_selectedIndicesCached` | private | `private List<int> m_selectedIndicesCached = new List<int>()` | OK |
| `bool m_selectedIndicesCacheIsValid = false` | private | `private bool m_selectedIndicesCacheIsValid = false` | OK |
| `int m_anchorIndex{ -1 }` | private | (eliminated, surfaced as `AnchorIndex { get; set; } = -1` auto-property) | OK semantically (Medium — see #4) |
| `int m_realizedChildrenNodeCount{ 0 }` | private | `private int m_realizedChildrenNodeCount = 0` | OK |
| `enum class SelectionState { Selected, NotSelected, PartiallySelected }` | file-scope | `internal enum SelectionState { Selected, NotSelected, PartiallySelected }` | OK |

#### Findings (SelectionNode)

**High**

1. **`auto_revoke` revoker not converted to `SerialDisposable` + `Disposable.Create(...)`** — rule 7 violation.

   C++:
   ```cpp
   winrt::ItemsSourceView::CollectionChanged_revoker m_itemsSourceViewChanged{};
   // ...
   void SelectionNode::HookupCollectionChangedHandler()
   {
       if (m_dataSource)
       {
           m_itemsSourceViewChanged = m_dataSource.get().CollectionChanged(winrt::auto_revoke, { this, &SelectionNode::OnSourceListChanged });
       }
   }
   void SelectionNode::UnhookCollectionChangedHandler()
   {
           m_itemsSourceViewChanged.revoke();
   }
   ```
   Uno C# (replaces revoker with direct subscribe/unsubscribe and adds manual unhook calls in `ClearSelection` and `OnItemsRemoved`):
   ```csharp
   // TODO Uno: Original C++ uses ItemsSourceView::CollectionChanged_revoker m_itemsSourceViewChanged{};
   // We use direct subscribe/unsubscribe through HookupCollectionChangedHandler / UnhookCollectionChangedHandler.
   ```
   This is a deliberate Uno-specific divergence — it should at minimum use a `SerialDisposable` field with `Disposable.Create(() => m_dataSource.CollectionChanged -= OnSourceListChanged)` per rule 7. Severity: **High** (rule violation + functional divergence: see finding #2 below).

**Medium**

2. **`ClearSelection` and `OnItemsRemoved` carry additional Uno-specific logic** (`#if HAS_UNO`) that walks children and explicitly calls `UnhookCollectionChangedHandler` to compensate for the lack of shared_ptr destruction. The `#if HAS_UNO` guard is present and the rationale is documented, but this is not present in WinUI. Severity: **Medium** (rule 6 — Uno-specific code in `#if HAS_UNO`, but adds extra behaviour not in WinUI which the original revoker approach would render unnecessary).

3. **`IndexPath` private property in C# is `private`** while WinUI exposes `IndexPath()` as a public method (declared in the public section of `SelectionNode.h`, lines 38-51). Uno widened nothing — it actually narrowed visibility from public to private. Used only by `GetAt`, so this is correct. However per rule 5 ("Private by default; widen only with IDL/docs/Generated evidence"), the C++ original being public means it was used elsewhere via WinRT interop, but in fact internally to SelectionNode only — so private is fine. Severity: **Medium** for the visibility change with note.

4. **`AnchorIndex` representation**:

   C++: explicit `int m_anchorIndex{ -1 }` field + `AnchorIndex()`/`AnchorIndex(int)` getter/setter pair.
   C#: `internal int AnchorIndex { get; set; } = -1;` auto-property — no backing field, no public/private distinction preserved. WinUI's getter/setter pair is **public** in the .h (declared in public section). C# made it `internal`. Severity: **Medium** (visibility narrowing — `SelectionNode` is itself `internal`, so this is fine downstream, but the layout is non-1:1).

5. **`RealizedChildrenNodeCount()` is private in C#** but is declared in the **public** section of WinUI's .h (line 32). In WinUI it's public; in Uno it's private. Used only via `EvaluateIsSelectedBasedOnChildrenNodes` inside the class. Severity: **Medium** (visibility mismatch).

**Low**

6. **`OnSourceListChanged` parameter type**: C++ uses `const winrt::IInspectable& dataSource`, C# uses `object dataSource`. Acceptable per "Expected conversions". Severity: **Low**.

7. **Method `IndexPath` C# implementation uses `List<int>.IndexOf` instead of `std::find_if` + iterator distance**. Both produce the same index, but the `IndexOf` uses `Equals` whereas the C++ uses pointer-comparison (`item.get() == child`). For shared_ptr vs reference identity, both yield the same result for SelectionNode reference semantics in C#. Severity: **Low**.

8. **`IsSelectedWithPartial()` (no args) C# uses `parentsChildren.IndexOf(this)` and checks `>= 0`** vs the C++ check `it != parentsChildren.end()`. Equivalent. Severity: **Low**.

9. **Empty-line preservation**: a few extra blank lines preserved between methods. OK. Severity: **Low**.

---

### SelectionTreeHelper

#### Method order verification (.cpp vs .mux.cs)

| # | C++ | C# | Order |
|---|-----|-----|-------|
| 1 | `TraverseIndexPath` | `TraverseIndexPath` | Yes |
| 2 | `Traverse` | `Traverse` | Yes |
| 3 | `TraverseRangeRealizeChildren` | `TraverseRangeRealizeChildren` | Yes |
| 4 | `IsSubSet` (private static) | `IsSubSet` (private static) | Yes |
| 5 | `StartPath` (private static) | `StartPath` (private static) | Yes |

#### Field verification

| C++ | C# | Match |
|-----|-----|-------|
| `struct TreeWalkNodeInfo { Node, Path, ParentNode; }` with two ctors | `internal struct TreeWalkNodeInfo` with two ctors | OK |

#### Findings (SelectionTreeHelper)

No discrepancies found. The port is faithful, comments preserved, method bodies match, including the depth-first walk logic and the `MUX_ASSERT(start.CompareTo(end) == -1)` invariant.

---

### SelectedItems

#### Findings

**High**

1. **C# `~SelectedItems()` finalizer present** — rule 8 violation: "No finalizers — destructor commented out under `#if HAS_UNO`."

   C++:
   ```cpp
   ~SelectedItems()
   {
       m_infos.clear();
   }
   ```
   Uno:
   ```csharp
   ~SelectedItems()
   {
       m_infos.Clear();
   }
   ```
   This should be commented out inside `#if HAS_UNO` (matching the SelectionModel/SelectionNode handling). Severity: **High** (rule violation + may cause garbage-collection nondeterminism).

**Medium**

2. **MUX Reference header missing** — file has no copyright/license header and no MUX Reference line. Severity: **Medium**.

3. **`SelectedItemsEnumerator` is an Uno-only addition** for `IEnumerable<T>` implementation. Not guarded by `#if HAS_UNO`. C++ uses `winrt::IIterator<T>` via `First()`. Severity: **Medium** (Uno-specific code not under `#if HAS_UNO`).

4. **Missing `IndexOf(T const& value, uint32_t &index)` and `GetMany` overrides from C++ IVectorView contract.** Uno's `IReadOnlyList<T>` doesn't need them. But these are intentionally `E_NOTIMPL` in C++. No functional behaviour change. Severity: **Medium** (port surface mismatch).

**Low**

5. **The internal `Iterator` class in C++ takes `IVectorView<T>` and stores it; the C# `SelectedItemsEnumerator` takes `IReadOnlyList<T>`** — equivalent.

6. **Comment "//TODO: Verify IEnumerator implementation" in Uno** — informational TODO. Severity: **Low**.

---

### IndexPath

#### Findings

**Medium**

1. **MUX Reference commit is `de78834` not `4b206bce3`** — rule 4 + header staleness. Severity: **Medium**.

2. **File layout split missing** — WinUI has `.cpp` (10 method bodies) + `.h` (4 ctors + 6 methods + 1 field). This should be split into `.cs` decl + `.h.mux.cs` (field) + `.mux.cs` (bodies). Severity: **Medium**.

3. **`CreateFrom`/`CreateFromIndices` static factories use C# `new IndexPath(...)`** instead of the templated `winrt::make<IndexPath>(std::forward<>())`. Behaviourally equivalent — but C# version exposes only `int`, `int+int`, `IList<int>` overloads (matches what the ctors accept). C++ has `winrt::IVector<int>` overload as well, which Uno does not have. Severity: **Medium** (missing constructor for `IVector<int>` source). Note: in Uno repeater code, `IndexPath` always receives `List<int>`, so this is unlikely to matter.

**Low**

4. **Standard copyright/license preamble missing** — only "//MUX Reference IndexPath.cpp, commit de78834" exists; no Copyright/License lines. Severity: **Low**.

5. **`m_path` is `readonly List<int>`** in C# — initialised once, mutated via `.Add`. C++ uses `std::vector<int> m_path` (no const). C# `readonly` is incorrect terminology since the list is mutated. Acceptable. Severity: **Low**.

6. **`CompareTo` and `IsValid` are not declared `internal`/`public`** consistently — public on the `CompareTo`/`ToString`, internal on `IsValid`/`CloneWithChildIndex`. Matches WinUI's split between public (IIndexPath) and private members. OK. Severity: **Low**.

---

### IndexRange

#### Findings

**Medium**

1. **MUX Reference commit `de78834` not `4b206bce3`** — rule 4 + stale. Severity: **Medium**.

2. **File layout split missing** — WinUI has `.cpp` (7 method bodies) + `.h` (fields + ctors). Should be split. Severity: **Medium**.

3. **C++ `IndexRange` is a `struct` (value type) with `IndexRange() = default`. Uno declares `internal partial class IndexRange`** — reference type. This has semantic implications:

   - In `SelectionNode.RemoveRange`, the iteration `foreach (IndexRange range in m_selected)` and subsequent `range.Split(...)` would mutate the wrong copy if it were a struct. The class version works, but the C++ version uses references-to-vector-element `for (IndexRange& range : m_selected)` to mutate in-place — which is **not** what happens functionally (C++ `Split` writes to `before`/`after`, not the source range).
   - However `OnItemsAdded`/`OnItemsRemoved` in C++ do `auto range = m_selected[i];` (copy), then `m_selected[i] = IndexRange(...)` (assign back). The C# code does `var range = m_selected[i];` — but as a reference type, this would alias rather than copy. Functionally, since `m_selected[i]` is then **reassigned** with a new `IndexRange` instance, the alias doesn't matter. The class-vs-struct choice is therefore not behavior-breaking, but is a porting layout divergence. Severity: **Medium** (semantic surface change).

4. **C# adds `operator !=`, `Equals(object)`, and `GetHashCode()` overrides** that are not in WinUI. Marked as `//TODO: Scan source code for potential accidental missing ref/out!` and used for `m_selected.Remove(remove)` (which needs equality). C++ uses `std::find` + `auto iter` + `erase(iter)` (so equality is invoked via `operator==`). Uno's added methods are necessary but not under `#if HAS_UNO`. Severity: **Medium** (Uno-specific helpers not guarded).

**Low**

5. **`Split(int, ref IndexRange, ref IndexRange)`** uses `ref` parameters in C#, the C++ uses non-const `IndexRange&` parameters. Functionally equivalent. The TODO comment "Scan source code for potential accidental missing ref/out!" remains — informational. Severity: **Low**.

6. **Default constructor `IndexRange()`** in C++ is `= default` (m_begin and m_end default to `-1` via member init). In C#, `internal IndexRange() {}` body explicitly does nothing — same effect since `m_begin = -1; m_end = -1;` are field initialisers. OK. Severity: **Low**.

7. **No copyright/license header**. Severity: **Low**.

---

## Cross-type observations

1. **MUX Reference headers are missing on `SelectionModelChildrenRequestedEventArgs.cs`, `SelectionModelSelectionChangedEventArgs.cs`, `SelectedItems.cs`** and **stale on `IndexPath.cs` / `IndexRange.cs` (commit `de78834` instead of `4b206bce3`).** This is a recurring rule 4 violation across the Selection support types.

2. **Monolithic single-file layout is used for `SelectionModelChildrenRequestedEventArgs`, `IndexPath`, `IndexRange`, `SelectedItems`.** Per rule 2 and the note in the task brief, these should be split (their `.h` files declare fields or inline bodies and `.cpp` files contain method bodies).

3. **`auto_revoke` revoker rule violation**: `SelectionNode::m_itemsSourceViewChanged` is converted to direct subscribe/unsubscribe rather than `SerialDisposable + Disposable.Create(...)` (rule 7). This is the single rule-7 violation in this set of files but the most consequential one because it cascades into extra Uno-specific manual unhook logic in `ClearSelection` and `OnItemsRemoved`.

4. **Finalizer rule violation**: `SelectedItems<T>` has a live `~SelectedItems()` finalizer not commented out under `#if HAS_UNO` (rule 8).

5. **`#pragma region`** markers in `SelectionModel.mux.cs` are converted to `// #pragma region` comments rather than C# `#region`/`#endregion`. Consistent across the file but not strictly preserved per rule 1.

6. **Visibility narrowing on `SelectionNode`**: `IndexPath`, `AnchorIndex`, and `RealizedChildrenNodeCount` are private/internal in the C# port while declared public in C++ `.h`. Since the class is `internal sealed`, this is not externally observable, but per rule 5 the original public surface should be preserved when there's evidence (the .h IS the evidence for public).

7. **`SelectionModel.ResolvePath` interface check uses `IList<object>`/`IEnumerable<object>` (covariant-only)** rather than `IBindableVector`/`IBindableIterable`. This will fail to auto-resolve generic `IList<T>` / `IEnumerable<T>` data sources when `T != object`, while WinUI's `IBindableVector`/`IBindableIterable` projections would.

8. **Source-comment fidelity is generally good** for SelectionModel, SelectionNode, and SelectionTreeHelper — comments are preserved nearly verbatim including the TODOs (`TODO: Check for duplicates (Task 14107720)`, `TODO: Prevent overlap of Ranges`).

9. **All TreeWalk logic (depth-first stack-based traversal) matches between C++ and C#.** No off-by-one or invariant differences detected in `Traverse`, `TraverseIndexPath`, or `TraverseRangeRealizeChildren`.

10. **IndexRange semantics (`Split`, `Contains`, `Intersects`, inclusive `[Begin, End]`)** match exactly. No off-by-one between C++ and C#.

---

## Conclusion

**Total findings by severity:** High: 2, Medium: 21, Low: 20.

**Top priority issues**

1. **(High) `SelectionNode` revoker not converted to `SerialDisposable` + `Disposable.Create` (rule 7)** — introduces Uno-specific manual unhook flow that is asymmetric with WinUI's RAII handling and required adding `#if HAS_UNO` cleanup paths to `ClearSelection` and `OnItemsRemoved`. Replacing with `SerialDisposable` would remove the cascade.

2. **(High) `SelectedItems<T>` has a live finalizer (rule 8)** — needs to be commented out under `#if HAS_UNO` with `TODO Uno:` note.

3. **(Medium) `SelectionModel.ResolvePath` auto-resolve type check** uses `IList<object>`/`IEnumerable<object>` instead of `IBindableVector`/`IBindableIterable`. Will silently fail to auto-resolve generic-typed collections.

4. **(Medium) `IndexPath` / `IndexRange` reference an outdated commit `de78834` in their MUX Reference comment instead of `4b206bce3`.** Suggests these files haven't been synced with the rest of the Selection system port.

5. **(Medium) Layout split missing** on `IndexPath`, `IndexRange`, `SelectionModelChildrenRequestedEventArgs`, and `SelectedItems` (rule 2). All four host fields and/or non-trivial method bodies that justify the standard tri-file split.

6. **(Medium) Missing MUX Reference headers** on `SelectionModelChildrenRequestedEventArgs.cs`, `SelectionModelSelectionChangedEventArgs.cs`, `SelectedItems.cs`, and incomplete on `IndexPath.cs`/`IndexRange.cs` (no copyright/license).

7. **(Medium) Visibility narrowing on `SelectionNode`** (`IndexPath`, `AnchorIndex`, `RealizedChildrenNodeCount`) where WinUI declared them public.

8. **(Medium) Uno-specific augmentations** (`SelectedItemsEnumerator`, `IndexRange` equality overrides, `SelectionNode.ClearSelection`/`OnItemsRemoved` cleanup blocks) are partially or fully missing `#if HAS_UNO` guards and/or `TODO Uno:` annotations.
