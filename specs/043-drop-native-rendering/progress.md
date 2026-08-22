# Progress — Drop Native Rendering (Spec 043)

Living execution tracker. Companion to [`spec.md`](./spec.md) + [`inventory.md`](./inventory.md).
Branch: `dev/mazi/dropnativetest`.

## Ground truth established (recon)
- **Baseline Skia-only build = green** (0 code errors; only a known-environmental
  `NETSDK1147: wasm-tools-net9` error on `SamplesApp.Skia.WebAssembly.Browser`,
  which needs `dotnet workload restore`). Validate on "no NEW errors".
- Projects use **per-flavor csproj names** (`*.Skia.csproj`, `*.Reference.csproj`,
  `*.Wasm.csproj`, `*.netcoremobile.csproj`, `*.Tests.csproj`) — not multi-target.
  "Generic Skia variant kept" = `*.Skia.csproj`.
- **Maintained flavors to keep green:** Skia, Reference, Tests. Native (`*.Android.cs`,
  `*.UIKit.cs`, `*.iOS.cs`, `*.Apple.cs`, `*.wasm.cs`) are NOT compiled by these, so
  deleting native files never breaks them. Risk is **runtime correctness on
  Skia-on-mobile** if a kept platform-API/service file is misclassified — handle
  `Uno.UWP`/`Uno.Foundation` with extra care.
- `MixinGeneration.targets` (defines `UNO_MIXIN_GENERATION`) is imported by
  Skia/Reference/Tests too ⇒ `DependencyPropertyMixinGenerator` emits **cross-platform**
  DPs (`Control.HorizontalContentAlignment`, `Popup.IsOpen`, `ListViewBase.*`, …) that
  Skia depends on. **NOT a safe leaf-delete** — defer; migrate props to
  `[GeneratedDependencyProperty]` first.

## Commit policy
Global CLAUDE.md "never auto-commit / stage only" vs project AGENTS.md "commit in
logical groups when autonomous" conflict → **staging only** per workstream unless the
user opts into the AGENTS.md cadence.

