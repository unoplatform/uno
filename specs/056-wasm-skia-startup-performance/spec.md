# WebAssembly Skia: Cold-Start Performance

**Repo**: `uno` (Uno.UI.Runtime.Skia.WebAssembly.Browser, Uno.Sdk, Uno.Wasm.Bootstrap)
**Created**: 2026-08-14
**Status**: Draft — findings measured, remediation proposed
**Scope**: `net10.0-browserwasm` + `UnoFeatures=SkiaRenderer`. The legacy DOM/"Native"
WASM renderer is explicitly out of scope.

## Overview & Objectives

Uno Skia WebAssembly apps start materially slower than comparable Avalonia Browser and
Blazor WebAssembly apps. This spec records a measured teardown of where the time goes,
separates the causes that are incidental from those inherent to Uno's WinUI-compatibility
mandate, and proposes a sequenced set of changes.

Avalonia Browser is the fair comparison: it is also Skia-on-canvas, on the same Mono
WebAssembly runtime, from the same .NET 10 runtime pack. Blazor is included as a
DOM-rendering floor.

### Measured result

Three blank templates, published `-c Release`, served as static files with brotli and no
caching, loaded in a cold browser context. The metric is time to first rendered frame —
first WebGL/Canvas2D draw for the Skia renderers, first real DOM content for Blazor.

| Scenario | Uno Skia | Avalonia | Blazor | Uno − Avalonia |
|---|---:|---:|---:|---:|
| Localhost, unshaped — first frame | 918 ms | 659 ms | 309 ms | +259 ms |
| Localhost — **splash actually cleared** | 1 140 ms | 659 ms | 309 ms | +481 ms |
| 20 Mbps / 20 ms — first frame | 4 524 ms | 2 499 ms | 1 053 ms | +2 025 ms |
| 4 Mbps / 150 ms — first frame | 21 161 ms | 11 500 ms | 5 533 ms | +9 661 ms |
| Transferred (brotli) | 11.87 MB | 4.79 MB | 2.24 MB | +7.08 MB |
| HTTP requests | 106 | 43 | 48 | +63 |

Median of 3–5 runs, Chromium 151 headless, macOS arm64.

### The gap has two separate causes

**Payload dominates on any real network.** Uno transfers 7.08 MB more than Avalonia. At
20 Mbps that difference alone is ~2.8 s of wire time, which more than accounts for the
whole 2.0 s gap.

**Managed startup is the residual.** With bandwidth removed, Uno still trails by 259 ms to
first frame, and by 481 ms to the moment the user actually sees the app — because splash
removal is chained behind font preloading.

### Where the 7.08 MB goes

| Component | Uno | Avalonia | Delta | Cause |
|---|---:|---:|---:|---|
| App assembly | 2 499 KB | 48 KB | +2 451 KB | 5.5 MB ICU blob embedded as a resource |
| Fonts (37 files vs 0) | 2 683 KB | 464 KB | +2 219 KB | Whole OpenSans family + Fluent icons over HTTP |
| UI framework | 1 835 KB | 679 KB | +1 156 KB | `Uno.UI` vs `Avalonia.Base` + `Avalonia.Controls` |
| Theme dictionary | 422 KB | 75 KB | +347 KB | `Uno.UI.FluentTheme.v2` vs `Avalonia.Themes.Fluent` |
| `dotnet.native.wasm` | 2 654 KB | 2 445 KB | +209 KB | `unoicu.a` + IDBFS statically linked |
| `System.Private.Xml` | 142 KB | — | +142 KB | Not shipped by Avalonia or Blazor |
| Bootstrap JS + CSS | ~57 KB | ~10 KB | +47 KB | `require.js`, `uno-bootstrap.js`, legacy DOM CSS |
| **Total transferred** | **11.87 MB** | **4.79 MB** | **+7.08 MB** | |

## Requirements

Ordered by impact per unit of effort. R1–R5 are build-configuration changes that alter no
public API and no WinUI behaviour.

