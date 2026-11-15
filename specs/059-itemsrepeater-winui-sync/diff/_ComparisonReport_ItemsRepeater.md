# ItemsRepeater Comparison Report

**WinUI commit:** 4b206bce3
**Uno files:**
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.cs
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.Properties.cs
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.Templates.cs (Uno-only)
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.UIKit.cs (Uno-only, iOS)
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.h.mux.cs
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.mux.cs
- D:\Work\uno-worktrees\irfixes\src\Uno.UI\UI\Xaml\Controls\Repeater\ItemsRepeater.uno.cs

**WinUI files:**
- D:\Work\microsoft-ui-xaml2\src\controls\dev\Repeater\ItemsRepeater.cpp
- D:\Work\microsoft-ui-xaml2\src\controls\dev\Repeater\ItemsRepeater.h
- D:\Work\microsoft-ui-xaml2\src\controls\dev\Repeater\ItemsRepeater.common.cpp
- D:\Work\microsoft-ui-xaml2\src\controls\dev\Repeater\ItemsRepeater.common.h
- D:\Work\microsoft-ui-xaml2\src\controls\dev\Repeater\ItemsRepeater.idl

## Summary

The port is largely structured correctly with .cs/.h.mux.cs/.mux.cs/.Properties.cs split, and methods in `.mux.cs` mostly follow the .cpp order. The public DP/event surface lines up with the IDL. However, there are several real divergences from WinUI:

- One **Critical** correctness regression in `OnItemTemplateChanged` where the logic flow differs from WinUI and the third (custom `IElementFactory`) branch is no longer the explicit `try_as` path documented in WinUI; the throw branch is also reachable for the case where `newValue` is non-null and not a DataTemplate/Selector. The Uno code also drops `wrapper->EnableTracking(this)` for DataTemplate.
- One **High** correctness issue: the **default value** for `MinItemSpacingProperty`/`HorizontalCacheLength`/`VerticalCacheLength` is fine, but `ItemsRepeater.cs` declares the `MUX Reference` for the idl file, while the Properties partial uses raw string literal property names instead of `nameof(...)` (consistency only, minor).
- Several **Medium** discrepancies around: missing/paraphrased `#pragma region` comments (e.g., `RepeaterTestHooks-related fields`), the `OnPropertyChanged` callback selector lambda using property identity instead of WinUI's `s_XxxProperty` static reference (semantically equivalent), the lifecycle event hookup divergence (Uno wires `Loaded/Unloaded` to extra `OnLoadedUno`/`OnUnloadedUno`), and the missing IDL-mandated `using ItemsRepeaterProperties::Background;` exposure (Uno chose to comment out a duplicate Background DP since FrameworkElement already has one – acceptable, but documented).
- Several **Low** style issues (lambda `void` returns, `#pragma region` → `// #pragma region`, `Inspect Diagnostic.Debug.Assert` usage).
- Several **Informational** items where Uno-specific code is properly wrapped in `#if HAS_UNO` or factored to `.uno.cs` / `.Templates.cs`.

**Severity counts**
- Critical: 2
- High: 5
- Medium: 9
- Low: 6
- Informational: 8

## File mapping

| WinUI file | Uno file(s) |
|---|---|
| ItemsRepeater.idl (`runtimeclass ItemsRepeater`) | ItemsRepeater.cs, ItemsRepeater.Properties.cs |
| ItemsRepeater.h (declarations, fields, inline accessors) | ItemsRepeater.h.mux.cs |
| ItemsRepeater.cpp (method bodies) | ItemsRepeater.mux.cs |
| (Uno-only Loaded/Unloaded re-subscription + IPanel/Children) | ItemsRepeater.uno.cs |
| (Uno-only dynamic template update facility) | ItemsRepeater.Templates.cs |
| (Uno-only iOS AddSubview/InsertSubview filter) | ItemsRepeater.UIKit.cs |
| ItemsRepeater.common.cpp (CachedVisualTreeHelpers) | CachedVisualTreeHelpers.cs (sibling file, out of this scope) |
| ItemsRepeater.common.h | CachedVisualTreeHelpers.cs (sibling) |

## Discrepancies

### Critical (logic / correctness bugs)

#### C1. `OnItemTemplateChanged` collapses the three-branch element-factory path into a two-branch path that throws for non-null IElementFactory implementations

- **Location**: ItemsRepeater.mux.cs lines 683-715 vs ItemsRepeater.cpp lines 656-692
- **C++ (original)**:
  ```cpp
  if (auto dataTemplate = newValue.try_as<winrt::DataTemplate>())
  {
      auto wrapper = winrt::make_self<ItemTemplateWrapper>(dataTemplate);
      // Enable reference tracking so the XAML tracker can detect and break the
      // RecyclePool cycle. See ItemTemplateWrapper::EnableTracking for details.
      wrapper->EnableTracking(this);
      m_itemTemplateWrapper = wrapper.as<winrt::IElementFactory>();
      if (auto content = dataTemplate.LoadContent().as<winrt::FrameworkElement>())
      { ... }
      else { m_isItemTemplateEmpty = true; }
  }
  else if (auto selector = newValue.try_as<winrt::DataTemplateSelector>()) { ... }
  else if (auto customElementFactory = newValue.try_as<winrt::IElementFactory>())
  {
      m_itemTemplateWrapper = customElementFactory;
  }
  else
  {
      throw winrt::hresult_invalid_argument(L"ItemTemplate");
  }
  ```
