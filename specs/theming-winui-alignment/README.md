# Theming WinUI Alignment — Exact 1:1 Port

> **Status (2026-06-10): Phases 0–7 complete** on `dev/mazi/align-theming`. The contract suite holds at 155/156 on Skia Desktop (the lone failure is a pre-existing environment-dependent GC-leak test, red on the base). The developer-facing model is documented at `doc/articles/uno-development/theming-model.md`. Phase 8 validation in progress (WASM suite, native compile, WinUI parity, benchmarks).

Align Uno Platform's system/application/element theming **structurally and behaviorally 1:1 with WinUI**, by porting WinUI's actual theming machinery from the C++ sources (`D:\Work\microsoft-ui-xaml2\src`, commit `fc2f82117`) following the `/winui-port` rules — instead of approximating WinUI behavior with Uno-specific mechanisms.

## Base and branches

Work happens on **`dev/mazi/align-theming`**, which sits **on top of `origin/dev/mazi/theming-winui`** (`ffd6ee2631`). That base is the completed prior effort and is kept because it contains:

- the **regression test coverage** (`Given_Theme_Materialization` T1–T10, `ThemeHelper.UseSystemThemeOverride`, OS-independent theming suite),
- the **deletion of the global requested-theme stack and all push band-aids** (including reverts of band-aids that had been merged to master),
- per-DO theme storage on `DependencyObjectStore`, the `Theme` enum, `ThemeResourceReference`/`ThemeWalkResourceCache` ports, and a partial `EnsureActiveThemeDictionary` port.

What this effort **replaces** is the base's *mechanism*, which remains an approximation of WinUI rather than a port of it:

1. **`ThemeResolution.ResolveOwnerTheme`** resolves an owner's theme by walking the inheritance-parent chain **at resolution time**. WinUI never walks at resolution time — `m_theme` is authoritative because `CDependencyObject::EnterImpl` established it at tree-Enter.
2. **The resolution leaf takes the theme as a parameter** threaded through `TryGetValue`/`GetActiveThemeDictionary`. WinUI's `EnsureActiveThemeDictionary` reads the **core ambient slot** (`CCoreServices::RequestedThemeForSubTree`), which is written from the owner's `m_theme` at exactly three places.
3. **`EstablishThemeAtEnter`** is a theme-only bolt-on walk. WinUI has no such thing — theme establishment is the tail of the real **`CDependencyObject::EnterImpl`**, whose enter-property walk also enters every DO-valued property value.
4. **The `NotifyThemeChanged` walk lives on `FrameworkElement`.** In WinUI it is a `CDependencyObject` method (`components/DependencyObject/Theming.cpp`), with `CFrameworkElement` adding only its documented overrides.
5. **No `FrameworkTheming`** — app/OS theme state is scattered across `Application.cs`; `Themes.Active` is a Uno-specific global.
6. **`EnterParams`/`LeaveParams` carry 3 of WinUI's 8 fields**, and Enter/Leave exists only at the `UIElement` level.

## Documents (read in this order)

1. **[`architecture.md`](./architecture.md)** — the WinUI model with C++ `file:line` refs, the base-branch inventory (kept vs replaced), the C++→C# port-mapping table, and the resolved design decisions.
2. **[`plan.md`](./plan.md)** — phases 0–8, each self-contained: goal, WinUI refs, files, steps, acceptance criteria, validation, commits.
3. **[`tests.md`](./tests.md)** — the regression suite (T1–T10, present on this base) and validation matrix.
4. **[`custom-theme.md`](./custom-theme.md)** — resolved decision record (custom-theme feature is ditched).
5. **[`prior-effort/`](./prior-effort/)** — historical records of the superseded approach (parity audit, performance review). Background only; do not derive implementation guidance from them.

## Standing decisions (carried over, still valid)

- **Custom-theme feature = DITCH** (`custom-theme.md`): `ApplicationHelper.RequestedCustomTheme` hard-deprecated to a no-op; theme keys are strictly Light/Dark (+ HC). Custom palettes use merged brush-override dictionaries.
- **Missing-theme-key fallback**: WinUI's order is authoritative — requested/base theme key first, then `"Default"` (`Resources.cpp:764-791`). Port it exactly.
- **Native scope**: element-level theming (per-DO theme, `Enter` inheritance, popup/flyout element theming) is an enhanced-lifecycle (Skia/WASM) feature. Native Android/iOS stay OS + application theme only, behavior unchanged (maintenance-only targets).
- **WinUI is the oracle**: every WinUI-runnable test must be green on `/winui-runtime-tests` before it judges Uno; Uno-only behaviors are confirmed in a throwaway WinUI probe app, never by reasoning.

## Definition of done

- The port-mapping table in `architecture.md` is fully realized: each listed C++ source has its C# counterpart at the mapped location with a `MUX Reference` header, matching member order, and no silently-dropped logic (`TODO Uno:` notes where Uno genuinely lacks a dependency).
- `Enter`/`Leave` runs at the DependencyObject level (via `DependencyObjectStore`) for every DO on enhanced-lifecycle targets, including DO-valued property values; the `depends.cpp` theme block is live code inside the real `EnterImpl`.
- `ThemeResolution.cs`, `EstablishThemeAtEnter`, the theme-parameter-threaded leaf, and `Themes.Active` are gone; the only ambient is `CoreServices.RequestedThemeForSubTree` set at WinUI's exact points, and app/OS state lives in `FrameworkTheming`.
- All theming runtime tests + `Given_Theme_Materialization` (T1–T10) green on Skia Desktop and WASM; native-applicable subset green on Android/iOS with native behavior unchanged; WinUI parity confirmed via `/winui-runtime-tests`.
- Resource-dictionary benchmarks neutral or improved.
