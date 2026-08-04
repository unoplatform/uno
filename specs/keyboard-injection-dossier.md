# Keyboard injection on Skia — design-input dossier

`Windows.UI.Input.Preview.Injection.InputInjector.InjectKeyboardInput` / `InjectShortcut`

Synthesis of six independent investigations (two salvaged from an earlier run, four fresh).
Every load-bearing claim below was re-verified against the working tree at
`D:\Work\uno-worktrees\keyboardinjection` (branch `dev/mazi/keyboardinjection`) before inclusion.
Line numbers are from that tree.

**Legend for provenance:**
- **OBSERVED** — measured in a running WinUI 3 app; artifact cited.
- **DOCS** — from Microsoft Learn / Win32 docs; no local measurement.
- **VERIFIED** — I read the cited repo file myself during synthesis.
- **REPORTED** — an investigator's claim I did not independently re-read (flagged where it matters).

---

## 1. WinUI ground truth

### 1.0 Repro provenance — is the empirical run trustworthy?

**Yes.** The earlier investigator's first run was genuinely contaminated (the harness never held
foreground). It was re-run with hard foreground enforcement and I verified the artifacts myself:

- `D:\Throwaway\InjectKbdRepro\results-run1.jsonl` — 315 lines, exists, matches the cited count.
- `D:\Throwaway\InjectKbdRepro\results-run2-batchlimits.jsonl` — 266 lines.
- **Every one of the 18 `step-end` records reports `wasForegroundBefore:true, wasForegroundAfter:true`.**
  I dumped all of them; there is no step where the app lost foreground.
- Three steps report `verdict:"CONTAMINATED"` — exactly `c1`, `c2`, `h`, all three of which are the
  **Unicode** steps. I read the raw events for `c1` and confirmed the "unexpected" keys are
  `key:"255", keyValue:255` — i.e. the harness's own allowlist omitted the value Windows reports for
  Unicode-injected keys. **These are false positives, not human keystrokes.** The `h` step is the
  surrogate-pair emoji test, same cause.
- Environment: packaged WinUI 3 desktop app, WinAppSDK 2.3.1, net10.0, Windows 11 26300, manifest
  declaring `rescap:inputInjectionBrokered`.

**Verdict: the OBSERVED claims below are trustworthy.** Where a behaviour was *not* exercised
(Alt / `WM_SYSKEYDOWN`, unpackaged apps, cross-process delivery confirmation), it is called out
explicitly as untested rather than inferred.

### 1.1 The contract (OBSERVED)

| # | Behaviour | Evidence |
|---|---|---|
| 1 | `TryCreate()` returns a real injector in a **packaged** app declaring `inputInjectionBrokered`. | `results-run1.jsonl` `{"kind":"try-create","succeeded":true,...}` — verified by grep |
| 2 | `InjectKeyboardInput` is **asynchronous**: it returns before any event is delivered. Reading `TextBox.Text` on the next statement still shows the old value. | `{"kind":"sync-probe","textOnNextLine":"","len":0}` then step `sync` `textAfterSettle:"q"` — verified |
| 3 | **Hard batch cap of 16.** 16 succeeds; 17..32 all throw `ArgumentException: Value does not fall within the expected range.` | `results-run2-batchlimits.jsonl` — verified by grep, `count:16 ok:true`, `count:17..32 ok:false` |
| 4 | `Unicode` option with a **non-zero `VirtualKey` throws** `ArgumentException` at the call site (validation is in-process, pre-queue). | step `c3`, `injectError` present, `textAfterSettle:""` |
| 5 | Unicode injection raises **`PreviewKeyDown`/`KeyDown`/`KeyUp` with `Key == 255`** *and* `CharacterReceived` carrying the real UTF-16 code unit. It is not char-only. | step `c1` lines 30–37, verified verbatim: 4 key events at `keyValue:255`, then `CharacterReceived charCode:233 ('é')`, then KeyUp |
| 6 | A **lone Unicode DOWN with no KeyUp partner still delivers the character**. | step `c2` — `textAfterSettle:"é"` with no KeyUp in the log |
| 7 | A **surrogate pair** sent as two consecutive Unicode events yields two independent key/char cycles (0xD83D then 0xDE00); the TextBox ends with the 2-char emoji. | step `h`, `textAfterSettle:"😀" textLen:2` |
| 8 | With the **`ScanCode` option**, `wScan` identifies the key and `wVk` is ignored: ScanCode `0x1E` + VirtualKey `0x42('B')` still produced `Key=A`, char `'a'`, `KeyStatus.ScanCode == 0x1E`. | steps `g1`/`g2`, both `textAfterSettle:"a"` |
| 9 | **Without** the ScanCode option, the supplied ScanCode is **discarded** — `KeyStatus.ScanCode == 0` on every plain-VK injection. Windows does not back-fill a VK→scan-code mapping. | step `g3` and all of a/b/d/e/f/order/repeat report `scanCode:0` |
| 10 | `ExtendedKey` surfaces faithfully as `KeyStatus.IsExtendedKey` (Right arrow: false vs true) but does **not** split `VK_CONTROL` into `RightControl` at the XAML layer — `Key` stays `Control` with `IsExtendedKey=true`. | steps `f1`/`f2`/`f3` |
| 11 | Injection **updates global key state**: while an injected 'A' is held, `GetKeyState`=0xFF80, `GetAsyncKeyState`=0x8001, `InputKeyboardSource.GetKeyStateForCurrentThread`=Down. | run1 before/during/after probes |
| 12 | Consequently **injected Shift genuinely shifts**: Shift-down + A-down/up + Shift-up → uppercase `'A'` (charCode 65). | step `d`, `textAfterSettle:"A"` — verified |
| 13 | **Injected Ctrl+A performs a real select-all.** TextBox marks the 'A' KeyDown handled (no `TextBox.KeyDown` for A in the log), `SelectionLength` becomes 11 on `"hello world"`, and **no `CharacterReceived` is raised**. | step `e`, `selStart:0 selLen:11` — verified |
| 14 | **There is no auto-repeat.** A key held 1500 ms with no further injection produced exactly one KeyDown, `RepeatCount=1`. The caller synthesises repeats. | step `repeat`, `injectCallMs:1511`, `textAfterSettle:"x"` (1 char) — verified |
| 15 | Three consecutive DOWNs in one call are **coalesced by the OS queue** into two delivered messages — the second with `RepeatCount=2, WasKeyDown=true` — producing 2 characters, not 3. | step `repeat2`, `textAfterSettle:"xx"` |
| 16 | A **KeyUp with no preceding KeyDown** is delivered as a bare KeyUp (`WasKeyDown=true, IsKeyReleased=true`), no KeyDown, no CharacterReceived, no text change. | step `b`, `textAfterSettle:""` |
| 17 | **Ordering is strictly preserved** within and across calls ('U','N','O' → `"uno"`). | step `order` |
| 18 | KeyDown carries `wasKeyDown:false, isKeyReleased:false, repeatCount:1`; KeyUp carries `wasKeyDown:true, isKeyReleased:true, repeatCount:1`. | every event record in run1 — verified in `c1` block |
| 19 | Delivery is **system-wide / foreground-targeted, not app-local**: with Notepad foreground, an injected 'Z' produced zero events and zero text change in the injecting app. | `{"kind":"cross-process","weAreForeground":false,...,"ourTextBefore":"q","ourTextAfter":"q"}` — verified |
| 20 | `InjectShortcut` is literally synthesised keystrokes: **Back = Win+Backspace, Start = Win alone, Search = Win+S**. The shell consumes the non-modifier key-**down** as a hotkey, so the app observes only LeftWindows-down, the bare key **UP**, then LeftWindows-up. | run1 shortcut blocks |
| 21 | `InjectShortcut(Search)` / `(Start)` take the foreground away (shell "Search" window); `(Back)` does not. All three return without throwing. | run1 shortcut records |

### 1.2 The contract (DOCS-DERIVED, not measured)

- `InjectedInputKeyOptions` values map 1:1 onto `KEYEVENTF_*` **by value**: `ExtendedKey=1`→`KEYEVENTF_EXTENDEDKEY 0x0001`, `KeyUp=2`→`KEYEVENTF_KEYUP 0x0002`, `Unicode=4`→`KEYEVENTF_UNICODE 0x0004`, `ScanCode=8`→`KEYEVENTF_SCANCODE 0x0008`.
- `InjectedInputKeyboardInfo.VirtualKey` is a raw `ushort` Win32 VK, **not** a `Windows.System.VirtualKey`. Valid range 1..254; must be 0 when `Unicode` is set.
- The extended-key flag physically distinguishes right Alt/Ctrl, the Insert/Delete/Home/End/PageUp/PageDown/arrow nav cluster, Break, PrintScreen, numpad Divide, numpad Enter, and the Windows/Application keys. NumLock is **not** extended; right Shift is **not** extended (own scan code 0x36).
- `InjectedInputShortcut` = `Back=0` ("traversal through a back stack"), `Start=1` ("traversal to a start, or home, screen"), `Search=2` ("traversal to a search screen"). **The docs never state the key mapping** — the Win+Backspace / Win / Win+S mapping in §1.1 #20 is empirical only.
- The whole namespace requires the `inputInjectionBrokered` **restricted** capability; `TryCreate()` returns null without it.
- `SendInput` is subject to UIPI (silent block across integrity levels) and does not reset the keyboard's current state.

### 1.3 What Uno structurally cannot reproduce

