# Software keyboard on TextBox focus for Skia desktop

**Status:** Implemented. Trigger + Win32 `IInputPaneExtension` + occlusion forwarding done; X11/macOS/FrameBuffer registered as documented no-ops. Trigger logic is runtime-test-green on Skia Desktop; the Win32 OS-keyboard path is compile-validated and needs manual touch-device validation. `NativeMethods.txt` changes (planned Task 4) proved unnecessary — the WinRT activation uses manual `[DllImport]`/`[ComImport]` (no removed `UnmanagedType.IInspectable`/`HString` marshaling), so only the already-generated `ScreenToClient`/`RECT`/`HWND` are consumed.
**Issue:** unoplatform/uno#17363 — "[Skia] Desktop targets should show software keyboard when `TextBox` is focused".

---

## Context

Uno already ships a cross-platform `Windows.UI.ViewManagement.InputPane` (`GetForCurrentView()`, `TryShow()`/`TryHide()`, `Visible`, `OccludedRect`, `Showing`/`Hiding`). On Skia (`InputPane.skia.cs`) the show/hide calls are delegated to a platform-provided `IInputPaneExtension` resolved through `ApiExtensibility.CreateInstance<IInputPaneExtension>(this, …)`. Native Android and iOS register such an extension (their native `EditText`/`UITextField` also raise the keyboard automatically); the Skia-on-Android and Skia-on-iOS hosts register one too.

On Skia **desktop** targets (Win32, X11, macOS, FrameBuffer) **no `IInputPaneExtension` is registered**, and **nothing in the Skia `TextBox` focus path ever calls `InputPane.TryShow()`**. The result: focusing a `TextBox` on a touch-capable Windows device (tablet / 2-in-1) raises no on-screen keyboard.

## Problem

Two independent gaps, both required for the feature:

1. **No platform implementation.** `Win32Host` (`Win32Host.cs:53-112`) registers ~25 extensions but not `IInputPaneExtension`, so `InputPane.skia.cs`'s lazy extension stays null and `TryShow`/`TryHide` silently return `false`. Same for X11/macOS/FrameBuffer hosts.
2. **No trigger.** `TextBox.OnFocusStateChangedPartial` (`TextBox.skia.cs:420`) runs `StartImeSession()`/`EndImeSession()` on focus/blur but never touches `InputPane`. Native Android/iOS get the keyboard for free from the native edit control; Skia must drive it explicitly.

A third, latent issue surfaces once we wire this up:

3. **`TryHide` short-circuit.** `InputPane.TryHide()` returns early when `!Visible`, and `Visible` is computed as `OccludedRect.Height > 0`. On desktop we never populate `OccludedRect`, so `Visible` is permanently `false` and **`TryHide` would never reach the platform**. The keyboard could be shown but never programmatically hidden. Fixing this requires the OS to report its occlusion rectangle back into `InputPane.OccludedRect` (which is also how Android already makes `Visible` accurate).

## How the references handle it (evidence)

| Source | Mechanism | Trigger / gating |
|--------|-----------|------------------|
| **WPF** (`PresentationCore/MS/internal/Interop/TipTsfHelper.cs`) | **Explicit** `IInputPaneInterop.GetForWindow(hwnd)` → `IInputPane2.TryShow()` | Called from `UIElement.Focus()`; gated on platform support + touch/stylus support + focused element exposing the UIA **Text** pattern. Does **not** call `Hide` (lets the OS auto-hide). |
| **Avalonia** (`Avalonia.Win32/Input/WindowsInputPane.cs`) | **Monitor-only** `IFrameworkInputPane.AdviseWithHWND` → handler `Showing(RECT*, BOOL)` / `Hiding(BOOL)`; keyboard itself raised implicitly by activating IMM32 on focus. | OS-driven. |
| **Flutter** (`shell/platform/windows/text_input_manager.cc`) | **Implicit** IMM32 (`CreateCaret` on `WM_SETFOCUS`, `ImmSetCompositionWindow`); `TextInput.show/hide` channel methods are **no-ops**. | OS-driven. |

