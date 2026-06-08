# Spec 043: Uno Platform 7.0 — Native Rendering Removal & Breaking-Changes Roundup

> **Status**: Draft - 2026-06-08
> **Author**: Martin Zikmund
> 2026-06-08 - Draft
>
> **Tracking epic**: [unoplatform/uno#8339](https://github.com/unoplatform/uno/issues/8339) — *[Epic] Breaking changes roundup*
> **Implementation branches**: `feature/breaking-changes` (CI-configured for breaking changes), native removal staged on `dev/mazi/drop-native`

---

## Executive Summary

### The Problem

Uno Platform 7.0 is a **major version**, and a major boundary is the *only* window in which
we can make source- and binary-breaking changes without violating SemVer. Two distinct bodies
of work converge on this release:

1. **Native UI rendering is being removed in favour of Skia.** Starting with 7.0 the native
   UI rendering layers — native Android Views (`UnoViewGroup`/`BindableView`), native iOS/UIKit
   views (`BindableUIView`), and native WASM DOM-element rendering — are deleted. All targets
   render through Skia. (Non-UI WinRT platform APIs in `Uno.UWP` / `Uno.Foundation` — sensors,
   pickers, storage, geolocation, etc. — are **kept**; Skia-on-Android/iOS still consume them.)

2. **A large backlog of accepted breaking changes** has accumulated in epic #8339 since 2022 —
   accidental public API, WinUI-parity defects, dead feature flags, and legacy code paths — that
   have been deliberately deferred because they could only land on a major boundary.

These two are not independent. Removing native rendering **unlocks** a class of breaking changes
that were previously impossible or too risky (most importantly, collapsing `DependencyObject`
from a source-generated interface back to a normal class), and it **renders obsolete** a second
class of items that simply disappear with the native UI code. Treating #8339 as one flat
checklist obscures this structure and risks double-tracking work that the native-removal PR
deletes for free.

### What We're Shipping

A coordinated 7.0 program with a clear dependency spine:

1. **Workstream 0 — Native rendering removal** (the enabler). Deletes the native UI substrate.
2. **Workstream 1 — `DependencyObject` as a class** (`#17099`), unblocked by WS0.
3. **Workstream 2 — Parity API-hygiene sweep**: ~15 starter-effort removals/visibility tightenings
   that erase accidental public API with no WinUI counterpart.
4. **Workstream 3 — Parity type/shape fixes**: ~9 binary-breaking corrections (collection bases,
   field→property, wrong enum types, wrong base classes).
5. **Workstream 4 — Native-unlocked removals**: items that flip from "risky" to "clean delete"
   once native rendering is gone, scheduled *with* WS0.
6. **Workstream 5 — Larger parity efforts** (consider-for-7.0, each gated on its own spike).

This spec is the **source-of-truth plan** for which items from #8339 are in/out of 7.0, why, and
in what order. The full per-item inventory (verified against code on `dev/mazi/drop-native`) is in
[§7 Change Inventory](#7-change-inventory).

### Why This Approach

- **A major version is the only window.** Every item here is source- or binary-breaking; deferring
  again means waiting for 8.0.
- **WinUI/WinAppSDK API parity is the north star.** The dominant rationale for each change is
  matching the real WinUI public API surface — removing members WinUI never exposes, fixing types
  and base classes to match the WinAppSDK IDL, and dropping Uno-specific deviations.
- **Native removal does most of the work.** A dozen #8339 items are deleted as a *side effect* of
  WS0 and must not be tracked as separate breaking-change tasks.
- **Sequencing is load-bearing.** `DependencyObject`-as-class and several layout/pointer cleanups
  are *blocked on* WS0 landing first; doing them out of order is the main execution risk.

---

## 1. Goals & Non-Goals

### Goals

- **G1** — Remove the native UI rendering substrate so all targets render through Skia.
- **G2** — Land the highest-value WinUI-parity breaking changes from #8339 that require a major boundary.
- **G3** — Reduce accidental public API surface (members/types/flags Uno exposes that WinUI does not).
- **G4** — Leave the public API of Uno 7.0 measurably closer to the WinAppSDK surface than 6.x.
- **G5** — Provide a migration story (documentation + analyzers/obsoletions where feasible) for every breaking change that can affect real app code.

### Non-Goals

- **NG1** — Removing or degrading **non-UI WinRT platform APIs** (`Uno.UWP`/`Uno.Foundation`). These stay and are still enhanced; Skia-on-mobile consumes them.
- **NG2** — Breaking **Skia-on-Android/iOS/WASM** behaviour. Native *rendering* is removed; Skia *on* those OSes remains a first-class target.
- **NG3** — Shipping new end-user features. 7.0 is a cleanup/parity release; feature work rides other specs.
- **NG4** — A repo-wide mechanical refactor with no user-facing or parity payoff (e.g. global `#nullable enable` flip) — these are explicitly deferred (see §8).
- **NG5** — Reviving stale/abandoned PRs verbatim. Where an idea is still valid but its PR is closed, a fresh, re-scoped change is required.

---

## 2. Guiding Principles

1. **Parity over cleverness.** Match WinUI's *actual* public mechanism 1:1, even where an Uno
   deviation looks cleaner. (e.g. `ItemsControl.OnItemsChanged(object e)` already matches WinUI —
   do **not** "fix" it to take `IVectorChangedEventArgs`; that would *diverge*.)
2. **Delete, don't deprecate, on a major.** Binary-compat shims kept "until the next breaking
   version" (`set_Property`, `op_Explicit`, throwing `Count`/`IsReadOnly` setters) are removed now.
3. **Verify against source before acting.** The #8339 text has drifted — stale line numbers,
   renamed files (`.iOS.cs` → `.UIKit.cs`), and several already-done items. Every change must be
   re-confirmed against current code (this spec's inventory already did this pass).
4. **Native cleanups ride the removal PR.** "Obsolete-after" items are not separate tasks.
5. **Risk is bought with tests.** Each behavioural break must have a fails-before/passes-after
   runtime or unit test.

---

## 3. Background: The Three Buckets

Every open #8339 item falls into exactly one of these. The bucket determines *how* it is scheduled.

| Bucket | Meaning | Scheduling |
|--------|---------|------------|
| **A — Unlocked by Skia-only** | Impossible / too risky while native view hierarchies exist | WS0 first, then the change |
| **B — Independent parity break** | Unrelated to native rendering; just needs a major boundary | Any time in 7.0 |
| **C — Free with native removal** ("obsolete-after") | The code is deleted by WS0 anyway | Folded into the WS0 PR(s); **not** a separate task |

The crown jewel sits in Bucket A and justifies coupling the two efforts:

> **`DependencyObject` is a `public partial interface`** (`DependencyObject.cs:12`), mixed into every
> dependency object by the 806-line `DependencyObjectGenerator`, **solely** because `UIElement` must
> inherit native view base classes (Android `ViewGroup → UnoViewGroup → BindableView`; iOS
> `UIView → BindableUIView`). Delete those base classes and `DependencyObject` can collapse to a
> normal, WinUI-shaped base **class** — removing the single largest architectural divergence from
> WinUI and a major source of generator complexity. This is only possible **after** WS0.

---

## 4. Scope — Workstreams

### Workstream 0 — Native rendering removal (the enabler)

Delete the native UI rendering substrate. Most is not individually enumerated in #8339 but is a
direct, mandatory consequence of the removal. Confirmed present in the tree today:

- Native base classes: `BindableView` (Android), `BindableUIView` (iOS), `UnoViewGroup.java` + the
  `Uno.UI.BindingHelper.Android` Java/AAR build and its JNI marshalling.
- `NativeRenderTransformAdapter.{Android,Apple,wasm}.cs` and the `INativeRenderTransformAdapter` abstraction.
- `_View` / `_ViewGroup` type aliases and the APIs typed in terms of them (e.g. `UIElementCollectionExtensions.Xamarin.cs`).
- Native pointer entry shims (`dispatchTouchEvent` / UIKit touch overrides) and the managed-vs-native bubbling coordination.
- Native measure/arrange bridges (`onMeasure`/`onLayout`, `LayoutSubviews`) and their `#if` branches across the layout core.
- Native popups (`NativePopup.Android.cs`, `Popup.Android.cs`, `NativePopupAdapter.cs`, `UseNativePopups`).
- Native list virtualization: the entire `UI/Xaml/Controls/ListViewBase/Legacy/` folder (20 files, all `.Android.cs`/`.UIKit.cs`).
- Native element hosting: `AdaptNative`/`TryAdaptNative` (`VisualTreeHelper.cs:304-362`).
- WASM DOM rendering shims: `HtmlImage` (`Image.wasm.cs`) and DOM-backed `UIElement` rendering; `IJSObject` DOM/animation interop (`Uno.Foundation.Runtime.WebAssembly`).
- Native iOS window glue: `WindowView.iOS.cs`, `NativeWindow.UIKit.cs` (`BypassCheckToCloseKeyboard`), `NSObjectExtensions.ValidateDispose`.
- Misc native helpers: `BinderDetails.Android.cs`, `StyleSelector2.UIKit.cs` / `StyleSelectorCollection.UIKit.cs`, native `BorderLayerRenderer` paths.

> **Prerequisite umbrella item — not currently in #8339.** Add an explicit task for *removing the
> native `UIElement` base classes* (`BindableUIView`, `UnoViewGroup`/`BindableView`). `#17099` and
> `#13201` are blocked on it.

### Workstream 1 — `DependencyObject` as a class (`#17099`)

After WS0: change `DependencyObject` from interface to class, collapse the `DependencyObjectGenerator`
mix-in mechanism, and align with the WinUI base-class shape. High value, high risk, **must not block
the release** if it slips — it can ship in a 7.x minor since by then native is already gone.

### Workstream 2 — Parity API-hygiene sweep (one PR)

~15 low-risk removals/visibility tightenings of accidental public API. See [Tier 1A](#tier-1a--api-hygiene-sweep).

### Workstream 3 — Parity type/shape fixes

~9 binary-breaking corrections. See [Tier 1B](#tier-1b--parity-typeshape-fixes).

### Workstream 4 — Native-unlocked removals

Items that become clean deletes once WS0 lands. See [Tier 1C](#tier-1c--native-unlocked-removals).

### Workstream 5 — Larger parity efforts (consider)

Each needs its own spike/design before committing. See [Tier 2](#tier-2--consider-for-70).

---

## 5. Functional Requirements

Phrased as "Uno 7.0 MUST…". `R` = risk, `E` = effort as assessed in §7.

### Native removal (WS0)

- **FR-001**: Uno 7.0 MUST render all targets through Skia and MUST delete the native UI rendering substrate listed in §4/WS0.
- **FR-002**: Uno 7.0 MUST NOT remove or behaviourally regress non-UI WinRT APIs in `Uno.UWP`/`Uno.Foundation`.
- **FR-003**: Uno 7.0 MUST NOT regress Skia-on-Android, Skia-on-iOS, or Skia-on-WASM rendering or input.
- **FR-004**: The native-removal PR(s) MUST also delete every "obsolete-after" item in §7 (Tier 3), rather than leaving dead code or accidental public types behind.

### Architectural unlock (WS1)

- **FR-010**: Uno 7.0 SHOULD make `DependencyObject` a class on all targets (`#17099`), sequenced after FR-001. If it cannot land safely in 7.0, it MUST be scheduled for the earliest 7.x minor and MUST NOT block the 7.0 release.

### Parity API-hygiene (WS2 — all MUST, low risk)

- **FR-020**: `Frame.BackStack` setter MUST NOT be public (WinUI: get-only).
- **FR-021**: `PageStackEntry.SourcePageType` setter MUST NOT be public.
- **FR-022**: `NavigatingCancelEventArgs` MUST NOT expose a public constructor.
- **FR-023**: `IMenu`, `IMenuPresenter`, `ISubMenuOwner` MUST be internal (no WinUI counterpart).
- **FR-024**: `XamlCompositionBrushBase.CompositionBrush` MUST be `protected`, not public (WinUI: protected).
- **FR-025**: `SetterBase.set_Property` (binary-compat shim) MUST be removed.
- **FR-026**: `IFrameworkTemplatePoolAware.OnTemplateRecycled` MUST be implemented **explicitly** on `ToggleButton`, `TextBox`, `ToggleSwitch`, `ScrollContentPresenter` (`#13083`).
- **FR-027**: The empty `RestoreBindings()` / `ClearBindings()` forwarders MUST be removed from `DependencyObject` and the generator (`#13046`).
- **FR-028**: `PointerPoint.op_Explicit` back-compat conversion MUST be removed (keep the implicit operator).
- **FR-029**: `PropertyPath` MUST be `sealed` (WinUI: sealed; VS designer expects it).
- **FR-030**: `ColorKeyFrameCollection` MUST NOT expose throwing `Count`/`IsReadOnly` setters.
- **FR-031**: `TextBox.OnVerticalContentAlignmentChanged` override MUST be removed and the base narrowed to `private protected`.
- **FR-032**: The `ShowClippingBounds`, `ApplySettersBeforeTransition` (`#13326`), and `RethrowNativeExceptions` feature flags MUST be removed.

### Parity type/shape fixes (WS3 — all MUST, binary-breaking)

- **FR-040**: `DoubleCollection` (and sibling collections following the same pattern) MUST use composition instead of `: List<double>` so they expose only the WinUI `IList<double>`/`IVector<Double>` surface.
- **FR-041**: `Duration.Type`, `Duration.TimeSpan`, and **`KeyTime.TimeSpan`** MUST be properties, not public mutable fields (`#13096`; `KeyTime` is a coverage gap not in #8339).
- **FR-042**: `WindowActivatedEventArgs.WindowActivationState` MUST be typed `Microsoft.UI.Xaml.WindowActivationState` (value-compatible enum), not `CoreWindowActivationState`.
- **FR-043**: `TimePickerFlyoutPresenter` MUST derive from `Control`, not `FlyoutPresenter` (WinUI base).
- **FR-044**: `FrameworkPropertyMetadata` MUST be internal (WinUI public type is `PropertyMetadata`).
- **FR-045**: `Background` MUST be declared per-type on `Control`/`Panel`/`Border` (WinUI shape), not as a single inherited DP on `FrameworkElement`.
- **FR-046**: `CompositionSpriteShape.StrokeDashArray` MUST be get-only, non-null, and non-nullable.
- **FR-047**: `VisualInteractionSource.TryRedirectForManipulation` MUST take `Microsoft.UI.Input.PointerPoint`, not `Windows.UI.Input.PointerPoint`.
- **FR-048**: `FrameworkElement` MUST NOT implement `IEnumerable` on Skia.

### Native-unlocked removals (WS4 — scheduled with WS0)

- **FR-050**: The `EventsBubblingInManagedCode` dependency property MUST be removed.
- **FR-051**: The `UseLegacyHitTest` feature flag and its native hit-test paths MUST be removed.
- **FR-052**: `IFrameworkElement.AdjustArrange` (`#14478`) MUST be removed (no-op on every non-native target).
- **FR-053**: The legacy `ListViewBase/Legacy` folder MUST be removed (strictly **after** FR-001, since it is the only native ListView implementation today).
- **FR-054**: `Uno.UI.DataBinding.BinderDetails` MUST be removed.

### Cross-cutting

- **FR-060**: Every behavioural breaking change MUST ship with a fails-before/passes-after test (runtime test for UI behaviour, unit test for logic).
- **FR-061**: Every breaking change that can affect app code MUST be listed in a 7.0 migration guide, and MUST carry an `[Obsolete]`/analyzer hint in the last 6.x minor where technically feasible.
- **FR-062**: Confirmed-done #8339 items MUST be checked off / closed (see §9).

---

## 6. Success Criteria

- **SC-001**: 100% of native UI rendering code (per §4/WS0 inventory) is deleted; no `BindableView`/`BindableUIView`/`UnoViewGroup`/`AdaptNative`/native-popup/native-ListView code remains in the shipped assemblies.
- **SC-002**: The full Uno.UI runtime test suite passes on Skia Desktop, Skia-Android, Skia-iOS, and Skia-WASM with **no** native-rendering regressions.
- **SC-003**: Public-API diff vs 6.x shows a net **reduction** in Uno-only public types/members, and **zero** new Uno-only public surface introduced by this work.
- **SC-004**: Every FR-020…FR-054 change has a corresponding merged PR with a fails-before/passes-after test.
- **SC-005**: A published 7.0 migration guide enumerates every breaking change with before/after code and a recommended remediation.
- **SC-006**: The #8339 epic is reconciled: done items checked, obsolete-after items annotated as "removed with native rendering", deferred items moved to a "Post-7.0 / 8.0" section.
- **SC-007**: No item classified "already-done" in §7 is re-implemented (wasted work guard).

---

## 7. Change Inventory

Decision legend: **DO** = do for 7.0 · **CONSIDER** = valuable, gated on a spike · **FOLD** = rides the native-removal PR · **DEFER** = post-7.0 · **DONE/SKIP** = already satisfied or premise false.
Native legend: **Unlocked** (only possible post-removal) · **Mandatory/Obsolete-after** (deleted by removal) · **Independent**.

### Tier 1A — API-hygiene sweep (DO · starter · low risk · one PR)

| # | Change | Verified anchor |
|---|--------|-----------------|
| FR-020 | `Frame.BackStack` setter → internal | `Frame.Properties.cs:17` has `// TODO: Setter should not be public` |
| FR-021 | `PageStackEntry.SourcePageType` setter → internal | `PageStackEntry.Properties.cs:34`, set only internally |
| FR-022 | `NavigatingCancelEventArgs` ctor → internal | `NavigatingCancelEventArgs.cs:13` public 4-arg ctor; WinUI sealed, no public ctor |
| FR-023 | `IMenu`/`IMenuPresenter`/`ISubMenuOwner` → internal | three `public partial interface` under `MenuFlyout/`, no WinUI equivalent |
| FR-024 | `XamlCompositionBrushBase.CompositionBrush` → protected | `XamlCompositionBrushBase.cs:29` public get/set; DP already internal |
| FR-025 | Remove `SetterBase.set_Property` | `SetterBase.cs:25` comment: removable "in Uno 6", `[EditorBrowsable(Never)]` |
| FR-026 | `IFrameworkTemplatePoolAware` → explicit on 4 controls | `ToggleButton.cs:90`, `TextBox.cs:1399`, `ToggleSwitch.cs:204`, `ScrollContentPresenter.Native.cs:16` |
| FR-027 | Remove empty `RestoreBindings`/`ClearBindings` | `DependencyObjectStore.Binder.cs:198/213` empty bodies; generator `DependencyObjectGenerator.cs:491-503` |
| FR-028 | Remove `PointerPoint.op_Explicit` | `PointerPoint.cs:60-67`, `[EditorBrowsable(Never)]` |
| FR-029 | `PropertyPath` → sealed | `PropertyPath.cs:3` currently unsealed |
| FR-030 | Remove throwing `ColorKeyFrameCollection` setters | `ColorKeyFrameCollection.cs:27-37` back-compat shims |
| FR-031 | Remove `TextBox.OnVerticalContentAlignmentChanged`, narrow base | `TextBox.cs:1410` already TODOs this; base `ContentPresenter.cs:538` |
| FR-032 | Remove `ShowClippingBounds`, `ApplySettersBeforeTransition`, `RethrowNativeExceptions` flags | `FeatureConfiguration.cs:730/785`; `FoundationFeatureConfiguration.cs:40` ("remove in next major") |

> **Note (FR-032):** `RethrowNativeExceptions` is **Independent**, not native-related — it is a WASM
> JS-exception rethrow stub. #8339 implies it is native; it is not. It is still a valid 7.0 removal.

### Tier 1B — Parity type/shape fixes (DO · binary-breaking · major-only)

| # | Change | E/R | Verified anchor |
|---|--------|-----|-----------------|
| FR-040 | `DoubleCollection`: composition, not `: List<double>` | M/M | `DoubleCollection.cs:9`; WinUI `coretypes.idl:2681` = `IVector<Double>` |
| FR-041 | `Duration.{Type,TimeSpan}` + `KeyTime.TimeSpan` field→property | S/L | `Duration.cs:18-19`, `KeyTime.cs:11` are public fields |
| FR-042 | `WindowActivatedEventArgs.WindowActivationState` correct enum type | S/M | both Uno.UWP + Uno.UI expose `CoreWindowActivationState` |
| FR-043 | `TimePickerFlyoutPresenter : Control` | M/M | `TimePickerFlyoutPresenter.cs:8`; WinUI Phone IDL:346 |
| FR-044 | `FrameworkPropertyMetadata` internal | M/M | `FrameworkPropertyMetadata.cs:169` says so |
| FR-045 | `Background` per-type on Control/Panel/Border | M/M | `FrameworkElement.Interface.skia.cs:90`; WinUI `coretypes.idl:4008` (Control) |
| FR-046 | `CompositionSpriteShape.StrokeDashArray` get-only/non-null | M/M | `CompositionSpriteShape.cs:91`; Skia callers only read |
| FR-047 | `VisualInteractionSource` → `Microsoft.UI.Input.PointerPoint` | M/M | `VisualInteractionSource.cs:85` self-tagged "Uno 6 breaking change" |
| FR-048 | `FrameworkElement` not `IEnumerable` (Skia) | M/M | `FrameworkElement.skia.cs:30` unconditional `GetEnumerator` |

### Tier 1C — Native-unlocked removals (DO · schedule with WS0)

| # | Change | Native | Verified anchor |
|---|--------|--------|-----------------|
| FR-050 | Remove `EventsBubblingInManagedCode` DP | Obsolete-after | `UIElement.RoutedEvents.cs:202`; no-op on Skia |
| FR-051 | Remove `UseLegacyHitTest` + native hit-test | Obsolete-after | `FeatureConfiguration.cs:286`; only WASM-DOM + legacy `IsViewHit` callers |
| FR-052 | Remove `AdjustArrange` (`#14478`) | Unlocked | no-op on all non-native; only native `AdjustArrangePartial` impl |
| FR-053 | Remove legacy `ListViewBase/Legacy` | Obsolete-after | 20 files, all `.Android.cs`/`.UIKit.cs`; **order after FR-001** |
| FR-054 | Remove `BinderDetails` | Obsolete-after | `BinderDetails.Android.cs`, `Java.Lang.Object`, all-stub |

### Tier 2 — Consider for 7.0 (each gated on a spike)

| ID | Change | Why gated |
|----|--------|-----------|
| `#17099` | **`DependencyObject` → class** (WS1) | Flagship; high risk; sequence after WS0; may slip to 7.x |
| `#13201` | `DataContext` → `FrameworkElement` only | Binder machinery keyed off `UIElement.DataContextProperty`; invasive |
| `#18672` | Remove legacy templated parent | In-code TODO; legacy path still default because tests fail without it |
| `#18875` | Remove `Windows.UI.Input.*` public API | ~50 types → `Microsoft.UI.Input`; dedicated workstream |
| `#12322` | Rename `Uno.UI.Toolkit` | High churn, no parity payoff; needs type-forwarder migration |
| `#14765` | **Finish** dropping Fluent V1 | Partly done; `Uno.UI.FluentTheme.v1` + public `XamlControlsResourcesV1` still ship |
| `#16148` | `ContentPresenter.ContentTemplateRoot` not public | Tied to native child-registration partials; needs usage sweep |
| — | `Border.ChildProperty` not public | Uno uses DP infra for `Child`; hide carefully |
| — | `ComboBox.OnIsDropDownOpenChanged` → private protected | Fans to a native partial; bundle with native cleanup |
| — | `RadioMenuFlyoutItem` base → `MenuFlyoutItem` | Drops a SyncGenerator suppression; needs compose-toggle design |
| — | Flags: `UseInvalidateMeasurePath`/`ArrangePath`, `IgnoreINPCSameReferences`, `UseLegacyContentAlignment`, `DefaultsStartingValueFromAnimatedValue`, `#13704 UseLegacyPrimaryLanguageOverride` | Each needs a "prove the false-branch dead" pass; layout/binding-sensitive |
| — | `IJSObject` removal | Folds into WASM-DOM removal |
| — | `rewriteuri` ms-resource:// for non-framework owners | `XamlFileGenerator.cs:5251` empty `// Breaking change` placeholder; needs spike |
| — | `Setter<T>`, `MaterializableList<T>`→internal, `DelegatedInkTrailVisual`, `Vector2Extensions` dedup, `BrushConverter` `[EditorBrowsable]`, `#13709` typo rename `Launched` | Low-risk surface cleanups; confirm no consumers |
| `#1833` | Android `PreferenceManager` (API-29) | **Kept** API; may change on-disk settings store |
| — | Geolocator iOS non-UWP members | **Kept** API; parity-only, confirm not the public entry point |

### Tier 3 — FOLD into the native-removal PR (do **not** track separately)

`AdaptNative`/`TryAdaptNative` · Android native popups · `StyleSelector*.UIKit.cs` · `WindowView.iOS.cs` ·
`BypassCheckToCloseKeyboard` · `NSObjectExtensions.ValidateDispose` · `HtmlImage` · `IsStatusBarTranslucent` (Android) ·
`#18582` BaseActivity-nullable · `#12563` Android conditionals · `#12593` BorderLayerRenderer · `#12595` brush subscriptions ·
plus the §4/WS0 substrate (`NativeRenderTransformAdapter.*`, `UnoViewGroup`/Java-AAR, `BindableView`/`BindableUIView`,
native pointer/measure bridges, `_View`/`_ViewGroup` aliasing).

### Tier 4 — DEFER (post-7.0 / re-scope)

`#9964` global `#nullable enable` (repo is intentionally per-file) · removing non-DP setter support (`#16134`
**added** it recently — apps depend on it; high risk) · `#15684` `DependencyPropertyValuePrecedences` cleanup
(abandoned PR, core plumbing) · MRT Core relocation (`Microsoft.Windows.ApplicationModel.Resources`) · `clr-namespace:`
removal (needs WinUI-behaviour investigation) · `#15891` Android drawable extensions · revert-of-`#17414`
(`PropertyChangedParams`; belongs to DP-pipeline refactor track) · `AutomationProperties.cs:44` / `Control.cs:997` /
"`UserControl` inheritance" (line-drifted / underspecified — need original repros).

### Tier 5 — DONE / SKIP (verified satisfied; check off in #8339 — see §9)

| #8339 item | State (verified) |
|------------|------------------|
| Update SkiaSharp to 3.x | `Directory.Build.targets:77` = `3.119.0` — **done** |
| `#17797` Drop deprecated RemoteControl package | PR merged — **done** |
| Unseal `Window` on Uno.WinUI (`#5286`) | `Window.cs:35` `public partial class Window` (unsealed) — **done** |
| Remove invalid ctors from `ItemsControlAutomationPeer`/`SelectorAutomationPeer` | only WinUI ctors remain (re-ported `winui3/release/1.8.4`) — **done** |
| `CoreWebView2.DocumentTitle` setter internal/private | `CoreWebView2.Properties.cs:23` already get-only — **done** |
| `UseSystemFocusVisuals` on UIElement (WinUI tree) | `UIElement.FocusMixins.cs:95`, matches WinUI `coretypes.idl:2331` — **done** |
| `ItemsControl.OnItemsChanged` IVectorChangedEventArgs param | **Won't do** — `ItemsControl.cs:254` already matches WinUI `(object e)`; the requested change would *diverge*. Close as won't-fix, do not check as "done". |
| `FlyoutBase.Close()` removal | **Premise false** — already `protected internal` (`FlyoutBase.cs:866`), not public. |
| `HResult` removal (comment) | no `HResult.cs` under `src/` — already removed |

---

## 8. Sequencing & Phasing

```
Phase 0  WS0  Native UI rendering removal  ──────────────┐  (gate)
              + prerequisite: delete native UIElement     │
                base classes (BindableUIView,             │
                UnoViewGroup/BindableView)                 │
                                                           ▼
Phase 1  WS4  Native-unlocked removals (FR-050..054)  ── folded with / immediately after WS0
         WS2  API-hygiene sweep (FR-020..032)        ── parallel, independent of WS0
         WS3  Parity type/shape fixes (FR-040..048)  ── parallel, independent of WS0
                                                           │
                                                           ▼
Phase 2  WS1  DependencyObject → class (#17099)       ── after WS0 lands & stabilises
                                                           │
                                                           ▼
Phase 3  WS5  Consider-items, each behind its own spike (Tier 2)
```

- **WS2 and WS3 do not depend on WS0** and can start immediately on `feature/breaking-changes`.
- **WS1 and WS4 depend on WS0.** FR-053 (legacy ListViewBase) is the sharpest ordering constraint — it is the *only* native ListView impl today, so it is safe to delete only after Skia is the sole renderer.
- Every phase must keep the build green and the Skia runtime test suite passing before the next begins.

---

## 9. #8339 Reconciliation

On acceptance of this spec:

1. **Check off** the Tier 5 done items (§7) in the #8339 description: SkiaSharp 3.x, `#17797`, Unseal `Window` (`#5286`), AutomationPeer ctors, `CoreWebView2.DocumentTitle`, `UseSystemFocusVisuals`.
2. **Annotate** Tier 3 obsolete-after items as "removed with native rendering (Spec 043 / WS0)".
3. **Annotate** the two non-actionable items: `ItemsControl.OnItemsChanged` (won't-fix — already WinUI-correct) and `FlyoutBase.Close()` (premise false).
4. **Add** the two coverage gaps as new checklist items: `KeyTime.TimeSpan` field→property (mirrors `#13096`), and the native `UIElement` base-class removal umbrella (WS0 prerequisite).
5. Move Tier 4 items under a new "Post-7.0 / 8.0" heading.

---

## 10. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| **Ordering violation** — a WS1/WS4 change lands before WS0, breaking native targets mid-flight | Enforce the §8 gate; CI on `feature/breaking-changes` must build all targets; FR-053 explicitly blocked on FR-001 |
| **`#17099` destabilises the release** | Treat as non-blocking (FR-010); can ship in 7.x once native is already gone |
| **`Move Background` regresses Panel/Border** | Add per-type DPs *before* removing the FE DP; full control-render runtime test pass |
| **Hidden consumers of "internalized" members** | Public-API-diff gate (SC-003) + 6.x `[Obsolete]` warning window (FR-061) to surface external usage early |
| **`DataContext`→FE (`#13201`) binder rework** | Spike first; keep behind the gate; do not couple to the 7.0 critical path |
| **Reviving stale PRs verbatim reintroduces bugs** | NG5 — re-scope as fresh changes with new tests; never cherry-pick abandoned branches |
| **Silent behavioural breaks** (flags flipped) | FR-060 fails-before/passes-after tests; "prove the false-branch dead" before deleting any default-true/false flag |

---

## 11. Open Questions / Needs Clarification

- **OQ-1** *(`#17099` in 7.0 or 7.x?)* — Decide whether `DependencyObject`→class is committed to 7.0 or explicitly deferred to the first 7.x minor. Affects the release timeline most of any single item. [NEEDS DECISION]
- **OQ-2** *(`#14765` Fluent V1)* — Fully remove the leftover `Uno.UI.FluentTheme.v1` assembly + `XamlControlsResourcesV1` in 7.0, or mark the issue resolved-as-shipped? [NEEDS DECISION]
- **OQ-3** *(`#12322` Rename `Uno.UI.Toolkit`)* — Worth the consumer churn in 7.0 given zero parity payoff, or drop entirely? If kept, define the type-forwarder migration path. [NEEDS DECISION]
- **OQ-4** *(`#18875` Windows.UI.Input.*)* — In 7.0 or its own follow-up major? It is large and cross-cutting. [NEEDS DECISION]
- **OQ-5** *(`clr-namespace:`)* — Pending the "does WinUI support it under any scenario?" investigation before deciding removal. [NEEDS INVESTIGATION]

---

## 12. Assumptions

- The native-rendering removal (WS0) is committed for 7.0; this spec plans the breaking changes *around* it, not whether to do it.
- `feature/breaking-changes` remains the CI-configured integration branch for breaking changes (per #8339 history); native removal is staged on `dev/mazi/drop-native` and merges into it.
- WinUI/WinAppSDK API parity is the accepted decision criterion; "matches WinUI" outranks "Uno's version is cleaner."
- The local WinUI sources at `D:\Work\microsoft-ui-xaml2\src` remain the parity oracle for IDL/type checks.
- Skia-on-Android/iOS/WASM remain supported targets in 7.0; only the native *rendering* layer is removed.

---

## 13. References (public)

- Epic: [unoplatform/uno#8339](https://github.com/unoplatform/uno/issues/8339) — Breaking changes roundup
- Flagship: [`#17099`](https://github.com/unoplatform/uno/issues/17099) — [Skia/Wasm] Make `DependencyObject` a class
- Parity issues: [`#13201`](https://github.com/unoplatform/uno/issues/13201), [`#13096`](https://github.com/unoplatform/uno/issues/13096), [`#13083`](https://github.com/unoplatform/uno/issues/13083), [`#13046`](https://github.com/unoplatform/uno/issues/13046), [`#13050`](https://github.com/unoplatform/uno/issues/13050), [`#16148`](https://github.com/unoplatform/uno/issues/16148), [`#18875`](https://github.com/unoplatform/uno/issues/18875), [`#14478`](https://github.com/unoplatform/uno/issues/14478), [`#13326`](https://github.com/unoplatform/uno/issues/13326)
- Larger efforts: [`#18672`](https://github.com/unoplatform/uno/issues/18672), [`#12322`](https://github.com/unoplatform/uno/issues/12322), [`#14765`](https://github.com/unoplatform/uno/issues/14765), [`#5286`](https://github.com/unoplatform/uno/issues/5286)
- Building Uno: `doc/articles/uno-development/building-uno-ui.md`

---

*This spec was produced from a verified review of every open #8339 item against the current
`dev/mazi/drop-native` worktree and the WinAppSDK IDL. Line numbers reflect code at review time
(2026-06-08) and should be re-confirmed at implementation time.*