### R1 — stop embedding `icudt.dat` into `browserwasm` heads

**Severity: critical. Saves ~2.45 MB brotli on the critical path.**

`Uno.icu.Common.targets` adds a 5,505,504-byte `icudt.dat` as an `EmbeddedResource` to any
project where `IsUnoHead == True`, with no platform condition:

```xml
<!-- ~/.nuget/packages/uno.icu-wasm/77.2.1/buildTransitive/Uno.icu.Common.targets -->
<Target Name="AddUnoIcuEmbeddedResource" BeforeTargets="BeforeBuild;BeforeCompile"
        Condition="'$(IsUnoHead)' == 'True' and '$(UnoIcuDataIncluded)' != 'true'">
    <EmbeddedResource Include="$(MSBuildThisFileDirectory)icudt.dat" />
</Target>
```

On WebAssembly this lands inside the app's own assembly, which is on the critical path —
the runtime cannot invoke `Main()` until it has downloaded and opened it. Confirmed against
the real publish output:

```
$ monodis --presources bin/Release/net10.0-browserwasm/UnoSkiaWasm.dll
386: UnoSkiaWasm.icudt.dat (size 5505504)     ← 91.7% of the 6,002,176-byte app DLL
```

A blank Avalonia app assembly is 48 KB; a blank Uno app assembly is 2 499 KB brotli.

Uno consequently ships ICU **three separate ways**: this embedded blob, the `unoicu.a`
static library linked into `dotnet.native.wasm`
(`Uno.UI.Runtime.Skia.WebAssembly.Browser.csproj:40` references `Uno.icu-wasm` with no
opt-out), and the standard sharded `icudt_EFIGS/CJK/no_CJK.dat` files — the last of which
are byte-identical to what Avalonia and Blazor ship.

Condition the embedded resource off for `browserwasm`, where the native `unoicu.a` already
provides the ICU entry points. Verify that `UnicodeText.ICU` resolution still succeeds
against the native library alone before shipping.

### R2 — do not gate splash removal on font preloading

**Severity: critical. Saves ~270 ms of perceived startup on localhost, far more on slow links.**

`RemoveSplashScreen()` is chained behind a task that awaits *every* entry in the font
manifest:

```
src/Uno.UI/UI/Xaml/FontFamilyHelper.cs:50-75
    manifest.Fonts.Select(...) → Task.WhenAll          ← no weight/style filter

src/Uno.UI/UI/Xaml/Application.cs:822-949
    InvokeOnLaunched: FontPreloadTask = PreloadFonts();

src/Uno.UI.Runtime.Skia.WebAssembly.Browser/.../WebAssemblyWindowWrapper.cs:97-128
    ShowCore: task.ContinueWith → NativeDispatcher.Main.Enqueue
            → LayoutUpdated → InvalidateRender → FrameRendered → host.RemoveSplashScreen()
```

Measured on localhost: first GL draw at ~870 ms, last font lands at 1 026–1 215 ms, splash
clears at 1 141–1 306 ms. The app has already rendered but stays hidden behind the loader
until the font barrier completes.

Note that the entire splash-removal path is nested inside
`if (Application.Current.FontPreloadTask is { } task)`. Font preloading is therefore not
merely a delay on splash removal — it is its **only** trigger. If the task is null, or its
continuation does not run, the loader is never taken down at all. This is very likely the
mechanism behind R10 and should be fixed together with it: splash removal needs a trigger
that does not depend on font preloading having been scheduled.

Render the first frame with the fallback typeface `FontDetailsCache.GetFontInternal`
already resolves synchronously, and let preloaded fonts trigger a re-render as they arrive.

Avalonia issues **zero** font requests: Inter ships as a manifest resource inside
`Avalonia.Fonts.Inter.wasm` (`Avalonia.Fonts.Inter.csproj:6`, `<AvaloniaResource Include="Assets\*" />`)
and arrives in the normal parallel assembly batch.

### R3 — stop shipping the whole OpenSans family by default