The **explicit `IInputPaneInterop` path is the right fit for Uno** because it maps **1:1 onto Uno's existing `InputPane` API and WinUI's public surface**, works without turning Uno's custom-rendered `TextBox` into a real TSF text store, and is the same mechanism dotMorten demonstrated for WPF/WinForms and that Firefox uses in production (`widget/windows/OSKInputPaneManager.cpp`). The implicit IMM32/TSF route is Windows-only, far heavier, and would leave the public `InputPane` API non-functional.

### Confirmed Windows API

- **Show/Hide:** `IInputPaneInterop` (IID `75CF2C57-9195-4931-8332-F0B409E916AF`, `IInspectable`-derived) `GetForWindow(HWND, REFIID, out InputPane)` → `IInputPane2` (IID `8A6B3F26-7090-4793-944C-C3F2CDE26276`) `bool TryShow()` / `bool TryHide()`. Acquired via `RoGetActivationFactory("Windows.UI.ViewManagement.InputPane", IID_IInputPaneInterop, …)`.
- **Occlusion monitoring:** plain-COM `IFrameworkInputPane` (CLSID `FrameworkInputPane` `D5120AA3-46BA-44C5-822D-CA8092C1FC72`, IID `5752238B-24F0-495A-82F1-2FD593056796`) `AdviseWithHWND(hwnd, IFrameworkInputPaneHandler, out cookie)`; the handler's `Showing(RECT* prcScreen, BOOL ensureInView)` / `Hiding(BOOL)` deliver the keyboard rectangle as a plain `RECT` — **no WinRT `TypedEventHandler` marshaling needed**.
- **Constraints:** minimum Windows 10 1607 (build 14393); `GetForWindow` wants the **top-level (GA_ROOT)** HWND and returns a **per-window** `InputPane` (cache per HWND); `TryShow` is **best-effort** — it returns `false` (and shows nothing) when the window isn't foreground or a **hardware keyboard is attached** (a second, OS-level guard on top of our touch gating).

Using `IInputPaneInterop`+`IInputPane2` for show/hide **and** `IFrameworkInputPane` for occlusion gives full functionality with only `[ComImport]`/CsWin32-friendly interfaces — combining the two halves WPF and Avalonia each use.

## Goals

- Focusing a `TextBox`/`PasswordBox` **via touch or pen** on Skia **Win32** raises the OS touch keyboard; moving focus away (to a non-text element) hides it.
- The public `InputPane.GetForCurrentView().TryShow()/.TryHide()` becomes **functional on Win32** (apps can call it directly), and `Visible`/`OccludedRect`/`Showing`/`Hiding` reflect reality.
- The keyboard's occlusion rectangle drives the existing `ScrollContentPresenter.Pad` pan-into-view so the keyboard doesn't cover the focused field (WinUI parity).
- X11/macOS/FrameBuffer keep compiling and behave honestly (documented no-op).

## Non-goals

- Native (non-Skia) UI targets — unchanged (they already work via native controls).
- Linux/macOS on-screen keyboards — no robust programmatic OS API exists (X11 OSKs are environment-specific; macOS Keyboard Viewer is strictly user-controlled). These return `false`.
- TSF/IMM32 auto-invoke re-architecture — out of scope; the explicit `InputPane` path supersedes it.
- A new public opt-in/opt-out API surface — see "Decisions" (gating is automatic; not configurable in v1).

## Decisions (confirmed)

1. **Trigger = touch/pen-initiated focus only.** Gate on `VisualTree.GetContentRootForElement(this)?.InputManager.LastInputDeviceType` ∈ { `Touch`, `Pen` } (the same `InputDeviceType` signal WinUI/Uno already use in `Control.mux.cs`, `FlyoutBase`, `Slider`). Mouse / keyboard / programmatic focus never raises the keyboard. The OS best-effort guard (hardware-keyboard attached → `TryShow` no-ops) is a second line of defense. Not configurable in v1.
2. **Platform scope = Win32 full; X11/macOS/FrameBuffer no-op.** Register an honest no-op `IInputPaneExtension` (returns `false`, logs once) on the non-Windows desktop hosts so the abstraction is consistent and the rationale is documented in code.
3. **Occlusion = forward the OS rectangle.** Subscribe via `IFrameworkInputPane.AdviseWithHWND`; on `Showing`, convert the screen `RECT` to client DIPs and set `InputPane.OccludedRect`; on `Hiding`, clear it. This makes `Visible` accurate, makes `TryHide` work, and feeds pan-into-view.
4. **Window association = singleton that dynamically targets the focused window.** There is exactly one OS touch keyboard, so `InputPane` stays a process singleton (`GetForCurrentView()` keeps working; Android/iOS untouched), but every operation targets the **window that owns the currently focused `TextBox`** rather than the foreground/primary window or the hardcoded initial window. See "Multi-display & multi-window".

