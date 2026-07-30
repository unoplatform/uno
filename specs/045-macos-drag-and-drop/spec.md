# Spec 045: macOS Drag and Drop (Skia)

> **Status**: Implemented — compiles (native + managed, net10.0); manual drag matrix run on macOS and passed (not runtime-testable, see §7)
> **Author**: Andres Pineda
> **Date**  : 2026-06-18
> **Branch**: `feat/macos.drag.and.drop`

### Reading Convention

This spec distinguishes **current behavior** (what the code does today) from **target behavior** (what this feature delivers). **The code is the source of truth for current behavior** — where this spec describes the implementation it must match the source under `src/Uno.UI.Runtime.Skia.MacOS/`; discrepancies are spec bugs. Source file paths are given so each claim can be verified.

---

## Executive Summary

### What This Adds

Inter-app **drag and drop** for the macOS Skia runtime (`Uno.UI.Runtime.Skia.MacOS`), in both directions:

- **Inbound** (external app → Uno): the user drags content from Finder or another app and drops it onto an Uno window. The payload is surfaced to managed code as a `DataPackage` and routed through the existing `DragDropManager` / `CoreDragDropManager` pipeline, so XAML `DragEnter` / `DragOver` / `Drop` handlers and `AllowDrop` work unchanged.
- **Outbound** (Uno → external): a XAML-initiated drag (via `CanDrag` / `DragStarting`) is projected onto an AppKit `NSDraggingSession` so the payload can be dropped into Finder or other apps.

There is **no new public API**. The feature implements the existing WinUI-aligned contract (`IDragDropExtension.StartNativeDrag` for outbound; `DragDropManager.ProcessMoved/ProcessReleased/ProcessAborted` for inbound) exactly as the Win32 and WASM Skia heads do. This keeps Constitution Principle I (WinUI API Fidelity) and Principle VI (Backward Compatibility) intact — the change is purely an additive platform implementation behind `ApiExtensibility`.

### Architecture at a Glance

```
Managed (C#)                         P/Invoke (NativeUno)        Native (Objective-C / AppKit)
─────────────────────────────       ─────────────────────       ───────────────────────────────────
MacOSDragDropExtension          ◀──  callbacks  ◀──────────────  UNODragDrop.m  (bridge)
  (IDragDropExtension)                                            UNOSoftView / UNOMetalFlippedView
  ├─ inbound  : OnDragEntered/Updated/Exited/Performed  ◀───────    <NSDraggingDestination>
  ├─ outbound : StartNativeDrag ──▶ uno_drag_start ──────────────▶  <NSDraggingSource> beginDraggingSession…
  └─ completion: OnDragSessionEnded ◀───────────────────────────    draggingSession:endedAtPoint:operation:
        │
        ▼
DragDropManager / CoreDragDropManager  (shared, platform-agnostic)
```

The rendering views (`UNOSoftView` for the software path, `UNOMetalFlippedView` for the Metal path) adopt both `NSDraggingDestination` and `NSDraggingSource`. All drag logic lives in `UNODragDrop.m`; the views are thin forwarders.

### Scope

| Area | File(s) |
|------|---------|
| Managed extension (inbound + outbound + completion) | `ApplicationModel/DataTransfer/DragDrop/MacOSDragDropExtension.cs` |
| P/Invoke surface + interop structs | `Native/NativeUno.cs` |
| Native bridge (pasteboard ↔ structs, session) | `UnoNativeMac/UnoNativeMac/UNODragDrop.{h,m}` |
| `NSDraggingDestination` / `NSDraggingSource` adoption | `UnoNativeMac/UnoNativeMac/UNOSoftView.{h,m}`, `UNOWindow.{h,m}` |
| Registration at startup | `Hosting/MacSkiaHost.cs` |
| Window-handle accessor for the extension | `UI/Xaml/Window/MacOSWindowHost.cs` |
| Xcode project membership | `UnoNativeMac/UnoNativeMac.xcodeproj/project.pbxproj` |
| Documentation (support matrix + limitations) | `doc/articles/features/pointers-keyboard-and-other-user-inputs.md` |

