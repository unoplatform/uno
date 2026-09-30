# ItemsRepeater Auxiliary Types Comparison Report

**WinUI commit:** 4b206bce3
**Types covered:** ItemsRepeaterElementClearingEventArgs, ItemsRepeaterElementIndexChangedEventArgs, ItemsRepeaterElementPreparedEventArgs, ItemsRepeaterScrollHost, RepeaterAutomationPeer

## Summary

| Type | Critical | High | Medium | Low | Info |
|---|---:|---:|---:|---:|---:|
| ItemsRepeaterElementClearingEventArgs | 0 | 1 | 2 | 2 | 1 |
| ItemsRepeaterElementIndexChangedEventArgs | 0 | 2 | 3 | 3 | 1 |
| ItemsRepeaterElementPreparedEventArgs | 0 | 2 | 2 | 2 | 1 |
| ItemsRepeaterScrollHost | 1 | 6 | 7 | 9 | 4 |
| RepeaterAutomationPeer | 0 | 1 | 2 | 3 | 2 |
| **Total** | **1** | **12** | **16** | **19** | **9** |

---

## Per-type sections

### ItemsRepeaterElementClearingEventArgs

#### File mapping

| WinUI | Uno |
|---|---|
| `ItemsRepeaterElementClearingEventArgs.h` | (no `.h.mux.cs` present) |
| `ItemsRepeaterElementClearingEventArgs.cpp` | (no `.mux.cs` present) |
| (logical decl) | `ItemsRepeaterElementClearingEventArgs.cs` (entire impl) |

