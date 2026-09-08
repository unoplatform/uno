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

## WinUI source and managed editor boundary

The control policy is based on Microsoft UI XAML commit [`3c9c168844f06c6ac000a97977f0bb3f4c90fd75`](https://github.com/microsoft/microsoft-ui-xaml/tree/3c9c168844f06c6ac000a97977f0bb3f4c90fd75). The public control declaration, dependency properties, source implementation, header fields, and Uno integration are separated into declaration, `.Properties.cs`, `.mux.cs`, `.h.mux.cs`, and `.uno.cs` partials.

| Microsoft source | Responsibility in Uno |
|------------------|-----------------------|
| `dxaml/xcp/tools/XCPTypesAutoGen/Modules/Controls/RichEditBox.cs` | Public control and event-argument contracts |
| `dxaml/xcp/dxaml/lib/RichEditBox_Partial.cpp` and `.h` | Routed-input forwarding, document access, lazy header/placeholder handling, reusable changing-event arguments, automatic-height animation, and template lifecycle |
| `dxaml/xcp/core/native/text/Controls/RichEditBox.cpp` and `.h` | Property dispatch and validation, formatting-accelerator masks, hyperlink policy, and content-change integration |
| `dxaml/xcp/core/native/text/Controls/TextBoxBase.cpp` and `.h` | Shared selection-cancellation policy, focus/candidate-window lifecycle, pointer policy, default spell checking, and caret-scroll coordination |
| `dxaml/xcp/dxaml/lib/TextBoxPlaceholderTextHelper.cpp` | Placeholder visibility and removal from automation descriptions when document content replaces the hint |
| `RichEditBoxAutomationPeer_Partial.cpp`, `TextBoxBaseAutomationPeer.cpp`, and generated changing-event-argument sources | Automation control identity/descriptions, accessibility-driven software-keyboard focus, and thread-affine event-argument state |
| `dxaml/xcp/plat/win/desktop/WindowLessSiteHost.cpp` | The rich-edit capability's explicit exclusion of the lossy Value automation pattern |
| `controls/dev/CommonStyles/RichEditBox_themeresources.xaml` | The FluentTheme template and resources |

Native host and automation-wrapper source is available too; it must not be confused with the editor engine itself. In particular, `CTextBoxBaseAutomationPeer` obtains RichEditBox's **windowless provider** through `GetRichEditRawElementProviderSimple` and `GetUnwrappedPattern`. The ordinary XAML `TextAdapter`/`TextRangeAdapter` sources are not the implementation of that provider. Uno uses explicitly managed automation adapters for its document, selection, geometry, and rich text-object children.

`WinUIEdit.dll` supplies the Windows RichEdit/TOM, RTF, math, and windowless-provider implementations; those implementations are not present in the pinned Microsoft UI XAML repository. Uno's story storage, formatting runs, range tracking, undo/redo, RTF codec, math layout, and clipboard/IME integrations are therefore **engine adapters**, not a claimed byte-for-byte port of those components. Native OLE/TSF/message and COM ownership boundaries remain identified with `TODO Uno:` in source-backed partials.

Two integration differences are important when maintaining the port:

- Uno's template binding updates a placeholder's text after the owner's property callback. The adapter observes the presenter text as well, so the source visibility policy runs against the updated value.
- Managed text replacement rebases the same selection object, and clipboard reads can be asynchronous. The selection adapter distinguishes explicit selection changes from content-induced rebasing when applying the source cancellation policy.

The source-gated experimental `HeaderPlacement` feature is not included. Platform-specific candidate-window tracking and linguistic alternatives depend on the installed input-method service. Pixel-identical text shaping and private Windows editor behavior are not compatibility guarantees.

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
