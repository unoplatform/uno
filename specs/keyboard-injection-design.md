# Keyboard input injection on Skia — design

`Windows.UI.Input.Preview.Injection.InputInjector.InjectKeyboardInput`

## Measured Windows behaviour

The contract below was measured against a WinUI 3 app calling the real `InputInjector`, not taken
from documentation. Recording it here because several of these are undocumented and none are
obvious.

| Behaviour | Detail |
|---|---|
| Asynchronous | `InjectKeyboardInput` returns before any event is delivered. |
| Batch cap | 16 events per call; 17 throws `ArgumentException`. Undocumented. |
| Unicode option | Raises `PreviewKeyDown`/`KeyDown`/`KeyUp` with `Key == 255` *and* `CharacterReceived` with the code unit — it is not a character-only channel. A non-zero `VirtualKey` throws. |
| Lone Unicode down | Delivers the character even with no matching key-up. |
| Surrogate pairs | Two consecutive Unicode events yield two key/char cycles; the text box ends with the full pair. |
| ScanCode option | `wScan` identifies the key and `VirtualKey` is ignored. |
| Without ScanCode option | The supplied scan code is **discarded** — `KeyStatus.ScanCode` is 0. |
| ExtendedKey | Surfaces as `KeyStatus.IsExtendedKey`, but does not split `VK_CONTROL` into `RightControl`. |
| Global key state | Injection updates `GetKeyState`/`GetAsyncKeyState`, so an injected Shift genuinely produces uppercase and an injected Ctrl+A really selects all (raising no `CharacterReceived`). |
| Auto-repeat | None. A key held 1.5 s produces exactly one `KeyDown`, `RepeatCount == 1`. |
| Key-up alone | Delivered as a bare `KeyUp`; no `KeyDown`, no `CharacterReceived`, no text change. |
| Ordering | Strictly preserved within and across calls. |
| Targeting | System-wide: delivery follows the foreground window, not the calling app. |

## Goal

Make `InjectKeyboardInput` deliver real key events through the Skia input stack — focus manager →
focused element → `PreviewKeyDown` → `KeyDown` → bubbling → accelerators → `KeyUp` — on every Skia
target. Today it is a generated stub that throws on all platforms.

## Scope

**In scope**

- `InjectedInputKeyboardInfo` — real implementation (`VirtualKey`, `KeyOptions`, `ScanCode`).
- `InputInjector.InjectKeyboardInput(IEnumerable<InjectedInputKeyboardInfo>)` on all Skia targets.
- A keyboard channel on `IInputInjectorTarget`, implemented by `InputManager`.
- Target resolution to the **active window's** `ContentRoot`, matching WinUI's foreground-window model.
- `InjectedInputKeyOptions.Unicode` text delivery; invariant-US character synthesis for plain keys.
- Runtime tests, including one that fails against the synthesized `KeyboardHelper` path.

**Out of scope**

- `InjectShortcut` — stays `[NotImplemented]`. Its three values are OS shell gestures with no Skia
  analogue; deferred to its own issue.
- Gamepad injection, `TryCreateForAppBroadcastOnly`.
- Migrating the ~355 `TestServices.KeyboardHelper` call sites — separate follow-up, large and
  mechanical, with its own regression risk.
- IME composition (documented limitation, see below).

## Key finding: no per-host work

The issue this design serves assumed each Skia host needs a new keyboard channel. It does not. Every
Skia host already implements `IUnoKeyboardInputSource` and every key funnels into a single method:

```
Host (Win32 WndProc / X11 / NSEvent / libinput / DOM / Android / UIKit)
  → IUnoKeyboardInputSource.KeyDown/KeyUp        src/Uno.UWP/UI/Core/Internal/IUnoKeyboardInputSource.cs
  → KeyboardManager.OnKey(KeyEventArgs, bool)    src/Uno.UI/UI/Xaml/Internal/InputManager.Keyboard.skia.cs:53
```

`OnKey` performs *all* routing: focus resolution, tunneling → bubbling, `KeyboardStateTracker`
feeding, flyout dismissal, context-menu triggers, `CharacterReceived`. Hosts are pure
`KeyEventArgs` factories.

Joining at `OnKey` is therefore exactly "after the host, before the focus manager", and gets
accelerators, Tab/arrow focus navigation, popup routing and modifier tracking for free — on all
eight Skia heads, with zero host changes. `Headless` and `Tizen`, which have no keyboard source at
all, gain keyboard input purely from the injector.

## Design

### 1. The seam

```csharp
// src/Uno.UWP/UI/Input/Preview.Injection/IInputInjectorTarget.cs
internal interface IInputInjectorTarget
{
    void InjectPointerAdded(PointerEventArgs args);
    void InjectPointerUpdated(PointerEventArgs args);
    void InjectPointerRemoved(PointerEventArgs args);

    void InjectKeyDown(KeyEventArgs args);
    void InjectKeyUp(KeyEventArgs args);

    bool IsActive { get; }
}
```