## Workstream status
**Validation capability:** Skia desktop + Reference + Tests compile here, **plus** the
Skia.Android / Skia.AppleUIKit / Skia.WebAssembly.Browser **host** builds (android/ios/
maccatalyst/wasm-tools workloads installed). Host compiles catch keep-list misdeletions.
On-device **runtime/behavioral** parity still needs CI device stages (flag, don't claim).

**Keep-list mechanism:** a native (`*.Android/.UIKit/.iOS/.Apple/.wasm.cs`) `Uno.UI` file
is KEEP iff a kept project explicitly `<Compile Include>`-links it (platform services:
native pickers, WebView native, IME/input helpers, Android activity infra, window
wrappers). The deleted UI variants use globs (not explicit links), so explicit links =
the authoritative keep-set (`/tmp/keepset_raw.txt`, 39 files). REMOVE = native files not
explicitly linked. Over-keeping is the safe error direction.

## Workstream status
| WS | Title | Status |
|----|-------|--------|
| W7 | Composition native backing | ✅ done (12 native files deleted; `CompositionConfiguration.UseCompositorThread` removed; Skia+Ref+Tests green) |
| W3 | Native Apple (UIKit) rendering | ✅ done (210 Uno.UI `.UIKit/.iOS/.Apple.cs` deleted; 14 host-linked kept; AppleUIKit host + Skia + Ref + Tests compile clean) |
| W9 | Media native backends | 🟡 partial: Uno.UI presenter `.Apple.cs` already removed via W3; `.Android.cs` to remove in W2; **Uno.UWP `MediaPlayer.Android/.Apple.cs` engine consumed by Skia-on-mobile** (no MediaPlayerExtension in Android/AppleUIKit hosts, unlike macOS/Browser) → migrate engine to hosts first (device-validated). DEFER engine. |
| W6 | Mixin generators | ✅ done (deleted FrameworkElementAndroid/UIKitMixinGenerator + BaseActivityCallbacksGenerator + their tests; Internal+Tests+Skia clean). KEPT `DependencyPropertyMixinGenerator` + `MixinGeneration.targets`/`UNO_MIXIN_GENERATION` (still emit cross-platform DPs for Skia). |
| W2 | Native Android rendering | ✅ done (204 `.Android.cs` deleted; 21 host-linked + ViewHelper/InsetsExtensions over-keep; Android host + Skia + Ref + Tests clean. Resolved W7 `ApplicationActivity.Android.cs` dangling ref). `.java` (17) + BindingHelper project → W1. |
| W4 | Native WASM DOM rendering | 🟡 partial: 114 `.wasm.cs` deleted + validated (WebAssembly.Browser host lib net9.0 clean). REMAINING: `.ts` DOM removal (`WindowManager.ts` etc., keep bootstrap/interop `Runtime/Xaml/input/focus` + `WebView.ts` + `ts/types/**`); delete `Uno.UI.Runtime.WebAssembly` project. Needs WASM (browserwasm / `wasm-tools-net9`) build validation — not available in this env. |
| SyncGen | WinAppSDK sync generator: omit native symbols for Skia-only libs | ✅ code done: `Uno.WinAppSDKSyncGenerator` now drops `__ANDROID__/__IOS__/__TVOS__/__WASM__` from generated `#if`/`[NotImplemented]`/dedup for libraries other than Uno.UWP/Uno.Foundation (flag threaded via `PlatformSymbols.emitNativeDefines`, keyed on `ShouldEmitNativeDefines`→Generated path). Compiles. **Regeneration deferred:** `Build()` loads the (now-broken) Uno.UI `.netcoremobile`/`.Wasm` compilations for transitive UWP/Foundation symbols — W1 must rewire it to load Uno.UWP/Uno.Foundation native compilations directly before regenerating. |
| W5 | Core shared abstractions (`#if` cleanup) | ✅ substantially done: removed `IShadowChildrenProvider` + orphaned fully-native files; cleaned dead **positive** native `#if`/`#elif`/`\|\|` branches across ~270 shared `.cs` files in 8 build-gated workflow batches (each validated: direct Uno.UI.Skia + Reference + Tests, 0 errors; one batch also Android-host-validated). Intentionally NOT touched (correct): host-linked files (`GlobalUsings`/`NativeApplication`/WebView headers — live in host builds), `.Xamarin.cs` (native-only via CrossTargetting), `FeatureConfiguration.cs` (W8), negated guards (`!__CROSSRUNTIME__`/`!__SKIA__` — live for Tests/Reference), and a few harmless ambiguous/`#if false`-nested stragglers (PivotItem/ToggleSwitch/MuxInternal/RoutedEvents + residue in NavigationView/TextBox/UIElement). **Validation gate = DIRECT `dotnet build Uno.UI.Skia.csproj`+Reference+Tests** (the `.slnf` bails early on missing `wasm-tools-net9`). **Rule learned (the hard way, build-caught): IS_UNIT_TESTS is NOT `__CROSSRUNTIME__`, so `#if !__CROSSRUNTIME__` is LIVE for Tests — never remove negated-native.** | Skia-neutral (branches already compile-excluded) → cosmetic/contributor-readability (#12). **Not safely bulk-automatable** (shared files mix live + dead branches; `!__SKIA__`/`#else` branches are used by Reference — must preserve; per-file `#if`/`#elif`/`#else` collapse risk). Best done as a reviewed pass with Skia+Reference+Tests validation. Also: remove `NativeOwner` from Reference/unittests (rework `ElementCompositionPreview` `#else`). |
| W8 | Controls with native impls + FeatureConfiguration native flags | ⬜ FeatureConfiguration native flags are all `#if __ANDROID__/__APPLE_UIKIT__/__WASM__`-guarded → **already excluded from Skia**, so removing them is Skia-neutral (pure public-API/dead-code cleanup, value realized only once netcoremobile/Wasm projects go in W1). Some are `__ANDROID__ \|\| UNO_REFERENCE_API` (exposed to Reference) — careful. Do with/after W1. |
| W10 | Window/safe-area/insets/keyboard | ⬜ highest risk; service-before-deletion. The kept host-linked `NativeWindowWrapper.Android/.UIKit.cs` need the W10 EDITs (drop GetVisualBounds insets etc.); `LayoutProvider.Android.cs`/`InputPane.Android/.Apple.cs` removal needs the Skia-host equivalents confirmed on-device. Device-validated. |
| W1 | Build/TFMs/packaging/deps | ⬜ structural anchor; affects Skia build. **Entanglement found — spec's removable list is imprecise:** `Uno.UI.Dispatching.netcoremobile/.Wasm` consumed by Skia hosts (dispatcher is platform-layered like Uno.UWP) → KEEP. `MediaPlayer.WebAssembly` → `Uno.UI.Wasm.csproj`, `SamplesApp.Skia.WebAssembly.Browser`, `Uno.UI.Wasm.Tests` reference Wasm UI variant(s) → rewire (likely to `Uno.UI.Skia`) before deleting. Validatable: Skia-only.slnf restore+build + host LIBS (net9.0/-android/-ios). NOT validatable here: browserwasm app heads (`wasm-tools-net9` missing) + WASM/device runtime. Touches `.sln`/`.slnx`/`*.slnf`/`Directory.Build.props _AdjustedOutputProjects`/`Directory.Build.targets` AndroidX pins/`Uno.Implicit.Packages...Android.targets`/nuspec — do incrementally, validate each. |
| W11 | Tests/samples/templates/CI | ⬜ remove native test/sample/template heads + 3 native CI stages (`wasm_tests`/`android_tests`/`ios_tests`) + `determine-test-scope.ps1`. Couple CI edits with W1 project removal. |
| W12 | Documentation | 🟡 partial: 7.0 migration guide + native-doc stubs (uids preserved) + toc done. REMAINING (deferred until W1/W5 land — they describe the contributor-facing tree which is mid-migration): AGENTS.md native-targeting + `.claude/rules/platform-targeting.md`/`code-style.md`, deep arch docs (`how-uno-works.md`, `Uno-UI-Performance.md`, `intro.md`, xf-migration). |

## Remaining work & handoff (for CI/device-equipped continuation)
**Done + validated here (11 commits):** native rendering C# fully removed across all four
backends (Composition W7, Apple W3, Android W2, WASM `.wasm.cs` W4) + native mixin
generators (W6) + W5 starts (IShadowChildrenProvider + 6 orphaned fully-native files);
~540 files / ~74k lines. Each commit compile-validated on **Skia desktop + Reference +
Tests + Skia.Android/AppleUIKit/WebAssembly.Browser host libs** (`Uno.UI-Skia-only.slnf`,
0 new errors; the lone pre-existing `wasm-tools-net9` error on the WASM *app* head is
environmental). Plus 7.0 migration guide (W12).

**Why active work stopped here:** the clean, self-contained, fully-validatable-in-this-env
removals are complete. Everything remaining is either (a) blocked on a CI/device
environment (W1 browserwasm rewiring, W4-ts, W9 media engine, W10 safe-area — need
`wasm-tools-net9` and/or on-device runtime), or (b) intricate per-item refactoring with
poor autonomous ROI and rising risk best done under human review (W5 #if-forest across
~225 files with Skia-vs-Reference branch subtlety; W8 mixed #if-guarded vs unconditional
flags needing call-site work). Pushing further would mean either unvalidatable changes on
the branch or large cosmetic churn — neither is appropriate to do blind.

**Methodology to reuse:** native file is KEEP iff a kept project `<Compile Include>`-links
it; REMOVE otherwise; validate by building the relevant Skia host. On-device
runtime/behavioral parity is NOT covered here — every removal still needs the spec's
device CI gates (notch/safe-area, IME, fling/snap, media) before release.

**What this environment CANNOT validate (hard gates for the rest):** browserwasm app-head
builds (`wasm-tools-net9` not installed) and any on-device/Skia-on-mobile *runtime*
behavior. W1 (WASM rewiring), W4-ts, W9 (media engine), W10 (safe-area/keyboard) therefore
need a CI/device environment.

## Deferred / notes
- `ApplicationActivity.Android.cs` references the now-deleted `CompositorThread`/
  `ICompositionRoot`/`UseCompositorThread`; it inherits native `NativePage`. Untangle in W2
  (only affects unmaintained netcoremobile build until then).
- `Visual.cs`/`VisualCollection.cs` `OnXChanged`/insert-remove `partial void`
  declarations are **Skia-implemented** (`*.skia.cs`) — keep them; the spec's
  "remove partial decls" is cosmetic and would break Skia if done blindly.
- `NativeOwner` still defined in `Visual.reference.cs`/`Visual.unittests.cs` and used by
  `ElementCompositionPreview` `#else`/`__APPLE_UIKIT__` branches — removing it requires
  reworking the Reference/unittest `GetElementVisual` path; defer to W5.
