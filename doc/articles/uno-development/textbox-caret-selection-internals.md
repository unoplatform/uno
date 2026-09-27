---
uid: Uno.Contributing.TextBoxCaretSelection
---

# TextBox caret and selection internals

A map of the classes involved in caret and selection handling for the Skia `TextBox` (and `PasswordBox`), aimed at contributors. It lists who owns what and how the pieces hand off to each other, so you know where to start reading. It is not a full description of every behavior.

## The big picture

```text
TextBox / PasswordBox  (ITextBoxHost)
        |
        v
TextBoxCore  -- selection, caret mode, blink timer, input, gestures
        |
        +--> TextBoxView --> DisplayBlock (TextBlock) --> ParsedText.Draw   (caret + highlight painting)
        |          |
        |          +--> IOverlayTextBoxViewExtension   (invisible native text input, WASM + iOS only)
        |
        +--> TextSelectionGripperPresenter --> CaretWithStemAndThumb x2     (touch grippers)
        |
        +--> IImeTextBoxExtension                                           (IME composition, per platform)
```

The managed side is always the source of truth for the selection. Native views only exist to receive input (keyboard, IME, autofill, system gestures), and their selection is kept in sync with the managed one.

## Managed core (`src/Uno.UI`)

| Class | File(s) | Role |
|-------|---------|------|
| `ITextBoxHost` | `Controls/TextBoxCore/ITextBoxHost.cs` | What `TextBox` and `PasswordBox` expose to the shared core (owner control, brushes, flyouts, properties). Its remarks explain why this is composition rather than a base class. |
| `TextBoxCore` | `Controls/TextBoxCore/TextBoxCore*.cs` | Shared text-input implementation. Owns the selection (`_selection`, `Select`, `SelectInternal`, `SelectPartial`), the caret state (`CaretMode`), the blink timer, keyboard and pointer handling, scrolling the caret into view (`UpdateScrolling`) and pushing the state to the renderer (`UpdateDisplaySelection`). |
| `TextBoxView` | `Controls/TextBox/TextBoxView.cs` | Owns the `DisplayBlock` and the optional native overlay extension. Forwards selection and text to the native side, and cancels an in-flight caret drag on focus loss. |
| `TextBlock` (as `DisplayBlock`) | `Controls/TextBlock/TextBlock.cs` | Renders the text. `TextBoxCore` drives it through the internal `Selection`, `RenderSelection` and `RenderCaret` properties; `Draw` turns the selection into a highlighter and hands the caret to `ParsedText`. |
| `ParsedText` / `IParsedText` | `Documents/ParsedText.cs`, `Documents/IParsedText.cs` | Text layout. Paints the caret and highlights, and provides the hit-testing used everywhere: `GetRectForIndex` (index to rect) and `GetIndexAt` (point to index). |

### Selection model

- `TextBoxCore.Select(start, length)` is the entry point behind `TextBox.Select`. It clamps to the text, raises `SelectionChanging`/`SelectionChanged`, and short-circuits when nothing changed.
- `SelectInternal(start, signedLength)` accepts a negative length for a backward selection (the caret sits at the start). It records the direction and the caret's X offset (used for up/down arrow navigation), then calls `Select`.
- `SelectPartial` is the Skia part of `Select`: it updates `_selection`, pushes it to the native proxy via `TextBoxView.Select`, adjusts `CaretMode` and restarts the blink timer.
- `UpdateDisplaySelection` copies the current selection and caret to the `DisplayBlock` and invalidates it. Call it whenever something that affects painting changes.

### Caret modes

`TextBoxCore.CaretDisplayMode` drives both the caret and the touch grippers:

| Mode | Meaning |
|------|---------|
| `ThumblessCaretShowing` | Plain caret, visible. The blink timer toggles between this and the hidden mode. |
| `ThumblessCaretHidden` | Plain caret in its "off" blink phase, or the box is unfocused. |
| `CaretWithThumbsOnlyEndShowing` | Touch: collapsed caret with a single gripper below it. No blinking. |
| `CaretWithThumbsBothEndsShowing` | Touch: non-empty selection with a gripper on each end. No blinking. |

Which touch mode a tap produces depends on `TouchSelectionConvention` (Desktop, Android or iOS). It defaults to the running device, and runtime tests override it to exercise the mobile behavior on Skia Desktop.

## Touch grippers (`src/Uno.UI/UI/Xaml/Controls/Primitives`)

