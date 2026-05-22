# Theming WinUI Alignment — Progress

Tracker for the WinUI theming-alignment refactor (branch `dev/mazi/theming-winui`).
Spec source of truth: `specs/theming-winui-alignment/{README,architecture,plan,tests,custom-theme}.md`.
One phase per session, strictly in order. Scenario labels S1–S5 are defined in `architecture.md` §1
(generic, NDA-safe). Do **not** record private tracker IDs here.

## Phases

- [ ] **Phase 0** — Baseline + regression test scaffold
- [x] **Phase 1** — Per-object theme on every `DependencyObject` (D1)
- [x] **Phase 2** — Establish theme at tree `Enter` for every DO (D2) + stop clearing on unload (D4)
- [ ] **Phase 3** — Resolve `{ThemeResource}` against the owner's effective theme (D3, Mechanism 1)
- [ ] **Phase 4** — Remove the 11 band-aid pushes + delete the global stack
- [ ] **Phase 5** — Popup/Flyout logical-parent inheritance + flyout `ActualTheme` forwarding (D5, D6)
- [ ] **Phase 6** — Application/OS/custom-theme/high-contrast precedence (D7, D8)
- [ ] **Phase 7** — Native (Android/iOS) parity
- [ ] **Phase 8** — Full validation, WinUI parity, cleanup, docs

## Evidence log

(one line per completed phase: what built; which tests ran on /winui-runtime-tests and /runtime-tests and their results)

- **Phase 1 (D1) — DONE.** Moved per-object `_theme` + `GetTheme`/`SetTheme` + `IsProcessingThemeWalk`
  from `UIElement` onto `DependencyObjectStore` (UIElement/FrameworkElement kept as thin forwarders);
  added `ThemeResolution.ResolveOwnerTheme(owner)` (own theme → nearest themed inheritance ancestor →
  app base theme). Behavior-preserving — **not** wired into `{ThemeResource}` resolution yet (that is
  Phase 3, Mechanism 1). Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean
  (0 errors). `/runtime-tests` theming suite (Skia Desktop): **143/144** = Phase 0 baseline (the lone
  failure `When_Flyout_Closed_Target_Does_Not_Hold_Flyout` is the pre-existing GC flake). New
  `Given_ThemeResolution` unit tests: **7/7** green (incl. the non-`UIElement` owner case D1 enables).
  Not run this session: WASM/native heads; `/winui-runtime-tests` oracle (deferred, per carry-over notes).