**Severity: critical. Saves ~2.4 MB brotli and 36 HTTP requests.**

The blank template ships 32 OpenSans TTF variants (Condensed, SemiCondensed, every weight,
every italic) plus the Fluent icon font — 6.0 MB raw / 2.6 MB brotli across 37 requests.

```
package_<hash>/Uno.Fonts.OpenSans/Fonts/   32 × OpenSans*.ttf   ≈ 5 275 KB raw / 2 390 KB br
package_<hash>/Uno.Fonts.Fluent/Fonts/     uno-fluentui-assets.ttf   767 KB raw / 227 KB br
```

`FontFamilyHelper.PreloadAllFontsInManifest` applies no filtering, so all of them are
fetched at startup regardless of what the app references.

Default the implicit font package to Regular + SemiBold and make the remaining variants
opt-in. Preload only the weight/style the startup page actually references.

### R4 — make `WasmShellEnableIDBFS` opt-in

**Severity: high. Saves ~971 KB raw from `dotnet.native.wasm`.**

`Uno.Common.Wasm.targets:20` sets `WasmShellEnableIDBFS=true` unconditionally whenever
`UnoFeatures` contains `skiarenderer`; `Uno.Wasm.Bootstrap.targets:213-214` then wires
`-lidbfs.js` and the IDBFS export. Apps that never persist anything still pay for it. This
is most of why Uno's native module is 971 KB larger than Avalonia's
(9,866,518 vs 8,895,201 bytes).

### R5 — stop emitting the legacy DOM-renderer asset stack for Skia heads

**Severity: high. Removes 3 render-blocking requests plus `require.js`.**

A Skia app draws into a canvas, yet the generated `index.html` blocks first paint on four
stylesheets inherited from the DOM renderer:

```html
<script type="text/javascript" src="/package_<hash>/require.js"></script>
<link rel="stylesheet" href="/package_<hash>/normalize.css" />
<link rel="stylesheet" href="/package_<hash>/uno-bootstrap.css" />
<link rel="stylesheet" href="/package_<hash>/uno.css" />
<link rel="stylesheet" href="/package_<hash>/Fonts.css" />
```

Avalonia loads one stylesheet. `Fonts.css` is additionally stale: its `@font-face` rules
point at `./Uno.Fonts.Roboto/Fonts/*.ttf`, a directory that does not exist in the Skia
publish output. No 404s occur only because the browser never resolves an unused
`@font-face`.

### R6 — remove the extra sequential hop before the runtime download

**Severity: high. Saves one full round trip (~100 ms on a 150 ms-RTT link).**

The fingerprinted `dotnet.js` filename is not known until `uno-config.js` has been fetched,
parsed and awaited, so the browser can neither preload nor discover the runtime until that
resolves:

```ts
// Uno.Wasm.Bootstrap/ts/Uno/WebAssembly/Bootstrapper.ts:110,123
var config = await import('./uno-config.js');
var m = await import(`../_framework/${config.config.dotnet_js_filename}`);   // blocked on the above
```

Avalonia's `main.js` uses a static import that the module-graph resolver discovers at parse
time:

```js
import { dotnet } from './_framework/dotnet.js'
```

Uno's chain is `index.html → require.js + uno-bootstrap.js → uno-config.js → dotnet.js →
dotnet.native.wasm`; Avalonia's is `index.html → main.js → dotnet.js → dotnet.native.wasm`.

Emit the filename into `uno-bootstrap.js` (or as a data attribute in `index.html`) at build
time and add `<link rel="modulepreload">`.

### R7 — do not gate `Main()` behind serially-loaded interop JS

**Severity: high. Saves one round trip plus ~57 KB brotli.**

Once the runtime signals ready, four more JS files are loaded through RequireJS and managed
`Main()` runs only when all have resolved. None appear in the HTML, so none can be
preloaded:

```js
// uno-config.js
config.uno_dependencies = [ Uno.Runtime.Wasm.js (218 KB), setImmediate.js (7 KB),
                            Uno.Wasm.js (136 KB), AppManifest.js ];
