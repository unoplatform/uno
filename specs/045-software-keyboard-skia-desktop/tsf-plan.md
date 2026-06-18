# Win32 TSF text input — implementation plan (issue #17363)

**Status:** In progress.
**Why:** The Windows touch keyboard (SIP) auto-invokes on focus **only via TSF** — the app activates `ITfThreadMgr`, creates an `ITfDocumentMgr` backed by an `ITextStoreACP` text store, and calls `SetFocus` on focus. IMM32 + `CreateCaret` (Uno's current path, also Flutter/Avalonia) gives a caret + composition placement but does **not** auto-show the SIP (confirmed: Flutter bug #36057). `InputPane.TryShow()` returns `true` but renders nothing without a TSF/edit context. Verified at runtime: trigger/gating/HWND-resolution all correct; `TryShow()`→True but `IFrameworkInputPane.Showing` never fires (no render). `ITipInvocation` force-shows it (proves the keyboard *can* render for our HWND) but is toggle-only/fragile. Decision: implement the proper TSF text store.

## Decisions

- **CCW:** classic `[ComImport, InterfaceType(InterfaceIsIUnknown)]`. Built-in COM CCW works in `Uno.UI.Runtime.Skia.Win32` — proven by the existing `OcclusionHandler : IFrameworkInputPaneHandler` managed object that native `AdviseWithHWND` accepted (cookie=1). All TSF interfaces are IUnknown-based (not WinRT) → no IInspectable vtable padding.
- **Integration:** a new TSF-backed `IImeTextBoxExtension` for Win32 (e.g. `Win32TsfTextInput`) that **replaces** the IMM32 `Win32ImeTextBoxExtension` on focus, driven by the existing `StartImeSession(TextBox)`/`EndImeSession()` seam. The text store mirrors the active `TextBox` (text + selection), and routes committed/composing text through the existing `IImeTextBoxExtension` composition events (`CompositionStarted/Updated/Completed/Ended`) that `TextBox.IME.skia.cs` already consumes.
- **Caret geometry:** reuse `Win32ImeCaretManager`'s caret→client-pixel logic; for TSF return the caret/range RECT in **screen** coords (`ClientToScreen`) from `GetTextExt`, and the field rect from `GetScreenExt`. `GetWnd` returns the host HWND.
- **Keyboard show/hide is OS-driven** by `ITfThreadMgr::SetFocus(docMgr)` on focus and `SetFocus(null)` on blur — no explicit `TryShow` needed in the auto path. The existing `InputPane.TryShow/TryHide` (Win32InputPaneExtension) stays as the explicit public-API path and will now *render* because a TSF context exists.

## COM surface (verified IIDs, vtable order after IUnknown)

- `CLSID_TF_ThreadMgr` `{529A9E6B-6587-4F23-AB9E-9C7D683E3C50}` (verify vs local SDK msctf.h before shipping).
- `ITfThreadMgr` `{AA80E801-2021-11D2-93E0-0060B067B86E}`: Activate(out uint), Deactivate, CreateDocumentMgr(out), EnumDocumentMgrs(out IntPtr), GetFocus(out), **SetFocus(docMgr)**, AssociateFocus(hwnd, new, out prev), IsThreadFocus(out bool), GetFunctionProvider(in Guid, out IntPtr), EnumFunctionProviders(out IntPtr), GetGlobalCompartment(out IntPtr).
- `ITfDocumentMgr` `{AA80E7F4-…}`: CreateContext(uint clientId, uint flags, [IUnknown] object store, out ITfContext, out uint editCookie), Push(ctx), Pop(uint), GetTop(out), GetBase(out), EnumContexts(out IntPtr).
- `ITfContext` `{AA80E7FD-…}`: declared opaque (no methods called for minimal SIP).
- `ITextStoreACP` `{28888FE3-C2A0-483A-A3EA-8CB1CE51FF3D}` — **app-implemented**, 26 methods (order matters): AdviseSink, UnadviseSink, RequestLock, GetStatus, QueryInsert, GetSelection, SetSelection, GetText, SetText, GetFormattedText, GetEmbedded, QueryInsertEmbedded, InsertEmbedded, InsertTextAtSelection, InsertEmbeddedAtSelection, RequestSupportedAttrs, RequestAttrsAtPosition, RequestAttrsTransitioningAtPosition, FindNextAttrTransition, RetrieveRequestedAttrs, GetEndACP, GetActiveView, GetACPFromPoint, GetTextExt, GetScreenExt, GetWnd.
- `ITextStoreACPSink` `{22D44C94-A419-4542-A272-AE26093ECECF}` — **TSF-provided, we call**: OnTextChange(uint, in TS_TEXTCHANGE), OnSelectionChange, OnLayoutChange(TsLayoutCode, uint), OnStatusChange(uint), OnAttrsChange(…), OnLockGranted(uint), OnStartEditTransaction, OnEndEditTransaction.
- `ITfContextOwnerCompositionSink` `{5F084B5E-1C6A-11D3-B6FB-00C04FC9DAA7}` — optional app-implemented: OnStartComposition(IntPtr, out bool), OnUpdateComposition(IntPtr, IntPtr), OnEndComposition(IntPtr).
- Structs/enums: `TS_SELECTION_ACP{acpStart,acpEnd,TS_SELECTIONSTYLE{TsActiveSelEnd ase;BOOL fInterimChar}}`, `TS_TEXTCHANGE{acpStart,acpOldEnd,acpNewEnd}`, `TS_STATUS{dwDynamicFlags,dwStaticFlags}`, `TS_RUNINFO{uCount,TsRunType type}`. Flags: `TS_LF_READ=0x2`, `TS_LF_READWRITE=0x6`, `TS_LF_SYNC=0x1`; `TS_SS_TRANSITORY=0x4`, `TS_SS_NOHIDDENTEXT=0x8`; `TS_AS_*` advise mask (`TS_AS_ALL_SINKS=0x1F`); `TS_DEFAULT_SELECTION=0xFFFFFFFF`; `TS_S_ASYNC=0x00040300`; errors `TS_E_NOLOCK=0x80040201`, `TS_E_NOSELECTION=0x80040205`, `TS_E_NOLAYOUT=0x80040206`, `TS_E_INVALIDPOS=0x80040207`, `CONNECT_E_ADVISELIMIT=0x80040201`.

## `ITextStoreACP` method plan

**MUST implement** (per Chromium `tsf_text_store.cc`): AdviseSink/UnadviseSink (single sink, QI for ITextStoreACPSink, store mask; 2nd different sink → CONNECT_E_ADVISELIMIT), RequestLock (sync grant via sink.OnLockGranted; one-slot async-queue for reentrant write-lock returning TS_S_ASYNC), GetStatus (`dwStaticFlags = TS_SS_TRANSITORY|TS_SS_NOHIDDENTEXT`), GetSelection, SetSelection, GetText (+ single TS_RT_PLAIN runinfo), SetText (replace range), InsertTextAtSelection (honor TS_IAS_QUERYONLY=0x1), QueryInsert (clamp), GetEndACP (doc length, needs read lock), GetActiveView (cookie=1), GetWnd (host HWND), GetTextExt (caret RECT in screen coords; TS_E_NOLAYOUT if not ready), GetScreenExt (field rect screen coords).

**No-op `S_OK`** (E_NOTIMPL here can abort some IMEs): RequestSupportedAttrs, RequestAttrsAtPosition, RequestAttrsTransitioningAtPosition, RetrieveRequestedAttrs (pcFetched=0), FindNextAttrTransition (pfFound=false).

**`E_NOTIMPL`:** GetFormattedText, GetEmbedded, QueryInsertEmbedded, InsertEmbedded, InsertEmbeddedAtSelection, GetACPFromPoint.

**Lock model:** read methods require `_lock != 0` else `TS_E_NOLOCK`; write methods require `(_lock & TS_LF_READWRITE)==TS_LF_READWRITE`. Never raise OnTextChange/OnSelectionChange/OnLayoutChange while a lock is held — buffer and fire after. Reentrant RequestLock only legal while holding read lock (queue one async write lock → TS_S_ASYNC, grant after outer lock releases).

## Lifecycle

- **Per UI thread (lazy, STA, on the message-pumping thread):** `CoCreateInstance(CLSID_TF_ThreadMgr)` → `ITfThreadMgr`; `Activate(out clientId)`.
- **Per focused field:** `CreateDocumentMgr(out docMgr)`; `new UnoTsfTextStore(hwnd, bridge)`; `docMgr.CreateContext(clientId, 0, store, out ctx, out editCookie)`; `docMgr.Push(ctx)`.
- **On focus:** `threadMgr.SetFocus(docMgr)` → SIP auto-shows.
- **On blur:** `threadMgr.SetFocus(null)` → SIP auto-hides; optionally `docMgr.Pop(TF_POPF_ALL=0x1)`.
- **Teardown:** Pop/release context+docMgr; `Deactivate()`; release threadMgr. Hold a strong ref (GCHandle) to the store CCW for the context lifetime.
- **HWND:** resolve exactly as `Win32InputPaneExtension.TryResolveHwnd` (XamlRootMap → Win32WindowWrapper → Win32NativeWindow.Hwnd).

## Composition / app-buffer flow

- TSF edits via SetText/InsertTextAtSelection under a READWRITE lock. Store keeps authoritative `string _text` + `_selStart/_selEnd` mirroring the TextBox. On write: update buffer + selection, fill TS_TEXTCHANGE, route text into the Uno TextBox via the existing `IImeTextBoxExtension` composition events (in-flight → CompositionUpdated; commit → CompositionCompleted).
- App→TSF (external text/selection/caret changes outside a lock): after updating, fire `OnTextChange`/`OnSelectionChange`, and `OnLayoutChange(TS_LC_CHANGE)` when caret geometry becomes available (resolves a prior TS_E_NOLAYOUT so the IME repositions).

## Phased implementation

- **Phase A — Interop:** `Win32/UI/Xaml/Controls/TextBox/Tsf/TsfInterop.cs` (or split): all `[ComImport]` interface declarations + structs/enums/IIDs/CLSID. Compile-validate.
- **Phase B — Text store:** `UnoTsfTextStore : ITextStoreACP (+ ITfContextOwnerCompositionSink)` mirroring the active TextBox via a small bridge; implement the MUST/no-op/E_NOTIMPL plan + lock model. Unit-test the pure store logic (locks, GetText/GetSelection/InsertTextAtSelection) with a fake sink — no OS needed.
- **Phase C — Lifecycle + focus (milestone: keyboard shows):** `Win32TsfTextInput` activates ITfThreadMgr, creates docMgr/context/store, `SetFocus` on StartImeSession / `SetFocus(null)` on EndImeSession. Register as the Win32 `IImeTextBoxExtension` (replacing IMM32). **Manual validation with the maintainer: keyboard auto-shows on touch focus, hides on blur.**
- **Phase D — Composition/typing:** wire `ITfContextOwnerCompositionSink` + the write path into Uno's `TextComposition` events; verify typing + IME composition through the on-screen keyboard. Reuse caret geometry for candidate placement.
- **Phase E — Cleanup:** remove diagnostics; keep `InputPane` interop as the explicit-show path (now renders); ensure IMM32 path still compiles/works where TSF isn't active; tests + sample updated.

## Risks

- `CLSID_TF_ThreadMgr` GUID must be verified vs the local SDK msctf.h.
- TSF is STA/apartment-affine — all calls on the window's UI thread (which calls `OleInitialize`).
- The store CCW must answer QI for both `ITextStoreACP` and `ITfContextOwnerCompositionSink`; keep it rooted for the context lifetime.
- Most of this can only be runtime-validated on a real Windows touch device (manual, with the maintainer); the pure text-store/lock logic is unit-testable.
- This replaces the IMM32 IME on Win32 — keep behavior parity for hardware-keyboard typing/composition.