### Capabilities & Limitations

| Standard format | Outbound (Uno → external) | Inbound (external → Uno) |
|-----------------|:--:|:--:|
| Text | ✅ | ✅ |
| Html | ✅ | ✅ |
| Rtf | ✅ | ✅ |
| Link (WebLink / ApplicationLink / Uri) | ✅ (as `NSPasteboardTypeURL`) | ✅ (URL parsed back to Web/App/Uri) |
| StorageItems (files/folders) | ✅ (one dragging item per file) | ✅ (lazy `DataProvider` → `StorageFile`/`StorageFolder`) |
| Bitmap (Image) | ✅ (re-encoded to PNG + used as drag image) | ❌ **not extracted** |

Documented limitations (mirrored in the public docs):

1. Outbound drags do **not** render a custom drag image from `DragUI.Content`; macOS uses the dragged payload (file icon / bitmap) or a transparent placeholder.
2. **Image content is not extracted from inbound drops** into the `DataPackage`.
3. Only **`Shift`** and **`Control`** modifiers are reported during an inbound drop; `Alt` / `Windows` are ignored.

### Key Constraints

- AppKit's `beginDraggingSessionWithItems:event:source:` **must** run on the main thread from within the originating mouse-event context (`[NSApp currentEvent]` must still be that mouse event). This drives the inline-vs-dispatch decision in `StartNativeDrag` and the mouse-event guard in `uno_drag_start`.
- AppKit supports **one drag session per view at a time** → the extension tracks a single pending completion per window.
- Native-allocated payload buffers for an inbound drop are valid **only for the duration of the synchronous managed callback**; managed code must copy everything it needs before returning.

---

## 1. Background & Goals

### 1.1 The WinUI-aligned contract being implemented

Drag and drop in Uno is split into a platform-agnostic core and a per-platform extension:

- **Outbound** is initiated by the core `DragOperation` (`src/Uno.UI/UI/Xaml/DragDrop/DragOperation.cs`), which calls `IDragDropExtension.StartNativeDrag(CoreDragInfo, Action<DataPackageOperation>)` (`src/Uno.UWP/.../DragDrop/Core/IDragDropExtension.cs`). The platform starts a native drag and invokes the completion `Action` with the operation the destination performed.
- **Inbound** is delivered by the platform calling `DragDropManager.ProcessMoved` / `ProcessReleased` / `ProcessAborted` (`src/Uno.UI/UI/Xaml/DragDrop/DragDropManager.cs`) with an `IDragEventSource`, plus `CoreDragDropManager.DragStarted` to open the session.

The macOS extension implements exactly this contract, registered through `ApiExtensibility` — identical in shape to `Win32DragDropExtension` and `BrowserDragDropExtension`. No XAML-facing API is added or changed.

### 1.2 Goals / Non-Goals

**Goals**

- G1 — Receive drops from external macOS apps (Finder files, text, links, html, rtf) into Uno XAML drop targets.
- G2 — Start native drags from Uno content to external apps (text, links, html, rtf, files, bitmap).
- G3 — Reuse the shared `DragDropManager` pipeline so intra-app behavior and XAML event semantics are unchanged.
- G4 — Fail safe: malformed payloads, missing mouse-event context, or window teardown must never crash the process or leak the completion callback.

**Non-Goals**

- Custom drag-image rendering from `DragUI.Content` (Limitation 1).
- Extracting raw image bitmaps from inbound drops (Limitation 2) — text/html/rtf/uri/file URLs cover the common cases.
- Native (non-Skia) macOS head — out of scope per the Skia-first development scope.

---

## 2. Architecture

### 2.1 Layers