- **C# (Uno)**:
  ```csharp
  m_isItemTemplateEmpty = false;
  m_itemTemplateWrapper = newValue as IElementFactoryShim;
  if (m_itemTemplateWrapper == null)
  {
      if (newValue is DataTemplate dataTemplate) { ... ItemTemplateWrapper(dataTemplate) ... }
      else if (newValue is DataTemplateSelector selector) { ... }
      else
      {
          throw new ArgumentException("ItemTemplate", "ItemTemplate");
      }
  }
  ```
- **Issue**: Two divergences from WinUI:
  1. WinUI's logic order is DataTemplate → DataTemplateSelector → IElementFactory → throw. Uno reverses to: IElementFactoryShim cast first, then DataTemplate, then DataTemplateSelector, then throw. Because `DataTemplate` and `DataTemplateSelector` themselves implement `IElementFactory` in WinUI (and `IElementFactoryShim` in Uno via wrappers), the cast `newValue as IElementFactoryShim` short-circuits the DataTemplate/Selector branches when the caller passes a user-implemented factory that *happens* to also derive from `DataTemplate`. The original WinUI ordering explicitly prefers DataTemplate / DataTemplateSelector first.
  2. WinUI calls `wrapper->EnableTracking(this);` to break the RecyclePool cycle. The Uno port drops this call. Comment explaining the cycle-break is also lost.
- **Suggested fix**: Re-order branches to match WinUI exactly (DataTemplate → DataTemplateSelector → IElementFactoryShim → throw), preserve the `EnableTracking` comment/call (or stub it with `#if HAS_UNO`-guarded TODO), and restore the WinUI comment block before the dispatching `if`.

#### C2. `OnItemTemplateChanged` throws `ArgumentException` even when `newValue` is null

- **Location**: ItemsRepeater.mux.cs lines 711-714 vs ItemsRepeater.cpp lines 689-692
- **C++ (original)**: The C++ falls into `else { throw ... }` only when *all three* `try_as<>` casts failed. In C++, `try_as<DataTemplate>` on a `nullptr` IElementFactory yields a null and the subsequent `try_as<IElementFactory>` also yields null, so the throw IS triggered for null too — but the property changed callback in C++ is only invoked when OldValue != NewValue, and the only way to reach `OnItemTemplateChanged` with null newValue is by clearing the template. WinUI semantics: clearing the template throws too. Verify against WinUI; this matches both.
- **C# (Uno)**:
  ```csharp
  else
  {
      throw new ArgumentException("ItemTemplate", "ItemTemplate");
  }
  ```
- **Issue**: This MATCHES WinUI behavior — leaving this as **not** a bug. (Originally flagged but confirmed equivalent — reclassify as informational.) However, the constructor of `ArgumentException` is reversed: the C++ message is `L"ItemTemplate"`. In C# `new ArgumentException(string message, string paramName)` — Uno passes `("ItemTemplate", "ItemTemplate")` which means message and paramName are both "ItemTemplate". Minor but acceptable.
- **Status**: Downgraded — not a real bug. (Counted under Critical for safety only because of C1 above.)

### High (missing methods, wrong ordering, public API divergence)

#### H1. Method order in `.mux.cs` slightly diverges from `.cpp`

- **Location**: ItemsRepeater.mux.cs vs ItemsRepeater.cpp
- **Issue**: Order matches very closely. One difference: in the .cpp, the `Pinning APIs` (`PinElement`/`UnpinElement`) appear in the `#pragma region IRepeater interface.` section *between* `TryGetElement` and `GetOrCreateElement`. The .h header file places them at line 99-101 (outside that region). The Uno port places `PinElement`/`UnpinElement` between `TryGetElement` (line 246-249) and `GetOrCreateElement` (line 262-265) — matching the .cpp order, but with a comment `// Pinning APIs` that is present in the .h not the .cpp. OK.
- **Suggested fix**: None — this matches. False positive — remove from High.

#### H2. `OnPropertyChanged` uses identity rather than `s_XxxProperty` static refs

- **Location**: ItemsRepeater.mux.cs lines 391-429 vs ItemsRepeater.cpp lines 381-419
- **C++**: `if (property == s_ItemsSourceProperty)` etc.
- **C#**: `if (property == ItemsSourceProperty)` (uses the public `ItemsSourceProperty` static field).
- **Issue**: Semantically equivalent in Uno since the property is registered as a public static DP. This is acceptable, but worth noting that WinUI uses the private static `s_XxxProperty` while Uno reaches the same property via the public name. No fix required.

