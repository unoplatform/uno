# Theming WinUI Alignment — Progress

Tracker for the WinUI theming-alignment refactor (branch `dev/mazi/theming-winui`).
Spec source of truth: `specs/theming-winui-alignment/{README,architecture,plan,tests,custom-theme}.md`.
One phase per session, strictly in order. Scenario labels S1–S5 are defined in `architecture.md` §1
(generic, NDA-safe). Do **not** record private tracker IDs here.

## Phases

- [ ] **Phase 0** — Baseline + regression test scaffold
- [ ] **Phase 1** — Per-object theme on every `DependencyObject` (D1)
- [ ] **Phase 2** — Establish theme at tree `Enter` for every DO (D2) + stop clearing on unload (D4)
- [ ] **Phase 3** — Resolve `{ThemeResource}` against the owner's effective theme (D3, Mechanism 1)
- [ ] **Phase 4** — Remove the 11 band-aid pushes + delete the global stack
- [ ] **Phase 5** — Popup/Flyout logical-parent inheritance + flyout `ActualTheme` forwarding (D5, D6)
- [ ] **Phase 6** — Application/OS/custom-theme/high-contrast precedence (D7, D8)
- [ ] **Phase 7** — Native (Android/iOS) parity
- [ ] **Phase 8** — Full validation, WinUI parity, cleanup, docs

## Evidence log

(one line per completed phase: what built; which tests ran on /winui-runtime-tests and /runtime-tests and their results)

---

## Phase 0 — working notes (IN PROGRESS — not complete; repro strategy decision pending)

**Impl location:** branch `dev/mazi/theming-winui` is checked out at `D:\Work\uno` (the main worktree),
1 commit past `master` (spec docs only). Spec: `specs/theming-winui-alignment/`.

### Build / run gotchas (IMPORTANT — saves rediscovery)
- `UnoFastDevBuild` is **not wired on this branch** (no-op; only referenced in plan.md). It's a
  build-acceleration feature from other branches.
- `-c Release` enforces code-style: `Directory.Build.props:248` sets `EnforceCodeStyleInBuild=true`
  unconditionally, and `IDE0055` is excused only in **Debug** (`:254`). Pre-existing `IDE0055`
  violations in `src/Uno.UI/UI/Xaml/Thickness.cs:70-76` therefore FAIL the Release build.
- **Working Skia build:**
  `dotnet build src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj -c Release -f net10.0 -p:UnoTargetFrameworkOverride=net10.0 -p:EnforceCodeStyleInBuild=false`
- Do **NOT** also pass `-p:RunAnalyzersDuringBuild=false` / `-p:EnableNETAnalyzers=false` — that
  breaks CsWin32 source-gen in `Uno.UI.Runtime.Skia.Win32` (`CS0234`). Leave generators on.
- **Run (Skia):** in ONE shell call (cwd + env reset between calls):
  set `$env:UITEST_RUNTIME_TESTS_FILTER = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("<filter>"))`,
  then from `src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net10.0` run
  `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=out.xml`.
- WASM head publishes OK (`SamplesApp.Skia.WebAssembly.Browser`, same `EnforceCodeStyleInBuild=false`);
  **not yet run**. Python + `build/test-scripts/skia-browserwasm-file-creation-server.py` present.

### Files added (this WIP commit)
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Theme_Materialization.cs` — T1–T10 scaffold;
  compiles + runs; **all 10 PASS on Skia** (no class-level native exclusion so popup tests can run
  on native; per-method native excludes on the non-popup tests).
- `.gitignore` += `/specs/theming-winui-alignment/baseline-results.md`.

### Reproduce-first findings (the important part)
- **T1–T10 all GREEN on Skia.** The 11 band-aids (`UNO_HAS_ENHANCED_LIFECYCLE` = Skia/WASM) already
  cover the materialization paths I synthesized: ListView realization → `FrameworkTemplate.LoadContent`
  (band-aid #5), runtime-add of an FE → `OnLoadingPartial` (#3), Skia popups. Per `architecture.md`,
  S4 (popup/flyout first-open) is **native-only** (band-aids are not present on native) — so T4/T5
  would be RED on Android/iOS, not Skia.
- **Existing theming suite on Skia: 134 total, 117 pass, 17 FAIL.** The failures ARE the leak the
  refactor targets (Light context resolving the Dark value), concentrated in **default-foreground**
  and **dynamic-theme-change** paths — NOT the static materialization paths my T1–T10 chose.
- **OS = Dark confirmed on this machine** (`AppsUseLightTheme=0`, `SystemUsesLightTheme=0`). The 17
  failures are **environment-sensitive**: they do not pin the app theme, so on OS=Dark the app's
  ambient `Themes.Active` follows OS=Dark and leaks into Light contexts. On a Light-OS / CI box,
  several would likely pass.
- My new repros **masked** the bug by pinning the ambient (`UseApplicationLightTheme`) or by comparing
  two equally-leaked values (T8 reference also leaked).

### 17 failing existing tests (Skia, OS=Dark) — real-leak vs likely-flaky
Default-foreground / theme-leak (look deterministic — the target bug):
`When_DefaultForeground_Fluent` (exp #E4000000, got #FFFFFFFF), `When_DefaultForeground_Non_Fluent`,
`When_ParentThemeChanges_BasicThemeResources_FullRepro` (exp Yellow, got Blue),
`When_Grandparent_RequestedTheme_Changes_Button_Foreground_Updates`,
`When_Theme_Changes_Button_Normal_Foreground_Updates`, `When_Theme_Changes_TextBlock_Foreground_Updates`,
`When_XamlParsed_Buttons_Theme_Change_Foreground_Updates`, `When_Theme_Default_To_Dark_Button_PointerOver_Updates`,
`When_Root_Theme_Cycles_Dark_Default_TextBlock_Foreground_Restores`, `When_Root_Theme_Cycles_New_Content_Has_Correct_Foreground`,
`When_RootTheme_Cycles_Then_Element_RequestedTheme_Dark_Button_Foreground_Updates`,
`When_ComboBox_Theme_Changed_After_First_Open`, `When_Detached_From_Window_While_Theme_Changed`,
`When_Parent_Resource_Override_On_Loaded`, `When_Theme_Changed`, `When_ActualThemeChanged_Throws`.
Possibly flaky (GC/lifetime): `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`.

### OPEN DECISION (blocking — pending maintainer input)
- **Q1 repro strategy:** (a) redesign T1–T10 to deterministically pin a Dark ambient + Light element and
  target the paths that actually leak (default-foreground / dynamic-change / uncovered), iterating to RED;
  (b) lean on the existing failing tests as the baseline repros (reclassify "keep green"); (c) maintainer
  describes the NDA-safe uncovered control+trigger so repros match S1–S5 closely.
- **Q2 OS-dark handling:** (a) always pin app theme for determinism + flag OS-only fails in baseline;
  (b) add a deterministic OS=Dark-leak repro (simulate via `SystemThemeHelper`); (c) record baseline as-is.

### Next steps
1. Resolve Q1/Q2. 2. (Re)author repros to be RED on Skia/WASM (and native for S4). 3. Run WASM + the
existing-suite baseline; write `specs/theming-winui-alignment/baseline-results.md` (gitignored, NDA-safe).
4. WinUI-oracle pass (`/winui-runtime-tests`) for WinUI-runnable tests; probe app for Uno-only behaviors.
5. Tick Phase 0 + commit + push.
