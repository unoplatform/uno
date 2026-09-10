# Distilled diff: Layout Core types

**Source report:** _ComparisonReport_LayoutCore.md
**WinUI commit:** 4b206bce3
**Types covered:** Layout, NonVirtualizingLayout, VirtualizingLayout, OrientationBasedMeasures

## TL;DR

The Layout core types are functionally faithful, but the public API surface
is narrower than WinUI. The most impactful issues are visibility narrowing on
the `*Override` / `*Core` virtuals on `NonVirtualizingLayout` /
`VirtualizingLayout` (IDL-public, ported as `protected internal`) and on
`Layout.CreateDefaultItemTransitionProvider` (IDL-public-overridable, ported
as `protected internal virtual`). The OBM "missing methods" findings in the
source report are spurious — the helpers exist as per-class wrappers.

---

## Confirmed behavioural / correctness issues

None found in this set. The C++ → C# algorithmic translations
(switch-on-runtime-type dispatch in `InitializeForContext` / `Measure` /
`Arrange`, the `MeasureInvalidated` / `ArrangeInvalidated` event invocation
order, the `IndexBasedLayoutOrientation` debug-override path, and all of the
`OrientationBasedMeasures` math) are line-by-line faithful.

The weak-event extension in `Layout.uno.cs`
(`_measureInvalidatedHandlers` / `_arrangeInvalidatedHandlers`, lines 19-20)
fires before the WinUI-style event in
`Layout.mux.cs:215-221` / `:227-233`. This is order-preserving and is gated
behind `#if HAS_UNO`, so it has no observable effect on Skia parity.

---

## Missing functionality / interface gaps

### None

The source report flagged three "missing" items that are NOT real gaps:

- **OBM-6 (`GetScrollOrientation` / `SetScrollOrientation` missing on the
  interface)** — verified false. Each implementer
  (`StackLayout.uno.cs:23-25`, `FlowLayout.uno.cs:24-26`,
  `UniformGridLayout.uno.cs:20-22`) defines private
  `GetScrollOrientation()` / `SetScrollOrientation()` wrappers around its
  backing `_scrollOrientation` field, so the call sites compile and behave
  correctly. The C++ base-class accessors are mirrored 1:1 per derived
  type.
- **OBM-2 (missing `SetMajor` / `SetMinor` for `Size` / `Point`)** —
  verified false. A scan of the WinUI Repeater C++ for `(Major|Minor)(...)
  = ...` lvalue assignments matched only `Rect` targets
  (`FlowLayout.cpp:211,215`, `FlowLayoutAlgorithm.cpp:86,437,438,447,461,...`).
  All `Rect` mutators are already provided
  (`SetMajorSize`, `SetMinorSize`, `SetMajorStart`, `SetMinorStart`,
  `AddMinorStart`). No `Size` / `Point` lvalue mutations exist in the
  source.

---

## Visibility / API surface

### V-1 (High) — `*Override` / `*Core` virtuals narrowed from public-overridable to `protected internal virtual`

**File:**
`NonVirtualizingLayout.mux.cs:24,33,45,55`,
`VirtualizingLayout.mux.cs:25,34,46,56,69`

**WinUI IDL** (`ItemsRepeater.idl:299-321`) declares these as
`overridable` members of an `unsealed runtimeclass` without `protected`:
```
overridable void InitializeForContextCore(...);
overridable void UninitializeForContextCore(...);
overridable Size MeasureOverride(...);
overridable Size ArrangeOverride(...);
overridable void OnItemsChangedCore(...);   // VirtualizingLayout only
```
In WinRT projection, `overridable` on a public runtime class member projects
as `public virtual`. The Uno port narrows them to
`protected internal virtual`, so external consumers can no longer call
`layout.MeasureOverride(...)` directly even though that is part of the
public surface in WinUI.

**Fix:** `public virtual` (matches WinUI IDL projection).

### V-2 (Medium) — `Layout.CreateDefaultItemTransitionProvider` narrowed from public overridable virtual to `protected internal virtual`

**File:** `Layout.h.mux.cs:22`