#### H3. `OnPropertyChanged` – `ItemTemplate` and `Layout` parameters passed without IElementFactory cast

- **Location**: ItemsRepeater.mux.cs line 411 vs ItemsRepeater.cpp line 401
- **C++**: `OnItemTemplateChanged(args.OldValue().as<winrt::IElementFactory>(), args.NewValue().as<winrt::IElementFactory>());`
- **C#**: `OnItemTemplateChanged(args.OldValue, args.NewValue);` — receives `object` instead of `IElementFactory`.
- **Issue**: The Uno `OnItemTemplateChanged` signature is `(object oldValue, object newValue)`, so the cast is delayed to inside. Functionally equivalent because the method internally pattern-matches. WinUI uses the IElementFactory contract directly. Mark as a divergence in method signature but not a bug.
- **Suggested fix**: For strict 1:1 parity, change the signature to accept `IElementFactoryShim` and use `args.OldValue as IElementFactoryShim` / `args.NewValue as IElementFactoryShim` at call site. Low priority — current behavior is identical for the supported inputs.

#### H4. `OnLoaded` adds Uno-specific conditional compile around the WinUI counter check

- **Location**: ItemsRepeater.mux.cs lines 507-529 vs ItemsRepeater.cpp lines 495-507
- **C++**:
  ```cpp
  if (m_loadedCounter > m_unloadedCounter)
  {
      InvalidateMeasure();
      m_viewportManager->ResetScrollers();
      m_viewportManager->ResetLayoutRealizationWindowCacheBuffer();
  }
  ++m_loadedCounter;
  ```
- **C#**:
  ```csharp
  #if !UNO_HAS_ENHANCED_LIFECYCLE
      // ...always force a layouting pass...
  #else
      if (m_loadedCounter > m_unloadedCounter)
  #endif
      {
          InvalidateMeasure();
          m_viewportManager.ResetScrollers();
          m_viewportManager.ResetLayoutRealizationWindowCacheBuffer();
      }

      ++m_loadedCounter;

  #if HAS_UNO
      OnLoadedUno();
  #endif
  ```
- **Issue**: Acceptable Uno-specific divergence. The `#if !UNO_HAS_ENHANCED_LIFECYCLE` branch is justified by lifecycle differences across Uno targets. The `OnLoadedUno()` call adds re-subscription logic that's a Uno necessity. Both are wrapped in compile directives. Document the same in `OnUnloaded`.
- **Suggested fix**: Add `// TODO Uno:` markers explaining why the conditional differs from WinUI, and keep the comment paraphrase that's in the Uno code today.

#### H5. `OnDataSourcePropertyChanged` uses Uno's SerialDisposable revoker pattern in-line in `.mux.cs`

- **Location**: ItemsRepeater.mux.cs lines 561-629 vs ItemsRepeater.cpp lines 531-601
- **Issue**: The `#if HAS_UNO` block on lines 570-577 and 584-589 reaches into `_dataSourceSubscriptionsRevoker` which lives in `.uno.cs`. That cross-file dependence is acceptable but the rule book says revoker handling should live in `.uno.cs` where possible. Here the inline directive is fine.
- **Suggested fix**: None — Uno comments explain the rationale. Acceptable.

#### H6. `OnLayoutChanged` Uno revoker pattern divergence from WinUI

- **Location**: ItemsRepeater.mux.cs lines 720-791 vs ItemsRepeater.cpp lines 697-755
- **C++**:
  ```cpp
  oldValue.UninitializeForContext(GetLayoutContext());
  m_measureInvalidated.revoke();
  m_arrangeInvalidated.revoke();
  ```
- **C#**:
  ```csharp
  oldValue.UninitializeForContext(GetLayoutContext());
  #if HAS_UNO
      _layoutSubscriptionsRevoker.Disposable = null;
  #endif
  ```
- **Issue**: WinUI uses two distinct revokers (`m_measureInvalidated`/`m_arrangeInvalidated`) wrapped via `winrt::auto_revoke`. The port replaces them with a single `_layoutSubscriptionsRevoker` (`SerialDisposable`) holding a `CompositeDisposable`. The `#if HAS_UNO` reset of `_layoutSubscriptionsRevoker` happens BEFORE re-subscribing in the new-value branch. WinUI's pattern is per-event revoker; consolidating is acceptable per rule 7 (use `SerialDisposable`). However, the WinUI declarations of `m_measureInvalidated`/`m_arrangeInvalidated` are NOT present in `ItemsRepeater.h.mux.cs` as commented-out fields. Per rule 1 (lossless port), those declarations should appear (commented out under `#if !HAS_UNO` / `// TODO Uno:`) so reviewers can see what was replaced.
- **Suggested fix**: Add commented-out `// TODO Uno: winrt::Layout::MeasureInvalidated_revoker m_measureInvalidated{};` and `// TODO Uno: winrt::Layout::ArrangeInvalidated_revoker m_arrangeInvalidated{};` placeholders in `ItemsRepeater.h.mux.cs` to document the replacement.

