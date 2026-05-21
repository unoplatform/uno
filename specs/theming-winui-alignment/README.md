# Theming WinUI Alignment

A phased plan to align Uno Platform's application- and element-level theming **1:1 with WinUI**, replacing the current "push the right theme at every materialization site" approach (11 band-aids and counting) with WinUI's model: **every `DependencyObject` carries a resolved theme established at tree-`Enter`, inherited from its (logical) parent, and every `{ThemeResource}` resolves against that owner theme.**

## Why

Element-level theming (PR #22803) + lazy theme-resource eval (PR #22887) shipped, then required a stream of fixes (#22887, #23127, #23178, #23197, #23243) and still leaves open production bugs (several internally-tracked theming regressions, described generically as scenarios S1–S5 in `architecture.md`, plus public regression uno #23177). They are all the **same defect**: the resolution leaf chooses the Light/Dark sub-dictionary from a **process-global ambient** (`ResourceDictionary.GetActiveTheme()` → a static stack / `Themes.Active`) instead of the resolving element's own theme. Any materialization path without a manual theme-push leaks the ambient/OS theme.

## Documents (read in this order)

1. **[`architecture.md`](./architecture.md)** — root-cause analysis, the WinUI model (with C++ `file:line` refs), the Uno-vs-WinUI discrepancy table (D1–D8), the 11 band-aid inventory, and the target design + the one decision to confirm (Mechanism 1 vs 2).
2. **[`plan.md`](./plan.md)** — the 9 phases (0–8), each self-contained: goal, dependencies, WinUI refs, files with `file:line`, steps, acceptance criteria, validation commands, commits. Includes the issue→phase traceability table and global build/test conventions.
3. **[`tests.md`](./tests.md)** — the regression suite (T1–T10) mapping each reported issue to a deterministic runtime test, the platform/parity matrix, and what existing coverage to keep green.
4. **[`custom-theme.md`](./custom-theme.md)** — the one still-open go/no-go: precise definitions of "preserve-minimal" vs "ditch" for Uno's app-level custom-theme feature, with costs and breaking-change scope.

## The fix in one paragraph

Move per-object theme from `UIElement` to `DependencyObjectStore` so every DO has it (D1, Phase 1). Establish that theme at tree-`Enter` from the logical inheritance parent, on all platforms, and stop clearing it on unload (D2/D4, Phase 2). Make the resolution leaf a pure function of (key, owner theme) by threading the owner's effective theme into `ResourceDictionary.TryGetValue` / `ThemeResourceReference.RefreshValue` / `ThemeWalkResourceCache` (D3, Phase 3). Delete the global theme stack and all 11 push band-aids (Phase 4). Then: popup/flyout logical-parent inheritance + flyout `ActualTheme` forwarding (D5/D6, Phase 5), app/OS/custom/high-contrast precedence (D7/D8, Phase 6), native parity (Phase 7), and full cross-platform + WinUI-parity validation (Phase 8).

## How to drive implementation agents

- **One phase per agent, strictly in order.** Do not start phase N+1 until N's acceptance criteria pass.
- Give each agent: `architecture.md`, `tests.md`, and **only its phase section** of `plan.md`, plus the "Global conventions" block at the top of `plan.md`.
- Tell every agent to obey `AGENTS.md`'s **Root-Cause First Debugging** and **Validation Evidence** protocols, to **work only on the current branch** (ignore other branches and any older theming plans), and to **commit continuously** (Conventional Commits, Co-Authored-By trailer).
### Resolved decisions (baked into the plan)

- **Resolution mechanism = Mechanism 1 (thread the owner theme as a parameter)**, under a hard **zero-behavior-difference constraint**: Phase 3 must *prove* via a transitional choke-point assert that the parameter never selects a different theme than today's resolution before Phase 4 deletes the global stack. See `architecture.md` §6.
- **Element vs custom-theme composition = standard Light/Dark** (element `RequestedTheme` selects the built-in Light/Dark dicts; custom theme stays app-level-only; no new element-level custom-theme API).
- **Missing-theme-key fallback = app base Light/Dark** (not the dark `"Default"` dictionary — that fallback was a likely dark-leak source).
- **Custom-theme feature = DITCH** ([`custom-theme.md`](./custom-theme.md)). `RequestedCustomTheme` is hard-deprecated to a no-op; `Themes.Active` becomes strictly Light/Dark/HC; custom palettes use merged brush-override dictionaries. Only breaks apps using a non-`Light`/`Dark` custom name (none observed; the only custom-theme usage seen in practice passes `"Light"`, which is unaffected).

## Definition of done

- All theming runtime tests + `Given_Theme_Materialization` (T1–T10) green on Skia Desktop, WASM, Android, iOS.
- WinUI parity confirmed via `/winui-runtime-tests` for nested themes, dynamic children, popup/flyout, and app-vs-element precedence.
- No `PushRequestedThemeForSubTree` and no process-global requested-theme stack anywhere in the codebase.
- Resource-dictionary benchmarks neutral or improved.
- Scenarios S1–S5 and public regression uno #23177 each have a green regression test (see traceability table in `plan.md`).