// Bootstrapper.ts:894-907 — require([dep], processDependency); mainInit() only when pending === 0
```

Convert these to static ES module imports so they are fetched in parallel with `dotnet.js`,
which also allows `require.js` (a render-blocking classic script) to be dropped for the
`Microsoft.NET.Sdk.WebAssembly` code path. Separately, split the WinRT surface in
`Uno.Wasm.js` so sensor/MIDI/picker/clipboard glue loads on first use rather than before
startup.

### R8 — defer eager theme-dictionary construction

**Severity: high. Part of the 259 ms compute gap. Effort: large.**

`App.xaml` unconditionally merges `XamlControlsResources`, whose constructor eagerly calls
`GlobalStaticResources.Initialize()`, `RegisterDefaultStyles()` and
`RegisterResourceDictionariesBySource()` for both v1 and v2 theme dictionaries before
`Source` is assigned. `XamlControlsResources.UpdateAcrylicBrushes` then forces construction
of ~20 `AcrylicBrush` values whether or not the app uses acrylic.

This is fixed framework cost, independent of app size, and it runs on the Mono interpreter
where managed code is roughly an order of magnitude slower than native.

Defer top-level dictionary construction until a resource lookup actually misses and needs to
walk `MergedDictionaries`; patch the Acrylic brushes from their generated accessors rather
than eagerly after construction.

### R9 — register the service worker after first frame

**Severity: medium.**

The template ships `manifest.webmanifest`, which enables a service worker whose
`offline_files` list names all 37 fonts, all seven splash-screen scale variants, every icon
and the framework files. That precache begins while the runtime is still downloading, so it
competes for connections and bandwidth exactly when startup is most sensitive.

### R10 — triage: splash removal is latency-fragile

**Severity: needs triage before it can be ranked.**

With the server adding as little as **20 ms of latency per response**, and no bandwidth
limit at all, the loader never disappears — observed out to 180 s — even though every HTTP
request completed within ~6 s and the canvas had rendered:

```
server unshaped        → loader removed at 1 165 ms
server 100 Mbps        → loader removed at 1 559 ms
server 20 Mbps         → NEVER (within 40 s)
server +20 ms latency  → NEVER (within 40 s)      ← latency alone is sufficient
```

This reproduces with plain server-side shaping, so it is not an artifact of CDP network
emulation. The likely mechanism is the chain in `WebAssemblyWindowWrapper.ShowCore` (R2):
`RemoveSplashScreen()` fires only from a `FrameRendered` callback, which is armed from a
`LayoutUpdated` handler, which is armed from a `NativeDispatcher.Main.Enqueue` continuation
on `FontPreloadTask`. If `ShowCore` runs before `XamlRoot.Content` is a `FrameworkElement`,
if `LayoutUpdated` has already fired for that pass, or if `InvalidateRender` produces no
further frame, nothing re-arms the chain and the loader stays up permanently. Different
asset arrival ordering under latency plausibly changes which of these races is lost.

Reproduce with a non-blank app under a shaped link and instrument that chain. If it affects
production apps on ordinary networks it outranks everything else here — but it needs
confirmation beyond the blank template before being treated as such.

## Ruled out

Plausible hypotheses the evidence does not support. Recorded so they are not
re-investigated.

| Hypothesis | Verdict |
|---|---|
| Sharded ICU `.dat` files are a Uno-specific cost | All three ship byte-identical `icudt_EFIGS/CJK/no_CJK.dat` from the same SDK. Zero delta. |
| Uno disables the jiterpreter | Not disabled on the default Skia WASM publish path — only during opt-in AOT profile generation. |
| AOT is on for competitors, off for Uno | Off by default for all three. Shared baseline. |
| Threading overhead | Off by default for all three. |
| SkiaSharp / HarfBuzz native size | Avalonia links the same libraries and pays the same ~2.7 MB brotli. |
| Linker root descriptors defeat trimming | No XML `TrimmerRootDescriptor` rooting large surface area found for WebAssembly. |
| `BindableTypeProviders` codegen bloats the app assembly by megabytes | Corrected: all IL and metadata in the app DLL totals ~495 KB. The 6 MB is the ICU blob (R1). |
| Trimming is disabled in Release | `UNO_BOOTSTRAP_LINKER_ENABLED=False` in `uno-config.js` is a stale diagnostic; ILLink does run. |
| Wide reflection / `Assembly.GetTypes()` at startup | None on the startup path; DP registration is genuinely per-type lazy. |
| WebGL context fallback probing is slow | Synchronous and cheap; not a measurable contributor. |
| Hot reload left enabled in Release | Correctly gated off in `Bootstrapper.ts`. |
| `HybridGlobalization` could remove ICU entirely | Removed from the .NET 10 browser-wasm SDK; not available on `net10.0-browserwasm`. |

## Sequencing

**Phase 1 — build configuration (R1, R3, R4, R5, R9).** Estimated −5.0 MB brotli. No public
API change, no WinUI behaviour change. Should take a blank app from 11.87 MB to roughly
6.5 MB transferred — about 2.1 s of wire time at 20 Mbps — putting Uno within range of
Avalonia on payload-bound loads without touching runtime code.

**Phase 2 — boot sequence and first-frame gating (R2, R6, R7, R10).** Estimated −270 ms
perceived and one round trip. Includes embedding the default font as a managed resource.

**Phase 3 — framework payload and managed startup (R8).** Reduce `Uno.UI`'s 1.8 MB brotli
footprint via per-control style linking; split `Uno.Wasm.js` per feature area; wire
profile-guided AOT into `Uno.Sdk` — a capability neither competitor has, currently left on
the table.

Phase estimates are projections from measured component sizes, not measured outcomes.

## Method & reproduction

Three blank templates: `dotnet new unoapp -preset blank -platforms wasm` (Uno.Sdk 6.6.42,
`UnoFeatures=SkiaRenderer`, `net10.0-browserwasm`), `dotnet new avalonia.xplat` (Browser
head), and `dotnet new blazorwasm`. All published `-c Release` and served by a static Node
server with brotli negotiation, `Cache-Control: no-store`, and optional token-bucket
bandwidth and latency shaping.

Each run uses a fresh browser context with service workers blocked, so every load is cold. A
page init script patches `WebGLRenderingContext.drawArrays/drawElements`,
`CanvasRenderingContext2D` draw methods, `HTMLCanvasElement.getContext` and the
`WebAssembly` compile/instantiate entry points to timestamp first paint precisely, with a
`MutationObserver` for DOM-rendered content and a 25 ms poll on `.uno-loader` for splash
removal.

Bandwidth shaping is applied **server-side** rather than through CDP
`Network.emulateNetworkConditions`, because CDP throttling also applies to `blob:` URLs and
stalls the image-loader web workers (`Uno.UI.Runtime.Skia.WebAssembly.Browser/ts/Runtime/ImageLoader.ts`
creates worker scripts via `URL.createObjectURL`), which corrupts the measurement.

Source claims were verified against this branch's checkout, the Uno.Wasm.Bootstrap and
Avalonia repositories, and the shipping NuGet targets. Assembly contents were confirmed with
`System.Reflection.Metadata` and `monodis --presources`.

### Caveats

- Timings come from headless Chromium on an arm64 Mac. Absolute values will differ
  elsewhere; the ratios were stable across runs.
- Avalonia's browser backend selected Canvas2D while Uno selected WebGL2, so a small part of
  the compute gap may reflect backend choice rather than framework overhead.
- The benchmark apps are blank templates. They isolate fixed framework cost, which is the
  right lens for startup, but a real app's own payload would dilute these ratios.
- Measurements were taken against Uno.Sdk 6.6.42 (the shipping stable that users experience);
  source was read from `feature/breakingchanges` (7.0-dev), where the fixes would land. The
  cited code paths were confirmed present on this branch.