Uno collapses both `.cpp` and `.h` into a single `.cs` file (`ItemsRepeaterElementClearingEventArgs.cs`). This deviates from the project layout rule (rule #2): `.cs` should be decl-only, `.h.mux.cs` should mirror `.h`, `.mux.cs` should mirror `.cpp`.

#### High

- **File layout violation (rule #2).** The single-file form means there is no separate `.h.mux.cs` (no field decl) and no `.mux.cs` (no `#pragma region` markers around the IElementClearingEventArgs section, no MUX Reference header indicating `.cpp` provenance).
  - **C++ (.h):** declares `private: tracker_ref<winrt::UIElement> m_element{ this };` and `#pragma region IElementClearingEventArgs ... #pragma endregion` around `Element()`.
  - **C# (.cs):** combines everything inline.
  - **Suggested fix:** split into `ItemsRepeaterElementClearingEventArgs.cs` (partial class decl, public surface XML doc), `ItemsRepeaterElementClearingEventArgs.h.mux.cs` (mirrors `.h`, no separate `m_element` field — `Element` auto-property is acceptable), and `ItemsRepeaterElementClearingEventArgs.mux.cs` (mirrors `.cpp`, contains `// #pragma region IElementClearingEventArgs` and `// #pragma endregion` markers and method bodies).

#### Medium

- **Missing MUX Reference header (rule #4).** No `// MUX Reference ItemsRepeaterElementClearingEventArgs.cpp, commit 4b206bce3` or `.h, commit 4b206bce3` header. The other types in this scope (ScrollHost, AutomationPeer) include it.
  - **Suggested fix:** add the MUX Reference header to whichever file holds the ported logic.

- **Constructor visibility widened to `internal` without justification.**
  - **C++ (.h:12-13):** `public: ItemsRepeaterElementClearingEventArgs(const winrt::UIElement& element);`
  - **IDL** does not expose a constructor (the IDL definition for the class has no ctor — it's instantiated only internally by the framework). However, the WinUI C++ ctor is `public` because of MIDL composability. The Uno `internal` is closer in semantic intent.
  - **Issue:** rule #5 says private by default; this is `internal` and is the documented "framework only instantiates it" pattern. Acceptable, but should be commented to clarify the visibility decision (IDL has no constructor surface).
  - **Suggested fix:** add a comment `// Internal: instances are created by ItemsRepeater; not exposed in IDL.`.

#### Low

- **`#pragma region` markers lost (rule "no `#pragma region` removed").**
  - **C++:** `#pragma region IElementClearingEventArgs` ... `#pragma endregion`
  - **C#:** no equivalent commented markers. Other ported files in the same area use `// #pragma region` / `// #pragma endregion`.
  - **Suggested fix:** add `// #pragma region IElementClearingEventArgs` / `// #pragma endregion` around the `Element` property.

- **Comments removed from `.h`/`.cpp`:** the `tracker_ref<winrt::UIElement> m_element{ this };` field declaration has no analog comment retained (this is informational — the C++ source itself has no comment here).

#### Info

- The `Element` getter is collapsed to an auto-property `public UIElement Element { get; private set; }`. WinUI splits it as a non-trivial getter that reads from `m_element`. Replacing the backing field with an auto-property and writing to `Element` inside `Update` is equivalent semantically (the framework property `m_element.set(element)` becomes setting the property). This is acceptable per the "Foo() getter → Foo" syntactic conversion rule. Note: the C# `private set` keeps the setter accessible only within the class — same as `m_element` being private.

---

### ItemsRepeaterElementIndexChangedEventArgs

#### File mapping

| WinUI | Uno |
|---|---|
| `ItemsRepeaterElementIndexChangedEventArgs.h` | (no `.h.mux.cs` present) |
| `ItemsRepeaterElementIndexChangedEventArgs.cpp` | (no `.mux.cs` present) |
| — | `ItemsRepeaterElementIndexChangedEventArgs.cs` |

Same single-file collapse as ElementClearing.

#### High

- **File layout violation (rule #2).** Same as ElementClearing.
  - **Suggested fix:** split into `.cs` / `.h.mux.cs` / `.mux.cs`.

- **`Update` signature changed: parameters marked `in` for value types.**
  - **C++ (.cpp:35):** `void ItemsRepeaterElementIndexChangedEventArgs::Update(const winrt::UIElement& element, int oldIndex, int newIndex)`
  - **C# (.cs:28):** `public void Update(UIElement element, in int oldIndex, in int newIndex)`
  - **Issue:** `in int` is a syntactic conversion the rules do NOT call out as expected. C++ takes `int` by value (not `const int&`); the conversion should be `int oldIndex, int newIndex`. The `in` modifier changes ABI subtly and is not idiomatic for a primitive. This is a port-style discrepancy.
  - **Suggested fix:** drop the `in` keyword: `public void Update(UIElement element, int oldIndex, int newIndex)`.

#### Medium

- **Missing MUX Reference header (rule #4).**

- **Constructor visibility widened to `internal` without comment.** Same as ElementClearing — acceptable but should be documented.

- **`#pragma region IElementPreparedEventArgs` mismatch / inconsistency.**
  - **C++ (.cpp:16):** `#pragma region IElementPreparedEventArgs` — note this is the same region name as in ElementPreparedEventArgs.cpp; appears intentional in WinUI source (likely a copy/paste in the original WinUI source, but it's the literal source content).
  - **C# (.cs:18):** `#region IElementPreparedEventArgs` — Uno uses C# `#region` rather than commented `// #pragma region`. Inconsistent with how the other files comment out C++ regions.
  - **Suggested fix:** change `#region`/`#endregion` to `// #pragma region IElementPreparedEventArgs` / `// #pragma endregion` to match the ported style.

#### Low

- **Method order doesn't strictly match `.cpp`.** In C#, the properties are inside the `#region` and `Update` is outside — matches C++. OK.

- **Comment removal:** none — neither source has descriptive comments here.

- **Constant-by-value vs ref:** C++ used `int` by value for `oldIndex`/`newIndex`. The Uno port shifts to `in int` which is non-idiomatic.

#### Info

- The fields `m_oldIndex` and `m_newIndex` from `.h:27-28` are merged into auto-property `OldIndex { get; private set; }` and `NewIndex { get; private set; }`. Acceptable per syntactic conversion rules.

---

### ItemsRepeaterElementPreparedEventArgs

#### File mapping

| WinUI | Uno |
|---|---|
| `ItemsRepeaterElementPreparedEventArgs.h` | (no `.h.mux.cs` present) |
| `ItemsRepeaterElementPreparedEventArgs.cpp` | (no `.mux.cs` present) |
| — | `ItemsRepeaterElementPreparedEventArgs.cs` |

Same single-file collapse.

#### High

- **File layout violation (rule #2).** Same as the other two EventArgs.

- **Missing field `m_viewType` from `.h:26` is dropped.**
  - **C++ (.h:26):** `winrt::hstring m_viewType;`
  - **C#:** field not present.
  - **Issue:** rule "Missing fields/constants" violated. The field is unused in `.cpp` (no read/write), but it is in the WinUI header and a strict 1:1 port should preserve it. This is unused state but represents a difference from the upstream `.h`.
  - **Suggested fix:** add `private string m_viewType;` in the `.h.mux.cs` (or in the merged file) with `#pragma warning disable 169, IDE0051` comment-block citing the parity intent (mirror what's done in `ItemsRepeaterScrollHost.h.mux.cs` for `m_horizontalEdge`/`m_verticalEdge`).

#### Medium

- **Missing MUX Reference header (rule #4).**

- **`#region`/`#endregion` instead of `// #pragma region`/`// #pragma endregion`.** Same style mismatch.

#### Low

- **Constructor visibility widened to `internal`** without justification comment — same as other EventArgs.

- **`#pragma endregion` placement.**
  - **C++ (.cpp:28):** the `#pragma endregion` is preceded by a blank line and followed by a blank line then `void ItemsRepeaterElementPreparedEventArgs::Update(...)`.
  - **C# (.cs:20):** `#endregion` is on the same line immediately after the property (no blank separator), then directly followed by `public void Update(...)`. Whitespace tightened.
  - **Suggested fix:** restore the blank-line separator around the region close.

#### Info

- `int32_t Index()` getter is converted to `public int Index { get; private set; }` — acceptable.

---

### ItemsRepeaterScrollHost

#### File mapping

| WinUI | Uno |
|---|---|
| `ItemsRepeaterScrollHost.h` | `ItemsRepeaterScrollHost.h.mux.cs` |
| `ItemsRepeaterScrollHost.cpp` | `ItemsRepeaterScrollHost.mux.cs` |
| (decl + XML doc) | `ItemsRepeaterScrollHost.cs` |

Multi-file layout is correct here.

#### Method order verification (`.cpp` vs `.mux.cs`)

| # | C++ method (`.cpp` order) | Uno method (`.mux.cs` order) | Match? |
|---|---|---|---|
| 1 | `ItemsRepeaterScrollHost()` ctor (line 13) | ctor (line 21) | YES |
| 2 | `CurrentAnchor()` (line 18) | property `CurrentAnchor` (line 103) | **MOVED** — appears after `ScrollViewer` |
| 3 | `ScrollViewer()` getter (line 23) | `ScrollViewer` get accessor (line 105+) | **MOVED** — appears after `CurrentAnchor` line |
| 4 | `ScrollViewer(value)` setter (line 35) | same property's set accessor | merged into property |
| 5 | `MeasureOverride` (line 48) | `MeasureOverride` (line 28) | **MOVED** — placed before properties |
| 6 | `ArrangeOverride` (line 60) | `ArrangeOverride` (line 40) | **MOVED** — placed before properties |
| 7 | `HorizontalAnchorRatio()` get/set (line 75/80) | property `HorizontalAnchorRatio` (line 99) | **MOVED** & visibility differs |
| 8 | `VerticalAnchorRatio()` get/set (line 85/90) | property `VerticalAnchorRatio` (line 101) | **MOVED** & visibility differs |
| 9 | `StartBringIntoView` (line 96) | `StartBringIntoView` (line 252) | **MOVED** — placed after IRepeaterScrollingSurface block |
| 10 | `IsHorizontallyScrollable` (line 115) | `IRepeaterScrollingSurface.IsHorizontallyScrollable` (line 150) | OK |
| 11 | `IsVerticallyScrollable` (line 120) | `IRepeaterScrollingSurface.IsVerticallyScrollable` (line 151) | OK |
| 12 | `AnchorElement` (line 125) | `IRepeaterScrollingSurface.AnchorElement` (line 153) | OK |
| 13 | `ViewportChanged` add/remove (line 131/136) | event add/remove (line 155) | OK |
| 14 | `PostArrange` add/remove (line 141/146) | event add/remove (line 161) | OK |
| 15 | `ConfigurationChanged` add/remove (line 151/157) | event add/remove (line 167) | OK |
| 16 | `RegisterAnchorCandidate` (line 162) | `RegisterAnchorCandidate` (line 177) | OK |
| 17 | `UnregisterAnchorCandidate` (line 188) | `UnregisterAnchorCandidate` (line 207) | OK |
| 18 | `GetRelativeViewport` (line 200) | `IRepeaterScrollingSurface.GetRelativeViewport` (line 219) | OK |
| 19 | `ApplyPendingChangeView` (line 233) | `ApplyPendingChangeView` (line 263) | OK |
| 20 | `TrackElement` (line 275) | `TrackElement` (line 304) | OK |
| 21 | `GetAnchorElement` (line 339) | `GetAnchorElement` (line 367) | OK |
| 22 | `OnScrollViewerViewChanging` (line 403) | `OnScrollViewerViewChanging` (line 427) | OK |
| 23 | `OnScrollViewerViewChanged` (line 410) | `OnScrollViewerViewChanged` (line 432) | OK |
| 24 | `OnScrollViewerSizeChanged` (line 430) | `OnScrollViewerSizeChanged` (line 450) | OK |

**Method-order verdict:** Significant reordering at the top of the file. WinUI orders: ctor → CurrentAnchor → ScrollViewer get/set → IFrameworkElementOverrides (Measure/Arrange) → HorizontalAnchorRatio/VerticalAnchorRatio → StartBringIntoView → IsHorizontallyScrollable...

The Uno port re-groups by interface region (Measure/Arrange come first, then the entire IScrollAnchorProvider region, then IRepeaterScrollingSurface, then StartBringIntoView). This violates rule #3 (method order in `.mux.cs` MUST match `.cpp`).

#### Field verification (`.h` vs `.h.mux.cs`)

| C++ field | Type | Uno field | Notes |
|---|---|---|---|
| `m_candidates` (line 158) | `std::vector<CandidateInfo>` | `m_candidates` (line 72) | OK — `List<CandidateInfo>` |
| `m_anchorElement` (line 160) | `tracker_ref<winrt::UIElement>` | `m_anchorElement` (line 74) | OK |
| `m_anchorElementRelativeBounds` (line 161) | `winrt::Rect{}` | `m_anchorElementRelativeBounds` (line 75) | OK |
| `m_isAnchorElementDirty` (line 163) | `bool{true}` | `m_isAnchorElementDirty = true` (line 77) | OK |
| `m_horizontalEdge` (line 165) | `double{}` | `m_horizontalEdge` (line 80) | **Logic change** — see High finding |
| `m_verticalEdge` (line 166) | `double{}` | `m_verticalEdge` (line 81) | **Logic change** — see High finding |
| `m_pendingBringIntoView{this}` (line 174) | `BringIntoViewState` (constructor-injected owner) | `m_pendingBringIntoView` (line 90, default-initialized) | OK — owner concept dropped (no tracker) |
| `m_pendingViewportShift{}` (line 181) | `double` | `m_pendingViewportShift` (line 97) | OK |
| `m_viewportChanged` (line 183) | `event_source<...>` | `event m_viewportChanged` (line 99) | OK — C# event |
| `m_postArrange` (line 184) | `event_source<...>` | `event m_postArrange` (line 100) | OK |
| **MISSING** | — | `event m_configurationChanged` (line 102) | **Extra field** — see High finding |
| `m_scrollViewerViewChanging` (line 186) | `ViewChanging_revoker` | `m_scrollViewerViewChanging` (line 106) | OK — converted to `IDisposable` per rule #7 |
| `m_scrollViewerViewChanged` (line 187) | `ViewChanged_revoker` | `m_scrollViewerViewChanged` (line 107) | OK |
| `m_scrollViewerSizeChanged` (line 188) | `SizeChanged_revoker` | `m_scrollViewerSizeChanged` (line 108) | OK |

#### Critical

- **`HorizontalAnchorRatio` / `VerticalAnchorRatio` get/set bodies do NOT touch `m_horizontalEdge` / `m_verticalEdge`. State is dropped.**
  - **C++ (.cpp:75-93):**
    ```cpp
    double ItemsRepeaterScrollHost::HorizontalAnchorRatio()
    { return m_horizontalEdge; }
    void ItemsRepeaterScrollHost::HorizontalAnchorRatio(double value)
    { m_horizontalEdge = value; }
    double ItemsRepeaterScrollHost::VerticalAnchorRatio()
    { return m_verticalEdge; }
    void ItemsRepeaterScrollHost::VerticalAnchorRatio(double value)
    { m_verticalEdge = value; }
    ```
  - **C# (.mux.cs:99-101):**
    ```csharp
    internal double HorizontalAnchorRatio { get; set; }
    internal double VerticalAnchorRatio { get; set; }
    ```
  - **Issue:** Uno replaces the explicit field-backed accessors with auto-properties — and the `.h.mux.cs` still keeps `m_horizontalEdge` / `m_verticalEdge` as private fields (with `#pragma warning disable 169` because they're now unused). The visible behavior is equivalent (auto-property generates a backing field), but the parity rule wants the named field preserved with explicit get/set bodies, or at least the auto-properties should not be wrapped with an unused field. Additionally, the IDL exposes these as `Double HorizontalAnchorRatio { get; set; }` (public surface). **The Uno port marks them `internal` — narrower than the IDL public surface.** This is a visibility regression vs IDL and rule #5.
  - **Suggested fix:**
    1. Restore named fields and explicit get/set on the property (or remove the dead `m_horizontalEdge` / `m_verticalEdge` fields entirely).
    2. Change `internal` to `public` on both properties to match IDL (IDL: `Double HorizontalAnchorRatio { get; set; };`, `Double VerticalAnchorRatio { get; set; };`).

#### High

- **Visibility regression vs IDL on `HorizontalAnchorRatio` / `VerticalAnchorRatio`.**
  - **IDL:** `Double HorizontalAnchorRatio { get; set; }; Double VerticalAnchorRatio { get; set; };` — public.
  - **C#:** `internal` (rule #5 violation).
  - **Suggested fix:** make `public`.

- **Extra `m_configurationChanged` event field added.**
  - **C++:** `ConfigurationChanged` add/remove (.cpp:151-160) intentionally returns `{}` and does nothing (no backing storage).
  - **C# (.h.mux.cs:102):** Uno adds a `private event ConfigurationChangedEventHandler m_configurationChanged;` field and the `add/remove` accessors store handlers in it. WinUI deliberately drops the handler on the floor. This is a behavior change — Uno would invoke handlers if the event were ever raised; WinUI never would.
  - **C# (.mux.cs:167-171):**
    ```csharp
    event ConfigurationChangedEventHandler IRepeaterScrollingSurface.ConfigurationChanged
    {
        add => m_configurationChanged += value;
        remove => m_configurationChanged -= value;
    }
    ```
  - **Suggested fix:** drop the field; the add/remove should be empty (or wrap in `#if HAS_UNO` and add `TODO Uno:`). The C++ comment uses `/*value*/` and `/*token*/` to mark the parameters as deliberately unused — preserve that intent.

- **`ArrangeOverride` heavily diverges from C++ (Uno-specific path not gated by `#if HAS_UNO` or `#if !HAS_UNO`).**
  - **C++ (.cpp:60-69):** the entire body of `ArrangeOverride` is:
    ```cpp
    const winrt::Size result = finalSize;
    if (auto scrollViewer = ScrollViewer())
    {
        scrollViewer.Arrange({ 0, 0, finalSize.Width, finalSize.Height });
    }
    return result;
    ```
    No anchor processing happens inside `ArrangeOverride`. WinUI relies on RS5+ ScrollViewer anchoring exclusively in current source.
  - **C# (.mux.cs:40-93):** runs an extensive RS4-style anchor pipeline (compute anchor, apply pending bring into view, TrackElement, raise `m_postArrange`, etc.) under `#if SCROLLVIEWER_SUPPORTS_ANCHORING` ELSE branch. The `else` branch is the active code path in Uno.
  - **Issue:** the active code in `ArrangeOverride` is older WinUI logic resurrected for Uno's ScrollViewer (which doesn't yet support anchoring). This must be flagged as Uno-specific. Currently NOT wrapped in `#if HAS_UNO`; the `#if SCROLLVIEWER_SUPPORTS_ANCHORING` define has the *true* branch matching upstream and the `false` branch holding the Uno legacy implementation — but neither path is labeled as "Uno-specific" per rule #6.
  - **Suggested fix:** Either (a) move the legacy RS4-style anchoring path into a `.uno.cs` file behind `#if HAS_UNO`, or (b) document with a leading comment block that the `#if !SCROLLVIEWER_SUPPORTS_ANCHORING` branch is *intentionally* mirroring a pre-RS5 WinUI commit, with a `TODO Uno:` referencing the upstream commit hash. (Note: pre-RS5 WinUI did have this exact logic in `ArrangeOverride`; the comment block should cite that.)

- **`ScrollViewer` property: bound to `Children()` vs `VisualTreeHelper.GetChildren(this)` is a structural Uno deviation not flagged.**
  - **C++ (.cpp:23-44):**
    ```cpp
    winrt::FxScrollViewer ItemsRepeaterScrollHost::ScrollViewer()
    {
        auto children = Children();
        ...
    }
    ```
    `Children()` is the inherited `Panel.Children` accessor. The class derives from `DeriveFromPanelHelper_base` (see `.h:10`).
  - **C# (.cs:17):** declared `FrameworkElement` — does NOT derive from Panel. `ScrollViewer` accessor uses `VisualTreeHelper.GetChildren(this).ToList()` and `VisualTreeHelper.ClearChildren(this) / .AddChild(this, value)` to manage its single child.
  - **Issue:** The base class is wrong. WinUI's `ItemsRepeaterScrollHost` is a Panel (so its IDL-exposed `ContentProperty(Name="ScrollViewer")` plus the panel `Children` form the actual contract). The Uno port is `FrameworkElement` and uses `VisualTreeHelper` as a workaround. This is a non-trivial architectural deviation and should be in `#if HAS_UNO` with `TODO Uno:` plus a public spec note. The comment at lines 107-108 only partially documents this.
  - **Suggested fix:** ideally change the base class to `Panel`. Failing that, mark the deviation with `// TODO Uno: ItemsRepeaterScrollHost should derive from Panel (DeriveFromPanelHelper_base). Tracked as alignment work.` and wrap the `VisualTreeHelper` paths in `#if HAS_UNO`.

- **`ConfigurationChanged` parameter naming lost.**
  - **C++ (.cpp:151-160):** `(winrt::ConfigurationChangedEventHandler const& /*value*/)` and `(winrt::event_token const& /*token*/)` — `/*value*/` and `/*token*/` are intentional "unused" markers. WinUI explicitly drops the value/token.
  - **C# (.mux.cs:167-171):** Uno actually wires the handler up to `m_configurationChanged`. The intent is changed.
  - **Suggested fix:** see the "Extra `m_configurationChanged` event field added" finding above.

- **`StartBringIntoView` body changed — drops `!!animate` and the `owner` arg.**
  - **C++ (.cpp:96-113):**
    ```cpp
    m_pendingBringIntoView = {
        this /* owner */,
        element,
        alignmentX,
        alignmentY,
        offsetX,
        offsetY,
        !!animate };
    ```
  - **C# (.mux.cs:252-261):** drops the `this /* owner */` argument (acceptable — owner is the C++ ITrackerHandleManager concept that doesn't apply to managed code). The `!!animate` is just a bool normalization, also fine to drop in C#.
  - **Issue (minor):** the `/* owner */` comment is lost; the comment is informational about ABI semantics. Low priority.
  - **Suggested fix:** add a brief comment noting that the C++ owner argument is intentionally omitted (`// Owner argument from C++ ITrackerHandleManager is not needed in managed code`).

#### Medium

- **Missing trace/log calls — message format strings lost.**
  - **C++ (.cpp:194):** `ITEMSREPEATER_TRACE_INFO_DBG(nullptr, TRACE_MSG_METH_STR_INT, METH_NAME, this, L"Unregistered candidate:", it - m_candidates.begin());`
  - **C# (.mux.cs:213):** `REPEATER_TRACE_INFO("Unregistered candidate %d\n", it);`
  - The format-string convention is fine for Uno's logging shim. But Uno's call uses `printf`-style and is missing the trailing index value being passed as `it` (which in C++ is the iterator subtraction = the offset). The C# `it` is `FindIndex` result already — same numerical value. OK semantically, but format-string semantics diverge (`%d` vs C++ macro tokens).

- **`REPEATER_TRACE_INFO` macro semantics.** `REPEATER_TRACE_INFO("Pending Shift - %lf\n", m_pendingViewportShift);` (.mux.cs:235) vs C++ `ITEMSREPEATER_TRACE_INFO_DBG(... L"Pending shift:", m_pendingViewportShift);` — the trace label changes slightly ("Shift" vs "shift", "-" vs ":"). Cosmetic.

- **`REPEATER_TRACE_INFO("ItemsRepeaterScrollHost scroll to absolute offset (%.0f, %.0f), animate=%d \n", ...)` (.mux.cs:294)** combines two C++ trace lines (lines 263 and 264) into one. Minor — but two separate trace lines vs one is a logging behavior difference.

- **`CandidateInfo.IsRelativeBoundsSet` semantics.**
  - **C++ (.h:99):** `bool IsRelativeBoundsSet() const { return m_relativeBounds != InvalidBounds; }`
  - **C# (.h.mux.cs:30):** `public bool IsRelativeBoundsSet => RelativeBounds != InvalidBounds;`
  - **Issue:** Equality on `Rect` in C# uses `Rect.Equals`, which uses bitwise float equality. C++ `winrt::Rect != InvalidBounds` does likewise. Same semantics — note that this includes NaN handling but `InvalidBounds = (-1, -1, -1, -1)` is finite. OK.

- **`CandidateInfo` constructor takes `UIElement element` directly, not `(owner, element)`.**
  - **C++ (.h:90-94):** `CandidateInfo(const ITrackerHandleManager* owner, winrt::UIElement element) : m_element(owner, element) { }`
  - **C# (.h.mux.cs:20-24):** `public CandidateInfo(UIElement element) { Element = element; RelativeBounds = InvalidBounds; }`
  - The `owner` argument is C++ tracker plumbing. Dropping it is correct per syntactic conversion rules. OK.
  - **Issue:** the C# version explicitly sets `RelativeBounds = InvalidBounds`. C++ has `winrt::Rect m_relativeBounds{ InvalidBounds };` as an in-class initializer. Same outcome.

- **`BringIntoViewState` constructor: C++ has two ctors (one for default with `owner` only, and one with all the params). C# has only one.**
  - **C++ (.h:109-126):** two constructors.
  - **C# (.h.mux.cs:35-52):** single constructor; default state achieved via field initializers on the wrapped properties.
  - **Issue:** `m_pendingBringIntoView{ this }` (the default-state ctor) is replaced by C# `private BringIntoViewState m_pendingBringIntoView;` (default struct). The first C++ ctor is used to construct `m_pendingBringIntoView` as a default state member. Since C# struct types can be default-constructed, the missing ctor is not needed. OK.
  - **Issue (minor):** `BringIntoViewState` is a `struct` in C# but a `struct` in C++ too — but the C++ has stateful semantics around `tracker_ref` that aren't relevant. OK.

- **`m_pendingBringIntoView = bringIntoView;` at end of `ApplyPendingChangeView`.**
  - **C++ (.cpp:272):** `m_pendingBringIntoView = std::move(bringIntoView);`
  - **C# (.mux.cs:301):** `m_pendingBringIntoView = bringIntoView;`
  - **Issue:** the local `bringIntoView` is a copy of the struct in both languages; assignment back is correct. However in C#, the *struct* is value-typed, so the local was a separate copy from `m_pendingBringIntoView`. The C++ approach mutates the local then assigns back. Same effect since both struct types have value semantics for the relevant fields. OK.

- **`Children()` ↔ `VisualTreeHelper.GetChildren/ClearChildren/AddChild`.** Discussed above as a base-class deviation.

#### Low

- **Comment paraphrased: `// We don't want to listen to events in RS5+ since this guy is a no-op.`**
  - **C++:** comment exists in `.cpp` line 129? — Actually this comment is not in `.cpp` upstream. Verify: searching upstream `.cpp` shows no such comment. **The Uno comment at `.mux.cs:129` "We don't want to listen to events in RS5+ since this guy is a no-op." appears to be Uno-added.** Acceptable as long as it's accurate; should be marked `// Uno: ...` to be clear it's not in upstream.

- **Comment lost from `.h` line 175:** `const auto it = std::find_if(m_candidates.cbegin(), m_candidates.cend(), [&elem](const CandidateInfo& c) { return c.Element() == elem; });` — the predicate is preserved as `Any` in C# but the lambda style/structure changes. Behavior is identical.

- **C++ comment `// DBG`** at `.cpp:181 (#endif // DBG)` is reproduced in C# as `// _DEBUG` — different macro name. Minor.

- **C++ uses `cbegin()`/`cend()` (const iterators); the C#'s `FindIndex` doesn't have a parallel.** Acceptable syntactic conversion.

- **`if (scrollViewer != default)` redundancy.**
  - **C# (.mux.cs:80):** `else if (scrollViewer != default)` inside the `if (ScrollViewer is { } scrollViewer)` block — the inner null check is dead because we already entered the outer block. This is dead code that doesn't exist in any matching position in C++ (C++ doesn't have this check at all because the corresponding logic isn't in `ArrangeOverride`). Minor; arises from the Uno-specific arrange path.

- **`auto_revoke` not used.** Per rule #7, `auto_revoke` → `SerialDisposable` + `Disposable.Create(...)`. Uno uses plain `IDisposable` (not `SerialDisposable`). This is a small deviation from the prescribed pattern.
  - **C# (.h.mux.cs:106-108):** `private IDisposable m_scrollViewerViewChanging;` (etc.)
  - **Suggested fix:** change to `private SerialDisposable m_scrollViewerViewChanging = new SerialDisposable();` and assign `.Disposable = Disposable.Create(...)`.

- **`m_viewportChanged?.Invoke(this, true /* isFinal */);` — comments preserved.** OK.

- **MUX Reference header on `.cs` file points to `.h` instead of "decl only".**
  - `ItemsRepeaterScrollHost.cs:3` says `// MUX Reference ItemsRepeaterScrollHost.h, commit 4b206bce3`. This is acceptable — the `.cs` file holds public XML doc and class declaration which corresponds to the `.h` declaration.

- **`Rect.InvalidBounds = new Rect(-1.0f, -1.0f, -1.0f, -1.0f)` is `static readonly`, but C++ is `static constexpr`.** Different semantics: in C# this is initialized at runtime. Acceptable per syntactic conversion.

- **`m_isAnchorElementDirty = true` initializer position differs from C++.** OK.

- **Method `CurrentAnchor` is a property in C# but a method in C++.** C++ `winrt::UIElement CurrentAnchor()` is a property getter via IDL `Microsoft.UI.Xaml.UIElement CurrentAnchor{ get; }`. Property mapping is the expected conversion. OK.

#### Info

- **`m_anchorElement` is `UIElement` directly (.h.mux.cs:74), not wrapped.** C++ uses `tracker_ref<winrt::UIElement> m_anchorElement{ this };` — syntactic conversion via rule "tracker_ref<T> → T". OK.

- **The `partial class ItemsRepeaterScrollHost` is missing the `: FrameworkElement, IScrollAnchorProvider, IRepeaterScrollingSurface` declaration on the `.h.mux.cs` and `.mux.cs` partials.** Standard C# partial pattern, but the IFrameworkElement/IRepeaterScrollingSurface inheritance is only on `.cs`. Mixed partial declarations are fine.

- **`m_viewportChanged?.Invoke(this, ...)` use of `?.Invoke` accounts for absent subscribers; C++ `event_source` similarly no-ops when empty. OK.

- **`MUX_ASSERT(false)` translated to `MUX_ASSERT(false)` (assumed to be a helper).** OK if `MUX_ASSERT` is defined in Uno's helpers — verify.

---

### RepeaterAutomationPeer

#### File mapping

| WinUI | Uno |
|---|---|
| `RepeaterAutomationPeer.h` | (no `.h.mux.cs`) |
| `RepeaterAutomationPeer.cpp` | `RepeaterAutomationPeer.mux.cs` |
| `RepeaterAutomationPeer.idl` | `RepeaterAutomationPeer.cs` (decl + XML doc) |

No `.h.mux.cs` exists. The class is small enough that the field-less `.h` doesn't need a separate decl file, but rule #2 says one should exist.

#### Method order verification

| # | C++ method (`.cpp`) | C# method (`.mux.cs`) | Match? |
|---|---|---|---|
| 1 | ctor (line 15) | ctor (line 12) | OK |
| 2 | `GetChildrenCore` (line 22) | `GetChildrenCore` (line 18) | OK |
| 3 | `GetAutomationControlTypeCore` (line 62) | `GetAutomationControlTypeCore` (line 56) | OK |
| 4 | `GetElement` (private, line 70) | `GetElement` (private, line 64) | OK |

Method order matches. Good.

#### Field verification

| C++ field | C# field |
|---|---|
| (none; class has no instance fields) | (none) |

#### High

- **`GetChildrenCore` signature visibility — `protected override` is correct for IAutomationPeerOverrides, but C++ pattern uses inheritance via `GetInner().as<winrt::IAutomationPeerOverrides>().GetChildrenCore()` rather than `base.GetChildrenCore()`.**
  - **C++ (.cpp:25):** `const auto childrenPeers = GetInner().as<winrt::IAutomationPeerOverrides>().GetChildrenCore();`
  - **C# (.mux.cs:21):** `var childrenPeers = base.GetChildrenCore();`
  - **Issue:** C++'s `GetInner()` reaches the framework's *next-most-derived* peer's `GetChildrenCore` (composition pattern), not the immediate base. In C#, `base.GetChildrenCore()` calls the immediate base (`FrameworkElementAutomationPeer.GetChildrenCore`). Functionally equivalent for this use case, but semantically distinct. Acceptable as the standard C# AutomationPeer pattern.

#### Medium

- **`childrenPeers.Count` vs `childrenPeers.Size()` — local `unsigned peerCount` becomes `int peerCount`.**
  - **C++ (.cpp:26):** `const unsigned peerCount = childrenPeers.Size();`
  - **C# (.mux.cs:22):** `var peerCount = childrenPeers.Count;` — type inferred as `int`.
  - **Issue:** C++ uses `unsigned` (uint32_t); C# uses `int`. The loop bound is identical; signedness mismatch is only an issue at extreme sizes (>2B). Acceptable.

- **`static_cast<int>(peerCount)` reservation hint lost.**
  - **C++ (.cpp:28-29):**
    ```cpp
    std::vector<std::pair<int, winrt::AutomationPeer>> realizedPeers;
    realizedPeers.reserve(static_cast<int>(peerCount));
    ```
  - **C# (.mux.cs:24):** `var realizedPeers = new List<KeyValuePair<int, AutomationPeer>>(peerCount);` — uses capacity constructor.
  - OK — same effect.

#### Low

- **The "Sort peers by index." comparator: C++ uses `lhs.first < rhs.first` returning bool; C# uses `lhs.Key - rhs.Key` returning int (delta).**
  - **C++ (.cpp:48):** `[](const auto& lhs, const auto& rhs) { return lhs.first < rhs.first; }`
  - **C# (.mux.cs:43):** `(lhs, rhs) => lhs.Key - rhs.Key`
  - **Issue:** subtracting two large `int` indexes could overflow producing incorrect ordering for extreme values. Strictly, the safer C# port is `lhs.Key.CompareTo(rhs.Key)`. Minor.
  - **Suggested fix:** change to `(lhs, rhs) => lhs.Key.CompareTo(rhs.Key)`.

- **`winrt::make<Vector<...>>` factory call lost.**
  - **C++ (.cpp:52-53):** uses MUX `Vector` adapter with `MakeVectorParam<VectorFlag::DependencyObjectBase>()`. C# uses plain `List<AutomationPeer>` then returns it. The IDL-exposed return type is `IVector<AutomationPeer>`. C# `List<T>` implements `IList<T>` which is the C# equivalent. Acceptable.

- **`childElement = parent;` casts.**
  - **C++ (.cpp:77):** `while (parent && parent.try_as<winrt::ItemsRepeater>() != repeater)`
  - **C# (.mux.cs:71):** `while (parent != null && parent as ItemsRepeater != repeater)`
  - The `as ItemsRepeater != repeater` comparison: when `parent` is not an `ItemsRepeater`, the `as` yields `null` and `null != repeater` is `true` if `repeater` is non-null — which is the desired loop continuation. Identical behavior.

- **Missing comment "Get the immediate child element of repeater under which this childPeer came from." has trailing whitespace in C++ (line 69 has trailing space). Preserved as-is in C# (no trailing). Trivial.**

#### Info

- **No `RepeaterAutomationPeer.h.mux.cs` file.** The `.h` is very small (no instance fields, only public/private method decls). Rule #2 technically requires it; pragmatically a no-op for a fieldless class. Low priority; consider adding for consistency.

- **IDL surface verification:**
  - IDL declares: `RepeaterAutomationPeer(MU_XC_NAMESPACE.ItemsRepeater owner);` — public constructor.
  - Uno: `public RepeaterAutomationPeer(ItemsRepeater owner) : base(owner) { }` — public. Matches IDL. OK.

---

## Cross-type observations

1. **Three EventArgs files (Clearing/IndexChanged/Prepared) all collapse `.h`+`.cpp` into a single `.cs`.** Rule #2 says split into `.cs` (decl), `.h.mux.cs` (header), `.mux.cs` (impl). Either accept this as a deliberate exception for small EventArgs (document it in `AGENTS.md`) or split them.
2. **MUX Reference header absent on all three EventArgs `.cs` files.** Consistently missing; add a single header citing both the `.cpp` and `.h` provenance.
3. **`#region` vs `// #pragma region` inconsistency.** EventArgs use C# `#region`; other ported files use `// #pragma region`. Pick one style.
4. **Constructors are widened to `internal` without justification comments.** Pattern is consistent across the three EventArgs.
5. **WinUI `tracker_ref<winrt::UIElement> m_element{ this };` → C# auto-property in all three EventArgs.** This is a legitimate syntactic conversion but loses the WinUI field name; consider a comment.
6. **The `ItemsRepeaterScrollHost` Uno port carries pre-RS5 anchoring logic in the `else` branch of `#if SCROLLVIEWER_SUPPORTS_ANCHORING`.** This is significant Uno-specific behavior that lacks both `#if HAS_UNO` guards and clear comments documenting it as legacy WinUI code resurrected for Uno-side ScrollViewer.
7. **`HorizontalAnchorRatio` / `VerticalAnchorRatio` properties marked `internal` in Uno conflict with IDL which marks them public.** This is a public-surface regression.
8. **Base-class deviation: `ItemsRepeaterScrollHost` should be `Panel` (WinUI), but Uno uses `FrameworkElement` and `VisualTreeHelper`.** Substantive API contract change.

---

## Conclusion

### Total findings by severity

- **Critical:** 1 — `HorizontalAnchorRatio`/`VerticalAnchorRatio` are `internal` (vs public IDL) and the named backing fields `m_horizontalEdge`/`m_verticalEdge` are dead, leaking out of `.h.mux.cs` under a warning-suppression block.
- **High:** 12 — File layout collapse for EventArgs; method-order reordering in `ItemsRepeaterScrollHost.mux.cs`; missing `m_viewType` field; extra `m_configurationChanged` event field altering observable semantics; large Uno-specific anchoring path in `ArrangeOverride` not gated by `#if HAS_UNO`; `ScrollViewer` accessor using `VisualTreeHelper` rather than `Panel.Children` because the base class differs from WinUI; `ConfigurationChanged` add/remove handler-storage semantics changed; `Update(in int, in int)` in IndexChanged uses `in` modifier; missing MUX Reference headers; missing `m_viewType`; visibility narrowing on AnchorRatio properties.
- **Medium:** 16 — IDL surface mismatches, trace-message format differences, region-style differences, etc.
- **Low:** 19 — comment paraphrasing, `auto_revoke` → `IDisposable` vs `SerialDisposable`, lambda subtraction ordering, dead code in `ArrangeOverride`.
- **Info:** 9 — file-layout suggestions and harmless syntactic conversions.

### Top priority issues

1. **`HorizontalAnchorRatio` / `VerticalAnchorRatio` visibility regression (internal vs public IDL).** Fix immediately — observable public API regression.
2. **`ItemsRepeaterScrollHost` base class is `FrameworkElement` not `Panel`.** Substantive contract break — `ScrollViewer` ContentProperty + `Children` semantics differ.
3. **`m_configurationChanged` event field present; WinUI deliberately drops handlers.** Behavior change.
4. **`ArrangeOverride` carries pre-RS5 Uno-specific anchoring path with no `#if HAS_UNO`/`TODO Uno:` markers.** Hidden architectural deviation.
5. **Three EventArgs files violate rule #2 (single-file layout).** Refactor for consistency.
6. **Method order in `ItemsRepeaterScrollHost.mux.cs` does not match `.cpp` (top half rearranged).** Refactor for 1:1 parity.
7. **`m_viewType` field missing from `ItemsRepeaterElementPreparedEventArgs`.** Restore for header parity.
8. **`Update(in int oldIndex, in int newIndex)` should drop the `in` modifier.**
9. **Add MUX Reference headers to all EventArgs files.**
10. **`auto_revoke` should use `SerialDisposable` per rule #7.**
