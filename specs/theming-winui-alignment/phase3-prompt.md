# Phase 3 — handoff prompt

Operational handoff prompt for the next session (paste via `/goal`, or as the first message). It is
self-contained so it survives a context clear. Source of truth remains `architecture.md` / `plan.md` /
`tests.md`; this file just front-loads the verified build/test commands and the carry-over lessons from
Phases 0–2 so they are not rediscovered.

NDA-safe: uses only the generic S1–S5 scenario labels and the public uno #23177; the private
scenario↔issue mapping lives only in the gitignored `baseline-results.md`.

---

```
Implement Phase 3 of the WinUI theming-alignment refactor — "Resolve {ThemeResource} against the
owner's effective theme (D3, Mechanism 1)" — on branch dev/mazi/theming-winui (worktree D:\Work\uno).
Phases 0,1,2 committed (D2/D4 are on origin; later commits may be local). Drive with
superpowers:executing-plans. Do Phase 3 IN FULL, then STOP (not Phase 4).

READ FIRST (truth; survive context clear). Specs under specs/theming-winui-alignment/:
- architecture.md (D3 = the central defect, §2/§4 row D3; §3 "Difference B"; §6 Mechanism 1 + the
  hard zero-behavior-difference constraint and the choke-point-assert equivalence proof).
- plan.md — "Global conventions" + "Decision gate (Mechanism 1, line 49)" + ONLY "Phase 3" (steps 1-6,
  files, acceptance, suggested commit).
- tests.md (theming suite + exact filter; T1/T2/T3/T6/T7 are the bugs D3 fixes — already GREEN on Skia,
  band-aid-covered, so the real proof is the divergence assert, NOT a RED-on-Skia repro).
- THEMING-PROGRESS.md (Phase 2 evidence log); baseline-results.md (gitignored; Phase 0 baseline +
  scenario↔issue map; S1-S5 ↔ private IDs — local only).

DONE — DO NOT REDO:
- Phase 1 (D1): per-object _theme on DependencyObjectStore (Get/SetTheme/IsProcessingThemeWalk).
  ThemeResolution.ResolveOwnerTheme(owner) EXISTS (ThemeResolution.cs): owner.GetTheme() → nearest
  themed inheritance ancestor → Application.ActualElementTheme. Never returns Theme.None. Phase 3 wires
  it into the leaf for the first time.
- Phase 2 (D2+D4): DependencyObjectStore.EstablishThemeAtEnter() (port of EnterImpl depends.cpp:1023-
  1048) runs from the enhanced-lifecycle Enter walk (UIElement.mux.cs DependencyObject_EnterImpl), so
  owner.GetTheme() is reliably non-None at first resolution. _theme no longer cleared on unload. NOTE:
  Phase 2 added a ~64-line region to DependencyObjectStore.Theming.cs, so plan.md's line numbers for
  UpdateThemeReference (:174-272) / UpdateAllThemeReferences (:128-156) have SHIFTED — search by name.

DO (per plan.md §Phase 3; WinUI src D:\Work\microsoft-ui-xaml2\src; use /winui-port). Mechanism 1
(architecture.md §6): thread the owner's effective theme as a parameter to the resolution leaf, computed
ONCE centrally. The leaf must become a pure function of (key, owner's effective theme).
1. Add a `Theme theme` (or theme ResourceKey) param to the leaf lookups; keep the existing parameterless
   overloads as thin wrappers passing GetActiveTheme() so unrelated callers compile during migration:
   - ResourceDictionary.TryGetValue(...) value + out-providingDictionary variants — replace
     GetActiveThemeDictionary(GetActiveTheme()) (currently ~:294 and ~:364) with the passed theme key;
     thread it into GetFromMerged recursion so merged dicts pick the same theme.
   - Single Theme→ResourceKey helper ("Light"/"Dark"/"Default"; HC base only — HC composition is Phase 6).
     (Pattern already used in FrameworkElement.Theming.cs NotifyThemeChangedCore and the OnLoadingPartial
     band-aid push: Theming.GetBaseValue(theme)==Theme.Light ? "Light" : "Dark".)
2. ThemeResourceReference.RefreshValue(Theme ownerTheme, …) + RefreshValueWithTreeWalk(Theme ownerTheme, …):
   convert ownerTheme→key and pass into dict.TryGetValue(key, themeKey, …). The `owner` param currently
   ignored for theme selection is replaced by ownerTheme (computed once by the caller).
3. DependencyObjectStore.UpdateThemeReference (the analog of WinUI SetThemeResourceBinding, Theming.cpp:
   349-400/368-376): compute `var ownerTheme = ThemeResolution.ResolveOwnerTheme(owner);` ONCE and pass it
   into Phase A ancestor walk (dict.TryGetValue(key, ownerTheme, …)) AND Phase B (themeRef.RefreshValue(
   ownerTheme, cache)). THIS is the single centralized "use the owner's theme" choke point.
4. ThemeWalkResourceCache: add `theme` to the cache key (TryGetCachedValue(dict, key, theme, …)/
   CacheValue(dict, key, theme, value)), replacing the internal GetActiveTheme() reads (~:104/:133).
   Callers pass the same ownerTheme.
5. Keep Themes.Active as the FALLBACK inside ResolveOwnerTheme only. LEAVE the _requestedThemeForSubTree
   stack + the 11 band-aid pushes in place (now harmless — leaf no longer reads the stack). They are
   removed in Phase 4 — do NOT remove them now.
6. Audit the static parse path: GetSimpleStaticResourceRetrieval → ResolveResourceStatic(key,type,context)
   (XamlFileGenerator.cs ~:4914) has no theme (fine for {StaticResource}). For {ThemeResource} on a DP the
   dynamic ApplyResource path (XamlFileGenerator.cs ~:4259) is what matters and now uses owner theme via
   step 3. Confirm no {ThemeResource} relies solely on the static path.

EQUIVALENCE PROOF (architecture.md §6 — MANDATORY gate before any Phase 4): while the legacy stack and the
new param coexist, add a transitional choke-point check at the single resolution point:
Debug.Assert(ThemeResolution.ResolveOwnerTheme(owner).Key == ResourceDictionary.GetActiveTheme().Key)
(or a logged divergence counter). Run the ENTIRE theming suite + Given_Theme_Materialization on Skia with
ZERO assertion hits. Any divergence is either (a) a genuine pre-existing bug the parameter fixes — capture
as an intended, test-backed change with a documented note — or (b) a plumbing error to fix. This is the
proof that Mechanism 1 is behavior-equivalent. Phase 3 must NOT proceed to deleting anything.

PROTOCOLS (AGENTS.md): Root-Cause First Debugging + Validation Evidence (label review/compile/runtime).
Branch only; do NOT push (maintainer pushes manually). NDA: S1-S5 labels only, no private issue IDs.
New .cs files: UTF-8 BOM + CRLF.

BUILD/VALIDATE (all verified working this session):
- UnoFastDevBuild IS wired (the Phase 0 "no-op" note is STALE; -c Release builds clean, no IDE0055 issue).
  Skia build (~2 min incremental):
  dotnet build src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
- Runtime tests (Skia), ONE PowerShell call (cwd+env persist between calls), ABSOLUTE results path:
    $filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ElementTheme|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_ThemeResources|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_MergedAppResources_ThemeResource|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_FrameworkElement_FocusVisuals|Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Given_Theme_Materialization"
    $env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($filter))
    $results = "D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0\theming-p3-skia.xml"
    Set-Location D:\Work\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0
    dotnet SamplesApp.Skia.Generic.dll --runtime-tests=$results
  Parse: [xml]$x=gc $results; $x.SelectNodes("//test-case") with result attribute Passed/Failed.
- Unit tests (if you add coverage for the theme-keyed leaf): dotnet test src\Uno.UI\Uno.UI.Unit.Tests.csproj
  -p:TreatWarningsAsErrors=false (dodges NU1510 on SDK 10.0.300).
- Whitespace before staging: dotnet format whitespace src\Uno.UI --folder --include <changed files> --verify-no-changes
- WASM head = SamplesApp.Skia.WebAssembly.Browser; it references Uno.UI.Skia.csproj (the SAME assembly as
  Skia Desktop), so the resolution-leaf code is identical and the Skia runtime suite is representative.
  wasm-tools workload IS installed (10.0.108); WASM native build is slow — run it only if the maintainer
  wants it (per Phase 2 they were fine skipping it when Skia runtime tests pass).
- /winui-runtime-tests deferred — don't fight local VS.

ACCEPTANCE:
- Builds clean on Skia.
- The divergence assert is SILENT across the full theming suite + Given_Theme_Materialization on Skia
  (the Mechanism-1 equivalence proof). Document any intentional divergence as a WinUI-correct change.
- Theming suite stays at the 143/144 Skia baseline (lone failure = pre-existing GC flake
  When_Flyout_Closed_Target_Does_Not_Hold_Flyout). T1/T2/T3/T6/T7 + existing element-theme/code-behind/
  uno-#23177 repros remain GREEN (already green via band-aids; must not regress with band-aids now no-ops).
- LESSON: do NOT chase a RED-on-Skia minimal repro — materialization repros are band-aid-covered GREEN on
  Skia; the band-aids are still present in Phase 3 (no-ops) and removed only in Phase 4.

WORKFLOW: scoped dotnet format whitespace --verify-no-changes on changed files before staging. Commit each
validated group (Conventional Commits + Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>);
do NOT push. Suggested commit per plan.md: `fix(theming): resolve ThemeResources against owner's effective
theme (D3)` — body explains Mechanism 1 and that it is behavior-equivalent to WinUI's SetThemeResourceBinding
owner-theme push (Theming.cpp:368). Update THEMING-PROGRESS.md (check Phase 3, add evidence log) as a separate
docs commit.
```
