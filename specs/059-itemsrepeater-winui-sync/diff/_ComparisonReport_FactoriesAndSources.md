# Factories + Data Sources Comparison Report

**WinUI commit:** 4b206bce3

**Types covered:**
- `ElementFactory`
- `ElementFactoryGetArgs` (Uno) / `ElementFactoryGetArgsDownlevel` (WinUI)
- `ElementFactoryRecycleArgs` (Uno) / `ElementFactoryRecycleArgsDownlevel` (WinUI)
- `IElementFactoryShim` (Uno-only)
- `ElementRealizationOptions` (Uno-only enum; not in C++ sources audited here)
- `RecyclePool`
- `RecyclingElementFactory`
- `ItemTemplateWrapper`
- `SelectTemplateEventArgs`
- `ItemsSourceView`
- `InspectingDataSource`
- `ListAdapter` (Uno-only helper)

---

## Summary

| Type | High | Med | Low |
|------|-----:|----:|----:|
| ElementFactory | 1 | 3 | 2 |
| ElementFactoryGetArgs | 2 | 2 | 2 |
| ElementFactoryRecycleArgs | 2 | 1 | 2 |
| IElementFactoryShim | 0 | 1 | 1 |
| ElementRealizationOptions | 0 | 0 | 1 |
| RecyclePool | 2 | 5 | 4 |
| RecyclingElementFactory | 1 | 4 | 3 |
| ItemTemplateWrapper | 2 | 3 | 4 |
| SelectTemplateEventArgs | 1 | 2 | 2 |
| ItemsSourceView | 2 | 4 | 3 |
| InspectingDataSource | 3 | 3 | 2 |
| ListAdapter | 0 | 0 | 1 |

Severity legend:
- **High** — visibility/API surface deviations, missing behavior, finalizers, missing TODO Uno markers for divergent code paths, or behavioral differences.
- **Medium** — file layout / split deviations, ordering differences, ported-only-as-monolithic file, comments dropped / paraphrased.
- **Low** — pragma region not preserved, header naming, formatting only.

---

## File layout audit