| Class | Role |
|-------|------|
| `TextSelectionGripperPresenter` | Owns the pair of grippers, positions them after every draw, and handles dragging, tapping and holding. Shared by `TextBox`, selectable `TextBlock` and `RichTextBlock`. |
| `ITextSelectionGripperHost` | Implemented by `TextBoxCore` and `TextBlock`. The presenter asks the host for the text surface, the clip bounds and the current selection, and reports gripper drags back to it. |
| `CaretWithStemAndThumb` | A single gripper visual (thumb, ring and stem), hosted in its own `Popup`. |

`RichTextBlock` additionally goes through the ported `TextSelectionManager` (`Controls/TextBlock/TextSelectionManager*.cs`), which `TextBox` does not use.

## Native input proxies

Where the platform needs a real native text input (for the virtual keyboard, autofill or system gestures), an invisible native view sits behind the Skia-rendered `TextBox`. `TextBoxView` creates it through `ApiExtensibility` as an `IOverlayTextBoxViewExtension`. Selection flows both ways: managed changes go out through `TextBoxView.Select`, and native changes come back into `TextBoxCore`.

| Platform | Classes |
|----------|---------|
| WebAssembly | `BrowserInvisibleTextBoxViewExtension` (`Uno.UI.Runtime.Skia.WebAssembly.Browser`) |
| iOS | `InvisibleTextBoxViewExtension`, `SinglelineInvisibleTextBoxView` (a `UITextField`), `MultilineInvisibleTextBoxView` (a `UITextView`), their delegates, and `NativeTextSelection` (`Uno.UI.Runtime.Skia.AppleUIKit`) |
| Android, desktop | No overlay proxy. Android talks to the soft keyboard through `TextInputConnection` (`Uno.UI.Runtime.Skia.Android`). |

IME composition is a separate channel: each runtime provides an `IImeTextBoxExtension` (`AndroidImeTextBoxExtension`, `AppleUIKitImeTextBoxExtension`, `MacOSImeTextBoxExtension`, `WasmImeTextBoxExtension`, `Win32ImeTextBoxExtension`, `X11ImeTextBoxExtension`). While `IsComposing` is true, the native marked text owns the selection, so the core avoids pushing its own selection to the proxy.

## Caret drag gesture (iOS space-bar trackpad)

Holding the space bar on the iOS keyboard turns it into a trackpad that moves the caret. The participants, from native to managed:

1. `SinglelineInvisibleTextBoxView` / `MultilineInvisibleTextBoxView` receive the UIKit floating-cursor callbacks (`begin`/`update`/`endFloatingCursor`). While a drag runs they report oversized bounds and a pinned caret rect, so UIKit does not clamp the gesture to the proxy's size.
2. `InvisibleTextBoxFloatingCursor` turns the callbacks into offsets measured from the gesture origin.
3. `InvisibleTextBoxViewExtension.ProcessCaretDragGesture` forwards the phases to `TextBoxCore`, and declines (cancelling any running drag) while IME composition is active.
4. `TextBoxCore.CaretDrag.cs` is the platform-agnostic part. `Begin` captures the caret anchor and switches to a non-blinking caret without grippers. `Update` hit-tests the anchor plus the offset and stores a preview index, which `UpdateDisplaySelection` paints instead of the real caret. `End` commits the preview through `SelectInternal` (a single `SelectionChanged` per drag), restores the previous caret mode and resyncs the native proxy. `Cancel` does the same without committing.
5. `TextBoxView.OnFocusStateChanged` and `TextBoxCore.OnUnloadedPartial` cancel an in-flight drag when the `TextBox` loses focus or is unloaded.

The managed part is covered on Skia Desktop by the caret-drag runtime tests in `Given_TextBox.skia.cs`, which call `ProcessCaretDragGesture` directly.

## Where to start

| Symptom | Look at |
|---------|---------|
| Caret in the wrong place, or selection highlight off | `ParsedText.GetRectForIndex` / `GetIndexAt`, then `TextBoxCore.UpdateDisplaySelection` |
| Caret not blinking, or stuck hidden | `CaretMode` transitions in `TextBoxCore.Input.cs` (`SelectPartial`, `TimerOnTick`, `OnFocusStateChangedPartial`) |
| Grippers misplaced or not showing | `TextSelectionGripperPresenter` and the `ITextSelectionGripperHost` implementation in `TextBoxCore.Input.cs` |
| Native and managed selection disagree (WASM, iOS) | `TextBoxView.Select` and the platform's `IOverlayTextBoxViewExtension` |
| Composition breaks the selection | The platform's `IImeTextBoxExtension` and `TextBoxCore.IME.cs` |