1. **Managed extension** — `MacOSDragDropExtension` implements `IDragDropExtension`. One instance per window, keyed by native window handle in a static `ConcurrentDictionary<nint, MacOSDragDropExtension>`. The four inbound callbacks and the session-ended callback are `[UnmanagedCallersOnly]` statics registered **once** globally (guarded by `Interlocked.Exchange(ref _callbacksRegistered, 1)`); they dispatch to the per-window instance via the dictionary.
2. **P/Invoke** — `NativeUno` declares the entry points and the blittable interop structs `NativeDragDropData` (inbound) and `NativeDragSourceData` (outbound), kept in sync with `struct DragDropData` / `struct DragSourceData` in `UNODragDrop.h`.
3. **Native bridge** — `UNODragDrop.m` converts between the `NSPasteboard` / `NSDraggingInfo` world and the flat C structs, owns the AppKit `NSDraggingSession`, and tracks the per-view source operation mask.

### 2.2 Inbound flow (external → Uno)

1. On window creation, `uno_window_create` calls `uno_window_register_for_drag_drop`, which calls `registerForDraggedTypes:` on the rendering view for: `FileURL, String, HTML, RTF, URL, PNG, TIFF`.
2. AppKit invokes the view's `NSDraggingDestination` methods, which forward to the bridge:
   - `draggingEntered:` → `uno_drag_drop_handle_entered` → fills `DragDropData` from the pasteboard → managed `OnDragEntered`.
   - `draggingUpdated:` → `uno_drag_drop_handle_updated` (position + modifiers only; pasteboard re-read is skipped for speed) → `OnDragUpdated`.
   - `draggingExited:` → `uno_drag_drop_handle_exited` → `OnDragExited`.
   - `performDragOperation:` → `uno_drag_drop_handle_performed` (full pasteboard read) → `OnDragPerformed`.
3. Managed side:
   - `HandleDragEntered` builds a `DataPackage` (`BuildDataPackage`), wraps it in a `CoreDragInfo`, calls `_coreDragDropManager.DragStarted(info)`, then `_manager.ProcessMoved(src)`.
   - `HandleDragUpdated` → `ProcessMoved`; `HandleDragExited` → `ProcessAborted(_fakePointerId)`; `HandleDragPerformed` → `ProcessReleased`.
   - The returned `DataPackageOperation` is mapped back to an `NSDragOperation` (`ns_operation_from_mask`) and returned to AppKit, which drives the drop cursor and the accept/reject decision (`performDragOperation:` returns `accepted != None`).

`NSDragOperationGeneric` (advertised by Finder without specific copy/move/link bits) is mapped to **Copy** so the managed side accepts it.

### 2.3 Outbound flow (Uno → external)

1. Core `DragOperation` calls `StartNativeDrag(info, onCompleted)`.
2. **Threading decision** (`MacOSDragDropExtension.StartNativeDrag`): if `CoreDispatcher.Main.HasThreadAccess`, run inline (`StartNativeDragSafeAsync`) to preserve `[NSApp currentEvent]`; otherwise marshal via `CoreDispatcher.Main.RunAsync(High, …)`. Deferring inline work to a later run-loop turn would lose the mouse event and AppKit would reject the drag.
3. `StartNativeDragCoreAsync` reads the `DataPackageView` asynchronously (text, html, rtf, combined uri, bitmap bytes, storage-item paths), sets `_pendingDragCompletion = onCompleted`, then calls into native via `StartNativeDragUnsafe`, which marshals everything into a `NativeDragSourceData` and calls `uno_drag_start`. If `uno_drag_start` returns `false`, the pending completion is cleared and `onCompleted(None)` is invoked immediately.
4. Native `uno_drag_start`:
   - Rejects the call (returns `NO`) unless `[NSApp currentEvent]` is a mouse event — this surfaces the "no mouse context" case to managed code rather than failing silently.
   - Builds an `NSPasteboardItem` (string / html / rtf / url) and, if a bitmap is present, **re-encodes it to PNG** via `NSBitmapImageRep` before advertising `NSPasteboardTypePNG` (the managed `DataPackage` may hand over non-PNG bytes).
   - Builds `NSDraggingItem`s: one primary item for non-file payloads, plus **one item per file URL** so Finder sees files individually.
   - Picks a drag image: the bitmap if available, else a 32×32 transparent placeholder (AppKit requires a non-nil image).
   - Records the allowed operation mask for the view in `active_source_masks`, then calls `beginDraggingSessionWithItems:event:source:`.