| WinUI source | Expected Uno split (per rule #2) | Actual Uno files | Status |
|---|---|---|---|
| `ElementFactory.cpp` / `.h` | `ElementFactory.cs` (decl), `ElementFactory.h.mux.cs`, `ElementFactory.mux.cs` | `ElementFactory.cs` only (monolithic) | **Layout violation** — split missing. Also missing MUX Reference header. |
| `ElementFactoryGetArgsDownlevel.cpp` / `.h` | `ElementFactoryGetArgs.cs` (decl), `ElementFactoryGetArgs.h.mux.cs`, `ElementFactoryGetArgs.mux.cs` | `ElementFactoryGetArgs.cs` only (monolithic, properties only) | **Layout violation** — no split, no MUX Reference header. |
| `ElementFactoryRecycleArgsDownlevel.cpp` / `.h` | `ElementFactoryRecycleArgs.cs` (decl), `ElementFactoryRecycleArgs.h.mux.cs`, `ElementFactoryRecycleArgs.mux.cs` | `ElementFactoryRecycleArgs.cs` only (monolithic) | **Layout violation** — no split, no MUX Reference header. |
| `RecyclePool.cpp` / `.h` / `.idl` / `.properties.cpp` | `RecyclePool.cs` (decl), `RecyclePool.h.mux.cs`, `RecyclePool.mux.cs`, `RecyclePool.Properties.cs` | All four present | OK |
| `RecyclingElementFactory.cpp` / `.h` / `.idl` | `RecyclingElementFactory.cs` (decl), `RecyclingElementFactory.h.mux.cs`, `RecyclingElementFactory.mux.cs`, `RecyclingElementFactory.Properties.cs` | All four present | OK |
| `ItemTemplateWrapper.cpp` / `.h` | `ItemTemplateWrapper.cs` (decl), `ItemTemplateWrapper.h.mux.cs`, `ItemTemplateWrapper.mux.cs` | `ItemTemplateWrapper.cs` only (monolithic) | **Layout violation**; no MUX Reference header. |
| `SelectTemplateEventArgs.cpp` / `.h` | `SelectTemplateEventArgs.cs` (decl), `SelectTemplateEventArgs.h.mux.cs`, `SelectTemplateEventArgs.mux.cs` | `SelectTemplateEventArgs.cs` only | **Layout violation**; no MUX Reference header. |
| `ItemsSourceView.cpp` / `.h` | `ItemsSourceView.cs` (decl), `ItemsSourceView.h.mux.cs`, `ItemsSourceView.mux.cs` | `ItemsSourceView.cs` + Uno-specific `ItemsSourceView.Impl.cs` | **Layout violation** — `.cpp/.h` not split; instead a custom `Impl.cs` split. |
| `InspectingDataSource.cpp` / `.h` | `InspectingDataSource.cs` (decl), `InspectingDataSource.h.mux.cs`, `InspectingDataSource.mux.cs` | `InspectingDataSource.cs` (tiny shim only). All logic moved to `ItemsSourceView.Impl.cs`. | **Layout violation** — type collapsed: ctor + override impls relocated. |
| `ItemsSourceViewFactory.cpp` / `.h` | No 1:1 equivalent expected (factory pattern) | Encoded as `ItemsSourceView(object source)` overload in `ItemsSourceView.Impl.cs` calling base | OK (factory translated to C# ctor delegation, documented in header comment) |
| (no C++ counterpart) | — | `IElementFactoryShim.cs` | **Uno-specific** — must be `#if HAS_UNO` + `TODO Uno:` (rule #6). Neither present. |
| (no C++ counterpart) | — | `ElementRealizationOptions.cs` | **Uno-specific** — must be `#if HAS_UNO` + `TODO Uno:` (rule #6). Neither present. WinUI defines this enum in `ItemsRepeater.idl`, but C# file lacks reference header. |
| (no C++ counterpart) | — | `ListAdapter.cs` | **Uno-specific** internal helper — must be `#if HAS_UNO` + `TODO Uno:` (rule #6). Neither present. |

---

## Per-type sections

### ElementFactory (`ElementFactory.cpp` / `.h` -> `ElementFactory.cs`)

#### File layout
Monolithic `ElementFactory.cs` instead of the three-file split (`Foo.cs`, `Foo.h.mux.cs`, `Foo.mux.cs`).

#### Method order verification
| WinUI .cpp order | Uno .cs order | Match |
|---|---|---|
| `GetElement` | `GetElement` | yes |
| `RecycleElement` | `RecycleElement` | yes |
| `GetElementCore` | `GetElementCore` | yes |
| `RecycleElementCore` | `RecycleElementCore` | yes |

#### Findings

- **High — Missing MUX Reference header** (rule #4). The file has only the standard MIT header.
  - Fix: add `// MUX Reference ElementFactory.cpp, commit 4b206bce3` (and split into the three-file layout).

- **Medium — File layout** (rule #2). `ElementFactory.cs` must contain only the type declaration; `.h.mux.cs` should hold field/constant/nested type declarations from the .h; `.mux.cs` should hold method bodies from the .cpp in the same order.

- **Medium — `#pragma region` lost.** C++ has `IElementFactory` and `IElementFactoryOverrides` regions; the C# file uses `#region` (good) but does not include the trailing `// #pragma region` comment marker that other files in the repo use to identify the C++ origin.

- **Medium — Comment dropped from `.h`.** The C++ header has the comment:
  `// TODO: Bug 14901501: Figure out a better way to have reference tracking for types doing in-component derivation (e.g. RecyclingElementFactory : ElementFactory)` and three `NonDelegating*` virtuals are exposed. The C# port omits this comment entirely. While `NonDelegating*` is a WinRT-only construct that has no C# counterpart, the explanatory note should be preserved.
  - Fix: include the comment as part of a `#if !HAS_UNO` block with `// TODO Uno:` or in an Uno-specific note explaining why the methods are not ported.

- **Low — `overridable()` not commented.**
  C++ `GetElement` body is `return overridable().GetElementCore(args);` — the WinRT `overridable()` dispatch is not directly representable in C#. The Uno port uses direct virtual dispatch (`=> GetElementCore(args);`). This is correct C# semantics but the divergence from the WinRT dispatch model should at minimum be noted (rule #6: comment ported decisions explicitly).

- **Low — `using System.Linq;` unused.** Carry-over import; cosmetic only.

---

### ElementFactoryGetArgs (`ElementFactoryGetArgsDownlevel.cpp` / `.h` -> `ElementFactoryGetArgs.cs`)

#### File layout
Monolithic property-only file. No method bodies, so a `.mux.cs` is at minimum trivial — but a `.h.mux.cs` should still capture the field declarations.

#### Field verification
| WinUI .h field | Uno field | Match |
|---|---|---|
| `tracker_ref<winrt::IInspectable> m_data` | `object Data` (auto-prop) | Acceptable conversion (`tracker_ref<T> → T`, prop replaces field+getter+setter). |
| `tracker_ref<winrt::UIElement> m_parent` | `UIElement Parent` (auto-prop) | OK |
| `int m_index{}` | `int Index` (auto-prop) | OK |

#### Findings

- **High — Visibility widening risk.** WinUI `Index` is a public method pair (`int Index()` / `void Index(int)`) on the down-level class. In Uno, `Index` is **internal** (rule #5 — widen only with IDL/Generated evidence; here we have evidence the property is part of the WinRT API). Either:
  - mark `Index` public to match WinUI, OR
  - confirm via `.idl` / Generated that `ElementFactoryGetArgs` does **not** publicly expose `Index`. The C++ `Index()` method sits **outside** `#pragma region IElementFactoryGetArgs`, suggesting it is intentionally not part of the public interface. If that is the case, the current `internal` is correct, but should carry a comment citing the IDL.

- **High — Missing MUX Reference header** (rule #4).

- **Medium — File layout violation.** Should be split: `ElementFactoryGetArgs.cs` (decl only) + `ElementFactoryGetArgs.h.mux.cs` (fields) + `ElementFactoryGetArgs.mux.cs` (methods).

- **Medium — Missing `#pragma region IElementFactoryGetArgs` markers** (rule #2 implies preservation of `.cpp` structure).

- **Low — No XML doc on public members** (rule #9). `Parent`, `Data` are public, no XML doc.

- **Low — `using` for `System` / `System.Linq` unused.** (File only imports `Microsoft.UI.Xaml`, but `Parent` / `Data` are still on public surface and the namespace is reasonable.)

---

### ElementFactoryRecycleArgs (`ElementFactoryRecycleArgsDownlevel.cpp` / `.h` -> `ElementFactoryRecycleArgs.cs`)

#### Field verification
| WinUI .h | Uno | Match |
|---|---|---|
| `tracker_ref<winrt::UIElement> m_element` | `UIElement Element` (auto-prop) | OK |
| `tracker_ref<winrt::UIElement> m_parent` | `UIElement Parent` (auto-prop) | OK |

#### Findings

- **High — Missing MUX Reference header** (rule #4).
- **High — Missing XML docs on public properties** (rule #9). Both `Parent` and `Element` are public; need documentation.
- **Medium — File layout violation** (rule #2). Three-file split missing.
- **Low — Missing `#pragma region IElementFactoryRecycleArgs` marker.**
- **Low — No comment explaining the Down-level fakery.** The WinUI `.h` has an extensive comment about the down-level WUX/MUX interface bridging; this is irrelevant for Uno but should be replaced with a one-line note that the type is the plain MUX type in Uno (rule #6).

---

### IElementFactoryShim (Uno-only)

#### Findings

- **Medium — Uno-only type not gated** (rule #6). The file does not use `#if HAS_UNO` and contains no `// TODO Uno:` justification. The shim presumably exists because the WinRT interface `IElementFactory` lives in the platform; an explanatory comment is required.

- **Low — `partial interface` is unusual.** No partial split exists in the codebase. Either drop `partial` or document why.

---

### ElementRealizationOptions (Uno-only file referencing `ItemsRepeater.idl` enum)

#### Findings

- **Low — Missing MUX Reference header / `#if HAS_UNO`.** The enum is sourced from the WinUI IDL `ItemsRepeater.idl`. Header `// MUX Reference ItemsRepeater.idl, commit 4b206bce3` should be added. If the enum exists in Generated, this file may be entirely redundant; verify.

---

### RecyclePool (`RecyclePool.cpp` / `.h` -> `RecyclePool.cs` / `.Properties.cs` / `.h.mux.cs` / `.mux.cs`)

File layout OK (four-file split present).

#### Method order verification (.mux.cs vs .cpp)

| WinUI .cpp order | Uno .mux.cs order | Match |
|---|---|---|
| `PutElement(elem, key)` | `PutElement(elem, key)` | yes |
| `PutElement(elem, key, owner)` | `PutElement(elem, key, owner)` | yes |
| `TryGetElement(key)` | `TryGetElement(key)` | yes |
| `TryGetElement(key, owner)` | `TryGetElement(key, owner)` | yes |
| `PutElementCore(elem, key, owner)` | `PutElementCore(elem, key, owner)` | yes |
| `TryGetElementCore(key, owner)` | `TryGetElementCore(key, owner)` | yes |
| `EnsureOwnerIsPanelOrNull(owner)` | `EnsureOwnerIsPanelOrNull(owner)` | yes |

#### Field / nested type verification (.h vs .h.mux.cs)

| WinUI .h | Uno .h.mux.cs | Match |
|---|---|---|
| `static GlobalDependencyProperty s_PoolInstanceProperty` | `s_PoolInstanceProperty` | yes |
| `static GlobalDependencyProperty s_reuseKeyProperty` | `s_reuseKeyProperty` | yes |
| `static GlobalDependencyProperty s_originTemplateProperty` | `s_originTemplateProperty` | yes |
| `struct ElementInfo` with tracker_ref fields | `struct ElementInfo` with raw refs | acceptable (`tracker_ref<T> → T`) |
| `std::map<hstring, std::vector<ElementInfo>> m_elements` | `Dictionary<string, List<ElementInfo>> m_elements` | OK |

#### Properties (`.idl`-derived vs `.Properties.cs`)

| WinUI member | Uno member | Match |
|---|---|---|
| `ReuseKeyProperty()` (static) | `ReuseKeyProperty` (static, **internal**) | **Visibility mismatch** — see findings. |
| `GetReuseKey(elem)` | `GetReuseKey(elem)` — internal | **Visibility mismatch.** |
| `SetReuseKey(elem, value)` | `SetReuseKey(elem, value)` — internal | **Visibility mismatch.** |
| `PoolInstanceProperty()` | `PoolInstanceProperty` — public | OK |
| `GetPoolInstance(template)` | `GetPoolInstance(template)` — public | OK |
| `SetPoolInstance(template, pool)` | `SetPoolInstance(template, pool)` — public | OK |
| `GetOriginTemplate(elem)` (`/* internal */`) | `GetOriginTemplate(elem)` — internal | OK |
| `SetOriginTemplate(elem, value)` (`/* internal */`) | `SetOriginTemplate(elem, value)` — internal | OK |

#### Findings

- **High — `ReuseKey` visibility mismatch.** In WinUI C++ `RecyclePool.h`, `ReuseKeyProperty()`, `GetReuseKey`, and `SetReuseKey` are inside `#pragma region IRecyclePoolStatics` and are public WinRT API. The Uno port marks them **internal** (rule #5 — narrowing is acceptable only with documented IDL evidence to the contrary).
  - Fix: confirm against `RecyclePool.idl`. If they are part of the public IDL (very likely, given they are inside the `IRecyclePoolStatics` region), they must be `public` in Uno.

- **High — `Clear()` is Uno-only without `// TODO Uno:` justification location.** `RecyclePool.mux.cs` lines 146–153 add an Uno-only `Clear()` method inside `#if HAS_UNO`. The block has a comment explaining its purpose (good), but rule #6 requires the marker to be `// TODO Uno:` rather than `// Uno specific:`. The phrasing should be normalized.

- **Medium — `#pragma region` text not preserved verbatim.** Files use `// #pragma region IRecyclePool` (comment form). Acceptable convention but inconsistent across the codebase.

- **Medium — `ElementInfo` constructor mismatch.** WinUI C++:
  ```cpp
  ElementInfo(const ITrackerHandleManager* refManager, const winrt::UIElement& element, const winrt::Panel& owner)
      :m_element(refManager, element), m_owner(refManager, owner) {}
  ```
  Uno:
  ```csharp
  public ElementInfo(UIElement element, IPanel owner) { Element = element; Owner = owner; }
  ```
  The `refManager` parameter is correctly dropped (no tracker), but the call sites differ: C++ `PutElementCore` passes `this /* refManager */, element, winrtOwnerAsPanel`; Uno passes `(element, winrtOwnerAsPanel)`. OK by conversion rules.

- **Medium — `m_elements` initialization style.** Uno initialises field with `new Dictionary<string, List<ElementInfo>>()` (full type spelled). Per the user style rule (target-typed `new()`), this should be `new()`.

- **Medium — `EnsureProperties` comment block dropped.** C++ doesn't have it; this is Uno-only initialization. The phrase `/* defaultValue */` etc. is preserved — good.

- **Medium — `TryGetElementCore` translation differences:**
  - C++ uses `std::find_if` with predicate; Uno uses a manual `for` loop with `iter = -1` flag pattern. Behaviorally equivalent. **Functionally OK** but loses the `std::find_if` semantics — acceptable.
  - C++: `elements.back()` / `elements.pop_back()` → Uno `elements[elements.Count - 1]` / `RemoveAt(...)`. OK.

- **Low — `ElementInfo` declared as `private struct` vs C++ `struct ElementInfo`.** In C++ it is declared **after** `private:`. Equivalent.

- **Low — `m_elements` field comment.** Uno keeps `/*key*/` comment from C++ — good.

- **Low — `s_PoolInstanceProperty` PascalCase suffix on field.** WinUI also uses `s_PoolInstanceProperty` (capital P) — match.

- **Low — `IPanel` vs `Panel`.** C++ uses `winrt::Panel` (concrete class). Uno uses `IPanel`. This is an Uno abstraction; ensure cast to `IPanel` matches `try_as<Panel>` semantics (the cast in `EnsureOwnerIsPanelOrNull` correctly uses `owner as IPanel`).

---

### RecyclingElementFactory (`RecyclingElementFactory.cpp` / `.h` -> `.cs` / `.Properties.cs` / `.h.mux.cs` / `.mux.cs`)

File layout OK (four-file split present).

#### Method order verification (.mux.cs vs .cpp)

| WinUI .cpp order | Uno .mux.cs order | Match |
|---|---|---|
| Constructor `RecyclingElementFactory()` | Constructor | yes |
| `RecyclePool` getter | `RecyclePool` get | yes |
| `RecyclePool` setter | `RecyclePool` set | yes |
| `Templates` getter | `Templates` get | yes |
| `Templates` setter | `Templates` set | yes |
| `OnSelectTemplateKeyCore` | `OnSelectTemplateKeyCore` | yes |
| `GetElementCore` | `GetElementCore` | yes |
| `RecycleElementCore` | `RecycleElementCore` | yes |

#### Field verification (.h vs .h.mux.cs)

| WinUI .h | Uno .h.mux.cs | Match |
|---|---|---|
| `tracker_ref<winrt::RecyclePool> m_recyclePool` | `RecyclePool m_recyclePool` | OK |
| `tracker_ref<winrt::IMap<hstring, DataTemplate>> m_templates` | `IDictionary<string, DataTemplate> m_templates = new Dictionary<...>()` | **Initialization moved** — see findings. |
| `tracker_ref<winrt::SelectTemplateEventArgs> m_args` | `SelectTemplateEventArgs m_args` | OK |

#### Findings

- **High — Constructor body moved/relocated.** C++:
  ```cpp
  RecyclingElementFactory::RecyclingElementFactory()
  {
      m_templates.set(winrt::make<HashMap<winrt::hstring, winrt::DataTemplate>>());
  }
  ```
  Uno:
  ```csharp
  public RecyclingElementFactory() { }
  ```
  Initialization moved to field initializer in `.h.mux.cs`. **Behavior is preserved** (same default value) but the .cpp method order is broken — the constructor is now empty in `.mux.cs`. Either keep the empty body and document the move, or move it back into the ctor body to match the .cpp.

- **Medium — Templates getter/setter lacks `IMap` semantics.** C++ uses `winrt::IMap<...>`; Uno uses `IDictionary<...>`. Per the WinRT-to-.NET interop rules `IMap<K,V>` projects to `IDictionary<K,V>` — OK.

- **Medium — Comment paraphrasing in `OnSelectTemplateKeyCore`.** C++:
  ```cpp
  if (templateKey.empty()) { throw ... L"Please provide a valid template identifier..." }
  ```
  Uno:
  ```csharp
  if (string.IsNullOrEmpty(templateKey)) { throw new InvalidOperationException("Please provide a valid template identifier..."); }
  ```
  `IsNullOrEmpty` is a stronger check than `empty()` (it also covers `null`). Acceptable but should be commented.

- **Medium — `m_args.set(winrt::make<SelectTemplateEventArgs>())` vs `m_args = new SelectTemplateEventArgs()`.** OK.

- **Medium — `m_templates.get().First().Current().Key()` vs `m_templates.First().Key`.** OK conversion.

- **Low — XML doc on `OnSelectTemplateKeyCore` retained** — good.

- **Low — `#pragma region IRecyclingElementFactory` markers as comments.** Consistent with rest of codebase.

- **Low — Templates collection comparison for HasKey.** C++: `!m_templates.get().HasKey(templateKey)`; Uno: `!m_templates.ContainsKey(templateKey)`. OK.

---

### ItemTemplateWrapper (`ItemTemplateWrapper.cpp` / `.h` -> `ItemTemplateWrapper.cs`)

#### File layout
Monolithic file. No split.

#### Method order verification

| WinUI .cpp order | Uno .cs order | Match |
|---|---|---|
| ctor(`DataTemplate`) | ctor(`DataTemplate`) | yes |
| ctor(`DataTemplateSelector`) | ctor(`DataTemplateSelector`) | yes |
| `EnableTracking` | **missing** | **NO — see findings.** |
| `GetDataTemplate` (private helper) | **missing — replaced by field access** | **NO** |
| `Template` get | `Template` get (auto-prop) | reordered/merged |
| `Template` set | `Template` set | reordered |
| `TemplateSelector` get | `TemplateSelector` get | reordered |
| `TemplateSelector` set | `TemplateSelector` set | reordered |
| `GetElement` | `GetElement` | yes |
| `RecycleElement` | `RecycleElement` | yes |

#### Findings

- **High — Missing `EnableTracking` method.** WinUI added a method `EnableTracking(const ITrackerHandleManager*)` to break reference cycles. This is a WinRT reference-tracker concern (XAML reference tracker for cycle detection). On Uno, the cycle is broken differently (GC handles cycles), so it is **acceptable to omit** but rule #1 (lossless port) requires either a `#if !HAS_UNO` stub with `// TODO Uno:` (rule #6) or a comment in the file explaining why the method is intentionally omitted. Neither is present.

- **High — Missing MUX Reference header** (rule #4).

- **Medium — File layout violation** (rule #2). Should be 3-file split.

- **Medium — Comment dropped.** The WinUI `.h` has extensive documentation on `m_trackedDataTemplate` and `EnableTracking` explaining cycle-breaking semantics. Even if `EnableTracking` is omitted in Uno, the documentation should be preserved as context.

- **Medium — `m_dataTemplate` getter no longer uses `GetDataTemplate()` helper.** C++ all internal accesses go through `GetDataTemplate()` (returns either tracked or raw); Uno collapses to a single field. Acceptable since tracking is dropped, but the original two-state model is lost.

- **Low — `using System.Linq;` unused** (cosmetic).

- **Low — Catch block — paraphrased comment.** C++:
  ```cpp
  catch (winrt::hresult_error e) {
      if (e.code().value != E_INVALIDARG) { throw e; }
  }
  ```
  Uno:
  ```csharp
  catch (ArgumentException) {
      // The default implementation of SelectTemplate(...) throws invalid arg ...
      //if (e.code().value != E_INVALIDARG) { throw e; }
  }
  ```
  Catching `ArgumentException` (rather than re-checking error code) is acceptable, but the original re-throw logic is commented out instead of translated. Either delete the dead comment or translate the logic (re-throw if not exactly an "invalid arg for null container").

- **Low — Cast pattern.** C++: `selectedTemplate.LoadContent().as<winrt::FrameworkElement>()`; Uno: `selectedTemplate.LoadContent() as FrameworkElement`. C++ `.as<>` throws on failure; Uno `as` returns null. This is a subtle behavior difference — fallback path immediately covers null with the "empty rectangle" branch, so acceptable.

- **Low — Constructor parameter ordering** — match.

- **Low — `args.Parent as FrameworkElement` in `GetElement`.** C++: `args.Parent().as<winrt::FrameworkElement>()` — throws if not FE. Uno: `as FrameworkElement` — returns null. Mismatch but covered by `recyclePool == null` path. Acceptable.

- **Low — `using Microsoft.UI.Xaml.Shapes;`** for `Rectangle` — match.

---

### SelectTemplateEventArgs (`SelectTemplateEventArgs.cpp` / `.h` -> `SelectTemplateEventArgs.cs`)

#### File layout
Monolithic. Property-only.

#### Field verification

| WinUI .h | Uno .cs | Match |
|---|---|---|
| `hstring m_templateKey{}` | `string TemplateKey` (auto-prop) | OK |
| `tracker_ref<IInspectable> m_dataContext` | `object DataContext` (auto-prop, public get / internal set) | OK |
| `tracker_ref<UIElement> m_owner` | `UIElement Owner` (auto-prop, public get / internal set) | OK |

#### Findings

- **High — Missing MUX Reference header** (rule #4).

- **Medium — `DataContext` and `Owner` setter visibility narrowed.** C++ exposes the setters as public (`void DataContext(IInspectable const& value);` outside the region). Uno marks them `internal set`. Per rule #5, narrowing without IDL evidence is incorrect; **however**, looking at the `.h` carefully: the setters appear **outside** `#pragma region ISelectTemplateEventArgs`, which is the public WinRT interface. They are public C++ methods used internally by `RecyclingElementFactory::OnSelectTemplateKeyCore`. The Uno `internal set` is therefore correct from an IDL perspective. Note this in a comment.

- **Medium — Missing XML docs** (rule #9). Class is `public sealed`; `TemplateKey`, `DataContext`, `Owner` are public; need XML docs.

- **Low — `internal ctor`** (private effectively). C++ default-constructible. Match.

- **Low — `using System; System.Linq;` unused.**

---

### ItemsSourceView (`ItemsSourceView.cpp` / `.h` -> `ItemsSourceView.cs` + `ItemsSourceView.Impl.cs`)

#### File layout
Two-file Uno split that does **not** match the rule-#2 `Foo.cs` / `Foo.h.mux.cs` / `Foo.mux.cs` model. Instead, Uno has merged `ItemsSourceView` and `InspectingDataSource` into a single class (with `Impl.cs` holding the constructor, fields, finalizer, and `IDataSourceOverrides`).

The header comments correctly explain *why* (C++ uses factory-redirect at instance creation; C# cannot, so the impl lives in the base class) — rule #6 partially satisfied via the file comment.

#### Method order verification (.mux.cs path)

| WinUI `ItemsSourceView.cpp` order | Uno location | Notes |
|---|---|---|
| `Count` (lazy init from `GetSizeCore`) | `ItemsSourceView.cs` (`Count` property) | OK |
| `GetAt(int index)` | `ItemsSourceView.cs` (`GetAt`) | OK |
| `HasKeyIndexMapping()` | `ItemsSourceView.cs` (`HasKeyIndexMapping` prop) | **Type changed — method became property.** See findings. |
| `KeyFromIndex(int index)` | `ItemsSourceView.cs` (`KeyFromIndex(int)`) | OK |
| `IndexFromKey(hstring id)` | `ItemsSourceView.cs` (`IndexFromKey(string)`) | OK |
| `IndexOf(IInspectable value)` | `ItemsSourceView.cs` (`IndexOf(object)`) | **Visibility narrowed — see findings.** |
| `CollectionChanged add/remove` | `ItemsSourceView.cs` (C# event) | OK |
| `OnItemsSourceChanged` (protected) | `ItemsSourceView.cs` (`private protected`) | **Visibility narrowed.** See findings. |
| `GetSizeCore` (throws) | **Moved to `ItemsSourceView.Impl.cs` with real impl** | Logic moved here from `InspectingDataSource`. |
| `GetAtCore` (throws) | Same | Logic moved. |
| `HasKeyIndexMappingCore` (throws) | Same | Logic moved. |
| `KeyFromIndexCore` (throws) | Same | Logic moved. |
| `IndexFromKeyCore` (throws) | Same | Logic moved. |
| `IndexOfCore` (throws) | Same | Logic moved. |

#### Findings

- **High — `HasKeyIndexMapping()` semantics changed.** In C++ it is a public **method**; in Uno it is a **property**. Rule #1 (lossless port) — but per the conversion guide, getter-style methods are translated to properties (allowed). However, `HasKeyIndexMapping` reads as boolean state — property is reasonable. Verify `.idl` defines this as property or method. If method, change back.

- **High — `IndexOf` visibility narrowed to internal.** C++ has `int IndexOf(IInspectable const& value)` as public (in `#pragma region ItemsSourceView`). Uno has `internal int IndexOf(object item)`. Confirm against IDL.

- **Medium — `Core` overrides relocated.** WinUI:
  - `ItemsSourceView` (base): all `*Core()` methods `throw hresult_not_implemented()` (virtual)
  - `InspectingDataSource` (derived): overrides them with real logic

  Uno:
  - `ItemsSourceView.Impl.cs` (still the base class): all `*Core()` are `private protected` and contain real logic
  - `InspectingDataSource.cs`: empty wrapper

  This is a structural divergence documented in header comments — acceptable since C# can't replicate the factory redirect, but creates a problem: subclasses of `ItemsSourceView` (other than `InspectingDataSource`) **cannot override the `*Core` virtuals**, because they no longer exist as virtuals in the base. WinUI explicitly designs these as virtuals on `ItemsSourceView`. **This is a behavioral break** for consumers who derive their own `ItemsSourceView`.
  - Fix: make `*Core` methods virtual and throw `NotImplementedException` by default, with `InspectingDataSource` overriding them (true to WinUI shape). The current header-comment-only mitigation is insufficient.

- **Medium — `Count` property — `m_cachedSize` field belongs to `.h` (`.h.mux.cs`).** Currently sits in `ItemsSourceView.cs` instead of a `.h.mux.cs`.

- **Medium — `OnItemsSourceChanged` visibility narrowed.** C++ exposes this in `#pragma region Consume API for internal use only.` as a public C++ method. Uno marks `private protected`. The C++ region label calls it "internal" — `internal` (in C# terms) is the closest match, **not** `private protected`. Narrows derivation surface.

- **Medium — Missing `Impl.cs` MUX Reference header citation includes two commits but the WinUI snapshot is at 4b206bce3 for both.** The current `ItemsSourceView.cs` and `Impl.cs` claim:
  ```
  // MUX Reference InspectingDataSource.cpp, commit 37ade09; ItemsSourceView.cpp, commit dc8d573
  ```
  These commit shas are stale (should both be `4b206bce3`).

- **Low — `m_cachedSize` is in `ItemsSourceView.cs` rather than `.h.mux.cs`.**

- **Low — `#pragma region IDataSource` / `IDataSourceProtected` / `IDataSourceOverrides` markers** are preserved as `#region` — good.

- **Low — `using System.Linq;` unused.**

---

### InspectingDataSource (`InspectingDataSource.cpp` / `.h` -> `InspectingDataSource.cs` + content moved to `ItemsSourceView.Impl.cs`)

#### File layout
`InspectingDataSource.cs` is a thin shim. All logic moved to `ItemsSourceView.Impl.cs`. The header comment explains why.

#### Method order verification (against `InspectingDataSource.cpp`, comparing to `ItemsSourceView.Impl.cs`)

| WinUI .cpp order | Uno location | Match |
|---|---|---|
| ctor `InspectingDataSource(source)` | `ItemsSourceView.Impl.cs` ctor | logic moved |
| `~InspectingDataSource()` | `~ItemsSourceView()` **(finalizer)** | **Rule #8 violation** — see findings. |
| `GetSizeCore` | `GetSizeCore` (Impl) | OK |
| `GetAtCore` | `GetAtCore` (Impl) | OK |
| `HasKeyIndexMappingCore` | `HasKeyIndexMappingCore` (Impl) | OK |
| `KeyFromIndexCore` | `KeyFromIndexCore` (Impl) | OK |
| `IndexFromKeyCore` | `IndexFromKeyCore` (Impl) | OK |
| `IndexOfCore` | `IndexOfCore` (Impl) | OK |
| `WrapIterable` | `WrapIterable` overloads | OK |
| `UnListenToCollectionChanges` | same | OK |
| `ListenToCollectionChanges` | `ListenToCollectionChanges(object vector)` — **takes parameter** | Mismatch — see findings. |
| `OnCollectionChanged` | same | Different logic — see findings. |
| `OnBindableVectorChanged` | same | OK |
| `OnVectorChanged` | same | OK |

#### Findings

- **High — Finalizer present (rule #8 violation).** `ItemsSourceView.Impl.cs` line 82:
  ```csharp
  ~ItemsSourceView()
  {
      UnListenToCollectionChanges();
  }
  ```
  Rule #8: **No finalizers.** The C++ `~InspectingDataSource()` is for the WinRT lifecycle; C# should use `IDisposable` (the file already uses `Disposable.Create` via `_collectionChangedListener`, so the finalizer is redundant for typical paths). Either remove the finalizer entirely or wrap in `#if HAS_UNO` with `TODO Uno:` justification.

- **High — `OnCollectionChanged` adds INCC `Move` decomposition logic without proper marker placement.** WinUI:
  ```cpp
  void InspectingDataSource::OnCollectionChanged(.., e) {
      OnItemsSourceChanged(e);
  }
  ```
  Uno:
  ```csharp
  void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
      // Uno specific: ItemsRepeater originally relied on VectorChange, which has no Move action.
      // Decompose Move into Remove+Add at the C#/WinUI bridge so consumers receive only the
      // actions the ported control code expects.
      if (e.Action == NotifyCollectionChangedAction.Move) {
          OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems, e.OldStartingIndex));
          OnItemsSourceChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems, e.NewStartingIndex));
          return;
      }
      OnItemsSourceChanged(e);
  }
  ```
  This is Uno-specific code without `#if HAS_UNO` (rule #6 requires `#if HAS_UNO` wrapping or `// TODO Uno:`). The comment says "Uno specific" but the marker is `// Uno specific:` not `// TODO Uno:`. Normalize.

- **High — Source-type detection logic differs.** WinUI tries `IVector<IInspectable>`, then `IBindableVector`, then `IVectorView<IInspectable>`, then `IIterable<IInspectable>`, then `IBindableIterable`. Uno tries `IList<object>`, then `IList` (via `_IBindableVector`), then `IReadOnlyList<object>`, then `IEnumerable<object>`, then `IEnumerable`. This is the standard WinRT-to-.NET projection but **the binding adapter `ListAdapter.ToGeneric` is invoked only for `_IBindableVector`** — WinUI uses `reinterpret_cast`. Behaviorally equivalent but a notable difference; should be documented (rule #6).

- **Medium — `IndexOfCore` logic divergence.** WinUI uses `IndexOf(value, out v)` boolean pattern; Uno uses straight `.IndexOf(value)` returning -1 or index. Behavior equivalent. **But:** Uno's branch `m_vector?.IndexOf(value) ?? -1` allows `m_vector` to be null; WinUI explicitly checks `m_vectorView` then falls through. Match (with null-cond as added safety).

- **Medium — `ListenToCollectionChanges` parameter mismatch.** WinUI is parameterless and uses `m_vector` / `m_vectorView` internally to find the INCC source. Uno takes an explicit `object vector` parameter so it can listen on the original `bindableVector` / `readOnlyList` directly (since the typed `m_vector` is a wrapper at that point). Behaviorally OK but creates an inconsistency: WinUI listens on `m_vectorView` if present, else `m_vector`; Uno listens on whatever was passed by the ctor. Tightening: in Uno the ctor calls `ListenToCollectionChanges(vector)` for `IList<object>` but `ListenToCollectionChanges(bindableVector)` (original) for `_IBindableVector` — these target different objects than the C++ port, which always works against the typed `m_vector` after reinterpret. Document the divergence.

- **Medium — `UnListenToCollectionChanges` simplified.** WinUI uses three tracker fields (`m_notifyCollectionChanged`, `m_observableVector`, `m_bindableObservableVector`) to remove the appropriate handler. Uno uses a single `IDisposable _collectionChangedListener` constructed via `Disposable.Create(...)`. Per rule #7 (revokers → `SerialDisposable` + `Disposable.Create(...)`). **`SerialDisposable` not used — single `IDisposable`.** Either upgrade to `SerialDisposable` or document.

- **Medium — `OnUntypedVectorChanged` is Uno-only.** It supports the Uno-only `IObservableVector` (untyped) interface and is not present in WinUI C++. Should be `#if HAS_UNO` or marked `// TODO Uno:`.

- **Low — `_collectionChangedListener` field naming.** Underscore prefix is Uno-style; WinUI uses `m_` prefix. Inconsistent with rest of port (which keeps `m_*`).

- **Low — Field initializer style.** `private int m_cachedSize = -1;` — could match `.h` field directly. OK.

---

### ListAdapter (Uno-only)

#### Findings

- **Low — Uno-only file without `#if HAS_UNO` / `// TODO Uno:`** (rule #6). The class is clearly a helper for adapting `IList` → `IList<T>`. A header comment explaining purpose and lifetime would suffice if `#if HAS_UNO` is too broad.

---

## Cross-type observations

1. **Missing MUX Reference headers** are systemic across non-split files (`ElementFactory.cs`, `ElementFactoryGetArgs.cs`, `ElementFactoryRecycleArgs.cs`, `IElementFactoryShim.cs`, `ElementRealizationOptions.cs`, `ItemTemplateWrapper.cs`, `SelectTemplateEventArgs.cs`, `ListAdapter.cs`). Add `// MUX Reference <file>, commit 4b206bce3` to each.

2. **File layout (rule #2) is violated for every type that has not been split.** ElementFactory, ElementFactoryGetArgs, ElementFactoryRecycleArgs, ItemTemplateWrapper, SelectTemplateEventArgs, ItemsSourceView (different split), InspectingDataSource (logic relocated). Either accept the deviation and document, or split per the standard.

3. **Uno-specific code not wrapped in `#if HAS_UNO`** in multiple places (e.g., `OnCollectionChanged`'s Move decomposition, `RecyclePool.Clear()` uses `#if HAS_UNO` correctly, `OnUntypedVectorChanged` does not, `ListAdapter` does not).

4. **`// Uno specific:` vs `// TODO Uno:`.** The repo uses inconsistent phrasing. Rule #6 mandates `// TODO Uno:`. Normalize.

5. **Visibility narrowing without IDL evidence** appears in `RecyclePool.{ReuseKey,GetReuseKey,SetReuseKey}`, `ItemsSourceView.{IndexOf,OnItemsSourceChanged,HasKeyIndexMapping}`, and `ElementFactoryGetArgs.Index`. Verify against `RecyclePool.idl` / `ItemsSourceView.idl` / `ElementFactoryGetArgs.idl` from WinUI before fixing.

6. **Tracking/cycle-breaking** (`tracker_ref`, `EnableTracking`, `ITrackerHandleManager`) is a WinRT-only concern. Uno relies on .NET GC, but the documentation comments around tracker usage are largely dropped — preserving them aids future syncs.

7. **`SerialDisposable` not used in `ItemsSourceView`** (rule #7). Single `IDisposable` could be replaced.

8. **Finalizer on `ItemsSourceView`** (rule #8 violation).

9. **`ItemsSourceView` Core overrides are no longer virtual** — this is a real consumer-facing API break versus WinUI for custom `ItemsSourceView` subclasses.

10. **XML doc coverage on public/protected** (rule #9) is incomplete on `ElementFactoryGetArgs`, `ElementFactoryRecycleArgs`, `SelectTemplateEventArgs`, and on public/internal API surface across several files.

11. **Stale MUX commits** in `ItemsSourceView.cs` / `Impl.cs` / `InspectingDataSource.cs` (commit shas `37ade09`, `dc8d573` should be `4b206bce3`).

---

## Conclusion

| Severity | Count |
|---|---:|
| High | 16 |
| Med  | 28 |
| Low  | 27 |

### Top priority issues

1. **Finalizer on `ItemsSourceView`** (rule #8) — must be removed or wrapped in `#if HAS_UNO` with `// TODO Uno:` justification.
2. **`ItemsSourceView.*Core` virtuals collapsed to `private protected`** — consumer-facing API break vs WinUI; restore as `virtual` methods overridden in `InspectingDataSource`.
3. **`ItemTemplateWrapper.EnableTracking` missing** without explicit documentation of intentional omission.
4. **Missing MUX Reference headers** on `ElementFactory.cs`, `ElementFactoryGetArgs.cs`, `ElementFactoryRecycleArgs.cs`, `ItemTemplateWrapper.cs`, `SelectTemplateEventArgs.cs`.
5. **File layout violations** (rule #2) on five types — split `.cs` / `.h.mux.cs` / `.mux.cs`.
6. **Stale MUX commits** (`37ade09`, `dc8d573`) on `ItemsSourceView` / `InspectingDataSource` — bump to `4b206bce3`.
7. **Visibility audit** — `RecyclePool.ReuseKey*`, `ItemsSourceView.IndexOf`, `ItemsSourceView.OnItemsSourceChanged`, `ElementFactoryGetArgs.Index`: confirm against IDL and align.
8. **`OnCollectionChanged` Move decomposition** and `OnUntypedVectorChanged` need proper `#if HAS_UNO` / `// TODO Uno:` markers.
9. **`Uno-specific` files** (`IElementFactoryShim.cs`, `ElementRealizationOptions.cs`, `ListAdapter.cs`) need either `#if HAS_UNO` wrap or `// TODO Uno:` documentation block.
10. **Constructor body relocation** in `RecyclingElementFactory` — empty C# ctor with init moved to field; either restore or document.
