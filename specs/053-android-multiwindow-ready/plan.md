# Android multi-window-ready architecture

Tracking: Android multi-window support (public issue **#13827**).

## Goal & scope

Skia-on-Android currently bakes "one window" into three process-wide static anchors.
This work **de-singletons** those anchors so Android converges onto the same
per-window ownership pattern the Skia desktop runtimes (Win32/X11/macOS) and the
Apple UIKit runtime already use, making the architecture **multi-window-ready**.

**In scope (this work):**
- Per-window `NativeWindowWrapper` instances (one per `Window`/`XamlRoot`), not a singleton.
- Per-window render stack on `ApplicationActivity` (render view, native-layer host,
  root layout) — instance state, not `static`.
- A per-window `IXamlRootHost` that resolves its own `RootElement`, render view and
  input sources instead of reaching `Window.Current` / `ApplicationActivity.Instance`.
- Per-window input sources (pointer + keyboard), resolved from the host (the Win32 pattern).
- `ContextHelper` **split + foreground fallback**: an explicit app-global
  `ApplicationContext`, and `Current` that tracks the *foreground* activity via the
  existing `BaseActivity` registry (fixing today's sticky "last-ever-set" behaviour)
  and falls back to the application context.

**Out of scope (deliberate follow-ups):**
- Flipping `SupportsMultipleWindows` to `true` and driving a *live* second Activity —
  that needs Activity↔Window lifecycle orchestration and on-device validation, and
  mirrors how iOS staged its own multi-window behind scene adoption (#8341).
- Threading an explicit owning-window `Context` through every `Uno.UWP`/AddIn
  `ContextHelper.Current` consumer. While only one window is live this is a no-op;
  those callers stay on the (now-correct) foreground fallback until live multi-window lands.

`SupportsMultipleWindows` stays **`false`**. Definition of done for this work is:
**per-window instances everywhere, and zero single-window regressions.**

## Current architecture — the three anchors

The framework layer (`Window → DesktopWindow → DesktopWindowXamlSource → XamlIslandRoot →
ContentRoot/VisualTree → XamlRoot`, plus `XamlRootMap` and `NativeWindowWrapperBase`) is
**already per-window**. Single-window-ness lives entirely in:

1. **`NativeWindowWrapper.Instance`** — `Lazy<>` singleton in
   `src/Uno.UI/UI/Xaml/Window/Native/NativeWindowWrapper.Android.cs` (linked into the
   Skia.Android assembly), returned by `AndroidSkiaWindowFactory.CreateWindow` for
   *every* window; its `NativeWindow`/`Title`/`ShowCore` defer to `ApplicationActivity.Instance`.
2. **`ApplicationActivity.Instance`** + `static _renderView` / `_renderViewAsView` /
   `_nativeLayerHost` / `RelativeLayout` / `_started`
   (`src/Uno.UI.Runtime.Skia.Android/ApplicationActivity.cs`).
3. **`ContextHelper.Current`** — app-global `Android.Content.Context`
   (`src/Uno.UWP/ContextHelper.cs`); ~40 activity-specific reads, ~55 app-context reads.

`BaseActivity` (`src/Uno.UI.Runtime.Skia.Android/UI/Xaml/Controls/BaseActivity.cs`) already
maintains a multi-activity registry (`_instances`, `Current`, `CurrentChanged`) — the seed
the foreground fallback and per-window ownership build on.

Reference: native Android UI is dropped on this branch — this is purely a Skia-Android host
consolidation; the `Uno.UWP`/`Uno.Foundation` Android assemblies must keep compiling.

## Target shape (mirrors Win32/X11/iOS)

- `AndroidSkiaWindowFactory.CreateWindow(window, xamlRoot)` → `new NativeWindowWrapper(...)`
  bound to the owning `ApplicationActivity`, registered in `XamlRootMap` via a per-window
  `AndroidSkiaXamlRootHost`.
- `AndroidSkiaXamlRootHost` owns/references its window's `ApplicationActivity`, render view,
  native-layer host and input sources; `RootElement => <its window>.RootElement`;
  `InvalidateRender()` invalidates *its* view.
- Consumers (`ContentPresenter` native hosting, TextBox notifications, IME) resolve the owning
  activity/render view via `XamlRoot → XamlRootMap.GetHostForRoot → host`.
- Input sources registered `ApiExtensibility.Register<IXamlRootHost>(typeof(IUnoCorePointerInputSource), host => …)`.
- `ContextHelper`: `ApplicationContext` (app-global) + `Current` = foreground activity
  (registry-backed) with app-context fallback.

## Phases (each compiles for `net10.0-android`; committed separately)

1. **ContextHelper split + foreground fallback.** `ContextHelper.ApplicationContext`;
   faithful foreground tracking driven from `BaseActivity` (`SetAsCurrent`/`ResignCurrent`
   repoint to the next live activity or app context). Unit-testable in isolation.
2. **Per-window host.** `AndroidSkiaXamlRootHost` captures its `Window` + `ApplicationActivity`;
   `RootElement`/`InvalidateRender` become per-window; `AndroidSkiaWindowFactory` wires owner.
3. **Per-window wrapper.** `NativeWindowWrapper.Android` singleton → instance bound to its
   activity; drop `Instance`; `BaseActivity` lifecycle drives *its* wrapper (activity→wrapper map).
4. **De-static the render stack.** `ApplicationActivity` `Instance`/render fields → instance;
   native-element hosting, TextBox provider, IME resolve via `XamlRoot`→host.
5. **Per-window input sources.** Host owns pointer + keyboard sources; register keyed on
   `IXamlRootHost`; `ApplicationActivity` dispatch targets its own host.
6. **Validate.** `net10.0-android` compile after every phase; unit tests for the registry /
   foreground-fallback / resolution logic; single-window runtime smoke where a device is
   available. Live two-window validation is explicitly deferred (see out of scope).

## Validation strategy

- **Compile:** `dotnet build …Skia.Android.csproj -p:UnoTargetFrameworkOverride=net10.0-android`
  after each phase (the only assembly that compiles `__ANDROID__` Skia code).
- **Unit:** logic tests for the activity registry foreground fallback and `XamlRoot→host`
  resolution where they can run without a device.
- **Runtime:** single-window smoke on an emulator/device where available; report honestly
  when device runtime validation isn't executed here. Two live windows is a follow-up.
