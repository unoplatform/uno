# Phase 6 — handoff prompt

Operational handoff prompt for the next session (paste via `/goal`, or as the first message). It is
self-contained so it survives a context clear. Source of truth remains `architecture.md` / `plan.md` /
`tests.md` / `custom-theme.md`; this file front-loads the verified build/test commands and the carry-over
lessons from Phases 0–5 — especially the **Phase 5 native-scope decision: element-level theming is
Skia/WASM-only; native (Android/iOS) supports OS theme + application theme only.**

NDA-safe: uses only the generic S1–S5 scenario labels and the public uno #23177; the private
scenario↔issue mapping lives only in the gitignored `baseline-results.md`.

---

```
Implement Phase 6 of the WinUI theming-alignment refactor — "Application/OS/custom-theme/high-contrast
precedence (D7, D8)" — on branch dev/mazi/theming-winui (worktree D:\Work\uno). Phases 0,1,2,3,4,5 committed
(later commits may be local; do NOT push). Drive with superpowers:executing-plans. Do Phase 6 IN FULL, then
STOP (not Phase 7). This phase is APP/OS-LEVEL precedence (app-vs-OS theme, custom-theme ditch, high-contrast
composition) — it is in scope for native, which supports OS + application theme — but do NOT add element-level
(per-object) theme machinery to the native path (that stays Skia/WASM; see CARRY-OVER).

READ FIRST (truth; survive context clear). Specs under specs/theming-winui-alignment/:
- architecture.md (§3 "The precedence rules WinUI uses" — FrameworkTheming::GetTheme app-vs-OS, ActualTheme,
  the Theme bit-composition base|highContrast; §4 rows D7+D8; §6 "Themes.Active is retained, but demoted";
  §7 "OS/theme leak narrative").
- plan.md — "Global conventions" + ONLY "Phase 6" (steps 1-6, files, WinUI refs, acceptance, commits).
- custom-theme.md — the DECISION box: **Option B (DITCH) is selected** (maintainer decision). Phase 6 implements
  the ditch; do NOT re-litigate A vs B.
- tests.md (§A T9 = OS-following-suppressed-when-app-theme-explicit; T10 = element-Dark-island + dark-"Default"
  fallback; §B/§C). T9/T10 are Uno-only → confirm expected values in a WinUI probe app (see Global conventions).
- THEMING-PROGRESS.md (Phase 0–5 evidence log — read the Phase 5 entry AND its "Native scope" bullet in full).
- baseline-results.md (gitignored; Phase 0 baseline + S1-S5 ↔ private IDs — local only).

DONE — DO NOT REDO (Phases 0–5 committed):
- Phase 1 (D1): per-object _theme + Get/SetTheme + IsProcessingThemeWalk on DependencyObjectStore;
  ThemeResolution.ResolveOwnerTheme(owner) (own theme → nearest themed inheritance ancestor →
  Application.ActualElementTheme fallback; never None).
- Phase 2 (D2+D4): DependencyObjectStore.EstablishThemeAtEnter() (port of EnterImpl depends.cpp:1023-1048) runs
  from the enhanced-lifecycle Enter walk; FrameworkElements inherit from their LOGICAL parent
  (FrameworkElement.Parent). _theme is no longer cleared on unload.
- Phase 3 (D3, Mechanism 1): resolution leaf is owner-theme-aware; the owner's effective theme is threaded as a
  parameter through the WHOLE chain (dictionary/merged, app/assembly/system retrieval, StaticResource aliases).
  Single choke point = DependencyObjectStore.UpdateThemeReference computing ResolveOwnerTheme(owner) once.
- Phase 4: ALL 11 band-aid pushes removed + the process-global requested-theme stack DELETED. GetActiveTheme()
  now just returns Themes.Active (the app fallback). A §B reflection leak-guard
  (Given_Theme_Materialization.When_Phase4_Global_Theme_Stack_Removed_Guard) fails if the push API/stack returns.
- Phase 5 (D5+D6): popup/flyout content is themed on the FIRST open from its logical opener at Enter (Skia/WASM);
  FlyoutBase.ForwardThemeToPresenter forwards the placement target's ActualTheme (Skia/WASM). ALL gated to
  UNO_HAS_ENHANCED_LIFECYCLE; T4/T5 GREEN on Skia/WASM and EXCLUDED on native; native flyout forwarding is a no-op.

CRITICAL CARRY-OVER (the premise for Phase 6):
- NATIVE SCOPE (maintainer decision, Phase 5): native (Android/iOS) supports OS theme + APPLICATION theme ONLY.
  Element-level theming (per-object _theme, Enter inheritance, popup/flyout element-theme forwarding) is a
  Skia/WASM (UNO_HAS_ENHANCED_LIFECYCLE) feature and is NOT brought to native. Phase 7 is now
  "confirm/document the native OS+app scope", NOT "native parity". Phase 6 (app-vs-OS precedence, custom-theme,
  HC) IS app/OS-level and therefore applies broadly — but keep it that way: do NOT introduce per-object/
  element-level theme code on the native path. App/OS theme, custom-theme removal, and HC detection are all
  app/OS-level concerns; the resolution leaf (GetThemeKey/GetActiveThemeDictionary) is already platform-neutral.
- There are NO theme pushes left anywhere (Phase 4). Do NOT add one. Resolution = f(key, owner's effective
  theme). Phase 6 changes the *inputs* to that theme (app-vs-OS precedence, HC bits, removing the custom-name
  axis), NOT the resolution mechanism.
- LINE NUMBERS ARE STALE in architecture.md/plan.md (Phases 2–5 shifted these files heavily). SEARCH BY NAME.
  Verified current anchors are listed under DO.
- The Theme enum (src/Uno.UI/UI/Xaml/Theming.cs) ALREADY defines BaseMask (0x03) + HighContrastMask (0x1C) + the
  HC variants + Theming.GetBaseValue (strips HC to base). So D8's bit-composition exists. What is MISSING for D8:
  (a) SETTING the HC bits — SystemThemeHelper has NO HC detection (its SystemTheme enum is Light/Dark only);
  Theming.FromElementTheme + ResolveOwnerTheme never OR in HC; and (b) the leaf SELECTING the HC sub-dictionaries
  — ResourceDictionary.GetThemeKey maps only base Light/Dark (it literally carries a "High-contrast composition
  is Phase 6 (D8)" comment), and GetActiveThemeDictionary has no HC branch.

DO (per plan.md §Phase 6; WinUI src D:\Work\microsoft-ui-xaml2\src; use /winui-port; WinUI line numbers below may
also drift — search by name there too). Recommended order:
1. D7 — app-vs-OS precedence. Verify Uno's chain matches FrameworkTheming::GetTheme() (FrameworkTheming.cpp:
   119-136): when the app set RequestedTheme (IsThemeSetExplicitly), an OS theme change must NOT flip the base
   theme; when not explicit, follow the OS. The Application.OnSystemThemeChanged `if (!IsThemeSetExplicitly)`
   guard is correct — KEEP it and add the regression test T9. Confirm ActualElementTheme + RequestedThemeForResources
   reflect this. SEARCH: Application.InitializeSystemTheme / UpdateRequestedThemesForResources / IsThemeSetExplicitly
   / SetExplicitRequestedTheme / OnSystemThemeChanged / ActualElementTheme / RequestedThemeForResources.
2. D7 — DITCH the custom-theme axis (custom-theme.md Option B). In Application.UpdateRequestedThemesForResources,
   remove the custom-name arm `(var custom, _) when !custom.IsNullOrEmpty() => custom` so RequestedThemeForResources
   / Themes.Active is strictly "Light"/"Dark" (+HC). Hard-deprecate ApplicationHelper.RequestedCustomTheme to an
   [Obsolete] no-op pointing to merged brush-override dictionaries (KEEP the property so existing code compiles).
   Confirm a "Light"/"Dark" custom name still resolves the standard theme (the harmless bucket). Document the
   breaking change: a genuine custom palette moves to a merged dictionary that overrides specific brush/color keys
   on top of the Light/Dark theme dictionaries. Element theme is then strictly Light/Dark (nothing custom to compose).
3. D7 — theme-dictionary fallback robustness. A dictionary that does NOT define the active theme key must fall back
   to the app's resolved BASE Light/Dark, NOT the raw "Default" sub-dictionary. Today
   ResourceDictionary.GetActiveThemeDictionary does `GetThemeDictionary(activeTheme) ?? GetThemeDictionary(Themes.Default)`,
   and Fluent's "Default" IS the Dark theme, so the old fallback silently yields dark (this is T10 part (b)). Match
   EnsureActiveThemeDictionary (Resources.cpp:687-819). SEARCH: GetActiveThemeDictionary / GetThemeDictionary / Themes.Default.
4. D8 — high contrast. Wire SystemThemeHelper HC detection per platform to SET the HC bits; OR HC into the app
   theme and into ResolveOwnerTheme; make GetThemeKey / GetActiveThemeDictionary select the HC sub-dictionaries
   FIRST when HC is active (EnsureActiveThemeDictionary HC branch, Resources.cpp:718-762). The Theme enum
   composition (base | highContrast) already exists (Theming.cs). HC is OS-level (within native's OS+app scope), so
   detection may be wired per platform where practical; if HC is currently entirely undetected, scope THIS phase to:
   detection + enum/app-theme composition + dictionary selection. Full HC brush parity may be a documented follow-up.
5. FrameworkApplication parity: confirm Application.RequestedTheme is settable only before app resources load (the
   Uno "before init" guard) matches FrameworkApplication.put_RequestedTheme (FrameworkApplication_Partial.cpp:
   981-1037); document.
6. Keep it app/OS-level. Do NOT add element-level / per-object theme code to the native path (native = OS + app).

VERIFIED CURRENT ANCHORS (search by name; these are post-Phase-5 positions, not guaranteed stable):
- src/Uno.UI/UI/Xaml/Application.cs: UpdateRequestedThemesForResources (the custom-name switch), RequestedThemeForResources
  (setter → ResourceDictionary.SetActiveTheme), ActualElementTheme, IsThemeSetExplicitly, SetExplicitRequestedTheme,
  InitializeSystemTheme, OnSystemThemeChanged (`if (!IsThemeSetExplicitly)` guard).
- src/Uno.UI/UI/Xaml/ResourceDictionary.cs: GetActiveThemeDictionary (the `?? GetThemeDictionary(Themes.Default)`
  fallback), GetThemeKey (base-only; "Phase 6 (D8)" comment), GetActiveTheme (== Themes.Active).
- src/Uno.UI/UI/Xaml/Theming.cs: the Theme enum (BaseMask/HighContrastMask + HC variants) + Theming.GetBaseValue /
  FromElementTheme / ToElementTheme.
- src/Uno.UWP/Helpers/Theming/SystemThemeHelper.cs (+ .Android.cs/.UIKit.cs/.wasm.cs/.skia.cs/.others.cs):
  GetSystemTheme returns Light/Dark only (no HC); the SystemThemeOverride test hook exists (Phase 0).
- src/Uno.UI/UI/Xaml/ApplicationHelper.cs: RequestedCustomTheme (public string).

PROTOCOLS (AGENTS.md): Root-Cause First (a leak after this phase = a wrong theme INPUT — app/OS precedence, a stale
custom key, or a missing HC/Default-fallback branch — never re-add a theme push); Validation Evidence (label
review/compile/runtime); Public-spec rule (no private trackers in committed artifacts); NDA S1-S5 labels only.
New .cs files: UTF-8 BOM + CRLF (edits to existing files preserve encoding via Edit). Branch only; do NOT push.

WinUI-first validation: T9/T10 are Uno-only (ThemeHelper.UseApplication*Theme / UseSystemThemeOverride /
RequestedCustomTheme have no WinUI equivalent) so they CANNOT run on /winui-runtime-tests. Confirm the expected
values in a throwaway native WinUI probe app (plan.md "WinUI probe app": Application.RequestedTheme, OS theme
toggle, element RequestedTheme/ActualTheme) and cite "confirmed in WinUI probe app" in the test.

BUILD/VALIDATE (verified working in the Phase 5 session):
- Build Uno.UI via the SamplesApp project (there is no src\Uno.UI\Uno.UI.Skia.csproj):
  dotnet build D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
  (~1 min incremental, ~2:20 clean; expect 0 errors + 4 pre-existing binding-redirect warnings, unrelated.)
- Runtime tests (Skia): PREFER the /runtime-tests skill (it builds, base64-encodes the filter, runs, and parses).
  LESSON (this cost a failed run in Phase 5): if you run manually, the --runtime-tests value MUST be a PLAIN
  RELATIVE path — --runtime-tests=theming-p6.xml — NOT a parenthesized PowerShell expression like
  --runtime-tests=(Join-Path $base ...), which mis-parses so the app launches in NORMAL mode and writes NO xml
  (exit 0, no results). One PowerShell call (the tool's cwd persists between calls — Set-Location into bin, use a
  relative results path, redirect output to a log so it isn't truncated):
    $filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization"
    $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($filter))
    Set-Location "D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0"
    dotnet SamplesApp.Skia.Generic.dll --runtime-tests=theming-p6.xml *> theming-p6.log
  Parse: [xml]$x=gc theming-p6.xml; $x.SelectNodes("//test-case") with result attribute Success/Failure.
- Baseline to HOLD: 144/145 on Skia (lone failure = the pre-existing GC flake
  When_Flyout_Closed_Target_Does_Not_Hold_Flyout — fails on baseline too). §B guard stays green. T9/T10 are Uno-only
  (#if HAS_UNO) and Skia/WASM + [RequiresFullWindow]; most of T9/T10 are already green guards — Phase 6 turns the
  still-red parts green (T9 OS-suppression assertion; T10 part (b) dark-"Default"-fallback).
- Whitespace before staging: dotnet format whitespace src\Uno.UI --folder --include <changed files> --verify-no-changes
  (and src\Uno.UI.RuntimeTests --folder --include <test files> for new/edited tests). Debug.Assert is COMPILED OUT
  in -c Release. WASM head = SamplesApp.Skia.WebAssembly.Browser (same Uno.UI.Skia assembly, so Skia is
  representative); /winui-runtime-tests deferred — don't fight local VS.

ACCEPTANCE:
- Builds clean on Skia.
- T9 (When_App_Theme_Explicit_OS_Change_Is_Suppressed): an explicit app theme is NOT flipped by a simulated OS
  change (IsThemeSetExplicitly guard); bound values stay the app theme — GREEN.
- Custom-theme ditch: a custom name other than "Light"/"Dark" no longer keys ThemeDictionaries["Foo"]
  (Themes.Active is strictly Light/Dark+HC); RequestedCustomTheme is an [Obsolete] no-op; "Light"/"Dark" still
  resolve as the standard themes. New test: app custom name + an element RequestedTheme="Dark" resolves the standard
  Dark dictionary, and a key missing from the active theme dict does NOT silently resolve the dark "Default"
  (T10 part (b)).
- D8: at least a dictionary-selection-level HC test green (HC sub-dictionary chosen when HC is active).
- Full theming suite + Given_Theme_Materialization stay at the 144/145 Skia baseline; the §B guard stays green.
- Native theming changes only by the intended app/OS/custom/HC precedence (app-level); NO element-level/per-object
  theme code is added to the native path.

WORKFLOW: scoped dotnet format whitespace --verify-no-changes on changed files before staging. Commit each
validated group (Conventional Commits + Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>);
do NOT push. Suggested commits per plan.md: `fix(theming): align app/OS theme precedence with FrameworkTheming
(D7)`; `feat(theming): ditch app-level custom-theme axis (D7)`; `feat(theming): compose high-contrast with base
theme (D8)`. Update THEMING-PROGRESS.md (check Phase 6, add evidence log) as a separate docs commit.
```
