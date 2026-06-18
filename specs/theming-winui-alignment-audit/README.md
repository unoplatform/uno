# Theming WinUI Alignment — Audit Findings & Remaining Work

> **Date:** 2026-06-18 · **Branch:** `dev/mazi/notifyanims` · **Reviewed commits:** the
> `dev/mazi/align-theming` merge (`529acfe4`, PR #23416) + the 7 post-merge `#23472` theming fixes
> (`bd89410340`, `044ebd3f49`, `6cac205035`, `5e14bb64ad`, `6ec52d0c4b`, `bcc8191375`, `54b2844f70`).
>
> **WinUI ground truth:** `D:\Work\microsoft-ui-xaml2\src`, commit `fc2f82117`.
>
> This spec is a **review output**, not a fresh design. It reconciles the shipped code against the two
> prior design specs ([`../theming-winui-alignment`](../theming-winui-alignment/README.md) and
> [`../theming-winui-resolution-scope`](../theming-winui-resolution-scope/README.md)) and against the
> WinUI C++ sources, and enumerates what still needs to change — both **adjustments to the current
> branch** and **follow-up work**.

## How this was produced

A multi-agent audit ground every theming-relevant production file against its WinUI C++ counterpart and
the relevant branch commits, across nine areas (Enter/Leave lifecycle; the `CDependencyObject::Theming`
slot writers; `FrameworkTheming`/interop/`Application`; `CThemeResource` + providing-dictionary pin;
`EnsureActiveThemeDictionary` + the slot reader + walk cache; FE high-contrast/`ActualThemeChanged`/foreground
freeze; **ALC theming preservation**; the freshest `#23472` fixes; and a definition-of-done reconciliation).
Every High/Medium finding was then **adversarially re-verified** by an independent agent reading both sides.
The verified register is [`findings.md`](./findings.md); the phased work is [`plan.md`](./plan.md).

## Executive summary

**The port is substantially faithful and behaviorally green.** Two structural halves of WinUI's mechanism
are genuinely live: (a) the per-DO theme is **established at tree Enter** (`depends.cpp:1023-1069` →
`DependencyObjectStore.mux.cs` `EstablishThemeOnEnterCore`), and (b) the core ambient slot
`CCoreServices::RequestedThemeForSubTree` exists (`CoreServices.Theming.cs`) and **the resolution leaf reads
it** on enhanced-lifecycle targets (`ResourceDictionary.GetActiveTheme` →
`IsThemeRequestedForSubTree() ? GetRequestedThemeForSubTree() : FrameworkTheming.GetBaseTheme()`, mirroring
`Resources.cpp:766-768`). `FrameworkTheming`, `SystemThemingInterop`, `NotifyThemeChange`, `CThemeResource`
(incl. `IsValueFromInitialTheme` and the **R1 providing-dictionary pin**), and `LookupThemeResource` are all
faithfully ported. `EstablishThemeAtEnter`, the theme-key-parameter-threaded leaf, and the resolution-time
ancestor walk are **gone**.

**The "two sources of truth" worry does not hold at the leaf.** Verified across four areas: the resolution
leaf reads **only** the slot. `ThemeResolution.ResolveOwnerTheme(owner)` is not a competing source — it
computes the value that the choke points **push onto that same slot** before resolving (the analog of WinUI's
`SetThemeResourceBinding` pushing `m_theme`), and the `ThemeWalkResourceCache` key is itself derived from the
slot. Parameter → slot → leaf is one chain. The `TODO Uno` claiming the cache is "not coupled to the slot" is
behavior-neutral (the coupling happens at the read site instead of the write site).

**But the README's top-line "ALL PHASES (0–8) COMPLETE" overstates the state**, and there is **one genuine
high-severity defect**.

### The one HIGH defect — runtime high-contrast is broken (current-branch)

Converged on independently by three areas (A3-2 **confirmed HIGH**, A6-3, A9-4). A runtime high-contrast
toggle — `WinRTFeatureConfiguration.Accessibility.HighContrast` setter, or an OS HC change — is **never routed
into `FrameworkTheming.OnThemeChanged`**, because nothing subscribes `AccessibilitySettings.HighContrastChanged`
to the theme pipeline (the only subscriber is `BackdropMaterial`) and the Win32 OS listener watches only the
light/dark `Personalize` key. Consequences:

- The `FrameworkTheming` HC snapshot stays stale → `HasHighContrastTheme()` / `IsHighContrastChanging()` stay
  `false`, and `FrameworkTheming.GetTheme()` omits the HC bit.
- **No `NotifyThemeChange` walk runs**, so already-materialized `{ThemeResource}` values never re-resolve to
  HC; `FrameworkElement.HighContrastChanged` (correctly added in this branch) is effectively **dead code**.
- **Two HC sources of truth**: the resolution leaf reads the **live** flag
  (`ResourceDictionary.cs:752/1421` → `SystemThemeHelper.IsHighContrast`) while `FrameworkElement.Theming`
  and `Application.Alc` read the **cached** `FrameworkTheming` snapshot (`FrameworkElement.Theming.cs:107/340/347`,
  `Application.Alc.cs:169`). The existing test only sets HC *before* first load, so the divergence is unguarded.

This is the top priority. See [`plan.md`](./plan.md) Phase 1.

### Current-branch adjustments (do now)

| Item | Severity | What |
|---|---|---|
| **HC runtime toggle** (A3-2/A6-3/A9-4) | **High** | Wire HC change → `Theming.OnThemeChanged`; make the leaf read the `FrameworkTheming` HC snapshot. |
| **Native secondary-ALC theme drop** (A7-1) | Medium | The unconditional `if (_isSecondaryAlcApplication) { … return; }` at the top of `SetExplicitRequestedTheme` no-ops a *native* secondary app's `RequestedTheme` (element pin is `#if __SKIA__\|\|__WASM__`). Gate the diversion to enhanced-lifecycle. |
| **Doc/policy fixes** (A9-5, A4-8, A4-9, A1-4/A2-6, stale citations) | Low | README over-claims completion; stale TODO/test comments; private `kahua-private` URLs in checked-in tests; several drifted C++ line citations. |

### Follow-up work (alignment completion)

| Item | Severity | What |
|---|---|---|
| **Modifier-being-set gate** (A2-4/A8-1/A8-2) | Medium | Port WinUI's per-property gate that *skips* the `SetValue(Local)` re-stamp under a held animation (`PropertySystem.cpp:1649-1651`); retire the counter-based `SuppressLocalCanDefeatAnimations` band-aid. Also covers the unguarded Enter-walk branch the band-aid misses (`mux.cs:280`). |
| **OS high-contrast variant** (A3-3/A5-6/A3-4) | Medium | White/Black/Custom collapsed to one bool; **no `HighContrastCustom` key exists**. Needs real OS system-color reading first. |
| **Slot ↔ walk-cache coupling** (A9-3) | Low | Port `SetSubTreeTheme` coupling (behavior-neutral today; completes the 1:1 port). |
| **Retire `ResolveOwnerTheme` / the owner-theme parameter** (A1-1/A1-2/A2-1/A5-4/A9-1) | Low | Blocked on per-DP InheritanceContext (#22949) so non-FE/standalone DOs establish `_theme` at Enter; then gate the slot push on `_theme != None` and delete the helper — **or** amend the DoD to sanction it. |
| **Resolution-scope completeness** (A4-1/A4-3/A4-4/A8-3) | Low | `GetDictionaryForThemeReference` canonicalization; `GetParentFollowPopups` hop (R2); construction-time `IsGlobal` + propagation to merged/theme children. |
| **ALC override source** (A4-2/A7-2/A8-4) | Low | `TryApplicationResourceOverride` / `LookupThemeResource` consult host `Application.Current`, not the resolving ALC's app. |
| **Misc** (A6-5 popup overlay, A6-6 double-invoke, A3-4/A3-5 dead palette code, A7-3 bootstrap walk, A1-5 VisualState theme source) | Low | See [`findings.md`](./findings.md). |

## ALC theming preservation (a first-class concern)

ALC theming is **correctly designed on the supported (Skia/WASM enhanced-lifecycle) targets and well-tested**,
but it now rides entirely on the element-level port — so element-level gaps regress ALC.

- There is **one** `FrameworkTheming` per process (on the singleton `CoreServices.Instance`, since `Uno.UI`
  loads only in the default ALC), owned by the host app. This is a deliberate, documented divergence from
  WinUI's per-`CCoreServices` `FrameworkTheming` (A3-1, **justified**).
- A **secondary-ALC app does not mutate** the shared `FrameworkTheming`; its explicit theme is per-instance
  (`_alcRequestedTheme`) and **pinned as an element-level `RequestedTheme` on the `AlcContentHost` boundary** —
  WinUI's per-island theming mechanism (`Application.Alc.cs:166-224`, A7-4, **verified correct**). High contrast
  is composed from the shared FrameworkTheming's global axis (correct — HC is OS-global).
- The slot, walk cache, and `ActualThemeChanged` listener table all live on the per-ALC `CoreServices`; the
  listener table is a **weak-keyed `ConditionalWeakTable`** so it never roots collectible-ALC elements
  (A6-8/A7-6, **verified ALC-safe**).

**ALC risks the alignment introduces or leaves open:**
1. **A7-1 (current-branch):** native secondary-ALC `RequestedTheme` is silently dropped.
2. **A3-2 (HIGH):** the stale HC snapshot also corrupts the **secondary-app** walk theme
   (`GetEffectiveWalkTheme` OR-s in `GetHighContrastTheme()`), so fixing HC fixes ALC HC too.
3. **A4-2/A7-2/A8-4 (low):** app-level overrides of system/theme keys resolve against the **host** app, not
   the secondary ALC's — widened by the new per-keyframe override path.
4. **A7-3 (low):** a secondary app's bootstrap can drive a host-wide theme walk.

**Because element-level theming is the ALC theme delivery mechanism, every Phase-1/2 fix below must keep the
ALC element-pin path green** (`Given_AlcContentHost.When_SecondaryAlcApp_ExplicitTheme_Then_IndependentFromHost`).

## Scope & doctrine (unchanged from the prior specs)

- **Enhanced-lifecycle (Skia + WASM) only.** Native Android/iOS stay OS + application theme only,
  behavior unchanged — but must keep compiling and behaving (A7-1 is exactly a native-path regression to fix).
- **WinUI is the oracle.** Every WinUI-runnable test must be green on `/winui-runtime-tests` before it judges
  Uno; Uno-only behaviors are confirmed in a throwaway WinUI probe, never by reasoning.
- **Resolution invariant (still holds):** a `{ThemeResource}` value is a pure function of
  *(key, owner effective theme, providing dictionary)*; the owner theme flows through the **single** core slot,
  and the providing dictionary is pinned at resolution. No process-global requested-theme stack.

## Documents

1. [`findings.md`](./findings.md) — the full verified discrepancy register (every finding with WinUI ref,
   Uno ref, post-verification verdict, disposition, and recommendation).
2. [`plan.md`](./plan.md) — phased, actionable work items with acceptance criteria and tests.
