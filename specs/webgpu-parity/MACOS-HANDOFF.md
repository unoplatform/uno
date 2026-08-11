# WebGPU backend — macOS / iOS handoff (read me first)

You are picking up the **neutral WebGPU drawing-backend** work on a macOS machine. This doc catches you up and
tells you exactly what to build, run, and look for. **Before doing anything, read the master doc:**
`specs/webgpu-parity/RUNNING-CONTEXT.md` — it is the durable source of truth (env vars, seam design rules,
per-primitive parity status, decisions). Sections **§33–§38** are the most recent and directly relevant.

Branch: `feature/drawing-backend-abstraction`.

---

## 1. What this is

Uno has a **backend-neutral drawing seam** (`IDrawingSession` / `ICommandRecorder` / `IRenderData` /
`CompositionTarget.Renderer`). A shared WebGPU backend (`src/Uno.UI.Composition.WebGpu/WebGpuBackend.cs`, ~4000 lines)
implements it and is used by **all** heads (desktop Win32/X11, WASM, Android, and now macOS/iOS). It is a faithful
port of the reference `ramez/webgpu-experiment` renderer.

Status by platform:
- **Desktop (Win32 / X11 / macOS-via-Skia)** and **WASM**: working.
- **Android**: runs and initializes WebGPU, but the on-screen render is **still visually broken** (open issue — do
  NOT assume it's correct). Two Android fixes already landed this cycle: an APK native-lib packaging leak (§33) and
  an sRGB present-blit gamma compensation (§35–§36). Fonts fail to load on Android for an **unrelated** asset reason.
- **macOS / iOS**: wiring was just authored (§37–§38) but is **NOT validated on an Apple machine** — that is your job.

---

## 2. Your mission (in order)

1. **macOS first** (this machine). Get the WebGPU path to build and render, then judge correctness vs the default
   Skia/Metal renderer.
2. **iOS** second (needs the iOS workload + a device/simulator).

Work the same loop we used for Android: build → run → read the `[webgpu]` log lines → fix the first concrete failure
→ repeat. Follow `.claude/rules/debugging-discipline.md`: reproduce, name the broken invariant, fix root cause, label
evidence (code-review vs compile vs runtime). Don't present compile-only as runtime-validated.

---

## 3. What was just wired (all opt-in via `UNO_WEBGPU`)

macOS (native source is in-repo — you can and must rebuild it):
- `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/…`:
  - `UNOMetalViewDelegate.{h,m}`: added `@property BOOL webgpuMode`. When set, `drawInMTKView` **skips** its own
    `currentDrawable`/`presentDrawable` and just ticks managed code with `texture == NULL` (so wgpu owns the layer's
    drawables — no contention).
  - `UNOWindow.{h,m}`: `uno_window_get_metal_layer(window)` (returns the MTKView's `CAMetalLayer`) and
    `uno_window_set_webgpu_mode(window, bool)`.
- `src/Uno.UI.Runtime.Skia.MacOS/Native/NativeUno.cs`: `LibraryImport` decls for both.
- `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs`: under `RenderSurfaceType.Metal` + `UNO_WEBGPU`,
  gets the layer, builds `WebGpuSwapChainContext(BGRA8Unorm, CreateMetalSurface(inst, layer))`, sets
  `CompositionTarget.Renderer = new WebGpuRenderer(ctx.Device)`, calls `uno_window_set_webgpu_mode(true)`, and routes
  `MetalDraw` through the neutral seam (`OnNativePlatformFrameRequested(null, size => ctx.AcquireRenderTarget(...))`
  + `ctx.Present()`). Skips `GRContext` in that case.
- Managed macOS side **compiles clean on Linux** (`net10.0`). The native `.m` changes were **not** compiled anywhere yet.

iOS/tvOS (all unvalidated):
- `src/Uno.UI.Composition.WebGpu/Native/WebGpuLoader.cs`: on iOS/tvOS/MacCatalyst, resolves `DllImport("webgpu")` to
  `NativeLibrary.GetMainProgramHandle()` (static link).
- `src/Uno.UI.Composition.WebGpu/wgpu-native.targets`: `_ProvisionWgpuNativeIos` fetches the static `libwgpu_native.a`
  slice by RID and links it via `@(NativeReference Kind=Static ForceLoad=True)`.
- `src/Uno.UI.Runtime.Skia.AppleUIKit/Rendering/UnoSKWebGpuMetalView.cs` (CAMetalLayer `UIView`),
  `IAppleUIKitRenderView`, `RootViewController` opt-in + `OnWebGpuFrameRequested`, csproj references the backend.
- `SamplesApp.Skia.netcoremobile` head imports the iOS provisioning.

---

## 4. Build & run — macOS

Setup (once):
```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props   # if not present
# set <UnoTargetFrameworkOverride>net10.0</UnoTargetFrameworkOverride> and <UnoFastDevBuild>true</UnoFastDevBuild>
```

**Step 1 — rebuild the native macOS helper** (the `.m` changes above must be compiled into `libUnoNativeMac.dylib`):
```bash
open src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac.xcodeproj   # build in Xcode
# or xcodebuild -project ... ; then place the built dylib where the build copies runtimes/osx/native/ from.
```
Confirm the new dylib is what the app loads (check its path in the build output / .app bundle). This is the step that
can't be done off-Mac, so verify it actually took (e.g. `nm -gU libUnoNativeMac.dylib | grep uno_window_get_metal_layer`).

**Step 2 — build the macOS head** (`SamplesApp.Skia.Generic` serves macOS; it already provisions the wgpu `osx` dylib):
```bash
dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Debug -f net10.0 \
  -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0
```

**Step 3 — enable WebGPU and run.** `UNO_WEBGPU` is read from the environment. The convenient local default we used on
other heads (`Generic/Program.cs`) is **LOCAL-ONLY and intentionally NOT committed**, so either:
- export it before launching the built binary directly:
  ```bash
  UNO_WEBGPU=1 dotnet src/SamplesApp/SamplesApp.Skia.Generic/bin/Debug/net10.0/SamplesApp.Skia.Generic.dll
  ```
- or re-add a local-only default at the top of `Generic/Program.cs` (do not commit):
  ```csharp
  if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNO_WEBGPU")))
      Environment.SetEnvironmentVariable("UNO_WEBGPU", "1");
  ```

**Success looks like:** the log line `Neutral graphics pipeline active: WebGpu context via WebGpuRenderer (macOS).`
and `[webgpu] backend init …` / `[webgpu] surface … format=…`.

---

## 5. Known likely first-run issues on macOS (predicted, unverified)

- **CAMetalLayer contention / config**: the layer belongs to the MTKView; wgpu reconfigures it (device, pixelFormat,
  drawableSize) each frame. Watch for a validation error or a blank/black layer. If the MTKView keeps fighting wgpu
  for the layer, consider pausing the MTKView's own display loop in `webgpuMode` (it already skips present; you may
  also need `enableSetNeedsDisplay`/`paused` tweaks in the native view).
- **sRGB gamma**: check the `[webgpu] surface … format=` line. If it's an `*_Srgb` format, the present blit already
  compensates (decode→re-encode; see §36 `BlitWgslSrgb`). If colors look washed out / too bright, that path may need
  the same attention it got on Android.
- **The Android render is broken and NOT yet root-caused.** macOS is a *different surface/present path* but shares the
  same `WebGpuBackend` scene rendering. If macOS shows the *same* visual breakage as Android, that's a strong signal
  the bug is in the shared backend (scene rendering), not the platform glue — a valuable data point. If macOS looks
  correct but Android doesn't, the bug is Android-specific. Either way, report which.

---

## 6. Build & run — iOS (after macOS)

```bash
dotnet workload install ios       # if missing
dotnet build src/SamplesApp/SamplesApp.Skia.netcoremobile/SamplesApp.Skia.netcoremobile.csproj \
  -c Debug -f net10.0-ios -p:UnoTargetFrameworkOverride=net10.0-ios -p:UnoFastDevBuild=true
```
Enable `UNO_WEBGPU` (env, or a local-only default in `Main.iOS`/the head, mirroring Android's `Main.Android.cs`).
Likely first failure is the **native link**: if it fails with undefined symbols, add the missing frameworks/`libc++`
as `@(NativeReference)` in `_ProvisionWgpuNativeIos` (`wgpu-native.targets`). Verify the release zip really lays the
archive at `lib/libwgpu_native.a`. Asset names are confirmed at pin `v29.0.1.1` (see §38).

---

## 7. Diagnostics you have

Env toggles (read by `WebGpuBackend` / `WebGpuSwapChainContext`):
- `UNO_WEBGPU=1` — enable the WebGPU render path.
- `UNO_WEBGPU_PROFILE=1` — per-frame timing/alloc profiler (`[webgpu-profile] …`).
- `UNO_WEBGPU_TRACE=1` — dump the command stream (pass/draw list) — use to check whether draws are even recorded.
- `UNO_WEBGPU_PRESENT=mailbox|immediate|fiforelaxed`, `UNO_WEBGPU_MSAA=1|4`, `UNO_WEBGPU_PIPELINE=0` (blocking drain).

There is also a sample-trace harness (local-only, in the uncommitted `App.xaml.cs` hack, `UNO_WEBGPU_SAMPLE_TRACE`)
that dumps the command stream for specific samples so you can diff against the reference. Re-add locally if useful.

---

## 8. Rules of the road

- **Skia-first scope**: new work targets Skia targets (incl. Skia-on-Android/iOS/macOS). Don't break native targets.
- **Seam neutrality** (see RUNNING-CONTEXT "seam design rules"): keep SkiaSharp out of the neutral core; `SKPath`/
  Metal/wgpu handles stay inside their backend. The WebGPU backend must not leak platform types up the seam.
- **Update `RUNNING-CONTEXT.md`** as you learn things (append a new numbered section). It is lossy memory insurance.
- Commit in logical, Conventional-Commit groups. Do **not** commit: `crosstargeting_override.props`, the `UNO_WEBGPU`
  local-default hacks (`Generic/Program.cs`, `Main.Android.cs`, WASM `Program.cs`, the `App.xaml.cs` trace harness),
  or delivered binaries (`*.apk`, `*.zip`).

Start by reading `RUNNING-CONTEXT.md` §33–§38, then do §4 above.
