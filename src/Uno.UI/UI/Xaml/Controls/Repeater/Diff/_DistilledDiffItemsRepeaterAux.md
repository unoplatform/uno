# Distilled diff: ItemsRepeater Aux types

**Source report:** _ComparisonReport_ItemsRepeaterAux.md
**WinUI commit:** 4b206bce3

## TL;DR

The only confirmed behavioural bug is that `HorizontalAnchorRatio` /
`VerticalAnchorRatio` on `ItemsRepeaterScrollHost` are declared `internal`
in Uno but the WinUI IDL exposes them as **public** `Double { get; set; }`.
Beyond that, the source report's "base class should be `Panel`" finding is
wrong (IDL says `FrameworkElement`), the `ConfigurationChanged` extra field
is a minor leak (event is never raised), and everything else is style/layout.

## Confirmed behavioural / correctness issues

### 1. `ItemsRepeaterScrollHost.HorizontalAnchorRatio` / `VerticalAnchorRatio` are `internal` (IDL says public)

- **File:** `ItemsRepeaterScrollHost.mux.cs:99-101`
- **Uno:**
  ```csharp
  internal double HorizontalAnchorRatio { get; set; }
  internal double VerticalAnchorRatio { get; set; }
  ```
- **WinUI IDL** (`ItemsRepeater.idl:387-388`):
  ```idl
  Double HorizontalAnchorRatio { get; set; };
  Double VerticalAnchorRatio { get; set; };
  ```
- **Why it matters:** Public API surface regression vs WinUI contract.
  External consumers that set these on `ItemsRepeaterScrollHost` (which is
  itself a public class) cannot reach them.
- **Fix:** change both to `public`. The `IScrollAnchorProvider` interface
  declares them too, so this also fixes any explicit-interface confusion.

## Missing functionality

None. All public-surface methods and properties listed in
`ItemsRepeater.idl` for `ItemsRepeaterScrollHost`
(`ItemsRepeaterScrollHost()`, `ScrollViewer { get; set; }`,
`CurrentAnchor { get; }`, `HorizontalAnchorRatio`, `VerticalAnchorRatio`)
are present in Uno. The `IRepeaterScrollingSurface` and
`IScrollAnchorProvider` interfaces are fully implemented (incl.
`ConfigurationChanged` add/remove, even if the event is never raised — same
as WinUI). `RepeaterAutomationPeer` matches its IDL surface (public ctor +
override of `GetChildrenCore` / `GetAutomationControlTypeCore`).

## Visibility / API surface

- `HorizontalAnchorRatio` / `VerticalAnchorRatio` — see issue #1 above.
- **Base class is correct.** The source report flags
  `ItemsRepeaterScrollHost : FrameworkElement` (Uno) vs `Panel` (WinUI .h)
  as a "substantive contract break". This is **wrong**:
  `ItemsRepeater.idl:379` declares
  `runtimeclass ItemsRepeaterScrollHost : Microsoft.UI.Xaml.FrameworkElement`.
  The C++ uses `DeriveFromPanelHelper_base` only as an internal MUX helper
  to gain access to `Children()` for storing the single `ScrollViewer`
  child — the *public* IDL contract is `FrameworkElement`. Uno's
  `FrameworkElement` base + `VisualTreeHelper.AddChild/ClearChildren` is a
  legitimate implementation strategy. Keep as-is.
- EventArgs constructors widened to `internal` — matches the IDL (which
  doesn't expose them). Fine; IDL has no ctor surface.
- `ItemsRepeaterElementIndexChangedEventArgs.Update(UIElement, in int, in int)`
  — non-idiomatic `in` modifier on `int`, but `Update` itself is internal
  (the method is `public` on a class whose ctor is internal, but the type
  is only ever produced by ItemsRepeater). Functionally equivalent at call
  sites; not a behavioural bug. Recommend dropping `in` for style.

## Lifecycle / leak risk

### `ItemsRepeaterScrollHost.m_configurationChanged` field retains handlers indefinitely

- **File:** `ItemsRepeaterScrollHost.h.mux.cs:102` and `.mux.cs:167-171`
- **Uno:** adds a private `event ConfigurationChangedEventHandler
  m_configurationChanged;` and the `IRepeaterScrollingSurface.ConfigurationChanged`
  add/remove stores/removes handlers in it.
- **WinUI** (`ItemsRepeaterScrollHost.cpp:151-160`): add returns `{}`,
  remove is a no-op — handlers are deliberately dropped on the floor.
- **Why it matters:** Uno will retain subscribers for the lifetime of the
  `ItemsRepeaterScrollHost` even though the event is **never raised**
  anywhere in the file. Low impact (no observable behaviour change because
  the event never fires), but the field is a small retention leak and
  diverges from WinUI's "intentionally dropped" semantics.
- **Fix:** either delete the field and make `add`/`remove` empty (matching
  WinUI), or leave a `// TODO Uno:` comment noting the intent.

### Dead backing fields `m_horizontalEdge` / `m_verticalEdge`

- **File:** `ItemsRepeaterScrollHost.h.mux.cs:79-82`
- The named fields are kept under `#pragma warning disable 169, IDE0051`
  but unused — the auto-properties on `.mux.cs:99-101` have their own
  compiler-generated backing fields. The values are stored correctly in
  those auto-property backing fields, so behaviour is fine. Once issue #1
  is fixed to keep auto-properties, drop these dead fields entirely (or
  switch the properties to explicit `get => m_horizontalEdge; set =>
  m_horizontalEdge = value;` form to reach 1:1 parity with `.cpp:75-93`).

## Dropped (rejected from source report)

- **"Base class is `FrameworkElement` not `Panel` — substantive contract
  break"** — wrong; IDL declares `FrameworkElement`.
- **"`ScrollViewer` accessor uses `VisualTreeHelper` not `Panel.Children`"**
  — implementation strategy consistent with the correct `FrameworkElement`
  base; not a contract break.
- **File-layout collapse on three EventArgs files** — style/rule #2 only.
- **Missing MUX Reference headers on EventArgs** — style; no behaviour.
- **`#region` vs `// #pragma region`** — style.
- **Constructor widened to `internal`** on EventArgs — IDL has no ctor;
  internal is the correct narrowing for framework-only instantiation.
- **Method-order rearrangement in `ItemsRepeaterScrollHost.mux.cs`** —
  cosmetic; rule #3 violation but no behaviour delta.
- **`m_viewType` field missing from `ItemsRepeaterElementPreparedEventArgs`**
  — WinUI never reads or writes it; it is dead state in WinUI too.
- **`Update(in int, in int)`** — style only; behaviour identical for
  `int`-sized value types.
- **`ArrangeOverride` carries pre-RS5 anchoring path under
  `#if !SCROLLVIEWER_SUPPORTS_ANCHORING`** — intentional Uno fallback for
  platforms whose `ScrollViewer` does not natively anchor; the active
  branch matches what WinUI shipped before the RS5 simplification.
  Cosmetic gating only (could be cleaner with `#if HAS_UNO` + TODO).
- **`auto_revoke` → `IDisposable` instead of `SerialDisposable`** — rule
  #7 style; dispose-on-reassign already handled explicitly in the
  `ScrollViewer` setter.
- **`childrenPeers.Count` unsigned/signed mismatch** in
  `RepeaterAutomationPeer` — not a real issue at any realistic peer count.
- **`(lhs, rhs) => lhs.Key - rhs.Key`** subtraction comparator — only an
  issue at `int.MaxValue / 2` element indices.
- **Trace string formatting differences** — informational only.
- **Comment paraphrasing / whitespace / region marker style** — style.
