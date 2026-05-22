# Phase 5 — handoff prompt

Operational handoff prompt for the next session (paste via `/goal`, or as the first message). It is
self-contained so it survives a context clear. Source of truth remains `architecture.md` / `plan.md` /
`tests.md`; this file just front-loads the verified build/test commands and the carry-over lessons from
Phases 0–4 (especially the Phase 4 result: Mechanism 1 is now complete — resolution is a pure function of
(key, owner's effective theme), so Phase 5 is purely about establishing the *right* owner theme for
popup/flyout content).

NDA-safe: uses only the generic S1–S5 scenario labels and the public uno #23177; the private
scenario↔issue mapping lives only in the gitignored `baseline-results.md`.

---

```
Implement Phase 5 of the WinUI theming-alignment refactor — "Popup/Flyout logical-parent inheritance +
flyout ActualTheme forwarding (D5, D6)" — on branch dev/mazi/theming-winui (worktree D:\Work\uno).
Phases 0,1,2,3,4 committed (later commits may be local; do NOT push). Drive with
superpowers:executing-plans. Do Phase 5 IN FULL, then STOP (not Phase 6). This phase is the CROSS-PLATFORM
logic + the flyout-forwarding fix; native tree-attach specifics are Phase 7 (keep the new logic
platform-neutral, leave a clearly-marked TODO + #if for native attach where one isn't yet uniform).

READ FIRST (truth; survive context clear). Specs under specs/theming-winui-alignment/:
- architecture.md (§4 rows D5+D6 — the two defects; §7 "S4 (popup first open)" — the target fix narrative;
  §3 Difference A + the "Enter inheritance parent is the LOGICAL parent for FrameworkElements … so
  popups/flyouts follow the theme of the control that opened them, not the PopupRoot" rule).
- plan.md — "Global conventions" + ONLY "Phase 5" (steps 1-4, files, WinUI refs, acceptance, commit).
- tests.md (§A T4/T5 = the popup/flyout first-open repros + exact filter; §B leak guard + sibling-isolation;
  §C validation matrix — T4/T5 are GREEN on Skia/WASM as band-aid-free guards and RED→green on native in P7).
- THEMING-PROGRESS.md (Phase 0–4 evidence log — read the Phase 4 entry in full; it documents that the global
  theme stack is gone and resolution now keys on the owner theme threaded through the WHOLE chain incl.
  StaticResource aliases).
- baseline-results.md (gitignored; Phase 0 baseline + S1-S5 ↔ private IDs — local only).

DONE — DO NOT REDO (Phases 0–4 committed):
- Phase 1 (D1): per-object _theme + Get/SetTheme + IsProcessingThemeWalk on DependencyObjectStore;
  ThemeResolution.ResolveOwnerTheme(owner) (own theme → nearest themed inheritance ancestor →
  Application.ActualElementTheme fallback; never None).
- Phase 2 (D2+D4): DependencyObjectStore.EstablishThemeAtEnter() (port of EnterImpl depends.cpp:1023-1048)
  runs from the enhanced-lifecycle Enter walk; _theme no longer cleared on unload.
- Phase 3 (D3, Mechanism 1): the resolution leaf is owner-theme-aware (theme threaded as a parameter).
- Phase 4: ALL 11 band-aid pushes removed + the process-global requested-theme stack DELETED. {ThemeResource}
  resolution is now f(key, owner's effective theme) threaded through the entire chain — dictionary/merged,
  app/assembly/system retrieval, and StaticResource aliases (TryResolveAlias/ResolveResourceStatic/
  TryVisualTreeRetrieval/TryTopLevelRetrieval/Try{System,Assembly}ResourceRetrieval are theme-aware), the
  analog of WinUI core->LookupThemeResource(theme, key). GetActiveTheme() now returns the app-fallback
  Themes.Active. A §B reflection leak-guard (Given_Theme_Materialization.When_Phase4_Global_Theme_Stack_
  Removed_Guard) fails if the push API/stack is reintroduced. Skia suite = 144/145 (lone failure = the
  pre-existing GC flake When_Flyout_Closed_Target_Does_Not_Hold_Flyout).

CRITICAL CARRY-OVER FROM PHASE 4 (the premise for Phase 5):
- There are NO theme pushes left anywhere. Do NOT add one. Popup/flyout content is themed CORRECTLY iff its
  per-object theme is ESTABLISHED correctly at Enter — once GetTheme() is right, resolution follows
  automatically (the leaf keys on the owner theme, including for nested {ThemeResource}/StaticResource-alias
  brushes). So Phase 5 is about WHERE the popup/flyout content inherits its theme FROM, not about resolution.
- The opener relationship is the LOGICAL inheritance parent. WinUI (depends.cpp:1023-1048 +
  GetInheritanceParentInternal(fLogicalParent), framework.cpp:3097-3130) inherits popup/flyout content theme
  from the control that opened it, NOT the PopupRoot. EstablishThemeAtEnter currently uses the store's
  inheritance Parent; Phase 2 explicitly DEFERRED the exact logical-parent-for-popups linkage to THIS phase
  (see the comment in DependencyObjectStore.EstablishThemeAtEnter citing "Popup.WithPopupRoot.cs"). Today
  popup content is themed explicitly by Popup open code — generalize that into the Enter logical-parent path.
- LINE NUMBERS ARE STALE. architecture.md §4/§9 and plan.md cite line numbers from before Phases 2/3/4
  shifted these files heavily (Phase 4 alone net -131 LOC across FrameworkElement(.Theming).cs,
  ResourceDictionary.cs, ResourceResolver.cs, etc., and re-pointed the control-level UpdateThemeBindings
  overrides incl. Popup). SEARCH BY NAME, never by line.
- A useful Phase 4 precedent for "establish theme at materialization from the logical parent": template
  content is themed at tree Enter from its templated (logical) parent — the repro for that
  (When_DataTemplate_LoadContent_Inside_Light_Subtree_Resolves_Light) was updated to ATTACH the realized
  content because WinUI establishes theme at Enter, not at build. Popups are the same shape: theme is
  established when the content goes live under its (logical) opener, on the FIRST open.

DO (per plan.md §Phase 5; WinUI src D:\Work\microsoft-ui-xaml2\src; use /winui-port). Recommended order:
1. D5 — logical-parent inheritance at Enter for popup/flyout content. Make the Phase 2 Enter theme step use
   the LOGICAL inheritance parent for Popup.Child so the content inherits the opener's effective theme on
   first attach. Verify/establish the Popup.Child → logical-parent linkage on all platforms (the
   enhanced-lifecycle path uses it today — generalize it). SEARCH: Popup.cs, Popup.WithPopupRoot.cs,
   PopupRoot.cs, FrameworkElement logical-parent plumbing, DependencyObjectStore.EstablishThemeAtEnter.
2. Remove the UNO_HAS_ENHANCED_LIFECYCLE gating that confines popup theme propagation to Skia/WASM
   (Popup.WithPopupRoot.cs / Popup.cs) so the establishment is platform-neutral. Native tree-attach hookup is
   Phase 7 — for now wire into the cross-platform enter where one exists; leave a marked TODO + #if for native.
3. D6 — FlyoutBase.ForwardThemeToPresenter (SEARCH BY NAME): today it walks up for the nearest non-Default
   RequestedTheme and forwards only that. Change it to forward the placement target's ActualTheme (the
   EFFECTIVE theme, incl. app/inherited), so a flyout opened over app-themed (not element-themed) content is
   themed on the FIRST open. Keep the ActualThemeChanged subscription (OnPlacementTargetActualThemeChanged)
   so runtime target-theme changes still propagate. PRESERVE m_isFlyoutPresenterRequestedThemeOverridden
   (don't override an explicitly-set presenter theme). Matches WinUI ActualTheme = effective theme
   (framework.cpp:3953-3978).
4. Confirm TextCommandBarFlyout / CommandBarFlyout presenters inherit via FlyoutBase and need nothing beyond
   step 3 (the S4 mobile text-selection context menu is the motivating case; full native validation = Phase 7).

PROTOCOLS (AGENTS.md): Root-Cause First Debugging (a first-open theme miss = the content's theme wasn't
established from its logical opener at Enter; fix the Enter/logical-parent establishment, NEVER re-add a
theme push — there is no push API anymore) + Validation Evidence (label review/compile/runtime). Public-spec
rule: no private trackers in committed artifacts. Branch only; do NOT push (maintainer pushes manually).
NDA: S1-S5 labels only. New .cs files: UTF-8 BOM + CRLF (edits to existing files preserve encoding via Edit).

BUILD/VALIDATE (all verified working in the Phase 4 session):
- UnoFastDevBuild IS wired; -c Release builds clean. Skia build ~1 min incremental, ~2:20 clean. Build Uno.UI
  via the SamplesApp project (there is no src\Uno.UI\Uno.UI.Skia.csproj at that path):
  dotnet build D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
- Runtime tests (Skia), ONE PowerShell call. LESSON: the tool's cwd PERSISTS between calls — after you
  Set-Location into bin, relative project paths break, so use ABSOLUTE paths for builds, and redirect run
  stdout to a log file so console output is not truncated:
    $filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization"
    $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($filter))
    $base = "D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0"
    Set-Location $base
    dotnet SamplesApp.Skia.Generic.dll --runtime-tests=(Join-Path $base "theming-p5.xml") *> (Join-Path $base "theming-p5.log")
  Parse: [xml]$x=gc theming-p5.xml; $x.SelectNodes("//test-case") with result attribute Passed/Failed.
- The Skia baseline to hold is 144/145 (143 pre-Phase-4 + the Phase 4 §B guard; lone failure = the GC flake
  When_Flyout_Closed_Target_Does_Not_Hold_Flyout). T4/T5 are already GREEN on Skia/WASM (they were band-aid-
  covered and are now establishment-covered) — keep them green; they go RED→green on NATIVE in Phase 7.
- Whitespace before staging: dotnet format whitespace src\Uno.UI --folder --include <changed files> --verify-no-changes
- Debug.Assert is COMPILED OUT in -c Release — don't rely on it. WASM head = SamplesApp.Skia.WebAssembly.Browser
  (same Uno.UI.Skia assembly, so Skia is representative). /winui-runtime-tests deferred — don't fight local VS.

ACCEPTANCE:
- Builds clean on Skia (and other heads as practical).
- T4 (TextCommandBarFlyout / Flyout first-open from a Light region uses that region's theme) and T5 (Popup
  first-open in a Light region has the region theme) GREEN on Skia + WASM, with identical result on the SECOND
  open (no "fix on second open"). Native = Phase 7.
- Existing popup/flyout theme tests in Given_ElementTheme ("Popup and Flyout Theme Propagation" region) and the
  full theming suite + Given_Theme_Materialization stay at the 144/145 baseline; the §B guard stays green.
- LESSON: a first-open theme miss after this phase = the popup/flyout content's theme is not being established
  from its LOGICAL opener at Enter (a D5 gap), or the flyout isn't forwarding the placement target's ActualTheme
  (a D6 gap). Fix the establishment/forwarding at the ROOT; do NOT reintroduce any theme push.

WORKFLOW: scoped dotnet format whitespace --verify-no-changes on changed files before staging. Commit each
validated group (Conventional Commits + Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>);
do NOT push. Suggested commit per plan.md: `fix(theming): theme popup/flyout content from placement target
ActualTheme on first open (D5/D6)`. Update THEMING-PROGRESS.md (check Phase 5, add evidence log) as a separate
docs commit.
```
