# Distilled diff: ItemsRepeater

**Source report:** _ComparisonReport_ItemsRepeater.md
**WinUI commit:** 4b206bce3

## TL;DR

After verification against both code bases, the source report contains no real runtime
correctness bugs. The headline "Critical" claim about `OnItemTemplateChanged` branch
ordering is a false positive in Uno because neither `DataTemplate` nor
`DataTemplateSelector` implement `IElementFactoryShim`, so the initial `as` cast is
guaranteed to fall through. The only items worth keeping are minor cleanup
(stale commented-out fork in ctor) and one already-handled API surface concern
(Background DP comes from `FrameworkElement` in Uno).

## Confirmed behavioural / correctness issues

None — see "Dropped" section. All Critical/High claims in the source report were either
already correct, equivalent under Uno's runtime, or non-applicable (WinRT tracker plumbing).

## Missing functionality

None at the public API surface that affects consumers. The only IDL member
not redeclared on `ItemsRepeater` is `Background` / `BackgroundProperty`, but in Uno
`FrameworkElement` already exposes both (see `FrameworkElement.Interface.skia.cs`
line 81-91), so the property is reachable via inheritance. WinUI redeclares
because its `FrameworkElement` lacks it; Uno does not need to.

## Visibility / API surface

None. Public DPs, events, and methods (`GetElementIndex`, `TryGetElement`,
`GetOrCreateElement`, `PinElement`/`UnpinElement`, events `ElementPrepared`,
`ElementClearing`, `ElementIndexChanged`) all match the IDL with public visibility.

## Lifecycle / leak risk

None. WinUI's `auto_revoke`-based `m_itemsSourceViewChanged`,
`m_measureInvalidated`, `m_arrangeInvalidated` are intentionally replaced by
`SerialDisposable` revokers in `ItemsRepeater.uno.cs`, which are reset in the same
places the C++ code revokes (`OnDataSourcePropertyChanged`,
`OnLayoutChanged`). Verified `_layoutSubscriptionsRevoker.Disposable = null`
fires before re-subscription in `ItemsRepeater.mux.cs:746`/`:773`. WinUI's
`EnableTracking` is a WinRT tracker_ref / `IReferenceTracker` concern and has no
analogue in .NET GC, so its absence is correct.

## Cleanup worth doing (not bugs)

These are not behavioural defects, but removing them keeps the port aligned
with the target commit and avoids reviewer confusion. Listed here only because
the source report flagged them as M5/H6 and the underlying observation is
factually correct.

- **Stale `IsRS5OrHigher` fork in ctor.** `ItemsRepeater.mux.cs:33-40` carries
  commented-out `if (SharedHelpers.IsRS5OrHigher()) { ... } else { ... ViewportManagerDownLevel ... }`
  branches. WinUI commit `4b206bce3` (`ItemsRepeater.cpp:31`) has a single
  unconditional `m_viewportManager = std::make_shared<ViewportManagerWithPlatformFeatures>(this);`.
  These branches are dead and should be removed for a 1:1 match with the
  pinned commit.

## Dropped (verified false positives or pure noise)

- **C1 – `OnItemTemplateChanged` branch reordering causes wrong dispatch.**
  Rejected. Uno's first cast is `newValue as IElementFactoryShim`. Neither
  `DataTemplate` (`DataTemplate.cs:19`) nor `DataTemplateSelector`
  (`Controls/DataTemplateSelector.cs:8`) implement `IElementFactoryShim`, so
  the cast returns null and the DataTemplate / Selector branches execute in
  the same order as WinUI for the supported input set. Custom
  `IElementFactoryShim` implementations bypass the wrapper — which is the
  intended behaviour (the WinUI wrapper path also short-circuits for objects
  that already implement `IElementFactory`).
- **C1 – missing `wrapper->EnableTracking(this)`.** Rejected.
  `EnableTracking` is `tracker_ref` / `IReferenceTracker` plumbing used to
  break GC cycles in WinRT's reference-counted world. Uno is GC-managed; the
  call has no semantic equivalent. The source report (FactoriesAndSources
  comparison line 547) acknowledges this.
- **C2 – throw on null `newValue`.** The source report itself downgrades this
  to "not a real bug".
- **H1 – method order divergence.** Source report's own verification table
  confirms full order parity.
- **H2 – `s_XxxProperty` vs `XxxProperty` identity comparison.** Both reference
  the same `DependencyProperty` static field. Semantically identical.
- **H3 – `OnItemTemplateChanged` signature `object` vs `IElementFactory`.**
  Method body pattern-matches `DataTemplate` / `DataTemplateSelector` directly;
  forcing the cast at the call site would not change behaviour.
- **H4 – `#if !UNO_HAS_ENHANCED_LIFECYCLE` divergence in `OnLoaded`.**
  Intentional and documented Uno lifecycle wrapping; the source report
  agrees it is acceptable.
- **H5 – `_dataSourceSubscriptionsRevoker` cross-file dependence.** Documented
  Uno `SerialDisposable` pattern; the source report classifies as acceptable.
- **H6 – missing commented-out `m_measureInvalidated` placeholders.** Pure
  documentation suggestion, no runtime impact.
- **M1 / M2 – `#pragma region RepeaterTestHooks` markers.** Style-only, in
  the drop list per the brief.
- **M3 – missing `// TODO Uno: RuntimeProfiler` marker.** Style-only.
- **M4 – field initializer style (`m_viewManager{this}` vs ctor-init).**
  Acknowledged as a C++/C# language limitation, no behavioural impact.
- **M6 – `OnPropertyChanged` callback registration.** Source report marks as
  "Good".
- **M7 – `uint` vs `uint8_t` for `s_maxStackLayoutIterations` and
  `m_stackLayoutMeasureCounter`.** Counter is reset to 0 well below 60 and is
  only compared against `s_maxStackLayoutIterations`; the wider type cannot
  change behaviour. Use of `byte` would only be cosmetic.
- **M8 – `m_layoutOrigin` initialisation.** Source report itself confirms
  identical default value.
- **M9 – missing XML docs.** Style-only, in the drop list per the brief.
- **L1–L6 – Low items.** All style-only.
- **I1–I8 – Informational.** All deliberate Uno extensions or already
  acknowledged as correct. The Background concern (I8) is resolved by Uno's
  `FrameworkElement.BackgroundProperty` providing the same surface.
- **DP `nameof(...)` vs string literal.** Style-only, explicitly excluded in
  the brief's "Drop" list.
