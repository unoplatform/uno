# Phase 4 — handoff prompt

Operational handoff prompt for the next session (paste via `/goal`, or as the first message). It is
self-contained so it survives a context clear. Source of truth remains `architecture.md` / `plan.md` /
`tests.md`; this file just front-loads the verified build/test commands and the carry-over lessons from
Phases 0–3 (especially the Phase 3 divergence map, which is the guide for what Phase 4 must scrutinize).

NDA-safe: uses only the generic S1–S5 scenario labels and the public uno #23177; the private
scenario↔issue mapping lives only in the gitignored `baseline-results.md`.

---

```
Implement Phase 4 of the WinUI theming-alignment refactor — "Remove the 11 band-aid pushes + delete the
global stack" — on branch dev/mazi/theming-winui (worktree D:\Work\uno). Phases 0,1,2,3 committed
(later commits may be local; do NOT push). Drive with superpowers:executing-plans. Do Phase 4 IN FULL,
then STOP (not Phase 5).

READ FIRST (truth; survive context clear). Specs under specs/theming-winui-alignment/:
- architecture.md (§5 = the inventory of the 11 band-aid push sites — the removal checklist; §6 Mechanism 1
  + the invariant "{ThemeResource} value = f(key, owner's effective theme)"; §2/§4 row D3).
- plan.md — "Global conventions" + ONLY "Phase 4" (steps 1-4, files, acceptance, suggested commits).
- tests.md (theming suite + exact filter; §B "no global theme stack leak" guard + sibling-isolation).
- THEMING-PROGRESS.md (Phase 0–3 evidence log — read the Phase 3 entry's equivalence-gate section in full;
  it is the map of which paths were divergent and therefore which removals carry risk).
- baseline-results.md (gitignored; Phase 0 baseline + S1-S5 ↔ private IDs — local only).

DONE — DO NOT REDO:
- Phase 1 (D1): per-object _theme + Get/SetTheme + IsProcessingThemeWalk on DependencyObjectStore;
  ThemeResolution.ResolveOwnerTheme(owner) (own theme → nearest themed inheritance ancestor →
  Application.ActualElementTheme fallback; never None).
- Phase 2 (D2+D4): DependencyObjectStore.EstablishThemeAtEnter() (port of EnterImpl depends.cpp:1023-1048)
  runs from the enhanced-lifecycle Enter walk; _theme no longer cleared on unload.
- Phase 3 (D3, Mechanism 1): the resolution leaf is now owner-theme-aware. ResourceDictionary.TryGetValue
  (value + providing-dict) + GetFromMerged have theme-keyed overloads (parameterless ones forward
  GetActiveTheme()); ThemeResourceReference.RefreshValue(Theme ownerTheme,…); ThemeWalkResourceCache keyed
  on the Theme enum base value (WinUI-aligned). The single choke point is
  DependencyObjectStore.UpdateThemeReference, which computes ResolveOwnerTheme(owner) ONCE and threads it
  into Phase A (ancestor walk) + Phase B (RefreshValue). FrameworkElement.NotifyThemeChangedCore now sets
  SetTheme(theme) BEFORE UpdateThemeBindings. NOTE: line numbers in ResourceDictionary.cs,
  FrameworkElement.Theming.cs, and DependencyObjectStore.Theming.cs have SHIFTED across Phases 2-3 —
  architecture.md §5 / plan.md line numbers are STALE. SEARCH BY NAME, not line.

CRITICAL CARRY-OVER FROM PHASE 3 (the divergence map — read before deleting anything):
Phase 3 left the stack + 11 pushes in place (no-ops for theme selection) and added a transitional counter
(ThemeResolution.RecordOwnerThemeDivergence + OwnerThemeDivergenceCount + _reportedOwnerThemeDivergences,
called from UpdateThemeReference). On the full Skia theming suite it logged 38 distinct divergences
([THEME-P3-DIVERGENCE] owner=X active=Y), root-caused (stack capture) to TWO paths — these are exactly the
paths whose pushes you are about to remove, so they tell you where regressions can hide:
  • Path 1 (majority) — EstablishThemeAtEnter enter-time resolution. The owner's island theme is already
    established; the leaf already resolves it correctly via owner theme. Removing the band-aids here is
    SAFE: resolution does not depend on a push (it uses owner theme). This is the D3 fix working.
  • Path 2 (few — e.g. RepeatButton/Thumb/TextBlock) — template build (FrameworkTemplate.LoadContent, #5)
    + ApplyStyleWithThemeContext (#4) of a NOT-YET-ENTERED part (GetTheme()==None at build) → ResolveOwnerTheme
    falls back to Themes.Active while the band-aid pushed the templated-parent theme. This is the HIGHEST-RISK
    removal: with #5 removed, a template part resolves the app theme at build time (parts have theme None
    before Enter), and is corrected only when the part enters the tree and EstablishThemeAtEnter re-themes it
    from its (logical) parent. Phase 4 must CONFIRM that Enter re-resolution holds (suite green) — if a
    materialized part renders the wrong theme after #5 is gone, the root cause is a missing/late
    theme-at-Enter (a Phase 2 gap), NOT a reason to re-add the push (plan.md Phase 4 premise).
The counter is the PHASE 3 artifact; it compares owner theme vs the legacy ambient. Once the stack is gone,
GetActiveTheme() is just Themes.Active, so the counter would "diverge" for every island and is meaningless —
REMOVE it as part of this phase (see DO step 5). Equivalence is then proven by the green suite, not the counter.

DO (per plan.md §Phase 4; WinUI src D:\Work\microsoft-ui-xaml2\src; use /winui-port). Recommended order so the
suite stays green at each step and regressions are isolated:
1. Re-point the real-work paths that resolve OUTSIDE UpdateThemeReference to pass the owner's theme key
   (GetThemeKey(ResolveOwnerTheme(target)) — both helpers exist from Phases 1/3), instead of pushing:
   - #2 foreground freeze (FrameworkElement.Theming.cs NotifyThemeChangedForInheritedProperties): resolve
     DefaultTextForegroundThemeBrush by passing the element's theme to the lookup. Add a theme-aware
     TryStaticRetrieval/ResolveResourceStatic overload in ResourceResolver if needed (the dict-level
     TryGetValue(key, themeKey,…) already exists from Phase 3).
   - #11 focus visual default (DependencyProperty.cs ResolveFocusVisualBrushDefault, PR #23243): resolve
     SystemControlFocusVisual…Brush passing GetThemeKey(ResolveOwnerTheme(targetObject)). Matches
     CDependencyProperty::GetDefaultFocusVisualBrush resolving against targetObject->GetTheme().
2. Remove each push/pop pair (architecture.md §5 table; SEARCH BY NAME). For the sites whose resolution
   already flows through UpdateThemeReference / UpdateResourceBindings (now owner-theme-aware) — #1 walk,
   #3 OnLoadingPartial, #4 OnStyleChanged, #5 LoadContent, #6/#7 visual states, #8 BindingHelper,
   #9 Hyperlink, #10 animation keyframes — removing the Push/Pop is sufficient; the leaf already uses owner
   theme. Validate (build + Skia suite) after each logical group; pay special attention to #5/#4 (Path 2).
3. Delete the stack + API: ResourceDictionary.PushRequestedThemeForSubTree / PopRequestedThemeForSubTree /
   PushRequestedThemeForSubTreeByName / PopRequestedThemeForSubTreeByName, and the Themes
   ._requestedThemeForSubTree field + RequestedThemeForSubTree property (RequestedThemeForSubTree collapses to
   Active). KEEP Themes.Active / SetActiveTheme / GetActiveTheme (now the app fallback only) + Themes.
   Light/Dark/Default + GetThemeKey.
4. Re-point control-level UpdateThemeBindings overrides (Popup, PopupRoot, CommandBar, ContentPresenter,
   IconElement, TextBlock, TextBox) to the owner-theme-aware resolution — they remain as propagation hooks,
   no longer relying on the stack. Then grep the whole repo for PushRequestedThemeForSubTree /
   RequestedThemeForSubTree / PushRequestedThemeForSubTreeByName and remove/convert every remnant.
5. Remove the Phase 3 transitional equivalence scaffolding (it is tied to the now-deleted stack):
   ThemeResolution.RecordOwnerThemeDivergence + OwnerThemeDivergenceCount + _reportedOwnerThemeDivergences,
   and the RecordOwnerThemeDivergence(...) call in DependencyObjectStore.UpdateThemeReference. Audit the
   transitional ThemeResourceReference.RefreshValue(DependencyObject? owner,…) wrapper + ResourceDictionary
   .GetActiveThemeValue(): once ApplyThemeResource (the only caller) resolves via the owner theme, remove
   both. (Leave the parameterless leaf TryGetValue/GetFromMerged wrappers — other unrelated callers use them;
   plan.md Phase 8 removes those.)

PROTOCOLS (AGENTS.md): Root-Cause First Debugging (a regression after removing a push = missing
theme-at-Enter; fix the Enter establishment, NEVER re-add a push) + Validation Evidence (label
review/compile/runtime). Branch only; do NOT push (maintainer pushes manually). NDA: S1-S5 labels only.
New .cs files: UTF-8 BOM + CRLF (edits to existing files preserve encoding via the Edit tool).

BUILD/VALIDATE (all verified working in the Phase 3 session):
- UnoFastDevBuild IS wired; -c Release builds clean. Skia build (~1 min incremental, ~2:20 clean). Build
  Uno.UI via the SamplesApp project (there is no src\Uno.UI\Uno.UI.Skia.csproj at that path):
  dotnet build D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
- Runtime tests (Skia), ONE PowerShell call. LESSON: the tool's cwd PERSISTS between calls — after you
  Set-Location into bin, relative project paths break, so use ABSOLUTE paths for builds, and redirect run
  stdout to a log file so divergence/console output is not truncated:
    $filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization"
    $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($filter))
    $base = "D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0"
    Set-Location $base
    dotnet SamplesApp.Skia.Generic.dll --runtime-tests=(Join-Path $base "theming-p4.xml") *> (Join-Path $base "theming-p4.log")
  Parse: [xml]$x=gc theming-p4.xml; $x.SelectNodes("//test-case") with result attribute Passed/Failed.
- Whitespace before staging: dotnet format whitespace src\Uno.UI --folder --include <changed files> --verify-no-changes
- Debug.Assert is COMPILED OUT in -c Release — do not rely on it for any check; the Phase 3 proof used a
  runtime counter for this reason (and you are removing it this phase).
- WASM head = SamplesApp.Skia.WebAssembly.Browser; it references the SAME Uno.UI.Skia assembly, so the Skia
  suite is representative. WASM native build is slow — run it only if the maintainer asks. /winui-runtime-tests
  deferred — don't fight local VS.

ACCEPTANCE:
- Builds clean on Skia. No PushRequestedThemeForSubTree / RequestedThemeForSubTree / _requestedThemeForSubTree
  anywhere in the repo (grep returns nothing); the Phase 3 divergence counter is gone.
- Full theming suite + Given_Theme_Materialization on Skia stays at the 143/144 baseline (lone failure =
  pre-existing GC flake When_Flyout_Closed_Target_Does_Not_Hold_Flyout). T1/T2/T3/T6/T7 + the existing
  element-theme / code-behind-style / uno #23177 repros remain GREEN with ZERO band-aids and no global stack.
- Add the §B leak-check guard: Given_ElementTheme sibling/context-isolation tests stay green (they prove no
  cross-subtree bleed without the stack); a compile/reflection guard fails if Push…ForSubTree is reintroduced.
- LESSON: a test that goes RED after a push is removed means a materialization path is missing/late
  theme-at-Enter (Phase 2 gap). Fix the Enter establishment (or its ordering vs first resolution) at the
  ROOT — do NOT re-add the push.

WORKFLOW: scoped dotnet format whitespace --verify-no-changes on changed files before staging. Commit each
validated group (Conventional Commits + Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>);
do NOT push. Suggested commits per plan.md: `refactor(theming): resolve focus/style/template/state/animation
ThemeResources via owner theme` then `refactor(theming): delete global requested-theme stack and all push
band-aids`. Update THEMING-PROGRESS.md (check Phase 4, add evidence log) as a separate docs commit.
```
