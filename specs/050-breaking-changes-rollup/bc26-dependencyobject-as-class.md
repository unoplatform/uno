# BC26 — Make `DependencyObject` a class (not an interface)

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Issue:** [#17099](https://github.com/unoplatform/uno/issues/17099) (OPEN, stale, originally scoped Skia/Wasm) · **Danger:** 4/5 · **Effort:** L · **Phase:** 7 (ship last, own PR) · **Status:** ✅ implemented

## TL;DR

`Microsoft.UI.Xaml.DependencyObject` used to be a **`public partial interface`** on every Uno target, with the `DependencyObjectGenerator` injecting the store + `GetValue`/`SetValue`/`IDependencyObjectStoreProvider`/`IDependencyObjectInternal`/`IWeakReferenceProvider` mixin into each implementing class. WinUI's `DependencyObject` is a **class**. This item re-rooted Uno's type hierarchy so `DependencyObject` is a real base class that `UIElement` (et al.) inherit directly.

The interface only ever existed because **native mobile `UIElement` had to inherit a native view** (`UIElement.Android.cs : BindableView`, `UIElement.UIKit.cs : BindableUIView`). With the **native targets being dropped**, that constraint disappears — the Skia/Wasm/reference `UIElement` partials already do `: DependencyObject` directly. So this change is *more natural and lower-risk than the 2024 issue framing implies*.

## Original state (before the change)

- `src/Uno.UI/UI/Xaml/DependencyObject.cs` declared `public partial interface DependencyObject` with **no platform `#if` guard** — it was an interface on all targets.
- `UIElement.skia.cs` / `.wasm.cs` / `.reference.cs` already declared `: DependencyObject`. Only the native partials inherited native views.
- `DependencyObjectGenerator` emitted the store mixin into every DO-derived class.

## What changes

1. Convert `DependencyObject` from interface → class (a `partial class` carrying the store and `GetValue`/`SetValue`).
2. Rework `DependencyObjectGenerator` to **stop emitting** the parts now provided by the base class, while keeping per-type generated bits that genuinely must stay per-type (e.g. property registrations).
3. Validate the entire control hierarchy + the binder/serialization partials (`DependencyObjectStore.cs`, `IDependencyObjectInternal.cs`).

## Pros

- **WinUI parity.** Code written against WinUI semantics (where `DependencyObject` is a class) breaks *less*, not more.
- **Removes the generator mixin complexity** that exists purely to fake class-like behaviour over an interface — less generated code, simpler debugging, smaller IL.
- **Enabled for free by the native-target drop** — the sole reason for the interface is going away regardless, so doing this now avoids leaving a vestigial interface behind.
- Potential perf/footprint win (no per-type re-emitted store plumbing; real fields on a base class).

## Cons / risks

- **Binary-breaking for every recompiled assembly** — `DependencyObject` is at the root of the whole hierarchy. Any pre-built library compiled against the interface shape must be rebuilt.
- Breaks **advanced/library** consumers that (a) hand-implement the `DependencyObject` interface on a non-derived type, (b) rely on interface-style multiple inheritance, or (c) reflect / source-gen over its interface shape.
- The generator rework is delicate: getting "what folds into the base vs what stays per-type" wrong produces subtle DP-resolution or weak-reference bugs across *every* control.

> Realistic source breakage for a *typical app* is narrow: using `DependencyObject` as a base/parameter/return type behaves identically whether class or interface.

## Decision (resolved)

- **End-state: a class on every remaining target**, not the Skia/Wasm-only scoping of the original 2024 issue. `src/Uno.UI/UI/Xaml/DependencyObject.cs` is now `public partial class DependencyObject : IDependencyObjectInternal, IWeakReferenceProvider`.
- **The whole mixin folded into the base class.** Rather than trimming what `DependencyObjectGenerator` emitted, the generator was removed outright (deleted in `c371084bb52`); DO detection is now by base type. Nothing DO-related is emitted per-type any more.
- Phase 2 went further than this spec anticipated: `DependencyObjectStore` was **dropped entirely** (uno-private#2210) and its storage folded onto `DependencyObject` itself — see `DependencyObject.Store.cs` / `.Binder.cs`.

## As implemented

- `src/Uno.UI/UI/Xaml/DependencyObject.cs` — interface → class.
- `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs` — **deleted**; DO-derived types are detected by base type instead.
- `DependencyObjectStore` — removed; storage now lives on `DependencyObject` (`DependencyObject.Store.cs`, `DependencyObject.Binder.cs`).
- Removals covered in `build/PackageDiffIgnore.xml` (regex entries keyed on `Microsoft.UI.Xaml.DependencyObjectStore`).
- Landed via unoplatform/uno#23537 and #23702; the `DependencyObjectStore` drop via uno-private#2210.
- Consumer- and contributor-facing docs updated: `doc/articles/api-differences.md`, `doc/articles/uno-development/uno-internals-overview.md`, `AGENTS.md`, `.claude/rules/dependency-properties.md`, `.claude/agents/architect.md`.

## Validation strategy

- Full `Uno.UI.RuntimeTests` pass on Skia Desktop + WASM; DP precedence, inheritance, weak-event, and binding suites are the canaries.
- `Uno.UI.SourceGenerators.Tests` golden regeneration + review of a representative generated control before/after.
- Smoke-test SamplesApp (control gallery) on Skia.

## Sequencing

Orthogonal to **BC58** (DataContext-on-FE-only) in principle, but both rewrite `DependencyObjectGenerator` output — **do BC58 first** to settle generator emission, which de-risks this change. Land as its own stabilized PR; never batch.