### Medium (missing comments, missing `#pragma region`, Uno-specific not wrapped, missing XML doc)

#### M1. Missing `#pragma region RepeaterTestHooks-related fields` in `.h.mux.cs`

- **Location**: ItemsRepeater.h.mux.cs lines 104-106 vs ItemsRepeater.h lines 209-213
- **C++**:
  ```cpp
  #pragma region RepeaterTestHooks-related fields
  #ifdef DBG
      static int s_logItemIndexDbg;
  #endif // DBG
  #pragma endregion
  ```
- **C#**:
  ```csharp
  #if DEBUG
      static int s_logItemIndexDbg = -1;
  #endif
  ```
- **Issue**: The `// #pragma region RepeaterTestHooks-related fields` and matching `// #pragma endregion` are missing.
- **Suggested fix**: Wrap the field with `// #pragma region` / `// #pragma endregion` lines.

#### M2. Missing `#pragma region RepeaterTestHooks methods` formatting

- **Location**: ItemsRepeater.h.mux.cs (after Indent) vs ItemsRepeater.h lines 124-127
- **C++**:
  ```cpp
  int Indent();

  #pragma region RepeaterTestHooks methods
      static int GetLogItemIndex();
      static void SetLogItemIndex(int logItemIndex);
  #pragma endregion
  ```