5. AppKit queries `draggingSession:sourceOperationMaskForDraggingContext:` → `uno_drag_source_operation_mask` (returns the stored mask, default Copy).
6. On completion, `draggingSession:endedAtPoint:operation:` → `uno_drag_source_session_ended` → `drag_session_ended(window, op)` → managed `OnDragSessionEnded`, which invokes and clears `_pendingDragCompletion` with the resulting `DataPackageOperation`.

### 2.4 Interop data model

Two flat, blittable structs cross the boundary. They are explicitly documented as "keep in sync with UNODragDrop.h".

`NativeDragDropData` (inbound, native → managed): `X, Y`, `Modifiers`, `AllowedOperations` (bit flags matching `DataPackageOperation`), `byte*` UTF-8 strings for `TextContent/HtmlContent/RtfContent/Uri`, `byte** FileUrls` + `FileCount`, and `BitmapPath` (currently always null — see Limitation 2).

`NativeDragSourceData` (outbound, managed → native): `AllowedOperations`, the same UTF-8 string fields, `FileUrls`/`FileCount`, and a raw `BitmapData` + `BitmapSize` blob.

Operation bit flags are identical on both sides: `None=0, Copy=1, Move=2, Link=4` (`UnoDragDropOperation` in `UNODragDrop.h`).

### 2.5 Lifecycle, threading & memory ownership

- **Instance lifecycle** — constructed lazily by `ApiExtensibility` per `DragDropManager`; registers itself in `_extensions[windowHandle]` and subscribes to `_host.Closed`. On close it removes itself and completes any pending outbound drag with `None` (so `DragOperation` never hangs).
- **Inbound memory ownership** — native `fill_drag_drop_data` `strdup`/`calloc`s the payload; the bridge logs and then `free_drag_drop_data`s it **after** the synchronous managed callback returns. Managed code therefore copies eagerly: strings via `Marshal.PtrToStringUTF8`, file paths into a managed `string[]` in `ExtractFilePaths`. The `StorageItems` `DataProvider` is deferred, but it closes over the **managed** `string[]`, so it remains valid after the native buffers are freed.
- **Outbound memory ownership** — `StartNativeDragUnsafe` allocates native buffers with `NativeMemory.Alloc`, calls the synchronous `uno_drag_start` (which copies bytes into `NSString`/`NSData`/`NSPasteboardItem`), then frees everything in a `finally`. Each per-file URL allocation is tracked (`allocatedFileUrls`) so a mid-loop failure frees only what was allocated.
- **Robustness** — every `[UnmanagedCallersOnly]` callback is wrapped in try/catch routing to `Application.Current.RaiseRecoverableUnhandledException`. `strdup` is guarded against a null `UTF8String`. Inbound URI strings are parsed with `Uri.TryCreate` so malformed external URLs are skipped rather than throwing and aborting the drag.

---

## 3. Implementation

### 3.1 `MacOSDragDropExtension.cs`

The managed core. Key members:

- `Register()` — registers the `ApiExtensibility` factory and (once) the native callbacks.
- `StartNativeDrag` / `StartNativeDragSafeAsync` / `StartNativeDragCoreAsync` / `StartNativeDragUnsafe` — the outbound pipeline (see §2.3). `Utf8Alloc` is a small helper that allocates a NUL-terminated UTF-8 buffer with `NativeMemory`.
- `OnDragEntered/Updated/Exited/Performed` (`[UnmanagedCallersOnly]`) → `HandleDrag*` instance methods → `DragDropManager`.
- `OnDragSessionEnded` (`[UnmanagedCallersOnly]`) → invokes the pending completion.
- `BuildDataPackage` / `ExtractFilePaths` / `ResolveStorageItems` — inbound payload → `DataPackage`.
- `DragEventSource` (`readonly struct : IDragEventSource`) — supplies position and modifiers to the manager; maps `Shift`/`Control` and always sets `LeftButton` (macOS does not report buttons during a drag).

### 3.2 `NativeUno.cs`

