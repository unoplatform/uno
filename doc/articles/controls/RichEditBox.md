# RichEditBox

> [!TIP]
> This article covers Uno-specific information for `RichEditBox`. For the complete API and usage guidance, see [RichEditBox class](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.richeditbox).

`RichEditBox` provides rich-text editing through the WinUI Text Object Model (TOM), including character and paragraph formatting, selection, undo and redo, clipboard operations, RTF streams, and automation.

## Supported features

| Feature | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|---------|---------|---------|-----|------------|-------|--------------|--------------|
| Rich-text editing and selection | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| Character and paragraph formatting | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| TOM ranges, navigation, and undo/redo | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| Plain-text and RTF streams | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| Clipboard and IME input | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| Text and Text2 automation patterns | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |
| MathML and Unicode math layout | ✔ | ✔ (Skia) | ✔ (Skia) | ✔ (Skia) | ✔ | ✔ | ✔ |

The Uno implementation targets the Skia renderer. Native Android views, UIKit controls, and the native WebAssembly DOM renderer are not included in this implementation.

Uno implements the public WinUI behavior with a managed document and layout engine because the Windows RichEdit and Text Services internals used by WinUI are not public cross-platform APIs. COM identity, private `ITextDocument2`/`ITextRange2` interfaces, OLE hosting, and reference-counting behavior are therefore not exposed.

For safe cross-platform transport, active or externally linked RTF destinations are removed during export. Unsupported embedded objects are represented by bounded text or image fallbacks. RTF table descriptors are retained through ordinary cell-content edits, but Uno does not host the native Windows RichEdit table or OLE UI.

Math layout uses an installed OpenType MATH font when available and otherwise falls back to bounded managed layout.

Very large documents (roughly 2 MiB of text or more) switch to bounded per-paragraph layout so editing stays incremental. On WebAssembly the browser's 32-bit heap limits how much shaped text can be held at once, so documents of that size may exhaust memory before they finish laying out; keep WebAssembly documents well below that threshold.

## Using RichEditBox with Uno

No Uno-specific setup is required when using a Skia target.

```xml
<RichEditBox
    AcceptsReturn="True"
    Header="Notes"
    IsSpellCheckEnabled="True"
    TextWrapping="Wrap" />
```

Use the `Document` property for text and formatting operations:

```csharp
editor.Document.SetText(TextSetOptions.None, "Uno Platform");
editor.Document.GetRange(0, 3).CharacterFormat.Bold = FormatEffect.On;
```

## See RichEditBox in action

The [SamplesApp RichEditBox samples](https://github.com/unoplatform/uno/tree/master/src/SamplesApp/SamplesApp.Samples/Windows_UI_Xaml_Controls/RichEditBox) cover basic editing, formatting, events, keyboard accelerators, automation, and advanced rich content.