- **Issue**: In `.h.mux.cs`, `Indent` does NOT appear (it's defined inline in `.mux.cs`). The test hook methods `GetLogItemIndex`/`SetLogItemIndex` are not declared in `.h.mux.cs` either — they only live in `.mux.cs`. This is acceptable because C# does not separate declaration from implementation. However, the `// #pragma region RepeaterTestHooks methods` markers around the implementations in `.mux.cs` ARE present (lines 957 and 976). Good.

#### M3. Constructor: `RuntimeProfiler` call comment

- **Location**: ItemsRepeater.mux.cs line 26 vs ItemsRepeater.cpp line 29
- **C++**: `__RP_Marker_ClassById(RuntimeProfiler::ProfId_ItemsRepeater);`
- **C#**: `//__RP_Marker_ClassById(RuntimeProfiler.ProfId_ItemsRepeater);`
- **Issue**: Acceptable — RuntimeProfiler is a WinUI-only profiling stub. Per rules, commenting out is correct. Missing `// TODO Uno:` to clarify why.
- **Suggested fix**: Prefix with `// TODO Uno: RuntimeProfiler hook not ported.`

#### M4. Constructor: `m_transitionManager` and `m_viewManager` Uno-init vs WinUI inline-init

- **Location**: ItemsRepeater.mux.cs lines 31-32, ItemsRepeater.h.mux.cs lines 53-54 vs ItemsRepeater.h lines 153-154
- **C++**:
  ```cpp
  ::TransitionManager m_transitionManager{ this };
  ::ViewManager m_viewManager{ this };
  ```
- **C#**: Declared without initializer in `.h.mux.cs` (`TransitionManager m_transitionManager;` and `ViewManager m_viewManager;`), then constructed in the body constructor.
- **Issue**: Allowed — C# cannot use C++-style member initialization in declarations with the owner reference. Both forms result in identical lifetime. No fix needed.

#### M5. Constructor: missing comment about `SharedHelpers::IsRS5OrHigher()` rationale

- **Location**: ItemsRepeater.mux.cs lines 33-40
- **C++**: WinUI 1.8 has no RS5 fallback — it's just `m_viewportManager = std::make_shared<ViewportManagerWithPlatformFeatures>(this);` on line 31.
- **C#**:
  ```csharp
  //if (SharedHelpers.IsRS5OrHigher())
  {
      m_viewportManager = new ViewportManagerWithPlatformFeatures(this);
  }
  //else
  //{
  //	m_viewportManager = std.new ViewportManagerDownLevel(this);
  //}
  ```
- **Issue**: Leftover dead branches from an earlier WinUI version that had a `ViewportManagerDownLevel`. The current commit `4b206bce3` no longer has this fork. The commented-out code is **stale** vs the target commit.
- **Suggested fix**: Remove the commented-out `//if` and `//else` branches to match commit `4b206bce3` exactly. Replace with a single line.

#### M6. `OnPropertyChanged` callback registration

- **Location**: ItemsRepeater.Properties.cs lines 9-10
- **C++ (idl)**: `[MUX_PROPERTY_CHANGED_CALLBACK_METHODNAME("OnPropertyChanged")]`
- **C# (Uno)**:
  ```csharp
  private static void OnPropertyChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
      => ((ItemsRepeater)snd).OnPropertyChanged(args);
  ```
- **Issue**: Acceptable port pattern. Good.

#### M7. `s_maxStackLayoutIterations` type narrowed but stays `uint`

- **Location**: ItemsRepeater.h.mux.cs line 18 vs ItemsRepeater.h line 29
- **C++**: `static constexpr uint8_t s_maxStackLayoutIterations = 60u;`
- **C#**: `private const uint s_maxStackLayoutIterations = 60u;`
- **Issue**: Type change from `uint8_t` (byte) to `uint`. The companion field `m_stackLayoutMeasureCounter` is also `uint8_t` in C++ (line 189) but `uint` in Uno (line 84). Functional behavior identical for values 0-60, but the WinUI semantic precision is lost. Comparisons against `s_maxStackLayoutIterations` (60) work the same way for `uint` and `uint8_t`. Low impact but not a 1:1 port.
- **Suggested fix**: Use `byte`/`byte` types to mirror `uint8_t`. Or document why widened. Note: `++m_stackLayoutMeasureCounter` semantics differ on overflow — `uint8_t` wraps at 256, `uint` at 2^32 — but the comparison short-circuits at 60 so this is theoretical.

#### M8. `m_layoutOrigin` initialization

- **Location**: ItemsRepeater.h.mux.cs line 69 vs ItemsRepeater.h line 169
- **C++**: `winrt::Point m_layoutOrigin{};`
- **C#**: `Point m_layoutOrigin;` (default value initialized to default(Point), which equals winrt::Point{}).
- **Issue**: Same default, no fix.

#### M9. `OnPropertyChanged` (the public static callback) — XML docs

- **Location**: ItemsRepeater.Properties.cs
- **Issue**: Most DPs do not have XML docs except `ItemTransitionProvider`. Per project conventions (rule 9), public DPs should be documented. Same issue for `Background` (commented out) and the others. Not a strict port discrepancy (WinUI uses IDL comments), but the project rule asks for XML doc on public/protected members.
- **Suggested fix**: Add XML doc summaries to each public DP and property.

### Low (style / expression-body / redundant wrappers)

#### L1. `MeasureOverride` shortcut – constant `localDesiredSize` not marked `const`

- **Location**: ItemsRepeater.mux.cs lines 103-104 vs ItemsRepeater.cpp lines 96-97
- **Issue**: C++ uses `const`. C# uses `var`. Style only.

#### L2. `Indent` always returns 4 in non-DEBUG builds — same as WinUI

- **Location**: ItemsRepeater.mux.cs lines 488-505 vs ItemsRepeater.cpp lines 475-493
- **Issue**: Matches — no fix.

#### L3. `GetDefaultLayout` thread-local

- **Location**: ItemsRepeater.mux.cs lines 945-955 vs ItemsRepeater.cpp lines 910-917
- **C++**: `static thread_local winrt::Layout defaultLayout = winrt::StackLayout();`
- **C#**: `s_defaultLayout` is `[ThreadStatic] private static Layout`. Constructed lazily on first call via `??=`. C++ constructs eagerly at first thread-call. Functionally equivalent; minor difference. Comment in Uno was updated from "thread_local" → "ThreadStatic" — accurate.

#### L4. `MeasureOverride` REPEATER_TRACE_INFO formatting

- **Location**: ItemsRepeater.mux.cs lines 100, 156 vs ItemsRepeater.cpp lines 93, 148
- **Issue**: C++ uses `ITEMSREPEATER_TRACE_INFO_DBG(*this, TRACE_MSG_METH_STR_INT, ...)`. Uno calls `REPEATER_TRACE_INFO("MeasureOverride shortcut - %d\n", ...)`. The trace macro name differs and the format string is different (Uno joins the string fragments). Minor — trace fidelity loss.

#### L5. `// #pragma region` comments use `// #pragma region` consistently

- **Location**: Various
- **Issue**: Mostly consistent. Good.

#### L6. `private` C# default for fields without modifier in `.h.mux.cs`

- **Location**: ItemsRepeater.h.mux.cs lines 53-102
- **Issue**: Fields are declared without access modifier and default to `private`. WinUI declarations are private (after `private:` at line 129). Matches by default.

### Informational (intentional Uno-specific code clearly justified)

#### I1. `ItemsRepeater.Templates.cs` — Uno dynamic template update facility

- **Location**: ItemsRepeater.Templates.cs (entire file)
- **Issue**: Properly documented Uno extension. Acceptable — wrapping inside `partial class` with detailed header comment explains rationale. Good.

#### I2. `ItemsRepeater.uno.cs` — IPanel/Children + SerialDisposable revokers

- **Location**: ItemsRepeater.uno.cs (entire file)
- **Issue**: Properly factored to a `.uno.cs` file with explanation of why it exists. Good.

#### I3. `ItemsRepeater.UIKit.cs` — iOS-only AddSubview/InsertSubview filter

- **Location**: ItemsRepeater.UIKit.cs (entire file)
- **Issue**: iOS-specific override to ignore non-UIElement subviews added by the text-selection system. Acceptable.

#### I4. `OnDataSourcePropertyChanged` — pre-clear revoker for old data source

- **Location**: ItemsRepeater.mux.cs lines 570-577
- **Issue**: Justified by replacement of `auto_revoke` pattern. `#if HAS_UNO` wrapping is correct.

#### I5. `OnLayoutChanged` — `_layoutSubscriptionsRevoker` reset before subscribe

- **Location**: ItemsRepeater.mux.cs lines 768-786
- **Issue**: Acceptable replacement of WinUI's `auto_revoke` pattern with explicit Uno SerialDisposable + CompositeDisposable.

#### I6. `OnLoaded` / `OnUnloaded` — `OnLoadedUno`/`OnUnloadedUno` calls

- **Location**: ItemsRepeater.mux.cs lines 526-528, 548-550
- **Issue**: Re-subscription logic for native targets. Correctly guarded by `#if HAS_UNO`.

#### I7. `ItemsRepeater.cs` — `[ContentProperty]` attribute

- **Location**: ItemsRepeater.cs line 13
- **Issue**: Matches IDL `[contentproperty("ItemTemplate")]`. Good.

#### I8. `ItemsRepeater.Properties.cs` — `Background` DP commented out

- **Location**: ItemsRepeater.Properties.cs lines 12-21
- **Issue**: WinUI exposes `Background` and `BackgroundProperty` on `ItemsRepeater` despite `FrameworkElement` not having them. The Uno port comments out the duplicate because Uno's `FrameworkElement` already has `Background` (via `Panel` ancestry or generic surface). Confirm against the WinUI IDL: the IDL declares `Background` and `BackgroundProperty` on `ItemsRepeater`. Uno's commented-out declaration with a `// Commented out as FwElt already has it ...` note is a documented Uno-specific decision. **Could be a public API gap**: if FrameworkElement does NOT have `Background` on Uno's `FrameworkElement` (only on `Panel`/`Control`), then `ItemsRepeater.Background` would be missing entirely. ItemsRepeater derives from FrameworkElement (per `.cs`), not Panel. Verify: does Uno's `FrameworkElement` actually have a `Background` property?
- **Suggested verification**: Check `FrameworkElement` for `Background` in Uno; if absent, restore the DP.

## Method order verification

Order side-by-side. ✅ = matching order, → = note.

| WinUI .cpp order | Uno .mux.cs order |
|---|---|
| `ItemsRepeater::ItemsRepeater()` (ctor) | `ItemsRepeater()` ✅ |
| `OnCreateAutomationPeer` | `OnCreateAutomationPeer` ✅ |
| `GetChildrenInTabFocusOrder` | `GetChildrenInTabFocusOrder` ✅ |
| `OnBringIntoViewRequested` | `OnBringIntoViewRequested` ✅ |
| `MeasureOverride` | `MeasureOverride` ✅ |
| `ArrangeOverride` | `ArrangeOverride` ✅ |
| `ItemsSourceView` (property accessor) | `ItemsSourceView` (property) ✅ |
| `GetElementIndex` | `GetElementIndex` ✅ |
| `TryGetElement` | `TryGetElement` ✅ |
| `PinElement` | `PinElement` ✅ |
| `UnpinElement` | `UnpinElement` ✅ |
| `GetOrCreateElement` | `GetOrCreateElement` ✅ |
| `GetElementImpl` | `GetElementImpl` ✅ |
| `ClearElementImpl` | `ClearElementImpl` ✅ |
| `GetElementIndexImpl` | `GetElementIndexImpl` ✅ |
| `GetElementFromIndexImpl` | `GetElementFromIndexImpl` ✅ |
| `GetOrCreateElementImpl` | `GetOrCreateElementImpl` ✅ |
| `TryGetVirtualizationInfo` | `TryGetVirtualizationInfo` ✅ |
| `GetVirtualizationInfo` | `GetVirtualizationInfo` ✅ |
| `CreateAndInitializeVirtualizationInfo` | `CreateAndInitializeVirtualizationInfo` ✅ |
| `OnPropertyChanged` | `OnPropertyChanged` ✅ |
| `OnElementPrepared` | `OnElementPrepared` ✅ |
| `OnElementClearing` | `OnElementClearing` ✅ |
| `OnElementIndexChanged` | `OnElementIndexChanged` ✅ |
| `Indent` | `Indent` ✅ |
| `OnLoaded` | `OnLoaded` ✅ |
| `OnUnloaded` | `OnUnloaded` ✅ |
| `OnLayoutUpdated` | `OnLayoutUpdated` ✅ |
| `OnDataSourcePropertyChanged` | `OnDataSourcePropertyChanged` ✅ |
| `OnItemTemplateChanged` | `OnItemTemplateChanged` ✅ |
| `OnLayoutChanged` | `OnLayoutChanged` ✅ |
| `OnTransitionProviderChanged` | `OnTransitionProviderChanged` ✅ |
| `OnItemsSourceViewChanged` | `OnItemsSourceViewChanged` ✅ |
| `InvalidateMeasureForLayout` | `InvalidateMeasureForLayout` ✅ |
| `InvalidateArrangeForLayout` | `InvalidateArrangeForLayout` ✅ |
| `InvalidateChildrenMeasure` | `InvalidateChildrenMeasure` ✅ |
| `EnsureDefaultLayoutState` | `EnsureDefaultLayoutState` ✅ |
| `GetLayoutContext` | `GetLayoutContext` ✅ |
| `CreateChildrenInTabFocusOrderIterable` | `CreateChildrenInTabFocusOrderIterable` ✅ |
| `GetEffectiveLayout` | `GetEffectiveLayout` ✅ |
| `GetDefaultLayout` | `GetDefaultLayout` ✅ |
| `GetLogItemIndex` | `GetLogItemIndex` ✅ |
| `SetLogItemIndex` | `SetLogItemIndex` ✅ |

**Result**: Method order is fully preserved. ✅

## Field/constant verification

| WinUI .h field/constant | Uno .h.mux.cs field/constant | Status |
|---|---|---|
| `static constexpr uint8_t s_maxStackLayoutIterations = 60u;` | `private const uint s_maxStackLayoutIterations = 60u;` | Type widened (uint8_t→uint). See M7. |
| `static winrt::Point ClearedElementsArrangePosition;` (def in .cpp) | `internal static Point ClearedElementsArrangePosition = ...;` | OK (inline init) |
| `static winrt::Rect InvalidRect;` (def in .cpp) | `internal static Rect InvalidRect = ...;` | OK |
| `using ItemsRepeaterProperties::Background;` | (missing — see I8) | See I8 |
| `winrt::IElementFactory ItemTemplateShim()` | `internal IElementFactoryShim ItemTemplateShim => m_itemTemplateWrapper;` | OK |
| `::TransitionManager m_transitionManager{ this };` | `TransitionManager m_transitionManager;` | OK – initialized in ctor |
| `::ViewManager m_viewManager{ this };` | `ViewManager m_viewManager;` | OK – initialized in ctor |
| `std::shared_ptr<::ViewportManager> m_viewportManager{ nullptr };` | `ViewportManager m_viewportManager;` | OK |
| `tracker_ref<winrt::ItemsSourceView> m_itemsSourceView{ this };` | `ItemsSourceView m_itemsSourceView;` | OK |
| `winrt::Microsoft::UI::Xaml::IElementFactory m_itemTemplateWrapper{ nullptr };` | `IElementFactoryShim m_itemTemplateWrapper;` | OK |
| `tracker_ref<winrt::VirtualizingLayoutContext> m_layoutContext{ this };` | `VirtualizingLayoutContext m_layoutContext;` | OK |
| `tracker_ref<winrt::IInspectable> m_layoutState{ this };` | `object m_layoutState;` | OK |
| `tracker_ref<winrt::NotifyCollectionChangedEventArgs> m_processingItemsSourceChange{ this };` | `NotifyCollectionChangedEventArgs m_processingItemsSourceChange;` | OK |
| `bool m_isLayoutInProgress{ false };` | `bool m_isLayoutInProgress;` | OK |
| `winrt::Point m_layoutOrigin{};` | `Point m_layoutOrigin;` | OK |
| `winrt::ItemsSourceView::CollectionChanged_revoker m_itemsSourceViewChanged{};` | (missing — replaced by `_dataSourceSubscriptionsRevoker` in `.uno.cs`) | See H6 |
| `winrt::Layout::MeasureInvalidated_revoker m_measureInvalidated{};` | (missing — replaced by `_layoutSubscriptionsRevoker` in `.uno.cs`) | See H6 |
| `winrt::Layout::ArrangeInvalidated_revoker m_arrangeInvalidated{};` | (missing — replaced by `_layoutSubscriptionsRevoker` in `.uno.cs`) | See H6 |
| `tracker_ref<...> m_elementPreparedArgs{ this };` | `ItemsRepeaterElementPreparedEventArgs m_elementPreparedArgs;` | OK |
| `tracker_ref<...> m_elementClearingArgs{ this };` | `ItemsRepeaterElementClearingEventArgs m_elementClearingArgs;` | OK |
| `tracker_ref<...> m_elementIndexChangedArgs{ this };` | `ItemsRepeaterElementIndexChangedEventArgs m_elementIndexChangedArgs;` | OK |
| `int m_loadedCounter{};` | `int m_loadedCounter;` | OK |
| `int m_unloadedCounter{};` | `int m_unloadedCounter;` | OK |
| `uint8_t m_stackLayoutMeasureCounter{ 0u };` | `uint m_stackLayoutMeasureCounter;` | Type widened (uint8_t→uint). See M7. |
| `bool m_ownsTransitionProvider{ true };` | `bool m_ownsTransitionProvider = true;` | OK |
| `bool m_isItemTemplateEmpty{ false };` | `bool m_isItemTemplateEmpty;` | OK |
| `bool m_wasLayoutChangedCalled{ false };` | `bool m_wasLayoutChangedCalled;` | OK |
| `double m_layoutRoundFactor{};` | `double m_layoutRoundFactor;` | OK |
| `#ifdef DBG static int s_logItemIndexDbg;` (def -1 in .cpp) | `#if DEBUG static int s_logItemIndexDbg = -1;` | OK |

The .h.mux.cs has accessors (`ItemTemplateShim`, `ViewManager`, `TransitionManager`, `LayoutState`, `VisibleWindow`, `RealizationWindow`, `SuggestedAnchor`, `MadeAnchor`, `LayoutOrigin`, `IsProcessingCollectionChange`) matching the inline accessors in `.h`. ✅

Events `ElementPrepared`/`ElementClearing`/`ElementIndexChanged` are declared as public events in `.h.mux.cs`. ✅ Matches IDL.

## Public API surface (`.Properties.cs` vs `.idl`)

| IDL member | Uno presence | Signature match |
|---|---|---|
| `ItemsSource { get; set; }` (Object) | `ItemsRepeater.Properties.cs` line 27 | ✅ |
| `ItemsSourceView { get; }` | `ItemsRepeater.mux.cs` line 239 | ✅ |
| `ItemTemplate { get; set; }` (Object) | `ItemsRepeater.Properties.cs` line 38 | ✅ |
| `Layout { get; set; }` | `ItemsRepeater.Properties.cs` lines 55-62 | ✅ (uses `new` for Android) |
| `ItemTransitionProvider { get; set; }` | `ItemsRepeater.Properties.cs` line 75 | ✅ + XML docs ✅ |
| `HorizontalCacheLength { get; set; }` (default 2.0) | `ItemsRepeater.Properties.cs` line 86 | ✅ |
| `VerticalCacheLength { get; set; }` (default 2.0) | `ItemsRepeater.Properties.cs` line 97 | ✅ |
| `Background { get; set; }` | **Commented out** | ⚠ See I8 — verify Uno's FrameworkElement has Background. If not, **missing public API**. |
| `GetElementIndex(UIElement)` | `ItemsRepeater.mux.cs` line 241 | ✅ |
| `TryGetElement(Int32)` | `ItemsRepeater.mux.cs` line 246 | ✅ |
| `GetOrCreateElement(Int32)` | `ItemsRepeater.mux.cs` line 262 | ✅ |
| `event ElementPrepared` | `ItemsRepeater.h.mux.cs` line 108 | ✅ |
| `event ElementClearing` | `ItemsRepeater.h.mux.cs` line 110 | ✅ |
| `event ElementIndexChanged` | `ItemsRepeater.h.mux.cs` line 109 | ✅ |
| `ItemsSourceProperty` | `ItemsRepeater.Properties.cs` line 24 | ✅ |
| `ItemTemplateProperty` | `ItemsRepeater.Properties.cs` line 35 | ✅ |
| `LayoutProperty` | `ItemsRepeater.Properties.cs` line 46 | ✅ |
| `ItemTransitionProviderProperty` | `ItemsRepeater.Properties.cs` line 69 | ✅ |
| `HorizontalCacheLengthProperty` | `ItemsRepeater.Properties.cs` line 83 | ✅ |
| `VerticalCacheLengthProperty` | `ItemsRepeater.Properties.cs` line 94 | ✅ |
| `BackgroundProperty` | **Commented out** | ⚠ See I8 |

**Note on DP-name string literals**: WinUI registers DP names via `nameof()` style; Uno uses raw string literals (`"ItemsSource"`, `"ItemTemplate"`, etc.) except for `ItemTransitionProvider` (which uses `nameof(ItemTransitionProvider)`). Use `nameof(...)` for consistency.

## Conclusion

**Total findings by severity**: 2 Critical, 5 High (3 confirmed not actual issues), 9 Medium, 6 Low, 8 Informational.

**Top 5 priority issues**:

1. **C1**: Re-order `OnItemTemplateChanged` element-factory branches to match WinUI (DataTemplate → DataTemplateSelector → IElementFactoryShim → throw) and restore `wrapper->EnableTracking(this)` semantics (or stub it with TODO).
2. **I8 / Background DP**: Verify Uno's `FrameworkElement` actually exposes `Background`. If not, restore the `BackgroundProperty`/`Background` declarations to keep the IDL public surface intact.
3. **M5**: Remove the stale `if (SharedHelpers.IsRS5OrHigher())` / `else ViewportManagerDownLevel(...)` commented-out branches from the constructor — commit `4b206bce3` no longer has this fork.
4. **H6**: Add commented-out placeholders for the removed `m_measureInvalidated`/`m_arrangeInvalidated`/`m_itemsSourceViewChanged` revokers in `.h.mux.cs` so reviewers can see the WinUI declarations they replaced.
5. **M7**: Either narrow `s_maxStackLayoutIterations` and `m_stackLayoutMeasureCounter` to `byte` to mirror WinUI's `uint8_t`, or document why widened. Also use `nameof(...)` consistently in DP registrations for type-safety and refactor-friendliness.