Adds the interop structs (§2.4) and the entry points: `uno_drag_drop_set_callbacks`, `uno_drag_drop_set_session_ended_callback`, `uno_drag_start`, `uno_window_register_for_drag_drop`.

### 3.3 `UNODragDrop.{h,m}`

The native bridge: pasteboard read (`fill_drag_drop_data`) and free (`free_drag_drop_data`); operation-mask conversions (`mask_from_ns_operation` / `ns_operation_from_mask`); the `NSDraggingDestination` handlers; the outbound `uno_drag_start`; and the `NSDraggingSource` helpers backed by `active_source_masks` (an `NSMapTable` with weak keys / strong values, so a destroyed view does not leak its mask entry).

### 3.4 View / window adoption

`UNOSoftView` and `UNOMetalFlippedView` declare conformance to `NSDraggingDestination, NSDraggingSource` and implement the six forwarding methods (`draggingEntered/Updated/Exited`, `performDragOperation`, `draggingSession:sourceOperationMaskForDraggingContext:`, `draggingSession:endedAtPoint:operation:`), each a one-line call into `UNODragDrop.m`.

### 3.5 Host wiring

`MacSkiaHost.Initialize` calls `MacOSDragDropExtension.Register()` alongside the other extension registrations. `MacOSWindowHost` exposes `internal nint NativeWindowHandle` so the extension can key itself and target `uno_drag_start`.

---

## 4. Supported Data Formats & Modifiers

See the Capabilities & Limitations table in the Executive Summary for the format matrix.

### Image handling (asymmetric)

Image support differs by direction — this is why "Image" appears in the **outbound** column of the support matrix but **not** the inbound one:

- **Outbound (uno → external): supported.** When the drag's `DataPackageView` contains `StandardDataFormats.Bitmap`, the bytes are read (`StartNativeDragCoreAsync`), re-encoded to PNG, and written to the pasteboard as `NSPasteboardTypePNG` — and also used as the drag image (`uno_drag_start`). Re-encoding ensures the advertised PNG type matches the payload even when the `DataPackage` carried BMP/JPEG/TIFF bytes, so common macOS apps can read it.
- **Inbound (external → uno): raw bitmap content is not surfaced.** `fill_drag_drop_data` intentionally does not copy PNG/TIFF blobs across the boundary (`bitmapPath` stays `null`), and `BuildDataPackage` never sets `StandardDataFormats.Bitmap` (Limitation 2).
- **Nuance — image *files* still work inbound.** An image dragged from Finder arrives as a file URL and is surfaced as `StandardDataFormats.StorageItems` (a `StorageFile`) — which is why "File" *is* in the inbound list. Only raw bitmap *content* not backed by a file is unavailable inbound; this is not a regression.

### Modifiers

Modifier mapping (inbound, `DragEventSource.GetState`):

| macOS modifier | Reported as |
|----------------|-------------|
| Shift | `DragDropModifiers.Shift` |
| Control | `DragDropModifiers.Control` |
| (drag implies) | `DragDropModifiers.LeftButton` |
| Option (Alt) / Command (Windows) | **ignored** (Limitation 3) |

---

## 5. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Run `StartNativeDrag` inline when on the main thread | `beginDraggingSession` requires the live mouse event; deferring via `RunAsync` resumes on a later run-loop turn where `[NSApp currentEvent]` is no longer the mouse event, and AppKit rejects the drag. |
| Native `uno_drag_start` validates `[NSApp currentEvent]` is a mouse event | Lets the managed side resolve the completion with `None` instead of leaving `DragOperation` hung when there is no valid mouse context. |
| Re-encode outbound bitmap to PNG | The managed `DataPackage` may carry BMP/JPEG/TIFF bytes; advertising `NSPasteboardTypePNG` must match the actual bytes, so PNG is only advertised when conversion succeeds. |
| One `NSDraggingItem` per file URL | Finder and other targets expect individual file items, not a single combined item. |
| Map `NSDragOperationGeneric` → Copy (inbound) and default source mask → Copy | Finder advertises Generic without specific bits; defaulting to Copy makes drags from Finder acceptable and matches macOS conventions. |
| Single `_pendingDragCompletion` per window | AppKit allows one session per view; a new drag means the previous session already ended, so replacing the slot is safe. |
| Complete pending drag with `None` on window close | Guarantees `DragOperation` always resolves even if the window is torn down mid-drag — satisfies Goal G4. |
| `Uri.TryCreate` for inbound URIs | External payloads can carry malformed URLs; parsing defensively skips bad data instead of throwing and aborting the whole drop. |
| Bitmap **not** extracted on inbound | Avoids copying large blobs across the boundary for the common case; text/html/rtf/uri/file URLs cover typical drops (documented Limitation 2). |
| `active_source_masks` uses weak keys | A destroyed rendering view must not keep a mask entry alive. |