## Design

### Component 1 — Trigger in the shared Skia `TextBox` (`Uno.UI`)

A new partial method on the Skia `TextBox` focus path drives the InputPane:

- In `TextBox.OnFocusStateChangedPartial` (`TextBox.skia.cs`):
  - **Focus gained** (`focusState != Unfocused`): if `LastInputDeviceType` is `Touch`/`Pen`, set the InputPane's target to `this.XamlRoot` (so the platform extension addresses *this* window — see "Multi-display & multi-window") and call `InputPane.GetForCurrentView().TryShow()`.
  - **Focus lost** (`focusState == Unfocused && !_forceFocusedVisualState`): request hide via a **deferred-hide** (see anti-flicker) — `InputPane.GetForCurrentView().TryHide()`.
- `PasswordBox` inherits this automatically (same `TextBox` focus path; it deliberately skips the *IME* session but must still show the keyboard).
- The gating + InputPane calls live behind a `partial void` so non-Skia builds are unaffected; the implementation is `TextBox.SoftwareKeyboard.skia.cs` (or folded into `TextBox.skia.cs`).

**Anti-flicker (TextBox → TextBox):** moving focus directly from one editable control to another must not flash the keyboard. On focus-out, **schedule** the `TryHide` on the dispatcher; if another editable control gains touch/pen focus (calls `TryShow`) before it runs, **cancel** the pending hide. (`InputPane`'s own `Visible` short-circuit also helps: a `TryShow` while already visible is a no-op.) This respects the project's "no user fighting / no flicker" rule.

### Component 2 — Win32 `IInputPaneExtension` (`Uno.UI.Runtime.Skia.Win32`)

`Win32InputPaneExtension : IInputPaneExtension` (singleton, mirroring `Win32ImeTextBoxExtension`):

- **Window resolution.** The extension resolves the **top-level HWND of the window that owns the focused control** — never the foreground/primary window. It reads the InputPane's target `XamlRoot` (set by the trigger; falling back to the focused element via `FocusManager` for direct app calls), then `XamlRootMap.GetHostForRoot(xamlRoot) as Win32WindowWrapper` → `((Win32NativeWindow)wrapper.NativeWindow).Hwnd` (the exact pattern in `Win32TextBoxNotificationsProviderSingleton.TryGetHwnd`). Passing the owning window's HWND is what makes the OS dock the keyboard on that window's monitor in a multi-display setup.
- **`TryShow()`:** lazily acquire (and cache per-HWND) the WinRT `InputPane` via `RoGetActivationFactory` → `IInputPaneInterop.GetForWindow(hwnd, IID_IInputPane2)`; call `IInputPane2.TryShow()`. Guard with `OperatingSystem.IsWindowsVersionAtLeast(10, 0, 14393)`. Lazily `AdviseWithHWND` an `IFrameworkInputPane` handler for that HWND. Return the honest `bool`.
- **`TryHide()`:** `IInputPane2.TryHide()`.
- **Occlusion handler** (`IFrameworkInputPaneHandler`): on `Showing(RECT* screenRect, …)` the `screenRect` is in **virtual-desktop screen pixels** → `ScreenToClient(owningHwnd, …)` + `/ RasterizationScale` → client-DIP `Rect` → marshal to UI thread → `inputPane.OccludedRect = rect`. Because `ScreenToClient` uses the owning window's HWND, the math is correct on whichever monitor the window/keyboard is on. On `Hiding(…)` → `inputPane.OccludedRect = default`. (`InputPane.OnOccludedRectChanged` already fires `Showing`/`Hiding` and schedules `EnsureFocusedElementInView` → `ScrollContentPresenter.Pad`.)
- **Lifetime:** `Unadvise` the cookie and release cached `InputPane` when its window closes.

**COM declarations.** Add to CsWin32 `NativeMethods.txt` (in `Uno.UI.Runtime.Skia.Win32.Support`): `RoGetActivationFactory`, `WindowsCreateString`, `WindowsDeleteString`, `IFrameworkInputPane`, `IFrameworkInputPaneHandler`, `CoCreateInstance` (present), `ScreenToClient` (present), `RECT` (present). Declare `IInputPaneInterop` and `IInputPane2` as small `[ComImport, InterfaceType(InterfaceIsIInspectable)]` interfaces in the extension file (following the WPF-Samples `InputPaneRcw.cs` shape and the project's existing `ComImport` precedent in `Accessibility/*`). The C# `IFrameworkInputPaneHandler` callback object is implemented as a managed COM-visible class.

### Component 3 — No-op extensions for X11 / macOS / FrameBuffer

`{X11,MacOS,FrameBuffer}InputPaneExtension : IInputPaneExtension` returning `false` from both methods, with a one-line comment + single debug log explaining there is no portable programmatic OSK API on that platform. Registered in each host's extension-registration block (`X11ApplicationHost`, `MacSkiaHost`, `FramebufferHost`).

### Registration

Mirror `ApiExtensibility.Register(typeof(IImeTextBoxExtension), _ => Win32ImeTextBoxExtension.Instance);`:

- `Win32Host` cctor: `ApiExtensibility.Register(typeof(IInputPaneExtension), _ => Win32InputPaneExtension.Instance);`
- X11/macOS/FrameBuffer hosts: register their no-op singletons.

### Multi-display & multi-window

There is exactly **one** OS touch keyboard. `InputPane` therefore stays a process singleton, but is made *window-aware*:

- **Target window.** A new internal `InputPane.XamlRoot` (set in `InputPane.skia.cs`) records the window the pane currently addresses. The `TextBox` trigger sets it to `this.XamlRoot` before `TryShow()`; direct app calls fall back to the focused element's `XamlRoot` via `FocusManager`. The Win32 extension resolves the HWND from this `XamlRoot`, so `IInputPaneInterop.GetForWindow` always gets the **owning** window's top-level HWND.
- **Multi-display placement.** Keyboard placement is OS-owned and follows the associated window — passing the touch-screen window's HWND makes the OS raise the keyboard on that monitor. We deliberately do **not** try to move the keyboard to a specific display (no public API; would fight the OS, violating the project's no-user-fighting rule).
- **Multi-display occlusion.** The `IFrameworkInputPane.Showing` `RECT` is in virtual-desktop coordinates; `ScreenToClient(owningHwnd, …)` makes the derived `OccludedRect` correct regardless of monitor.
- **Pan-into-view fix.** `InputPane.skia.cs`'s `EnsureFocusedElementInViewPartial` currently hardcodes `Window.InitialWindow` (`InputPane.skia.cs:40`) — wrong for any non-initial window. It will instead use the focused element's actual `XamlRoot` (the pane's target), so the focused field is brought into view in the correct window. (Native Android/iOS keep their existing single-window behavior.)
- **`GetForCurrentView()` semantics.** Preserved for back-compat and to model the single OS keyboard; because every operation targets the focused window dynamically, it behaves correctly in multi-window apps. (WinUI 3 desktop doesn't support `GetForCurrentView()` at all and requires `InputPaneInterop.GetForWindow`; introducing explicit per-window Uno accessors is a possible future parity enhancement, out of scope here.)

## Platform behavior matrix

| Target | Behavior after this change |
|--------|----------------------------|
| Skia Win32 (Windows 10 1607+) | Touch/pen focus → OS touch keyboard; blur → hide; occlusion reflow; public `InputPane` API works. |
| Skia Win32 (older Windows) | Version guard → no-op (`TryShow` returns `false`). |
| Skia X11 / macOS / FrameBuffer | No-op (`false`), documented. Touch focus still works; no programmatic keyboard. |
| Skia-on-Android / -iOS | Unchanged (existing extensions). |
| Native Android / iOS / WASM | Unchanged. |

## Testing strategy

- **Runtime tests** (`Uno.UI.RuntimeTests`, Skia): inject a **fake `IInputPaneExtension`** (mirror `TextBox.SetImeExtensionForTesting`) capturing `TryShow`/`TryHide` calls, plus a way to set `LastInputDeviceType`. Assert:
  - Touch/pen focus of a `TextBox` → exactly one `TryShow`; mouse/keyboard/programmatic focus → **no** `TryShow` (RED before the trigger exists).
  - Blur to a non-text element → `TryHide`.
  - `TextBox` A → `TextBox` B (both touch) → **no** intervening visible hide (anti-flicker; pending hide cancelled).
  - `PasswordBox` behaves like `TextBox`.
  - Setting `InputPane.OccludedRect` raises `Showing`/`Hiding` and the focused element is brought into view (existing pan logic).
- **Manual / sample:** a `SamplesApp` sample with a `TextBox`/`PasswordBox` inside a `ScrollViewer` for touch validation on a Windows tablet (the issue's repro).
- **WinUI parity:** the API shape matches WinUI 1:1; document that runtime touch-keyboard behavior can't be asserted headlessly and give the exact manual steps.

## Files (planned)

**Add**
- `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.SoftwareKeyboard.skia.cs` — touch/pen-gated `TryShow`/`TryHide` trigger + deferred-hide.
- `src/Uno.UI.Runtime.Skia.Win32/UI/ViewManagement/Win32InputPaneExtension.cs` — `IInputPaneExtension` + `IFrameworkInputPaneHandler` + COM interop.
- `src/Uno.UI.Runtime.Skia.X11/UI/ViewManagement/X11InputPaneExtension.cs` — no-op.
- `src/Uno.UI.Runtime.Skia.MacOS/UI/ViewManagement/MacOSInputPaneExtension.cs` — no-op.
- `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/UI/ViewManagement/FrameBufferInputPaneExtension.cs` — no-op.
- Runtime tests + a SamplesApp sample.

**Change**
- `src/Uno.UI.Runtime.Skia.Win32/Hosting/Win32Host.cs` — register `IInputPaneExtension`.
- `src/Uno.UI.Runtime.Skia.X11/Hosting/X11ApplicationHost.cs`, `…MacOS/Hosting/MacSkiaHost.cs`, `…FrameBuffer/Hosting/FramebufferHost.cs` — register no-ops.
- `src/Uno.UI.Runtime.Skia.Win32.Support/NativeMethods.txt` — add WinRT-activation + `IFrameworkInputPane*` symbols.
- `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.skia.cs` — call the new `partial void` from `OnFocusStateChangedPartial`.
- `src/Uno.UI/UI/ViewManagement/InputPane/InputPane.skia.cs` — internal window-target `XamlRoot` property; `EnsureFocusedElementInViewPartial` uses the target/focused `XamlRoot` instead of `Window.InitialWindow`; test hook to inject a fake `IInputPaneExtension`.

## Build & validation

- Win32 Skia: `cd src && dotnet build Uno.UI-Skia-only.slnf -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true`.
- Runtime tests via the `/runtime-tests` skill (Skia Desktop).
- Manual touch validation on a Windows tablet / touch laptop.

## Risks / open items

- **CsWin32 coverage** of `IFrameworkInputPane`/`IFrameworkInputPaneHandler`/`RoGetActivationFactory` — if any symbol isn't in the Win32 metadata, declare it manually (`[ComImport]`). `IInputPaneInterop`/`IInputPane2` are declared manually regardless.
- **AOT/trimming:** `[ComImport]` built-in marshaling may warn under full AOT. Matches existing project precedent; revisit with `[GeneratedComInterface]` only if the Win32 runtime enforces AOT-clean.
- **Multi-window / multi-display:** the singleton targets the focused control's window (HWND from its `XamlRoot`), so the keyboard docks on that window's monitor and occlusion is monitor-correct via `ScreenToClient`. Explicit per-window `InputPane` accessors (full WinUI-3 `GetForWindow` parity) are intentionally out of scope. Keyboard *placement* on a specific monitor remains OS-controlled (no public override API).
- **Coordinate space** of the `IFrameworkInputPane` `Showing` RECT is screen pixels → must `ScreenToClient` + divide by `RasterizationScale` to match what `ScrollContentPresenter.Pad` expects (root/client DIPs).
- **Pen** is currently treated as mouse in some `TextBox` pointer code; the trigger uses `LastInputDeviceType` (which distinguishes `Pen`) so pen focus is included.
