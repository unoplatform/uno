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
| W6 | Mixin generators (3 of 4; defer `DependencyPropertyMixinGenerator`) | ⬜ |
| W2 | Native Android rendering | ✅ done (204 `.Android.cs` deleted; 21 host-linked + ViewHelper/InsetsExtensions over-keep; Android host + Skia + Ref + Tests clean. Resolved W7 `ApplicationActivity.Android.cs` dangling ref). `.java` (17) + BindingHelper project → W1. |
| W4 | Native WASM DOM rendering | 🟡 next: `.wasm.cs` (110 to remove, 4 host-linked kept) validatable (WebAssembly.Browser host lib is net9.0). `.ts` DOM-vs-bootstrap split + `Uno.UI.Runtime.WebAssembly` project → W4-proper/W1. |
| W5 | Core shared abstractions (`#if` cleanup) | ⬜ |
| W8 | Controls with native impls | ⬜ |
| W10 | Window/safe-area/insets/keyboard | ⬜ (highest risk; service-before-deletion) |
| W1 | Build/TFMs/packaging/deps | ⬜ (structural anchor; affects Skia build) |
| W11 | Tests/samples/templates/CI | ⬜ |
| W12 | Documentation | ⬜ |

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