---

## 6. Known Limitations & Future Work

1. **No custom drag image** from `DragUI.Content` (uses payload/placeholder). Future work: render the `DragUI` visual to a bitmap and pass it as the dragging item's contents.
2. **Inbound image not extracted** into the `DataPackage` — applies to raw bitmap *content* only; image *files* dropped from Finder arrive as `StorageItems` and work (see §4 "Image handling"). Future work: populate `BitmapPath` (or a blob) in `fill_drag_drop_data` and surface `StandardDataFormats.Bitmap` on the managed side.
3. **Alt / Windows modifiers not reported** on inbound drops. Future work: extend `DragEventSource.GetState` and the native modifier capture.
4. **Win32 parity note**: the Win32 head implements inbound drops but its `StartNativeDrag` currently throws `NotImplementedException`; macOS now implements outbound as well. The cross-platform support matrix in the docs should be kept consistent as these heads converge.

---

## 7. Testing & Validation

### 7.1 Why this is not runtime-testable

This feature is **not** covered by automated tests, by nature rather than by omission:

- **It requires a live AppKit windowing session and a second application.** A real inter-app drag is driven by the macOS drag manager from a physical mouse gesture (`[NSApp currentEvent]` must be the live mouse event), with another app (Finder, a browser, an editor) acting as the source or destination. There is no API to synthesize this end-to-end inside a test process.
- **The code paths exist only on the macOS Skia runtime.** `MacOSDragDropExtension` and `libUnoNativeMac.dylib` load only on macOS Skia; the headless `Uno.UI.RuntimeTests` runner (Windows/Linux CI, or a windowless host) never instantiates them, so even the managed handlers (`BuildDataPackage`, the operation-mask mapping, the completion lifecycle) cannot be reached there. The internals are `private`/`unsafe` over raw native pointers and are not designed as a unit-testable surface.

This matches the precedent in Spec `002-x11-error-exit`, whose validation is compile + manual A/B because the path depends on a real X11 environment. The shared, platform-agnostic `DragDropManager` pipeline that this feature feeds into **is** covered by the existing intra-app drag-and-drop tests; only the macOS native bridge is environment-bound.

### 7.2 Validation method: manual, via the existing SamplesApp samples

Validation is manual, using the existing drag-and-drop samples under `src/SamplesApp/SamplesApp.Samples/Windows_UI_Xaml/DragAndDrop/` (e.g. `DragDrop_Basics`, `DragDrop_Files`, `DragDrop_TestPage`) running on `SamplesApp.Skia` on macOS. No new sample is required. Matrix:

| # | Scenario | Expected |
|---|----------|----------|
| 1 | Drag a text selection from TextEdit onto an Uno drop target | `DragEnter`/`Drop` fire; `DataPackage` contains Text |
| 2 | Drag a file/folder from Finder onto an Uno drop target | `Drop` yields `StorageItems` resolving to `StorageFile`/`StorageFolder` |
| 3 | Drag a URL from Safari's address bar | Link surfaces as WebLink/Uri |
| 4 | Drag html/rtf from a rich editor | Html/Rtf present in the `DataPackage` |
| 5 | Hold Shift / Control while dropping | Modifier reflected in `DragEventArgs` |
| 6 | `DragStarting` in Uno → drop text/link onto another app | Destination receives the payload; completion reports the operation |
| 7 | Uno → Finder drag of a `StorageFile` | File appears in Finder |
| 8 | Uno → external drag of a bitmap | Image pasted/dropped where supported |
| 9 | Start a drag, then close the window mid-drag | No crash; `DragOperation` resolves to `None` |
| 10 | Drop a payload with a malformed URL | Drop still completes; bad URI skipped |