Two directional methods mirror both the pointer trio and the host's `KeyDown`/`KeyUp` events.
`KeyEventArgs` lives in `Uno.UWP`, so the interface stays reference-clean.

`InputManager` gets a new unsuffixed `InputManager.Keyboard.cs` carrying the explicit interface
implementations forwarding to `partial void`s, with bodies only in `InputManager.Keyboard.skia.cs`:

```csharp
partial void InjectKeyDown(KeyEventArgs args) => Keyboard.Inject(args, down: true);
partial void InjectKeyUp(KeyEventArgs args)   => Keyboard.Inject(args, down: false);
```

Off-Skia the partial has no body → silent no-op, exactly like the pointer partials. That is what lets
the shared `InjectKeyboardInput` body compile on native Android/iOS/Reference.

### 2. Target resolution — the active window

WinUI's injection is system-wide and lands on the **foreground window**. Uno should match: resolve the
active window's `ContentRoot` at inject time.

On Skia every `Window` is a `DesktopXamlSource` with its own `ContentRoot`/`InputManager`/
`FocusManager`. `InputInjector` currently holds a `[ThreadStatic]`, set-once target — the first
`ContentRoot` constructed wins forever, so a second window is unreachable. Pointers hide this because
they hit-test; keyboard is focus-driven and cannot.

Fix:

```csharp
// Weak, so a closed window's InputManager and visual tree are not pinned for the process lifetime.
[ThreadStatic] private static List<WeakReference<IInputInjectorTarget>>? _inputManagers;

private IInputInjectorTarget ResolveKeyboardTarget()
{
    foreach (var weak in _inputManagers ?? [])
    {
        if (weak.TryGetTarget(out var target) && target.IsActive)
        {
            return target;
        }
    }

    return _target;   // no window reports active
}
```

Hosts that never report activation leave every window in the default `CodeActivated` state, so the
first registered target wins there. macOS is in that category today, so multi-window keyboard
injection is not yet supported on it.

`IsActive` is implemented on `InputManager` from the existing activation chain:

```csharp
bool IInputInjectorTarget.IsActive
    => ContentRoot.GetOwnerWindow()?.NativeWrapper?.ActivationState
       is not (null or CoreWindowActivationState.Deactivated);
```

Registration moves from `ConstructPointerManager_Managed` into the `InputManager` constructor — an
in-tree TODO already asks for exactly this, and constructor ordering (`ConstructKeyboardManager()`
runs first) makes it safe.

Pointer behaviour is unchanged: `TryCreate()` and `TryCreate(object relativeRoot)` keep their current
semantics, and `relativeRoot` remains purely a hit-test scope.

If no window is active (host not yet attached, `XamlRoot` still null), `InjectKeyDown`/`InjectKeyUp`
no-op with a warning rather than letting `FocusManager.GetFocusedElement(null)` throw
`ArgumentNullException` out of a public API.

### 3. `InjectedInputKeyboardInfo` → `KeyEventArgs`

Hand-written partial at `src/Uno.UWP/UI/Input/Preview.Injection/InjectedInputKeyboardInfo.cs`,
following the `InjectedInputMouseInfo.ToEventArgs` template.

| Option | Behaviour |
|---|---|
| `KeyUp` | routes to `InjectKeyUp` instead of `InjectKeyDown`; sets `KeyStatus.IsKeyReleased` |
| `Unicode` | `VirtualKey` **must** be 0 (else `ArgumentException`, matching WinUI); `ScanCode` carries a UTF-16 code unit delivered as `KeyEventArgs.UnicodeKey`; the key surfaces as `(VirtualKey)255` |
| `ExtendedKey` | sets `KeyStatus.IsExtendedKey` |
| `ScanCode` | echoed into `KeyStatus.ScanCode`; key identification still comes from `VirtualKey` (documented limitation — Uno has no layout table) |

`CorePhysicalKeyStatus` contract, matching the measured WinUI records:

| Field | Value |
|---|---|
| `ScanCode` | `info.ScanCode` only when the `ScanCode` option is set, else `0` |
| `IsExtendedKey` | from the `ExtendedKey` option |
| `IsKeyReleased` | from the `KeyUp` option |
| `WasKeyDown` | `true` on key-up; on key-down, `true` iff that key is already `Down` in `KeyboardStateTracker` |
| `RepeatCount` | always `1` — WinUI never auto-repeats; the caller synthesises |
| `IsMenuKeyDown` | from tracked `Menu` state |

### 4. Character synthesis