| WinUI behaviour | Why not on Skia |
|---|---|
| System-wide / cross-process delivery (#19) | Uno's injector is bound to an in-process `IInputInjectorTarget` (**VERIFIED** `src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs:17-33`). It can only reach the app's own visual tree. |
| Shell actions for `Start` / `Search` (#21) | No shell. `OnKey` routes only into the local visual tree. |
| Real scan-code→VK→char translation (#8) | No OS keyboard-layout table available at Uno's chokepoint; `OnKey` consumes an already-resolved `(VirtualKey, modifiers, KeyStatus, unicodeKey)` tuple. |
| OS-level key-state (`GetKeyState`) update (#11) | Injection updates Uno's own `KeyboardStateTracker`, not the OS. Real keys arriving afterwards on Win32/X11/macOS/FrameBuffer will not see injected modifiers (see §7). |

### 1.4 Contradictions between investigators — resolved

1. **⚠️ The supporting enums are NOT stubs.** The "repo conventions" investigator asserted that
   `InjectedInputKeyOptions` and `InjectedInputShortcut` "are pure stubs today and must also be
   hand-written to be falsed out". **This is wrong.** I read both generated files: they emit real,
   usable enum members guarded only by a platform `#if` that *includes* `__SKIA__`, with **no
   `[Uno.NotImplemented]` attribute at all**. `InjectedInputKeyOptions.Unicode` is already a working
   value on Skia today. The WinUI investigator got this right. **Only the class
   `InjectedInputKeyboardInfo` throws and needs hand-writing.** Hand-writing the enums is optional
   cosmetic cleanup, not required work. (Verified: `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Input.Preview.Injection/InjectedInputKeyOptions.cs`, `.../InjectedInputShortcut.cs`.)
2. **`VirtualKey` numeric value for Unicode injection.** WinUI reports **255 (0xFF)**, not
   `VK_PACKET` (231/0xE7). This is OBSERVED and unambiguous, but *why* is unresolved — nobody
   determined whether Windows posts 0xFF or WinUI remaps `VK_PACKET`. Uno must simply pick 255 for
   parity. `Windows.System.VirtualKey` has no member for 231 or 255 (**VERIFIED**: grep of
   `src/Uno.UWP/System/VirtualKey.cs` for `= 231|255` returns nothing; `GoBack = 166` is the only
   hit in that neighbourhood), so an `unchecked((VirtualKey)255)` cast is required.
3. **Wayland host.** One investigator explicitly checked; **VERIFIED**: `ls -d src/Uno.UI.Runtime.Skia*/`
   returns Android, AppleUIKit, Headless, Linux.FrameBuffer, MacOS, Tizen, WebAssembly.Browser,
   Win32, Win32.Support, X11, and shared. **No Wayland host exists on this branch.**
4. **`HAS_INPUT_INJECTOR` line numbers.** Investigators cited 74/79; actual is **73** (WebAssembly)
   and **79** (Skia) in `src/Uno.CrossTargetting.targets`. Immaterial; the claim holds.

---

## 2. Uno Skia keyboard pipeline today

### 2.1 Call graph (real key press)

```
Host (Win32 WndProc / X11 event thread / NSEvent / libinput / DOM / Android Looper)
  → IUnoKeyboardInputSource.KeyDown(sender, Windows.UI.Core.KeyEventArgs)
      src/Uno.UWP/UI/Core/Internal/IUnoKeyboardInputSource.cs:8-19
  → lambda wired in KeyboardManager.Init
      src/Uno.UI/UI/Xaml/Internal/InputManager.Keyboard.skia.cs:48-49
  → KeyboardManager.OnKey(KeyEventArgs args, bool down)        ← THE CHOKEPOINT
      src/Uno.UI/UI/Xaml/Internal/InputManager.Keyboard.skia.cs:53
```

Inside `OnKey` (all line numbers **VERIFIED** by reading the file):

| Line | Step |
|---|---|
| 55–62 | `InputManager.LastInputDeviceType = Keyboard` (or `GamepadOrRemote` via `XboxUtility.IsGamepadNavigationInput`) |
| 64 | `originalSource1 = FocusManager.GetFocusedElement(ContentRoot.XamlRoot) as UIElement ?? ContentRoot.VisualTree.RootElement` |
| 66–70 | build **one** `KeyRoutedEventArgs(originalSource1, args.VirtualKey, args.KeyboardModifiers, args.KeyStatus, args.UnicodeKey) { CanBubbleNatively = false, Handled = args.Handled }` |
| 72 | `originalSource1.RaiseTunnelingEvent(PreviewKeyDownEvent \| PreviewKeyUpEvent, routedArgs)` |
| 75 | **re-resolve** the focused element (WinUI parity — focus may move during preview) |
| 79 | `originalSource2.RaiseEvent(KeyDownEvent \| KeyUpEvent, routedArgs)` (args reused to reduce allocations) |
| 83–89 | transient-flyout dismissal on unhandled, modifier-less key-down |
| 93–99 | `_contextMenuProcessor.ProcessContextRequestOnKeyboardInput(originalSource2, args.VirtualKey, args.KeyboardModifiers)` — Shift+F10 / Application / GamepadMenu |
| 105–108 | `if (down && args.UnicodeKey is { } character) RaiseCharacterReceived(character, args.KeyStatus)` |
| 110–121 | trace logging |
| 123 | `args.Handled = routedArgs.Handled` |
| 133–143 | `RaiseCharacterReceived` — re-resolves focus, builds `CharacterReceivedRoutedEventArgs`, single `RaiseEvent(UIElement.CharacterReceivedEvent, …)` in the whole framework |
| **148** | `internal void OnKeyTestingOnly(KeyEventArgs args, bool down) => OnKey(args, down);` — the existing back door |

### 2.2 Side effects that come free with `OnKey`

- **`KeyboardStateTracker`** is fed from `UIElement.RaiseEvent` (`src/Uno.UI/UI/Xaml/UIElement.RoutedEvents.cs:677-680`) and `RaiseTunnelingEvent` (:781-784) via `TrackKeyState` (:808-829), keyed on `keyArgs.OriginalKey`. **VERIFIED.**
- **Keyboard accelerators** are ordinary KeyDown handlers reached during bubbling — `Control.OnKeyDownHandler` → `ProcessAcceleratorsIfApplicable` (`Control.cs:1194-1203`), non-Control via `PrepareManagedKeyEventBubbling` → `UIElement.OnKeyDown` (`UIElement.mux.cs:632-684`), and **global** accelerators last in `UnoFocusInputHandler.OnKeyDown` on the root element (`UnoFocusInputHandler.cs:19,54-74`).
- **Tab / arrow focus** — `UnoFocusInputHandler` (subscribed to `RootElement.KeyDown`, one per `VisualTree`, `VisualTree.cs:105`).
- **`PostKeyDown`** is raised from inside `RaiseEvent` when `routedEvent == KeyDownEvent` (`UIElement.RoutedEvents.cs:714-717`) — this is how `TextBox.OnPostKeyDown` inserts text and sets `HandledShouldNotImpedeTextInput`.

### 2.3 Injector seam as it exists today (the precedent to mirror)

```
InputInjector.InjectMouseInput(...)                      src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs:249
  → DispatchPointerUpdated(args)  { args.RelativeRoot = _relativeRoot; ... }        :341-345
  → _target.InjectPointerUpdated(args)                   IInputInjectorTarget, :12
  → InputManager explicit impl                            src/Uno.UI/.../InputManager.Pointers.cs:44
  → partial void InjectPointerUpdated(args)               :45  (no body ⇒ silent no-op off-Skia)
  → body in InputManager.Pointers.Managed.cs:51           (#if UNO_HAS_MANAGED_POINTERS)
  → PointerManager.InjectPointerUpdated → the SAME OnPointerX methods the host calls, isInjected:true
```

Everything is **synchronous on the calling thread**; only the internal `Inject*Async` helpers await
`WaitForIdle` *between* events (`InputInjector.cs:313-330`). **VERIFIED.**

`InputInjector.SetTargetForCurrentThread(this)` is called from `ConstructPointerManager_Managed`
(`src/Uno.UI/UI/Xaml/Internal/InputManager.Pointers.Managed.cs:38-43`) with the comment
*"Injector supports only pointers for now, so configure only in by managed pointer (should be moved
to the InputManager ctor once the injector supports other input types)"*. **VERIFIED — the codebase
literally asks for this move as part of this work.**

Ordering note (**VERIFIED**, `InputManager.cs:21-30`): the ctor runs `ConstructKeyboardManager()`
**before** `ConstructPointerManager()`. Moving the registration into the ctor is therefore safe —
`Keyboard` is already non-null.

### 2.4 Current API state

`src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Input.Preview.Injection/InputInjector.cs` (**VERIFIED**):

- class header is already `#if false || false || false || false || false || false` → the type has a
  hand-written partial on all six platforms; **this header will not change**.
- `InjectKeyboardInput` at :19-25 and `InjectShortcut` at :33-39 are live
  `#if __ANDROID__ || __IOS__ || __TVOS__ || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__` stubs
  calling `TryRaiseNotImplemented`.
- `InjectMouseInput` / `InjectTouchInput` / `InitializePenInjection` / … are already collapsed to
  `// Skipping already declared method …` lines — the exact target state for the two new members.
- `InjectedInputKeyboardInfo.cs` is a fully throwing stub (`KeyOptions`, `ScanCode` ushort,
  `VirtualKey` ushort, public ctor) — **the one type that must be hand-written**.

---

## 3. The seam

### 3.1 Where to join — settled

**Join at `InputManager.KeyboardManager.OnKey`.** All six investigations converge on this and I
verified the enabling facts:

- `InputManager.Keyboard.skia.cs` is a `.skia.cs` file, so it compiles for **every** Skia head
  (Win32, X11, macOS, FrameBuffer, WASM-Skia, Skia-Android, Skia-AppleUIKit, plus Headless/Tizen
  which have no keyboard source at all).
- The hosts are pure `KeyEventArgs` factories. `OnKey` performs 100% of routing, focus resolution,
  `KeyboardStateTracker` feeding, flyout dismissal, context-menu triggering and `CharacterReceived`.
- `OnKeyTestingOnly` (:148) already proves the seam works with no host involvement, and is exercised
  by a passing runtime test (`Given_TextBox.skia.cs:7356`, **VERIFIED**).

**Consequence to accept:** `CoreWindow.KeyDown`/`KeyUp` are wired to the **host source**, not to
`OnKey` (`InputManager.Keyboard.skia.cs:46` → `CoreWindow.Keyboard.cs:26-36`). Injected keys will
not raise them. This mirrors pointer injection, which likewise bypasses the host pointer source.

### 3.2 Recommended `IInputInjectorTarget` shape

```csharp
// src/Uno.UWP/UI/Input/Preview.Injection/IInputInjectorTarget.cs
internal interface IInputInjectorTarget
{
    void InjectPointerAdded(PointerEventArgs args);
    void InjectPointerUpdated(PointerEventArgs args);
    void InjectPointerRemoved(PointerEventArgs args);

    // NEW
    void InjectKeyDown(KeyEventArgs args);
    void InjectKeyUp(KeyEventArgs args);

    // NEW — lets TryCreate(object) select the right ContentRoot (see §3.4)
    bool OwnsRoot(object root);
}
```

Two directional methods (rather than one `InjectKey(args, bool down)`) mirror both the pointer
methods and the host's `KeyDown`/`KeyUp` events. `KeyEventArgs` lives in `Uno.UWP`
(`Windows.UI.Core`), so the interface stays reference-clean.

**Do not add `InjectCharacterReceived`.** WinUI's Unicode path (§1.1 #5) is a *key* event carrying a
character; `OnKey` already raises `CharacterReceived` from `args.UnicodeKey`. A separate channel
would double-fire (see §6).

### 3.3 Recommended `InputManager` wiring

New **unsuffixed** file `src/Uno.UI/UI/Xaml/Internal/InputManager.Keyboard.cs` (compiles on every
runtime, exactly like `InputManager.Pointers.cs`):

```csharp
partial class InputManager
{
    #region IInputInjectorTarget (keyboard)
    void IInputInjectorTarget.InjectKeyDown(KeyEventArgs args) => InjectKeyDown(args);
    partial void InjectKeyDown(KeyEventArgs args);

    void IInputInjectorTarget.InjectKeyUp(KeyEventArgs args) => InjectKeyUp(args);
    partial void InjectKeyUp(KeyEventArgs args);

    bool IInputInjectorTarget.OwnsRoot(object root) => OwnsRootCore(root);
    #endregion
}
```

Bodies in `InputManager.Keyboard.skia.cs`:

```csharp
partial void InjectKeyDown(KeyEventArgs args) => Keyboard.OnKey(args, down: true);
partial void InjectKeyUp(KeyEventArgs args)   => Keyboard.OnKey(args, down: false);
```

`OnKey` becomes `internal` (or `KeyboardManager` grows `internal void Inject(KeyEventArgs, bool)`),
and **`OnKeyTestingOnly` is deleted**, its two call sites re-pointed at the injector or at the new
internal entry point.

Off-Skia the `partial void` has no body → silent no-op, exactly like the pointer partials. This is
what makes the shared `InjectKeyboardInput` body compile on native Android/iOS/Reference.

### 3.4 Reaching the right `ContentRoot` — the `[ThreadStatic]` problem

**The problem (VERIFIED, `InputInjector.cs:17-33`):**

```csharp
[ThreadStatic] private static IInputInjectorTarget? _inputManager;
internal static void SetTargetForCurrentThread(IInputInjectorTarget manager)
{
    …warn if different…
    _inputManager ??= manager; // Set only once per thread.
}
```

On Skia **every `Window` is a `DesktopXamlSource`** (`Window.cs:64`, hard `#if __SKIA__`), so every
window owns its own `ContentRoot` → `InputManager` → `FocusManager`. The **first** ContentRoot
constructed on the thread wins the injector forever. Injecting into window 2 is impossible today.
This matters far more for keyboard than for pointers, because the keyboard target is the *focused
element of that specific ContentRoot* — there is no hit-test fallback.

`FocusManager.GetFocusedElement(XamlRoot)` **throws `ArgumentNullException` on a null XamlRoot**
(`FocusManager.mux.static.cs:789`), and the obsolete parameterless overload returns `null` on Skia
because it reads the CoreWindow content root that Skia never creates. So injection *must* be
XamlRoot-scoped.

**Recommended shape (three changes, all low-risk):**

1. **Move the registration into the `InputManager` ctor.** Delete
   `InputInjector.SetTargetForCurrentThread(this)` from `ConstructPointerManager_Managed` and call
   it from `InputManager(ContentRoot)`. The in-tree TODO explicitly asks for this, and ctor ordering
   (`ConstructKeyboardManager()` first) makes it safe.
2. **Keep a per-thread *list*, not a single slot.**
   ```csharp
   [ThreadStatic] private static List<IInputInjectorTarget>? _targets;
   internal static void SetTargetForCurrentThread(IInputInjectorTarget t) => (_targets ??= new()).Add(t);
   public static InputInjector? TryCreate() => _targets is { Count: > 0 } ? new(_targets[0]) : null;
   ```
   `TryCreate()` keeps first-wins semantics (no behaviour change for existing tests), but now logs a
   warning listing the other candidates instead of silently discarding them.
3. **Make the existing Uno-only `TryCreate(object relativeRoot)` escape hatch also *select* the
   target**, not just stamp pointer args:
   ```csharp
   public static InputInjector? TryCreate(object relativeRoot)
   {
       var target = _targets?.FirstOrDefault(t => t.OwnsRoot(relativeRoot)) ?? _targets?.FirstOrDefault();
       return target is null ? null : new InputInjector(target) { _relativeRoot = relativeRoot };
   }
   ```
   with `InputManager.OwnsRootCore(object root)` implemented as
   `ContentRoot.XamlRoot is { } xr && (ReferenceEquals(root, xr) || (root as UIElement)?.XamlRoot == xr)`.

   ⚠️ This *changes* `TryCreate(object)` semantics for pointers too — today `relativeRoot` is purely
   a hit-test root and does not influence which ContentRoot receives the event. Arguably a bug fix,
   but it is a behaviour change and belongs in §13.

**Null-XamlRoot guard:** `InjectKeyDown/Up` must early-out with a warning when
`ContentRoot.XamlRoot is null` (host not yet attached), rather than letting
`FocusManager.GetFocusedElement` throw `ArgumentNullException` from inside a public API. Pointer
injection throws `InvalidOperationException` in that situation, so there is no established
precedent to copy — this is a fresh decision (§13).

### 3.5 `InjectKeyboardInput` body sketch (Uno.UWP)

```csharp
public void InjectKeyboardInput(IEnumerable<InjectedInputKeyboardInfo> input)
{
    foreach (var info in input)
    {
        var (args, isUp) = info.ToEventArgs(this);   // ToEventArgs lives on the hand-written info partial,
        if (isUp) _target.InjectKeyUp(args);          // mirroring InjectedInputMouseInfo.ToEventArgs
        else      _target.InjectKeyDown(args);
    }
}
```

Synchronous, in-order, one stack frame — matching pointer injection exactly. Add an
`internal async ValueTask InjectKeyboardInputAsync(…, CancellationToken)` awaiting `WaitForIdle`
between events, for the `Keyboard` test driver (§12), following the existing `InjectMouseInputAsync`
pattern (`InputInjector.cs:266-281`).

`InjectedInputKeyboardInfo` must be hand-written at
`src/Uno.UWP/UI/Input/Preview.Injection/InjectedInputKeyboardInfo.cs`, following
`InjectedInputMouseInfo.cs` (**VERIFIED** template: plain settable properties + an
`internal … ToEventArgs(…)` converter).

---

## 4. Per-host work required

**Headline: none.** Every Skia host is a `KeyEventArgs` factory feeding the same `OnKey`; joining
there requires **zero host changes**. The table records what each host does that injection *skips*,
and the resulting risk.

| Host | Entry point (file:line) | Work needed | What injection skips | Risk |
|---|---|---|---|---|
| **Win32** | `src/Uno.UI.Runtime.Skia.Win32/Devices/Input/Win32WindowWrapper.Keyboard.cs:16` (`OnKey(WPARAM, LPARAM, bool)`) | **None** | Destructive `PeekMessage(PM_REMOVE)` of the paired `WM_CHAR`; Tab-char suppression (:37); control-char filter except CR/LF (:44); IME-composition char suppression; `WM_SYSCOMMAND/SC_KEYMENU` suppression; Alt+numpad `CharacterReceived` (:88) | **Low.** Its char-derivation rules are the reference behaviour the injector's own table should copy (§5). |
| **X11** | `src/Uno.UI.Runtime.Skia.X11/Devices/Input/X11KeyboardInputSource.cs` (`XLookupString` :111, `VirtualKeyFromKeySym` :181, dispatch :190-200) | **None** | D-Bus IBus/Fcitx pre-filter with a 100 ms blocking wait that can swallow the key (:114-157); `QueueAction`→`Dispatcher.RunAsync` marshalling | **Low.** Its IME `ForwardKey` path already passes `unicodeKey: null` — precedent that a null char is acceptable at `OnKey`. |
| **macOS** | `src/Uno.UI.Runtime.Skia.MacOS/UI/Xaml/Window/MacOSWindowHost.cs:395-402` (managed), `UnoNativeMac/UNOWindow.m:735` `get_virtual_key`, `:893` `get_unicode` | **None** | Escape-exits-fullscreen; `NSTextInputClient` IME routing; modifier synthesis from `NSEventTypeFlagsChanged`; returning Handled to native so the OS suppresses the key | **Low.** |
| **Linux FrameBuffer** | `src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Devices/Input/FrameBufferKeyboardInputSource.cs:96-158` | **None** | Mutation of `_pressedKeys` + xkb latch state on every key | **Medium.** Only host computing modifiers from its **own** tracker, so an injected Shift is invisible to a subsequent *real* key there. Accelerators still work (they read `KeyboardStateTracker`). Mixed real+injected input is unreliable — document. |
| **WASM-Skia** | `src/Uno.UI.Runtime.Skia.WebAssembly.Browser/Devices/Input/BrowserKeyboardInputSource.cs:30-97` | **None** | `LastTabWasForward` recording (consumed by the a11y focus-departure sentinel, `WebAssemblyAccessibility.cs:1250`); `evt.preventDefault()` when Handled; `NativeDispatcher.Main.SynchronizationContext.Apply()` | **Medium-low.** An injected Tab will not update `LastTabWasForward`, so the WASM a11y focus sentinel may misbehave in injected-Tab tests. |
| **Skia-Android** | `src/Uno.UI.Runtime.Skia.Android/Devices/Input/AndroidKeyboardInputSource.cs:28-39`, from `ApplicationActivity.DispatchKeyEvent` | **None** | `base.DispatchKeyEvent` fallthrough; IME re-entry for soft-keyboard Enter (`TextInputPlugin.cs:300`) | **Low.** Host passes `default(CorePhysicalKeyStatus)` with a `/*TODO*/`; injection will be the first source of a populated `KeyStatus` there. |
| **Skia-AppleUIKit** | `src/Uno.UI.Runtime.Skia.AppleUIKit/Devices/Input/UnoKeyboardInputSource.cs:24-28` | **None** | Everything — **this host never raises `KeyDown`/`KeyUp`** (`#pragma warning disable CS0067`, **VERIFIED**). Its only key path is MacCatalyst-only `TryHandlePresses`, which raises `CoreWindow.KeyUp` and `fe.RaiseEvent(KeyUpEvent, …)` **directly**, bypassing `InputManager` entirely (:64-73) | **Medium.** Injection would be the **first and only** producer of `OnKey` on Skia-iOS — and the only source of a `KeyDown` there at all. Great for tests; a real divergence from the host path on MacCatalyst. |
| **Headless** | *(no keyboard source)* | **None** | n/a | **None.** Gains keyboard input purely via the injector — arguably a feature for headless runtime tests. |
| **Tizen** | *(no keyboard source)* | **None** | n/a | **None.** Same as Headless. |

**Optional follow-ups, explicitly out of scope for the injector work:** migrating the MacCatalyst
`PressesEnded` path onto `IUnoKeyboardInputSource` (which would give Skia-iOS a real `KeyDown` for
the first time), and populating `CorePhysicalKeyStatus` on the Android host.

---

## 5. VirtualKey → character synthesis

### 5.1 What exists today — exactly one helper, and it is tiny

`KeyRoutedEventArgs` (**VERIFIED**, `src/Uno.UI/UI/Xaml/Input/KeyRoutedEventArgs.cs:21`):

```csharp
UnicodeKey = unicodeKey ?? MapToChar(key, modifiers);
```

`MapToChar` (:59-70) verbatim:

```csharp
(VirtualKey.Space,  VirtualKeyModifiers.None)  => ' ',
(>= Number0 and <= Number9, VirtualKeyModifiers.None) => (char)key,
(>= A and <= Z, VirtualKeyModifiers.None)  => char.ToLowerInvariant((char)key),
(>= A and <= Z, VirtualKeyModifiers.Shift) => (char)key,
(VirtualKey.Back,   VirtualKeyModifiers.None) => (char)key,
_ => null,
```

Three things follow, all **VERIFIED**:

1. **Modifier matching is by exact equality on the whole `VirtualKeyModifiers` value, not `HasFlag`.**
   `Ctrl+A`, `Shift+Space`, `Shift|Control+A` all return `null`.
2. **CapsLock is structurally unrepresentable** — `VirtualKeyModifiers` has only
   `None/Control/Menu/Shift/Windows` (`src/Uno.UWP/System/VirtualKeyModifiers.cs`).
3. **No Enter, Tab, punctuation, numpad, or OEM keys.**

No other VirtualKey→char producer exists. Everything else runs the other direction
(`VirtualKeyHelper.{Android,UIKit}`, `BrowserVirtualKeyHelper.From{Key,Code}`,
`X11KeyTransform.VirtualKeyFromKeySym`) or is dead: `src/Uno.UI/Helpers/InputHelper.cs`
(`ToUnicodeEx` + `GetKeyboardState` + `GetKeyboardLayout`) has **zero callers**, hard-P/Invokes
`user32` from the cross-platform `Uno.UI`, and reads the *OS* key state — which injection never
updates. **Do not resurrect it.**

### 5.2 The asymmetry that decides the design

`OnKey` raises `CharacterReceived` from `args.UnicodeKey` — the **`KeyEventArgs`** property — not
from `routedArgs.UnicodeKey` (`InputManager.Keyboard.skia.cs:105`, **VERIFIED**). So a
`MapToChar`-derived character *inserts text into a TextBox* but **never raises `CharacterReceived`**.

**Therefore: the injector must compute the character itself and pass it into `KeyEventArgs`.**
Relying on the `MapToChar` fallback would produce text without `CharacterReceived`, diverging from
WinUI (§1.1 #5, #12).

**Corollary: do not extend `MapToChar`.** Three live paths depend on its current behaviour —
X11's IBus branch (passes `unicodeKey: null`), Skia-AppleUIKit, and
`TestServices.KeyboardHelper` — and changing it would silently alter all three.

### 5.3 What the injector's table must produce

Copy the **Win32 host's** rules, which are the de-facto Uno reference (**VERIFIED**,
`Win32WindowWrapper.Keyboard.cs:37-48`):

- **No character for `Tab`** (`key != VirtualKey.Tab` at :37) — otherwise an injected Tab would
  insert `'\t'` into a TextBox *and* break Tab focus navigation.
- **Filter control characters except `'\r'` / `'\n'`** (:44). Enter must produce `'\r'`
  (TextBox auto-converts `'\n'`→`'\r'` at `TextBox.skia.cs:1132-1136`, and only inserts when
  `AcceptsReturn`).
- **No character when a shortcut modifier is held.** WinUI's injected Ctrl+A produced **no**
  `CharacterReceived` (§1.1 #13) because Windows emits a control char that the host filters. The
  injector must suppress the char when `Control` or `Windows` is down (and, on non-Apple, when
  `Menu` is down without `Control` — AltGr, i.e. `Ctrl+Alt`, must stay exempt, matching
  `TextBox.skia.cs:1119-1125`).
- Beyond `MapToChar`: A–Z, 0–9 + the US shifted row `)!@#$%^&*(`, `Space`, `Enter`→`'\r'`,
  and — if desired — the US OEM punctuation set. Keep it **invariant US**, so injected sequences
  behave identically on every Skia target.

The table belongs in `Uno.UWP` (next to `InjectedInputKeyboardInfo`), private to the injector.

### 5.4 The Shift / CapsLock question

**Shift — settled.** WinUI derives case from *global* key state, not from the call (§1.1 #12: an
injected Shift-down really produced `'A'`). The injector must therefore read
**`KeyboardStateTracker.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down)`**, not the
`KeyboardModifiers` on the info. `KeyboardStateTracker` lives in
`src/Uno.UWP/UI/Core/Internal/KeyboardStateTracker.cs` — **the same assembly as `InputInjector`**, so
it is directly reachable with no new plumbing. **VERIFIED.**

Note `SetStateOnNonSideKeys` (:83-99) maps `LeftShift`/`RightShift` onto generic `Shift` (likewise
Control/Menu), so both an injected `VK_SHIFT (0x10)` and an injected `VK_LSHIFT (0xA0)` work. There
is **no** generic `Windows` key — `PlatformHelpers.GetKeyboardModifiers` queries `LeftWindows` and
`RightWindows` explicitly (**VERIFIED**, `PlatformHelpers.cs:32-42`).

**CapsLock — needs a decision (§13).** `CoreVirtualKeyStates.Locked` is *not* a lock-LED state; the
tracker's own remarks call it a generic per-key toggle, and — a quirk nobody surfaced — it is
**corrupted on Skia anyway**: `TrackKeyState` runs inside `RaiseEvent`, and `RaiseOnParent` calls
`parent.RaiseEvent(...)` recursively (**VERIFIED**, `UIElement.RoutedEvents.cs:833-847`), so
`KeyboardStateTracker.OnKeyDown` fires **once per ancestor plus once for tunneling**. The `Down` bit
is stable across those calls; the `Locked` bit toggles on every one. **Nothing may depend on
`Locked` on Skia.**

Options, ranked:
- **(A) Recommended — private toggle in the injector.** Flip an injector-private `bool _capsLock` on
  each injected `VirtualKey.CapitalLock` key-**down**, and XOR it with Shift for A–Z only. Cheap,
  matches WinUI for the case that actually matters, no framework change.
- (B) Ignore CapsLock entirely and document it. Cheapest; injected CapsLock becomes a no-op key.
- (C) Add real lock tracking to `KeyboardStateTracker`. Correct but touches shared state read by
  accelerators, and the multi-raise quirk above would have to be fixed first. Out of scope.

---

## 6. Unicode / text / IME

### 6.1 Unicode option — feasible, with a caveat

Win32 `KEYEVENTF_UNICODE` semantics: `wVk` must be **0** and `wScan` carries the **UTF-16 code
unit**. WinUI enforces the `VirtualKey == 0` rule with an `ArgumentException` (§1.1 #4 — OBSERVED).

Map it as:

```
KeyOptions.Unicode set  →  VirtualKey must be 0, else throw ArgumentException (WinUI parity)
                        →  KeyEventArgs(virtualKey: unchecked((VirtualKey)255),
                                        modifiers: <from tracker>,
                                        keyStatus: { ScanCode = 0, RepeatCount = 1, … },
                                        unicodeKey: (char)info.ScanCode)
```

`OnKey` then raises PreviewKeyDown → KeyDown → `CharacterReceived` in exactly the observed WinUI
order. A lone Unicode DOWN with no KeyUp still delivers the character (§1.1 #6) — nothing extra
needed, since the character is produced on the down pass.

**Surrogate pairs work for free**: two consecutive Unicode events produce two independent
key/char cycles, and `TextBox` concatenates the two code units into the complete emoji — matching
§1.1 #7.

**`KeyStatus.ScanCode` must be 0 for Unicode injection**, per the observed WinUI record
(`c1` events all report `scanCode:0` even though the code unit rode in `wScan`).

### 6.2 ScanCode option — must be a documented limitation

WinUI's `ScanCode` mode requires the OS keyboard layout to turn scan code `0x1E` into `VK_A` and
`'a'` (§1.1 #8). Uno has **no layout table on Skia** and `OnKey` takes an already-resolved tuple.

**Recommendation:** echo the caller's `ScanCode` into `CorePhysicalKeyStatus.ScanCode` (so the value
round-trips), but keep resolving the key from `VirtualKey`, and log a one-time warning that
scan-code-driven key identification is not supported. Do **not** ship a hardcoded US scan-code table
— it would be silently wrong on every non-US layout and is unlikely to be what a caller wants.

### 6.3 IME — hard limitation, must be documented

All six IME implementations of `IImeTextBoxExtension` (Win32 IMM32, X11 D-Bus IBus/Fcitx, macOS
`NSTextInputClient`, AppleUIKit `UITextInput`, Android `InputConnection`, WASM `CompositionEvent`;
FrameBuffer has none) are driven **exclusively by native platform events**. A key raised at `OnKey`
never reaches them.

Worse, injecting **during** a live composition is silently swallowed:
`ShouldSwallowKeyDuringComposition` (`TextBox.IME.skia.cs:26`) gates the default insertion branch on
every host except Android (`TextBox.skia.cs:1111-1114`, **VERIFIED**).

**Recommendation:**
- Document plainly: *injected keys cannot start, feed, or commit an IME composition.*
- Have `InjectKeyboardInput` log an informational warning (not throw) when the focused `TextBox` has
  `IsComposing == true`, so the silent drop is diagnosable.
- The managed test seam `TextBox.SetImeExtensionForTesting(IImeTextBoxExtension)`
  (`TextBox.IME.skia.cs:274-297`) remains the only way to exercise composition in tests. Don't try
  to route injection through it.

### 6.4 CharacterReceived double-fire — **definitive answer: it cannot double-fire, and you must not add a second channel**

Three independent facts, all **VERIFIED**:

1. `OnKey` raises `CharacterReceived` itself, once, after KeyDown, when `args.UnicodeKey` is set
   (`InputManager.Keyboard.skia.cs:105-108`). `RaiseCharacterReceived` (:133-143) is the **only**
   `RaiseEvent(UIElement.CharacterReceivedEvent, …)` site in the entire framework.
2. `TextBox` inserts the character from the **KeyDown** path (`args.UnicodeKey`,
   `TextBox.skia.cs:1126-1141`), and `TextBox.OnCharacterReceivedPartial` **early-returns unless
   `e.KeyStatus.IsKeyReleased` is true** (`TextBox.skia.cs:949`). A keydown-derived
   `CharacterReceived` therefore **cannot** insert a second character.
3. An existing passing runtime test proves the exact injection pattern:
   `Given_TextBox.skia.cs:7356-7365` calls `OnKeyTestingOnly(… unicodeKey:'a' …)` down then up and
   asserts `Text == "a"` and the sequence `["KeyDown", "CharacterReceived:a"]`.

**So:** set `KeyEventArgs.unicodeKey` and let `OnKey` do the rest. Do **not** add
`InjectCharacterReceived` to `IInputInjectorTarget`, and do **not** call
`RaiseCharacterReceived` from the injector.

**One caveat worth flagging:** `ComboBox` queues its own `OnCharacterReceived` from
`OnKeyDownPrivate` whenever `pArgs.UnicodeKey` is non-null
(`ComboBox.partial.mux.cs:1624-1631`, **REPORTED**, not re-read). That is a pre-existing double-fire
risk for char-carrying keys on `ComboBox`, independent of injection — but injection will exercise it,
so keyboard-injection tests against `ComboBox` may surface it.

### 6.5 The bare-character channel

`IUnoKeyboardInputSource.CharacterReceived` (Alt+numpad style, `IsKeyReleased = true`) is
implemented **only on Win32**; every other host declares `add { } remove { }`. If a future
Uno-only "inject a bare character with no key press" API is wanted, that is the channel — but it is
**not** part of the WinUI `InjectKeyboardInput` contract and should not be built now.

---

## 7. Modifier & key state

### 7.1 How injected modifiers reach accelerators (this works)

Every accelerator/menu code path reads **ambient** modifier state, not the routed args:

```
Control.mux.cs:212 / UIElement.mux.cs:663 / UnoFocusInputHandler.cs:56
  → CoreImports.Input_GetKeyboardModifiers()        src/Uno.UI/DirectUI/CoreImports.cs:11
  → PlatformHelpers.GetKeyboardModifiers()          src/Uno.UI/UI/Xaml/Controls/PlatformHelpers.cs:10-45  (VERIFIED)
  → InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey)
  → KeyboardStateTracker.GetKeyState(key)           src/Uno.UWP/UI/Core/Internal/KeyboardStateTracker.cs:26
```

And `KeyboardStateTracker` is fed purely as a side effect of raising routed key events
(`TrackKeyState`, `UIElement.RoutedEvents.cs:808-829`), including the tunneling pass on Skia.

**Consequence:** an injected `Shift`/`Control`/`Menu` key-down that goes through `OnKey`
automatically updates the tracker, and `Ctrl+X` accelerators match. **The caller must inject the
modifier key events themselves**, exactly as WinUI requires — setting `KeyboardModifiers` on the
args alone is not enough for accelerators.

Which VirtualKey to inject: generic (`Shift`=0x10) or side-specific (`LeftShift`=0xA0) both work, via
`SetStateOnNonSideKeys`. For the Windows key, **only** `LeftWindows`/`RightWindows` work (no generic
key is tracked, and `PlatformHelpers` queries both explicitly).

### 7.2 Ctrl+Click across mouse + keyboard injection — **a real gap nobody flagged**

`PointerRoutedEventArgs.KeyModifiers` comes from the **args**, not from `KeyboardStateTracker`
(**VERIFIED**: `PointerRoutedEventArgs.Managed.cs:56` → `pointerEventArgs.KeyModifiers` →
`PointerEventArgs.cs:10-20`, set by `InjectedInputMouseInfo.ToEventArgs(state, modifiers)`).

And the **public** `InputInjector.InjectMouseInput` hardcodes `VirtualKeyModifiers.None`
(`InputInjector.cs:258`, **VERIFIED**). Only the *internal*
`InjectMouseInput(IEnumerable<(InjectedInputMouseInfo, VirtualKeyModifiers)>)` overload — used by the
`Mouse` test driver's `Press(VirtualKeyModifiers)` — carries modifiers.

**So today: injecting a Ctrl key-down and then calling `Mouse.Press()` will NOT produce
`KeyModifiers == Control` on the pointer args.** Keyboard state and pointer modifier state are two
disconnected worlds.

**Recommendation (cheap, high value):** have `InjectedInputMouseInfo.ToEventArgs` — or the public
`InjectMouseInput` — default its modifiers from `PlatformHelpers`-equivalent tracker reads
(`KeyboardStateTracker.GetKeyState(Shift/Control/Menu/LeftWindows/RightWindows)`) instead of
hardcoding `None`, so a held injected modifier flows into injected pointer events. `KeyboardStateTracker`
is in the same assembly, so this needs no new plumbing. Flag as a **behaviour change to the existing
mouse injector** in §13.

### 7.3 Unbalanced key-down hazard

An injected key-down with no matching key-up leaves the modifier **stuck Down** in the process-wide
tracker, silently breaking every subsequent accelerator match. The runtime-test harness already
resets it for exactly this reason (`SamplesApp.UnitTests.Shared/Controls/UnitTest/UnitTestsControl.cs:1118-1127`,
`KeyboardStateTracker.Reset()`, **REPORTED**).

Mitigations, in order of preference:
1. **The `Keyboard` test driver must be `IDisposable` and release everything it pressed** on
   `Dispose()` — exactly like `Mouse.ReleaseAny()`. This is the primary mitigation.
2. Keep `KeyboardStateTracker.Reset()` in the per-test harness teardown (already there).
3. Do **not** auto-balance inside `InjectKeyboardInput` — WinUI does not, and callers legitimately
   hold keys across calls (§1.1 #12).

### 7.4 Injected modifiers are invisible to the hosts

Injection updates Uno's `KeyboardStateTracker`, never the host's own modifier source (Win32
`GetKeyState`, X11 event state mask, macOS `_previousFlags`, FrameBuffer `_pressedKeys`). A **real**
key pressed while an **injected** Shift is held will not carry Shift in its args. Accelerators still
fire (they read the tracker); character case will be wrong. **Document: do not mix real and injected
keyboard input.**

---

## 8. Accelerators, access keys, focus, Tab

### What the pipeline gives for free (join at `OnKey` → all of this works)

| Feature | Where | Free? |
|---|---|---|
| **Focus targeting** — key goes to `FocusManager.GetFocusedElement(XamlRoot)`, falling back to `VisualTree.RootElement` | `InputManager.Keyboard.skia.cs:64,75` | ✅ |
| **Focus re-resolution after Preview** (WinUI parity) | :75 | ✅ |
| **Per-`Control` accelerators** | `Control.cs:1194-1203` → `Control.mux.cs:210-236` | ✅ |
| **Per-`UIElement` accelerators** | `UIElement.Keyboard.cs:13-22` → `UIElement.mux.cs:632-684` | ✅ |
| **Global accelerators** (root, last) | `UnoFocusInputHandler.cs:54-74` → `KeyboardAcceleratorUtility.ProcessGlobalAccelerators` | ✅ (given injected modifier keys, §7.1) |
| **Tab / Shift+Tab focus movement** | `UnoFocusInputHandler.cs:36-39,77-117` → `FocusManager.FindAndSetNextFocus`, `IsProcessingTab=true`, `FocusState.Keyboard` | ✅ |
| **Arrow / DPad directional focus** | `UnoFocusInputHandler.cs:41-52,119-170` | ✅ |
| **Transient flyout dismissal** on unhandled modifier-less key-down | `InputManager.Keyboard.skia.cs:83-89` | ✅ |
| **Context-menu triggers** (Shift+F10 / Application / GamepadMenu) | :93-99 → `ContextMenuProcessor.cs:46-57` | ✅ |
| **Popup/flyout routing** — `PopupRoot` is a child of the same `VisualTree.RootElement`, sharing the ContentRoot's FocusManager; Tab inside a popup cycles | `VisualTree.cs:168-174,201,258`; `FocusManager.mux.cs:486-493` | ✅ |
| **`TextBox` text-input priority** over global accelerators (`HandledShouldNotImpedeTextInput`) | `TextBox.cs:1234-1243`, consumed at `UnoFocusInputHandler.cs:54` | ✅ |

### What is genuinely missing

- **Access keys do not exist on Skia — at all.** `AccessKeyManager` is a `[NotImplemented]` stub
  including `__SKIA__`; `ContentRoot.AccessKeyExport` is a no-op with an explicit
  `//TODO Uno: … (see #3219)`; `AKCommon.h.mux.cs` is entirely commented-out C++. Injecting `Alt+X`
  will never invoke an access key. **This is a pre-existing gap, not injection's problem — document
  it as an accepted limitation.** (**REPORTED** by the focus investigator; the `[NotImplemented]`
  stub state is consistent with everything else I verified in that area.)
- **`CoreWindow.KeyDown`/`KeyUp` will not see injected keys** (§3.1). Whether that matters is a
  design decision (§13).
- **Per-element accelerators are skipped on `Control`s that don't override `OnKeyDown`** —
  `Control` only subscribes `OnKeyDownHandler` when the override is detected
  (`Control.cs:459-462`), while `UIElement.OnKeyDown` early-returns for `this is Control`. Global
  accelerators still fire at the root. This is a **pre-existing WinUI divergence**; injection tests
  must not assume per-element accelerators fire on a plain `Control`.
- **Accelerator eligibility is gated**: `KeyboardAcceleratorUtility.IsKeyValidForAccelerators`
  restricts which keys can ever be accelerators, matching requires **exact modifier equality**, and
  live accelerators are registered **per-ContentRoot**. Tests must respect these gates.

---

## 9. `InjectShortcut`

WinUI's implementation is *synthesised keystrokes that the shell consumes* (§1.1 #20 — OBSERVED).
None of the three shell surfaces exist on Skia. Recommendation per value:

### `Back` = 0 → **route to `SystemNavigationManager.RequestBack()`**

This is the one value with a real, correct Uno target.

- `SystemNavigationManager.RequestBack()` is **`internal` and in `Uno.UWP`** (**VERIFIED**,
  `src/Uno.UWP/UI/Core/SystemNavigationManager.cs:122-134`), and `GetForCurrentView()` is public
  static (:14). `InputInjector` is in the *same assembly* — so
  `SystemNavigationManager.GetForCurrentView().RequestBack()` is a direct, dependency-free call.
- It raises `InternalBackRequested` first, which reaches `BackButtonIntegration`
  (`src/Uno.UI/DirectUI/BackButton/BackButtonIntegration.cs`) and dismisses the topmost
  dialog/flyout/popup in **LIFO** order — the semantically correct "back" behaviour.
- Runtime tests already depend on exactly this via
  `TestServices.Utilities.InjectBackButtonPress()` (used by `Given_Popup.cs`, `Given_Flyout.cs`).
- **On Win32/X11/macOS/FrameBuffer there is no back producer at all today** — only Skia-Android
  (`ApplicationActivity.OnBackPressed`) and the browser popstate handler. So `InjectShortcut(Back)`
  becomes genuinely useful, not just parity theatre.

Do **not** additionally inject `VirtualKey.GoBack` (166) key events — that would double-dispatch.

### `Start` = 1 and `Search` = 2 → **synthesise the chord into the app's own pipeline + log a warning**

Two defensible options:

- **(A) Recommended — synthesise the keystrokes**, matching what WinUI actually sends:
  - `Start`  → `LeftWindows` down, `LeftWindows` up
  - `Search` → `LeftWindows` down, `S` down, `S` up, `LeftWindows` up

  Then log an informational warning that no OS shell action is taken. This is **not** a pure no-op:
  `KeyboardAccelerator` supports `VirtualKeyModifiers.Windows`, and `PlatformHelpers` reads
  `LeftWindows`/`RightWindows` from the tracker (**VERIFIED**, `PlatformHelpers.cs:32-42`) — so an
  app-defined `Win+S` accelerator will genuinely fire. It is also the most faithful reproduction of
  the observed WinUI event stream minus the shell.

  ⚠️ WinUI's observed stream is *asymmetric* because the shell eats the non-modifier key-**down**
  (the app sees only `Win`-down, `S`-**up**, `Win`-up). Uno should emit the **balanced** sequence —
  reproducing the shell's swallow would be actively harmful (unbalanced state, §7.3).

- **(B) No-op + warning.** Defensible, zero risk, strictly less useful.

**Never** call the real OS shortcut (`SendInput(VK_LWIN)` on Win32): an unsandboxed, untestable side
effect that opens the Start menu mid-test, with no analogue on any other Skia target.

### Platform coverage

`InjectShortcut` should be implemented **unconditionally in the shared partial** so all six
compilations see it (§11). `Back` works everywhere `SystemNavigationManager` does; `Start`/`Search`
degrade to key synthesis on Skia and to a no-op elsewhere via the empty `partial void`.

---

## 10. `CorePhysicalKeyStatus` contract

The struct (**VERIFIED**, `src/Uno.UWP/UI/Core/CorePhysicalKeyStatus.cs:9-14`) has six public
mutable fields: `RepeatCount`, `ScanCode`, `IsExtendedKey`, `IsMenuKeyDown`, `WasKeyDown`,
`IsKeyReleased`.

**`KeyRoutedEventArgs.KeyStatus` is genuinely implemented on Skia** — the `[NotImplemented]` list at
`KeyRoutedEventArgs.cs:28-29` is `__ANDROID__ || __APPLE_UIKIT__ || IS_UNIT_TESTS || __WASM__ || false || __NETSTD_REFERENCE__`,
with `__SKIA__` **absent**. **VERIFIED.**

**No Skia host populates repeat state today** — every host sets at most `ScanCode` and
`RepeatCount = 1`; Skia-Android passes `default(CorePhysicalKeyStatus)` with a `/*TODO*/`. The only
place `WasKeyDown`/`IsKeyReleased` are ever set is the Win32 Alt+numpad `CharacterReceived` path
(`Win32WindowWrapper.Keyboard.cs:86-97`, **VERIFIED**). **So the injector effectively defines this
contract**, and only `TextBox.OnCharacterReceivedPartial`'s `IsKeyReleased` guard consumes it today.

### Recommended contract (matching the observed WinUI records exactly)

| Field | Value | Rationale |
|---|---|---|
| `ScanCode` | `info.ScanCode` **only when `KeyOptions.ScanCode` is set**; otherwise **0** | WinUI discards the supplied scan code for plain-VK injection (§1.1 #9, OBSERVED). Echoing it unconditionally would diverge. |
| `IsExtendedKey` | `KeyOptions.HasFlag(ExtendedKey)` | Surfaced faithfully by WinUI (§1.1 #10). |
| `IsKeyReleased` | `KeyOptions.HasFlag(KeyUp)` | Matches every observed KeyUp record (`isKeyReleased:true`). |
| `WasKeyDown` | `true` on key-up; on key-down, `true` iff the same `VirtualKey` is already `Down` in `KeyboardStateTracker` | Matches WinUI: KeyDown `wasKeyDown:false` on first press, `true` on the coalesced repeat (§1.1 #15, #18). |
| `RepeatCount` | `1` always | WinUI never auto-repeats (§1.1 #14). Coalescing (§1.1 #15) is an OS message-queue artifact Uno has no equivalent of — **do not emulate it**. |
| `IsMenuKeyDown` | `KeyboardStateTracker.GetKeyState(VirtualKey.Menu).HasFlag(Down)` | ⚠️ **Untested against WinUI** — the repro never injected Alt, so the `WM_SYSKEYDOWN` path is unverified. Tracker-derived is the sane default; flag as an assumption. |

**Do not** add an `IsInjected` flag to `KeyRoutedEventArgs` for parity with
`PointerRoutedEventArgs.IsInjected` unless a consumer needs it. It is a public-ish surface change
with no identified caller; defer.

---

## 11. Generated-folder "falsing out"

### 11.1 The mechanism (verified against the live generated file)

`src/Uno.UWP/Generated/3.0.0.0/**` is produced by `src/Uno.WinAppSDKSyncGenerator` (`sync` mode),
which loads six Uno compilations **from source** and Roslyn-diffs them against WinAppSDK 2.1.3
reference assemblies. Two `#if` forms appear and they mean different things:

- **Class-level `#if false || false || false || false || false || false`** — one slot per platform
  (Android, iOS, tvOS, WASM, Skia, Reference); a slot is `false` when the *type* has at least one
  non-generated declaration on that platform. `InputInjector` already shows six `false`s
  (**VERIFIED**, line 6 of the generated file). **This header will not change.**
- **Member-level `#if __ANDROID__ || … || __SKIA__ …`** — emitted when at least one of the six
  per-platform symbol lookups returned `null`. For *members*, the outcome is **binary**: either the
  stub survives listing the still-missing platforms, or the whole `#if…#endif` block **collapses in
  place to a single line**:
  ```
  // Skipping already declared method Windows.UI.Input.Preview.Injection.InputInjector.InjectMouseInput(System.Collections.Generic.IEnumerable<…>)
  ```
  `__SKIA__` can **never** drop out of a member's `#if` list while the stub survives.

Live proof, from the same file (**VERIFIED**, `Generated/.../InputInjector.cs:26-32`): seven
`InjectMouseInput`/`InjectTouchInput`/`InjectPenInput`/… members are already collapsed to skip
comments, because they are declared unconditionally in
`src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs`.

### 11.2 What that means concretely

**To collapse `InjectKeyboardInput` and `InjectShortcut`, declare them unconditionally in the
existing cross-platform partial** `src/Uno.UWP/UI/Input/Preview.Injection/InputInjector.cs` — no
`.skia.cs` suffix, no `#if` — exactly as `InjectMouseInput` already is, hand-annotating
`[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "__NETSTD_REFERENCE__")]` for the
platforms where the body is a no-op. **The attribute does not affect the skipping** — only symbol
null-ness does.

**Signature-matching rules** (`src/Uno.WinAppSDKSyncGenerator/Helpers/SymbolMatchingHelpers.cs`,
**VERIFIED** — `AreParametersMatching` compares `IsOptional && (Name == Name || IgnoreParameterName) && IsParams && RefKind && Type`,
and `IgnoreParameterName`'s carve-outs are limited to `_`-prefixed WinUI names, `windowsruntimeStream`,
`Equals`, and `Duration.Compare` — **none of which apply here**):

- must be `public`, non-static, non-virtual
- `public void InjectKeyboardInput(IEnumerable<InjectedInputKeyboardInfo> **input**)`
- `public void InjectShortcut(InjectedInputShortcut **shortcut**)`
- **the parameter names `input` and `shortcut` are load-bearing.**

**`InjectedInputKeyboardInfo`** must gain a hand-written partial at
`src/Uno.UWP/UI/Input/Preview.Injection/InjectedInputKeyboardInfo.cs` (properties `KeyOptions`,
`ScanCode` ushort, `VirtualKey` ushort + an internal `ToEventArgs` converter), following
`InjectedInputMouseInfo.cs`. Its generated twin then keeps the class header and becomes
`// Skipping already declared property KeyOptions` / `ScanCode` / `VirtualKey` plus a skip line for
the ctor — compare the already-collapsed `InjectedInputMouseInfo.cs` / `InjectedInputPenInfo.cs`.

**⚠️ Correction to one investigator: the enums need NO work.** `InjectedInputKeyOptions` and
`InjectedInputShortcut` already emit real values on `__SKIA__` with no `[NotImplemented]`
(**VERIFIED**, §1.4 item 1). Hand-writing them (which *would* fully `#if false` out the generated
enum, as `InjectedInputPenButtons.cs` demonstrates — **VERIFIED**, generated body is
`#if false || … { // Skipping already declared field … }`) is optional cleanup only.

### 11.3 Hand-edit vs regenerate — the honest answer

**Both happen; the repo tolerates hand-editing but verifies it.**

- Precedent 1: `85db07bfc6` *"feat: Add support for Pen input injection"* hand-edited the Generated
  files and got the format wrong (full-name property comments, declaration order instead of metadata
  order, a mis-indented `#if`). `244a31af41` *"chore: Run sync"* corrected all of it months later.
- Precedent 2: `17ed10ac5f` (CharacterReceived) dropped a class-level block entirely;
  `7b801ec8e8` *"chore: Align generated file with sync generator output"* restored it 37 minutes later.

Since mid-2026, `.github/workflows/winappsdk-sync-check.yml` runs the generator on every PR
(windows-latest, pinned SDK, `dotnet workload install android ios maccatalyst tvos`) and **fails on
any drift**, uploading a `sync-generator.patch` artifact; commenting `/apply-sync-gen` auto-pushes
`chore: Sync generator run`.

This resolves the apparent conflict with `.claude/rules/build-system.md` ("never edit Generated/
folders"): **hand-edits are acceptable only when they reproduce generator output byte-for-byte, and
CI proves it.**

### 11.4 Running the generator locally

```
build\run-api-sync-tool.cmd
```

**VERIFIED** — the script moves `src/crosstargeting_override.props` aside (restoring it even on
failure), then:

```
dotnet restore filters\Uno.UI-top-projects-for-sync-gen.slnf -p:Configuration=Release -p:CI_Build=true -p:_IsCIBuild=true -p:SyncGeneratorRunning=true
dotnet build ..\src\Uno.WinAppSDKSyncGenerator\Uno.WinAppSDKSyncGenerator.csproj -c Release
dotnet build ..\src\Uno.WinAppSDKSyncGenerator.References\Uno.WinAppSDKSyncGenerator.References.csproj -c Release
..\src\Uno.WinAppSDKSyncGenerator\bin\Release\Uno.WinAppSDKSyncGenerator.exe sync
```

Traps:
- It **deletes all five Generated directories first** — an aborted run guts the tree.
- It loads `net9.0` / `net9.0-android` / `net9.0-ios18.0` / `net9.0-tvos18.0` TFMs, so a
  `crosstargeting_override.props` pinning net10.0 breaks the restore graph (hence the move-aside).
- Mobile workloads (`android ios maccatalyst tvos`) are required, and the references project needs
  Windows + WinAppSDK 2.1.3.
- **Fallback if workloads aren't installed here:** hand-edit to match the expected output, push, and
  let `winappsdk-sync-check.yml` either pass or hand back `sync-generator.patch`.

### 11.5 Documentation requirements

- **`GenerateDocumentationFile` / CS1591 are NOT enabled** for `Uno.UWP` or `Uno.UI` anywhere —
  repo-wide the only hits are `<GenerateDocumentationFile>false</GenerateDocumentationFile>` in
  SamplesApp projects. XML docs on public surface are **convention, not a build gate**.
- `TreatWarningsAsErrors=true` repo-wide, but `CS1570;CS1572;CS1574;CS1711;CS1712` are `NoWarn`'d
  (`src/Directory.Build.props:262-263`), so malformed XML docs will not break the build.
- **House style for partially-implemented WinRT surface**: `<summary>` + `<param>`/`<returns>` +
  a `<remarks>` naming the divergence in **full "Uno Platform"** prose, alongside the
  `[NotImplemented]` attribute. There is an in-file precedent right where the new members go:
  `InputInjector.TryCreate(object relativeRoot)` (`InputInjector.cs:35-59`, **VERIFIED**), including
  a `<param>` explaining why the type is `object`.
- **Required `<remarks>` content for the new members:**
  - `InjectKeyboardInput`: in-process only (no cross-process/OS delivery); no IME participation;
    `ScanCode`-driven key identification unsupported; no auto-repeat (caller synthesises);
    invariant-US character synthesis.
  - `InjectShortcut`: `Back` maps to the system back request; `Start`/`Search` synthesise the
    keystrokes into the app's own pipeline and take no OS shell action.

### 11.6 Analyzer consequence

`UnoNotImplementedAnalyzer` (Uno0001, **Warning**) fires at every call site when the target's
`[NotImplemented]` list contains a define active in that compilation. With
`TreatWarningsAsErrors=true`, **leaving `"__SKIA__"`/`"__WASM__"` in the attribute list while the
runtime tests call the API will break the RuntimeTests build.** The attribute list on the new members
must therefore be exactly the platforms where the body genuinely no-ops.

There is also a Reference-API special case: under `UNO_REFERENCE_API` without `__SKIA__`/`__WASM__`,
the analyzer warns only if **both** `__SKIA__` and `__WASM__` are listed — so implementing on
Skia + WASM automatically clears the Reference build. (**REPORTED**; consistent with the analyzer's
documented behaviour but not re-read.)

---

## 12. Test plan

### 12.1 The `Keyboard` driver

Add `src/Uno.UI.Toolkit/DevTools/Input/Keyboard.cs` + `GetKeyboard()` on `InputInjectorExtensions`
(**VERIFIED** the file today has `GetPointer/GetFinger/GetMouse/GetPen`). `Uno.UI.Toolkit` has
`InternalsVisibleTo` for `Uno.UI.RuntimeTests` and `.Windows`, and is a `ProjectReference` from every
RuntimeTests head — so the driver can be `internal` like `Mouse`/`Finger`/`Pen`.

```csharp
internal sealed class Keyboard : IDisposable
{
    private readonly InputInjector _injector;
    private readonly List<ushort> _pressed = new();   // for ReleaseAny()

    public void Press(VirtualKey key);                 // down only
    public void Release(VirtualKey key);               // up only
    public void Tap(VirtualKey key);                   // down+up
    public void Type(string text);                     // per-char, Unicode option
    public IDisposable Hold(VirtualKey modifier);      // scoped modifier, auto-release
    public void Chord(VirtualKey key, VirtualKeyModifiers mods);  // mods down, key down/up, mods up

    public void ReleaseAny();                          // release everything still down
    public void Dispose() => ReleaseAny();             // ← the §7.3 hazard mitigation
}
```

Mirror `Mouse.cs`'s dual `#if HAS_UNO` / WinAppSDK shape so the same tests run against native WinUI.

**⚠️ WinAppSDK-head trap:** `Keyboard.Type`/`Chord` must not use Uno-only extension methods, and the
WinAppSDK path must batch **≤ 16 `InjectedInputKeyboardInfo` per call** (§1.1 #3) or it will throw
`ArgumentException` on real Windows. Chunk in the driver.

**⚠️ `IsXamlIsland` guard:** the existing injector tests bail out with
`if (TestServices.WindowHelper.IsXamlIsland) return;` — *"Input injection is not supported in
XamlIslands"* (**VERIFIED**, `Given_InputInjector.cs:39-43`). New keyboard tests must do the same
until §3.4 is resolved.

### 12.2 Tests to write

New file `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Input_Preview_Injection/Given_InputInjector_Keyboard.cs`,
guarded `#if HAS_INPUT_INJECTOR || WINAPPSDK`, `[RunsOnUIThread]`,
`[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]` where WinUI's
system-wide semantics make an assertion untenable.

| # | Test | Asserts | WinUI-parity? |
|---|---|---|---|
| 1 | `When_InjectKey_Types_Into_Focused_TextBox` | Focus a TextBox, `Tap(A)` **without naming an element** → `Text == "a"`, event sequence `[PreviewKeyDown, KeyDown, CharacterReceived:a, PreviewKeyUp, KeyUp]` | ✅ |
| 2 | **`When_InjectKey_Raises_CharacterReceived`** ← **the discriminating test** | see §12.3 | ✅ |
| 3 | `When_InjectShiftA_Produces_Uppercase` | Shift-down, A down/up, Shift-up → `Text == "A"` | ✅ (§1.1 #12) |
| 4 | `When_InjectCtrlA_Selects_All` | Seed `"hello world"`, Ctrl+A → `SelectionLength == 11`, **no** `CharacterReceived` | ✅ (§1.1 #13) |
| 5 | `When_InjectCtrlModifier_Fires_Accelerator` | Ctrl-down + injected `X` fires a `KeyboardAccelerator{Key=X, Modifiers=Control}`; **fails** if only `KeyboardModifiers` is set on the args | ✅ |
| 6 | `When_InjectTab_Moves_Focus` | Two focusable elements; injected Tab moves focus; `FocusState == Keyboard` | ✅ |
| 7 | `When_InjectUnicode_Delivers_Character` | `KeyOptions.Unicode`, `VirtualKey = 0`, `ScanCode = 0x00E9` → `Text == "é"`, key events report `Key == 255` | ✅ (§1.1 #5) |
| 8 | `When_InjectUnicode_With_NonZero_VirtualKey_Throws` | `ArgumentException` | ✅ (§1.1 #4) |
| 9 | `When_InjectUnicode_SurrogatePair` | 0xD83D then 0xDE00 → `Text.Length == 2`, complete emoji | ✅ (§1.1 #7) |
| 10 | `When_InjectKeyUp_Without_KeyDown` | Bare KeyUp; no KeyDown, no CharacterReceived, no text change | ✅ (§1.1 #16) |
| 11 | `When_InjectKeyDown_Held_Does_Not_Repeat` | One down, wait 1 s, no further injection → exactly one KeyDown, `RepeatCount == 1`, one character | ✅ (§1.1 #14) |
| 12 | `When_InjectKeys_Preserve_Order` | U,N,O → `"uno"` | ✅ (§1.1 #17) |
| 13 | `When_InjectKeyStatus_Is_Populated` | KeyDown: `WasKeyDown==false, IsKeyReleased==false, RepeatCount==1`; KeyUp: `WasKeyDown==true, IsKeyReleased==true`; `IsExtendedKey` follows the option | ✅ (§1.1 #18, #10) |
| 14 | `When_InjectShiftF10_Opens_ContextFlyout` | Shift+F10 on an element with a `ContextFlyout` opens it | Uno-only (`ContextMenuProcessor`) |
| 15 | `When_InjectKey_Dismisses_Transient_Flyout` | An open transient flyout closes on an unhandled modifier-less injected key | Uno-only |
| 16 | `When_InjectShortcut_Back_Dismisses_Popup` | Open a `Popup`/`Flyout`, `InjectShortcut(Back)` closes it (LIFO) | Uno-only |
| 17 | `When_Keyboard_Driver_Disposed_Releases_Modifiers` | `using (kb.Hold(Control)) { }` then assert `KeyboardStateTracker.GetKeyState(Control)` is not Down | Uno-only (§7.3) |
| 18 | `When_Inject_Into_Second_Window` | ⚠️ Write only once §3.4 is decided; today it must fail. | Uno-only |

### 12.3 The discriminating test — must fail against `TestServices.KeyboardHelper`, pass against real injection

**Test 2, `When_InjectKey_Raises_CharacterReceived`:**

```csharp
var tb = new TextBox();
await UITestHelper.Load(tb);
tb.Focus(FocusState.Programmatic);

var sequence = new List<string>();
tb.AddHandler(UIElement.KeyDownEvent,
    new KeyEventHandler((_, _) => sequence.Add("KeyDown")), handledEventsToo: true);
tb.AddHandler(UIElement.CharacterReceivedEvent,
    new TypedEventHandler<UIElement, CharacterReceivedRoutedEventArgs>(
        (_, e) => sequence.Add($"CharacterReceived:{e.Character}")), handledEventsToo: true);

using var keyboard = InputInjector.TryCreate()!.GetKeyboard();
keyboard.Tap(VirtualKey.A);
await WindowHelper.WaitForIdle();

Assert.AreEqual("a", tb.Text);
CollectionAssert.AreEqual(new[] { "KeyDown", "CharacterReceived:a" }, sequence);
```

**Why it discriminates (VERIFIED):** `TestServices.KeyboardHelper` raises routed events **directly
on an element** via `SafeRaiseTunnelingEvent`/`SafeRaiseEvent` and hand-patches
`InputManager.LastInputDeviceType` (`TestServices.KeyboardHelper.cs:303-355`). It never enters
`InputManager.KeyboardManager.OnKey`, and `RaiseCharacterReceived`
(`InputManager.Keyboard.skia.cs:133-143`) is the **only** `CharacterReceivedEvent` raise site in the
framework. So the `CharacterReceived:a` entry is **structurally unreachable** through the helper —
the assertion fails today and passes with real injection. It also proves the whole chain: focus
resolution, tunneling→bubbling, `UnicodeKey` transport, and the WM_KEYDOWN→WM_CHAR ordering.

**Secondary discriminators** (same reasoning, also unreachable from `KeyboardHelper`): **test 14**
(`ContextMenuProcessor.ProcessContextRequestOnKeyboardInput` is called *only* from `OnKey`:93-99) and
**test 15** (flyout dismissal at :83-89).

### 12.4 Migration

Once injection lands, re-point `TestServices.KeyboardHelper` at
`InputInjector.GetKeyboard()` (it already ships a complete `string → VirtualKey` table and a
`$d$_key#$u$_key` mini-language). This deletes the `UpdateLastInputDeviceType`
*"Workaround for not simulating the last input device type correctly yet"* hack and makes the
existing `KeyboardAcceleratorTests` and `Given_UnoFocusInputHandler` suites exercise the **real**
pipeline. **Do this as a separate follow-up PR** — it will surface latent test breakage (tests that
pass today only because they bypass `OnKey`).

Also delete `KeyboardManager.OnKeyTestingOnly` and re-point its two call sites
(`Given_UIElement.cs:398-404`, `Given_TextBox.skia.cs:7357`).

---

## 13. Open design decisions

Ranked by how much they change the implementation. Duplicates across investigators merged.

### D1 — ContentRoot targeting on multi-window Skia ★★★ (biggest API-shape question)

`[ThreadStatic]`, set-once, first-ContentRoot-wins. Every Skia `Window` is its own ContentRoot with
its own FocusManager, so injecting into window 2 is impossible.

- **(a) Recommended** — per-thread *list*; `TryCreate()` keeps first-wins (+ warning);
  `TryCreate(object relativeRoot)` selects the owning target via a new
  `IInputInjectorTarget.OwnsRoot(object)`; registration moves to the `InputManager` ctor.
- (b) Resolve the target at inject time from the activated/focused window — needs a notion of
  "active window" on Skia that does not exist today.
- (c) Keep thread-static, re-point on window activation — surprising, racy.
- (d) Do nothing; document single-window-only.

**Recommendation: (a).** Caveat: it changes `TryCreate(object)` semantics for pointers too (today
`relativeRoot` only scopes hit-testing). Arguably a bug fix; call it out in the PR.

### D2 — Should injected keys raise `CoreWindow.KeyDown`/`KeyUp`? ★★★

They are wired to the **host source**, not `OnKey`, so joining at `OnKey` skips them.

- **(a) Recommended** — no. Pointer injection likewise bypasses the host pointer source; consistency
  wins, and a shim `IUnoKeyboardInputSource` would drag in host-specific side effects.
- (b) Raise them from `OnKey` — changes ordering for *real* keys on every host too.
- (c) Push injection through a shim host source — most faithful, most invasive.

**Recommendation: (a), documented.** Revisit only if a concrete consumer is found.

### D3 — Character synthesis: injector-private table vs extending `MapToChar`? ★★★

- **(a) Recommended** — injector-private, invariant-US, in `Uno.UWP`, computed *before* constructing
  `KeyEventArgs`. Required anyway for `CharacterReceived` parity (§5.2), and touches nothing else.
- (b) Extend `MapToChar` — silently changes X11-IBus, Skia-AppleUIKit and `KeyboardHelper`.
- (c) Rely on the existing `MapToChar` fallback — produces text with **no** `CharacterReceived`;
  no Enter, no punctuation. Fails WinUI parity.

**Recommendation: (a).**

### D4 — Replicate the hard 16-item `ArgumentException` cap? ★★

WinUI throws at 17 (OBSERVED); the docs only say it "can result in an ArgumentException".

- **(a) Recommended** — **do not** throw on Skia; there is no batching constraint to justify it, and
  throwing would break callers that batch longer sequences. Instead, **chunk to 16 in the
  `Keyboard` test driver** so the same tests run against native WinUI.
- (b) Replicate exactly for bug-for-bug parity — hostile to callers, no benefit.

**Recommendation: (a).** Document the divergence.

### D5 — `InjectShortcut(Start)`/`(Search)` semantics ★★

Synthesise the (balanced) chord + warning, vs pure no-op + warning. See §9.

**Recommendation: synthesise.** It makes `Windows`-modifier accelerators genuinely fire and is the
closest faithful reproduction minus the shell. Never call the real OS shortcut.

### D6 — CapsLock modelling ★★

Private injector toggle (A) / ignore + document (B) / real tracker support (C). See §5.4.

**Recommendation: (A).** Note the `Locked` bit is already meaningless on Skia (multi-raise, §5.4) —
do not build on it.

### D7 — Should injected pointer events pick up injected modifier state? ★★

Today `InjectMouseInput` hardcodes `VirtualKeyModifiers.None`, so injected Ctrl + `Mouse.Press()`
does **not** produce `KeyModifiers == Control` (§7.2 — a real gap).

- **(a) Recommended** — default the modifiers from `KeyboardStateTracker` (same assembly) when the
  caller does not supply them.
- (b) Leave as-is; require the internal `(info, modifiers)` overload.

**Recommendation: (a)**, but flag it as a **behaviour change to the existing mouse injector** and
consider landing it as a separate commit so it can be reverted independently.

### D8 — Declare the new members on native Android/iOS/tvOS/Reference too? ★★

The clean `// Skipping already declared method` collapse **requires** a declaration in all six
compilations.

**Recommendation: yes** — declare unconditionally in the shared partial with
`[Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "__NETSTD_REFERENCE__")]`, exactly as
`InjectMouseInput`/`InjectTouchInput` do. The shared body compiles there because the new
`IInputInjectorTarget` members forward to `partial void`s with no off-Skia body.

### D9 — Behaviour when the ContentRoot's `XamlRoot` is null ★

`FocusManager.GetFocusedElement(null)` throws `ArgumentNullException`; the pointer path throws
`InvalidOperationException`. Options: throw `InvalidOperationException` (pointer-consistent),
no-op + warning, or silently no-op.

**Recommendation: no-op + `LogLevel.Warning`.** Injection is a test/automation API; a hard throw
during app startup races is worse than a diagnosable no-op. Explicit decision needed.

### D10 — Delete `OnKeyTestingOnly` and migrate `TestServices.KeyboardHelper` now or later? ★

**Recommendation: delete `OnKeyTestingOnly` in this PR** (two call sites), **migrate
`KeyboardHelper` in a follow-up PR** — the migration will surface latent breakage in the accelerator
and focus suites, and mixing that into the feature PR would obscure the diff.

### D11 — `IsInjected` flag on `KeyRoutedEventArgs`? ★

`PointerRoutedEventArgs` has one; `KeyRoutedEventArgs` does not.

**Recommendation: defer.** No identified consumer; add when one appears.

### D12 — Public `Keyboard` driver surface? ★

`Mouse`/`Finger`/`Pen` are `internal` + IVT. Keyboard automation may have a Hot Design / external
automation requirement.

**Recommendation: `internal` for now**, matching the siblings; promote if Hot Design needs it.

### D13 — GitHub issue number ★

**No issue was identified.** grep for `2273` returns nothing; `#22254` is the *CharacterReceived*
parent (`17ed10ac5f` body: *"Part of #22254."*). Per AGENTS.md, every non-doc PR must reference an
issue — **identify or create one before opening the PR.**

---

## 14. Risks and unknowns

### Risks

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | **Unbalanced injected key-down poisons `KeyboardStateTracker` process-wide**, silently breaking every later accelerator match. | High | `Keyboard : IDisposable` with `ReleaseAny()`; keep `KeyboardStateTracker.Reset()` in test teardown. |
| R2 | **`TryCreate()` silently targets the wrong window** in multi-window apps (first-ContentRoot-wins). Keys vanish with no error. | High | D1. At minimum, log a warning listing the other candidates. |
| R3 | **Migrating `TestServices.KeyboardHelper` will break latent tests** that pass today only because they bypass `OnKey` (no `CharacterReceived`, no context-menu processor, no flyout dismissal, no focus re-resolution). | Medium-high | Separate follow-up PR (D10). |
| R4 | **`CoreVirtualKeyStates.Locked` is already corrupted on Skia** — `TrackKeyState` fires once per ancestor during bubbling (`RaiseOnParent` → `parent.RaiseEvent`), toggling `Locked` every time. `Down` is stable. | Medium | Never build CapsLock/NumLock on `Locked`. Consider a separate fix. |
| R5 | **Injected + real input do not mix.** Hosts compute modifiers from OS/native state that injection never updates; FrameBuffer is worst (own `_pressedKeys` tracker). | Medium | Document as unsupported. |
| R6 | **WASM-Skia a11y sentinel**: injected Tab does not set `LastTabWasForward`, consumed by `WebAssemblyAccessibility.cs:1250`. | Low-medium | Note in the WASM test guard; consider setting it from the injector later. |
| R7 | **WinAppSDK head build breakage** — the shared `Keyboard` driver must avoid Uno-only extensions and must chunk to ≤16 per call. | Medium | Dual `#if HAS_UNO` shape, like `Mouse.cs`. |
| R8 | **`Uno0001` analyzer + `TreatWarningsAsErrors`**: leaving `__SKIA__`/`__WASM__` in the `[NotImplemented]` list breaks the RuntimeTests build the moment a test calls the API. | Medium | Get the attribute list exactly right; build `Uno.UI.RuntimeTests` as part of validation. |
| R9 | **Sync-generator drift fails the PR.** Hand-edits must match byte-for-byte (member order, comment format, indentation) — two historical commits got it wrong. | Medium | Run `build\run-api-sync-tool.cmd` locally if workloads permit; otherwise use CI's `sync-generator.patch` / `/apply-sync-gen`. |
| R10 | **`ComboBox` pre-existing double-fire** (`OnKeyDownPrivate` queues its own `OnCharacterReceived` when `UnicodeKey != null`) will be newly exercised by injection tests. | Low | Avoid `ComboBox` in the initial suite; file separately if it reproduces. |
| R11 | **`IsXamlIsland` bail-out**: existing injector tests skip entirely in XamlIsland mode. New tests inherit that blind spot. | Low | Same guard; revisit with D1. |

### Unknowns (things nobody could settle)

| # | Unknown | Impact | How to settle |
|---|---|---|---|
| U1 | **Why is the Unicode `VirtualKey` 255 and not `VK_PACKET` (231)?** OBSERVED as 255 for both `KEYEVENTF_UNICODE` and a `VirtualKey=0` no-option injection. | Low — Uno just picks 255. | Raw `WndProc`/`PreTranslateMessage` hook logging `wParam`. |
| U2 | **`IsMenuKeyDown` / the Alt (`WM_SYSKEYDOWN`) path is completely untested.** The repro never injected `VK_MENU`. Alt+key accelerators and access keys go down a different Win32 message path. | Medium — §10's `IsMenuKeyDown` recommendation is an assumption. | Re-run the harness with an Alt step. Cheap; the harness is re-runnable via `BuildAndRun.ps1 -Detach`. |
| U3 | **Does `inputInjectionBrokered` work for an UNPACKAGED WinUI 3 app?** The repro was packaged. Restricted capabilities are declared in an appx manifest, which unpackaged apps lack — so it almost certainly returns null, but this is untested. | Low for Uno. | Build the same repro with `WindowsPackageType=None`. |
| U4 | **The cross-process test is only half-conclusive.** It proved the injecting app receives nothing when not foreground, but Notepad's title showed no modified marker, so it was not positively confirmed the key landed there. UIPI may have blocked it silently. | Low — the Uno limitation stands either way. | Repro with a target that echoes input. |
| U5 | **Does `InjectShortcut(Start)` genuinely open Start?** The foreground moved to an HWND titled "Search" — the same HWND the preceding `Search` shortcut opened. The observed keystroke (LeftWindows alone) is solid; the resulting UI is not. | None for Uno (no shell anyway). | Run `Start` in isolation. |
| U6 | **Does the extended-key flag change what an arrow key *does*?** The f1/f2 test seeded `"abcdef"` with the caret already at the end, so neither case moved the caret. `IsExtendedKey` was answered; behaviour was not. | Low. | Re-run with the caret mid-text. |
| U7 | **Exact member order for the regenerated `InjectedInputKeyboardInfo` skip comments.** Methods keep WinRT metadata order (verified for `InputInjector`); *properties* were reordered by the real generator run in the pen precedent. Predicted `KeyOptions, ScanCode, VirtualKey`. | Low — CI hands back the patch. | Run the generator, or push and read `sync-generator.patch`. |
| U8 | **Can the sync generator actually run on this machine?** Needs Windows (yes), SDK 10.0.105, and `dotnet workload install android ios maccatalyst tvos` — the workloads were **not verified** here. | Medium for workflow. | `dotnet workload list`. |
| U9 | **Whether the `[global::System.FlagsAttribute]` line is dropped correctly** if the enums *are* hand-written (optional per §11.2). The pen precedent suggests yes; the code path was not traced end-to-end. | Very low — the enums need no work. | Only relevant if D-optional cleanup is done. |