### 7.3 Validation evidence (as of 2026-06-18)

- **Code-review assessment**: the managed/native contracts, struct layouts, memory ownership, and threading rationale were reviewed by inspection (this spec). Logic appears correct.
- **Compile validation — native**: ✅ `xcodebuild -project UnoNativeMac.xcodeproj -scheme UnoNativeMac -configuration Release` → **BUILD SUCCEEDED**; `libUnoNativeMac.dylib` produced. The new `UNODragDrop.m` and the modified `UNOSoftView.m` / `UNOWindow.m` compiled clean; the only warnings are pre-existing `-Wdeprecated-implementations` in `UNOAccessibility.m` (unrelated).
- **Compile validation — managed**: ✅ `dotnet build Uno.UI.Runtime.Skia.MacOS.csproj -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true` → **Build succeeded, 0 errors**; `Uno.UI.Runtime.Skia.MacOS.dll` produced. None of the build's warnings are attributable to the drag-and-drop, interop (`NativeUno.cs`), or host (`MacSkiaHost.cs` / `MacOSWindowHost.cs`) files.
- **Runtime validation**: ✅ the manual §7.1 matrix was run on macOS (SamplesApp `DragAndDrop` samples) and passed. No automated runtime test exists for this path by design (§7.1).

### 7.4 Constitution gate check

| Principle | Status |
|-----------|--------|
| I. WinUI API Fidelity | ✅ No new/changed public API; implements existing `IDragDropExtension` + `DragDropManager` contracts. |
| II. Cross-Platform Parity | ✅ Isolated in `Uno.UI.Runtime.Skia.MacOS`; native targets unaffected; gaps documented (§6, docs). |
| III. Test-First Quality Gates | ⛔ **Documented exception.** Inter-app native drag is not runtime-testable (§7.1); the shared pipeline it feeds retains its existing tests, and this native bridge is validated manually via the SamplesApp DragAndDrop samples (§7.2). |
| IV. Performance & Resource Discipline | ✅ No shared hot-path changes; per-drag allocations are bounded and freed deterministically. |
| V. Generated Code Boundaries | ✅ No `Generated/` edits. |
| VI. Backward Compatibility | ✅ Additive; no breaking change. |

---

## 8. References

- `src/Uno.UI.Runtime.Skia.MacOS/ApplicationModel/DataTransfer/DragDrop/MacOSDragDropExtension.cs` — managed extension
- `src/Uno.UI.Runtime.Skia.MacOS/Native/NativeUno.cs` — P/Invoke + interop structs
- `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNODragDrop.{h,m}` — native bridge
- `src/Uno.UI.Runtime.Skia.MacOS/UnoNativeMac/UnoNativeMac/UNOSoftView.{h,m}`, `UNOWindow.{h,m}` — view/window adoption
- `src/Uno.UI.Runtime.Skia.MacOS/Hosting/MacSkiaHost.cs` — registration
- `src/Uno.UI/UI/Xaml/DragDrop/DragDropManager.cs`, `DragOperation.cs` — shared pipeline
- `src/Uno.UWP/ApplicationModel/DataTransfer/DragDrop/Core/IDragDropExtension.cs` — outbound contract
- `src/Uno.UI.Runtime.Skia.Win32/ApplicationMode/DataTransfer/DragDrop/Win32DragDropExtension.cs` — sibling implementation (inbound)
- `doc/articles/features/pointers-keyboard-and-other-user-inputs.md` — public support matrix & limitations
- Spec `002-x11-error-exit` — precedent for environment-dependent runtime-validation framing
- Apple AppKit: `NSDraggingDestination`, `NSDraggingSource`, `NSDraggingSession`, `NSPasteboard`