WinUI IDL (`ItemsRepeater.idl:291-294`) places this in `[MUX_PUBLIC_V5]`
without `protected`, so the projection is `public virtual`:
```
[MUX_PUBLIC_V5]
{
    overridable ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider();
}
```
Uno port:
```csharp
protected internal virtual ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider() => null;
```
The accompanying comment ("widen to protected internal so ItemsRepeater can
invoke it") indicates the change was made to expose the method to
`ItemsRepeater` — but it is actually *narrowing* from public to
`protected internal`. Public consumers cannot call this.

**Fix:** `public virtual` (matches IDL).

### V-3 (Low) — `Layout.InvalidateMeasure` widened from `protected` to `protected internal`

**File:** `Layout.mux.cs:215`

IDL (`ItemsRepeater.idl:282`): `protected void InvalidateMeasure();`. Uno:
`protected internal void InvalidateMeasure()`. This leaks the method to all
Uno.UI assembly callers (it is invoked by `LayoutsTestHooks`). Note
`InvalidateArrange` was kept `protected` (line 227) — the change is also
inconsistent within the same file.

**Fix:** Restore `protected`. Either gate the test-hook call via a separate
non-public bridge, or use `InternalsVisibleTo` on the test assembly.

### V-4 (Info) — `LayoutId` is `internal` (correct)

The source report flagged this as L-1 (claimed `LayoutId` should be
`public`). Verified false. `LayoutId` is **not** in
`ItemsRepeater.idl` — it is a debug-only field
(`Layout.h:60-62` comment: "TODO: This is for debugging purposes only. It
should be removed when the Layout.LayoutId API is removed."). Uno's
`internal` keeps it out of the public surface, which matches the IDL.

---

## Lifecycle / leak risk

### LL-1 (Info, acceptable) — weak-event collections protect against dictionary leaks

`Layout.uno.cs:18-34` introduces
`WeakEventHelper.WeakEventCollection _measureInvalidatedHandlers` /
`_arrangeInvalidatedHandlers` to address a real lifetime problem: `Layout`
instances stored in long-lived dictionaries
(e.g., the `ItemsView` default-styles dictionary referenced in the file
header) would otherwise root every subscriber strongly. The standard
`event` add-handler path is preserved; the weak path is invoked first in
`InvalidateMeasure` / `InvalidateArrange` (`Layout.mux.cs:217-220,229-232`)
under `#if HAS_UNO`, then the strong event.

No leak risk — the design is the fix. Order is documented.

### LL-2 (None) — no finalizers, no revokers required

`Layout` owns only the two .NET events plus the weak-event collections;
no native interop or auto-revoke registrations are required. Matches WinUI
(C++ class has no destructor either).

---

## Dropped (rejected from source report)

- **L-1 `LayoutId` should be public** — `LayoutId` is not in
  `ItemsRepeater.idl`; debug-only field per Layout.h comment.
- **L-4 anonymous-namespace helpers `internal` vs `private`** — both
  `GetVirtualizingLayoutContext` / `GetNonVirtualizingLayoutContext`
  are static helpers used only from `Layout`; widening to `internal` has
  no public API impact and is harmless. Style-only.
- **L-5 / NVL-3 / VL-3 `#pragma region` → `//` comments** — IDE outlining
  only; no functional impact.
- **L-6 `LogItemIndexDbg()` mapped as method, not property** — both are
  `internal`, called only by test hooks. Style-only.
- **L-7 `SetLayoutAnchorInfoDbg` visibility (`private protected`)** —
  internal test hook; called only by same-assembly LayoutsTestHooks paths.
  No API impact.
- **L-8 explicit `= false` initializer elided** — C# default already
  matches. Cosmetic.
- **L-9 weak-event documentation** — already documented in
  `Layout.uno.cs:4-9` file header; cross-file comment is nice-to-have.
- **L-10 / L-11 / L-12 / L-13 / L-14 / L-15 / L-16 / L-17 / L-18** —
  comment placement, MUX header drift, XML doc, no destructor, etc.
- **NVL-2 / VL-2 dropped `__RP_Marker_ClassById`** — runtime profiler
  trace stub, not behavioural. Adding a `TODO Uno:` wrapper is
  style-only.
- **NVL-4 / VL-4 / OBM-10 / OBM-11 (info-level)** — file layout / header
  / no destructor; informational.
- **OBM-1 default `ScrollOrientation.Vertical` invariant** — each
  implementer (`StackLayout.uno.cs:14`,
  `FlowLayout.uno.cs:15`, `UniformGridLayout.uno.cs:11`) declares
  `private ScrollOrientation _scrollOrientation;`, which defaults to
  `Vertical` (first enum value). The invariant is preserved via the
  enum ordering, called out by the `// MUST be default` comment in
  `ScrollOrientation.cs`. No bug.
- **OBM-2 missing `SetMajor` / `SetMinor` for `Size` / `Point`** — no
  `Size` / `Point` lvalue assignment exists in the WinUI Repeater C++
  source. Verified by grep. Not a real gap.
- **OBM-3 `MinorMajorRect`/`Point`/`Size` parameters still `float`** —
  parameters silently widen to `double` on the `new Rect(...)`
  constructor; behaviour matches C++. Cosmetic.
- **OBM-4 `ScrollOrientation` enum file style** — file-scoped namespace
  preference; cosmetic.
- **OBM-5 `AddMinorStart` has no C++ counterpart** — justified
  workaround for the missing C# ref-return semantics; documented inline.
- **OBM-6 missing `GetScrollOrientation` / `SetScrollOrientation`** —
  Verified false. Each implementer provides private wrappers.
- **OBM-7 / OBM-8 / OBM-9** — informational only.
- **Cross-type observation 3 (`__RP_Marker` consistency)** — style.
- **Cross-type observation 4 (`Foo()` → property rule consistency)** —
  style; the methods involved are all `internal` or test-hook only.