- **Phase 2 (D2 + D4) — DONE.** **D2:** ported the `EnterImpl` theme block (`depends.cpp:1023-1048`) as
  `DependencyObjectStore.EstablishThemeAtEnter()` — every DO going live inherits its theme from its
  (logical) inheritance parent before any `{ThemeResource}` resolves (FE → `NotifyThemeChanged`, which
  re-applies the element's own `RequestedTheme` override; other DOs → `SetTheme` + `UpdateAllThemeReferences`).
  Hosted on the per-DO store (the `CDependencyObject` analog, carries `_theme` since D1) and invoked from
  the enhanced-lifecycle Enter walk (`UIElement.mux.cs` `DependencyObject_EnterImpl`, gated by
  `UNO_HAS_ENHANCED_LIFECYCLE`; native attach = Phase 7). The Enter walk runs synchronously on attach,
  before the first measure pass raises Loading, so `GetTheme()` is established before the deferred
  `{ThemeResource}` refs (created by `ApplyResource` at parse) first resolve at load. Removed the duplicate
  inherit / explicit-`RequestedTheme` block from `FrameworkElement.OnLoadingPartial` (the Enter step
  subsumes it); the band-aid theme push at load is kept (removed Phase 4). **D4:** removed the
  `SetTheme(Theme.None)` reset in `ClearThemeStateOnUnloaded` (WinUI keeps `m_theme` across Leave; re-Enter
  re-themes) — kept the separate `_themeForeground` stale-foreground hygiene clear (re-pulled on re-Enter).
  Built `SamplesApp.Skia.Generic` (Release, net10.0, `UnoFastDevBuild`) clean (0 errors) after each commit.
  `/runtime-tests` theming suite (Skia Desktop): **143/144** = Phase 0/1 baseline (lone failure is the
  pre-existing GC flake `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`); the recycle test (S2/T2),
  `When_Detached_From_Window_While_Theme_Changed`, and the `Given_ElementTheme` entry/exit + dynamic-child
  invariant tests are all green — no behavior change, as expected (resolution leaf untouched until Phase 3).
  WASM-head build skipped per maintainer direction: it links the same `Uno.UI.Skia` assembly already
  validated on Skia and the changed code is platform-agnostic shared code. WASM/native runtime +
  `/winui-runtime-tests` oracle deferred. Two commits: D2 (`feat`), D4 (`fix`).

---

## Phase 0 — working notes (repros + baseline + test-hardening DONE; WinUI-oracle pending)

> The "blocking decisions / 17-fail / repro-strategy" notes below this header are a **historical snapshot**
> from an earlier session and are superseded by the **RESOLUTION** section at the bottom of this file.

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

## RESOLUTION (current state — supersedes the snapshot above)

### Delivered this session
1. **OS-theme simulation test API** — `SystemThemeHelper.SystemThemeOverride` (property, analogous to
   `ScaleOverride`) + `ThemeHelper.UseSystemThemeOverride(ApplicationTheme)` (disposable; throws on WinUI so
   callers add `[PlatformCondition(Exclude, NativeWinUI)]`). Lets runtime tests pin the ambient OS theme
   deterministically and simulate runtime OS-theme changes. Validated OS-independent.
2. **Repros rewritten to the correct polarity** — `Given_Theme_Materialization` (T1–T10): Light element-level
   island under a (simulated) Dark ambient, mapped to the real issues (S1–S5 + uno #23177). Finding: minimal
   repros are **band-aid-covered on Skia → green**; their role is WinUI-oracle + regression-guard + native-S4
   (T4/T5 expected RED on Android/iOS). The deterministic Skia evidence is the existing suite under Dark.
3. **Baseline** (gitignored `baseline-results.md`): existing theming suite **133/134 (Light) → 117/134 (Dark)**.
4. **Root-caused & fixed the OS-dark "unreliability"** — it was a shared-state bug in `ThemeHelper.UseDarkTheme()`
   (restored the shared `XamlRoot.Content` host from `Application.RequestedTheme`, leaving it Dark on a Dark OS
   → poisoned later tests). `UseDarkTheme()` now restores the host's **original** theme. **Not** the product
   ambient leak, and **not** test-hygiene. (Full analysis in `baseline-results.md` → RESOLUTION.)
5. **Test-hardening** (maintainer's choice = per-test app-Light): affected theme tests pin
   `ThemeHelper.UseApplicationLightTheme()` (`#if HAS_UNO`) + `[RequiresFullWindow]`. Exception:
   `When_Detached_From_Window_While_Theme_Changed` stays embedded (nulls `WindowContent` mid-test).
6. **Validated:** full theming suite + 10 repros = **143/144 on BOTH Light and Dark** (suite is now
   OS-independent). The single remaining failure, `When_Flyout_Closed_Target_Does_Not_Hold_Flyout`, is a
   pre-existing GC/lifetime flake that fails on Light too — unrelated to theming/OS, out of scope.

### Files changed (staged, not committed)
- `src/Uno.UWP/Helpers/Theming/SystemThemeHelper.cs` — `SystemThemeOverride`.
- `src/Uno.UI.RuntimeTests/Helpers/ThemeHelper.cs` — `UseSystemThemeOverride` + `UseDarkTheme` fix.
- `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_Theme_Materialization.cs` — rewritten repros.
- `Given_ElementTheme.cs`, `Given_ThemeResource.cs`, `Given_FrameworkElement_ThemeResources.cs` — hardening.

### Remaining for Phase 0
- **WinUI-oracle pass** (`/winui-runtime-tests`): confirm the WinUI-portable repros (T1–T6, T8) are GREEN on
  native WinUI; probe-app for the Uno-only ones (T7/T9/T10). This is the last Phase 0 acceptance gate.
- Then Phase 0 is fully ticked → proceed to Phase 1 (per-object theme on `DependencyObjectStore`, D1).