Non-Unicode injection must compute the character itself and pass it into `KeyEventArgs`. Relying on
the existing `KeyRoutedEventArgs.MapToChar` fallback would insert text *without* raising
`CharacterReceived`, because `OnKey` raises it from `KeyEventArgs.UnicodeKey`, not from the routed
args — a WinUI divergence.

An injector-private, invariant-US table in `Uno.UWP`, following the Win32 host's rules:

- No character for `Tab` — otherwise injected Tab inserts `'\t'` *and* breaks focus navigation.
- Filter control characters except `'\r'`/`'\n'`; `Enter` → `'\r'`.
- No character while `Control` or `Windows` is held (AltGr, i.e. `Ctrl+Alt`, stays exempt). This is
  why WinUI's injected `Ctrl+A` raises no `CharacterReceived`.
- A–Z, 0–9 plus the US shifted row, `Space`, `Enter`.

Case comes from **`KeyboardStateTracker`**, not from the info's modifiers — WinUI derives case from
global key state, which is why an injected Shift-down genuinely produces `'A'`. CapsLock is handled
by a private toggle in the injector: `CoreVirtualKeyStates.Locked` is unusable on Skia because
`TrackKeyState` fires once per ancestor during bubbling, toggling the `Locked` bit each time.

`MapToChar` itself is **not** extended — three live paths depend on its current behaviour.

### 5. Modifiers

Injected modifier keys must be injected as their own key events, exactly as WinUI requires. Setting
`KeyboardModifiers` on the args is not enough: every accelerator path reads ambient state via
`CoreImports.Input_GetKeyboardModifiers()` → `KeyboardStateTracker`, which is fed as a side effect of
raising routed key events. Going through `OnKey` therefore makes `Ctrl+X` accelerators match.

Separately (own commit): the public `InjectMouseInput` hardcodes `VirtualKeyModifiers.None`, so a held
injected Ctrl never reaches injected pointer args and `Ctrl+Click` is impossible. It should default
its modifiers from `KeyboardStateTracker`. This is a behaviour change to a shipped API, kept in an
isolated commit so it can be reverted independently.

## Documented limitations

- **In-process only.** Unlike WinUI, injection does not reach other applications or the OS.
- **No IME participation.** All six `IImeTextBoxExtension` implementations are driven exclusively by
  native platform events; an injected key cannot start, feed or commit a composition. Keys injected
  *during* a live composition are silently swallowed by `ShouldSwallowKeyDuringComposition`.
- **No scan-code-driven key identification** — the value round-trips through `KeyStatus.ScanCode` but
  does not select the key.
- **No auto-repeat**, matching WinUI.
- **Invariant-US character synthesis**, so injected sequences behave identically on every Skia target.
- **Access keys never fire** — `AccessKeyManager` is unimplemented on Skia. Pre-existing gap.
- **Native WASM (DOM) is not supported** — only the Skia targets have a keyboard body, so the API
  stays `[Uno.NotImplemented]` for `__WASM__`.
- **Do not mix real and injected keyboard input.** Hosts compute modifiers from OS/native state that
  injection never updates.
- **`CoreWindow.KeyDown`/`KeyUp` do not observe injected keys** — they are wired to the host source.
  Pointer injection likewise bypasses the host pointer source.

## Public surface & generated files

`InjectKeyboardInput` is declared unconditionally in the existing cross-platform partial
`src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs`, exactly as `InjectMouseInput` already is,
carrying `[Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "__NETSTD_REFERENCE__")]`.

The sync generator then collapses the generated stub to a
`// Skipping already declared method …` line. This requires the parameter name to be exactly `input`.
Regenerate with `build\run-api-sync-tool.cmd`; CI (`winappsdk-sync-check.yml`) fails on any drift and
publishes a `sync-generator.patch`.

All new public members carry XML documentation: `<summary>`, `<param>`, and a `<remarks>` naming the
Uno Platform divergences above.

## Test plan

New `Given_InputInjector_Keyboard.cs`, guarded `#if __SKIA__`:
typing into a focused TextBox, Shift and CapsLock casing, Ctrl+A select-all with no
`CharacterReceived`, accelerator firing via injected modifier keys, Tab focus movement (and Tab not
typing a character), Unicode + surrogate pairs, `ArgumentException` on Unicode with a non-zero
`VirtualKey` (asserting nothing was dispatched), bare KeyUp, ordering, `KeyStatus` population, and
Ctrl+Click composed across keyboard and mouse injection.

The discriminating test is `When_InjectKey_Raises_CharacterReceived`: `TestServices.KeyboardHelper`
raises routed events directly on an element and never enters `OnKey`, and `RaiseCharacterReceived` is
the only `CharacterReceivedEvent` raise site in the framework. The assertion is therefore
structurally unreachable through the helper — it fails today and passes with real injection.

Tests must dispose/release any held modifier: an unbalanced injected key-down leaves the key stuck
`Down` in the process-wide tracker and silently breaks every later accelerator match.
