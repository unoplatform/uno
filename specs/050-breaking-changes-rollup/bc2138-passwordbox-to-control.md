# #2138 — `PasswordBox` no longer inherits `TextBox`

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Danger:** 3/5 · **Effort:** L · **Phase:** 5 (reparenting track, own PR)

## TL;DR

Uno's `PasswordBox` derives from `TextBox`; WinUI's derives from `Control`. The `TextBox` base mirrors the password into the inherited `Text` property — a confidentiality leak WinUI does not have — and leaks the whole text-input API onto `PasswordBox`, so `is TextBox` wrongly matches a `PasswordBox`. Both controls move to `Control` and share the editing implementation by **composition**: an `internal sealed TextBoxCore` reached through an `internal ITextBoxHost` that each control implements.

## Current state (verified)

- `PasswordBox : TextBox` at `src/Uno.UI/UI/Xaml/Controls/PasswordBox/PasswordBox.cs:12`. The mirroring is explicit: `OnPasswordChanged` does `SetValue(TextProperty, (string)e.NewValue)` (`PasswordBox.cs:112`) and `OnTextChanged` does `SetValue(PasswordProperty, …)` (`PasswordBox.cs:187`). So `passwordBox.Text` returns cleartext and `TextChanged` fires on every keystroke.
- Uno carries ~16 `new`/`override` shadows on `PasswordBox` (`Description`, `SelectionHighlightColor`, `SelectAll`, `PasteFromClipboard`, `Paste`, `SelectionFlyout`, `CanPasteClipboardContent`, `ContextMenuOpening`, …) purely to re-surface or suppress inherited `TextBox` members — direct evidence the parent is wrong.
- The implementation `PasswordBox` genuinely needs all sits on `TextBox`: `TextBox.cs` (~1,600 lines), `TextBox.skia.cs` (~1,860 lines — selection, caret, undo history, grippers, selection flyout, proofing menu), `TextBox.IME.skia.cs`, `TextBox.pointers.skia.cs`, `TextBox.mux.cs`, `TextBox.reference.cs`.
- **The source issue's scope section is stale.** It cites `PasswordBox.Android.cs:8` / `PasswordBox.Apple.cs:7` and says the change "spans Skia plus the native Android/Apple/WASM partials". Those files do not exist. `src/Uno.UI/` now ships only `Uno.UI.Skia.csproj` and `Uno.UI.Reference.csproj`, and `TextBox.cs` contains zero `__ANDROID__` / `__APPLE_UIKIT__` / `__WASM__` references. The L·~12-person-day estimate is built on that stale scope.
- **An axis the issue does not mention.** Four Skia platform runtimes consume `TextBox`-typed **internal** extension interfaces that serve `PasswordBox` today purely through inheritance: `IImeTextBoxExtension.StartImeSession(TextBox)`, `ITextBoxNotificationsProviderSingleton` (7 members), `IOverlayTextBoxView.IsCompatible(TextBox)` / `UpdateProperties(TextBox)`, `IOverlayTextBoxViewExtension`. Implementations live in `Uno.UI.Runtime.Skia.{Android,AppleUIKit,MacOS,WebAssembly.Browser}`. They are `internal`, so retyping them is not a public break — but it is why the implementation has to be *shared* rather than duplicated.
- `PasswordBoxAutomationPeer : FrameworkElementAutomationPeer` already matches WinUI and already masks through `TextAdapter` (`src/Uno.UI/DirectUI/TextAdapter.cs:50`). No peer work required.
- Template parts already match WinUI's (`HeaderContentPresenter`, `BorderElement`, `ContentElement`, `PlaceholderTextContentPresenter`, `RevealButton`, `DescriptionPresenter`) — compare `Generic.xaml:4330` with WinUI's `controls/dev/CommonStyles/PasswordBox_themeresources.xaml:189`. **No template restructuring.**
- `RichEditBox` is a generated stub only, so there is no third consumer today.
- This item had **no row in `spec.md`** (only BC52's reparent was listed); the row is added by this change.

## WinUI's architecture, and why Uno diverges from it here

The source issue proposes "extract a shared **internal** text-input base shared with `TextBox`". That exact shape is **illegal in C#** — `public class TextBox : InternalBase` is **CS0060**, base class less accessible than the class (verified empirically). WinUI's own implementation *is* a shared base class:

- `CPasswordBox final : CTextBoxBase`, `CTextBox final : CTextBoxBase`, `CRichEditBox final : CTextBoxBase` — `dxaml/xcp/core/native/text/Controls/{PasswordBox,TextBox,RichEditBox}.h`.
- `DirectUI::TextBoxBase : DirectUI::Control`, registered in the XAML type table as `Microsoft.UI.Xaml.Internal.TextBoxBase` with null core *and* framework constructors — abstract and non-instantiable (`dxaml/xcp/core/metadata/DynamicMetadata.g.cpp:5078`).
- `TextBoxBase` declares **zero dependency properties**. Every DP lives on the leaf type: `runtimeclass PasswordBox : Control` in `microsoft.ui.xaml.controls.controls.idl` declares its own `MaxLength`, `Header`, `HeaderTemplate`, `PlaceholderText`, `SelectionHighlightColor`, `SelectionFlyout`, `InputScope`, `TextReadingOrder`, `PreventKeyboardDisplayOnProgrammaticFocus`, `Description`, `CanPasteClipboardContent`.
- The base reaches derived state through virtual accessors, never a derived DP: `IsPassword()`, `IsPasswordRevealed()`, `IsReadOnly()`, `AcceptsReturn()`, `GetTextWrapping()`, `GetAlignment()`, `TxGetMaxLength()`, `TxGetPasswordChar()`, `GetTextReadingOrder()`, plus pure virtuals `IsEmpty()` and `GetInputScope()` (`TextBoxBase.h:105-159`).

But WinUI's *projected* surface is `runtimeclass PasswordBox : Control` — C++/WinRT keeps the implementation hierarchy separate from the projected one. C# has only one hierarchy, so in Uno the two goals conflict and one must give.

| | Intermediate base class | **Composition (chosen)** |
|---|---|---|
| `PasswordBox` base | `TextBoxBase` | **`Control`** — matches WinUI's projection |
| `TextBox` base | changes to `TextBoxBase` | **unchanged** |
| New public API | one type — CS0060 forces `public` | **none** — core and host interface are both `internal` |
| Shared `protected override` input handlers | written once | duplicated as ~30 one-line forwards per control |
| Package-diff story | new public type + two base-type changes | only `PasswordBox`'s lost surface |

Composition preserves what app developers observe, at the cost of forwarding boilerplate. WinUI's accessor design carries over unchanged — it just becomes an interface instead of a set of virtuals.

## What changes

1. **New `internal sealed partial class TextBoxCore`** under `src/Uno.UI/UI/Xaml/Controls/TextBoxCore/`, holding the editing implementation: selection, caret, undo history, IME, pointers, grippers, clipboard, selection flyout, proofing menu, and the `TextBoxView` wiring. It sits alongside the existing `internal TextBoxView` (text layout/rendering).

2. **New `internal interface ITextBoxHost`**, implemented by both controls, through which the core reads host state and raises host events. It mirrors `CTextBoxBase`'s `Tx*`/`Is*` virtuals: `Owner`, `TextValue`, `RaiseTextChangeEvents`, `IsPassword`, `IsPasswordRevealed`, `PasswordChar`, `IsReadOnly`, `AcceptsReturn`, `TextWrapping`, `TextAlignment`, `Header`, `PlaceholderText`, `Description`, `MaxLength`, `InputScope`, `SelectionHighlightColor`, `SelectionFlyout`, and the selection/undo/paste/context-menu notifications. `IsEmpty` is computed on the core from `TextValue`.

3. **`TextBox : Control, ITextBoxHost`** — public surface untouched; its implementation moves into the core and it keeps thin `protected override` shells that call `base.OnX(…)` then forward.

4. **`PasswordBox : Control, ITextBoxHost`** with its own `TextBoxCore`, declaring its own DPs. Its surface becomes exactly the WinUI IDL list: `Password`, `PasswordChar`, `PasswordRevealMode`, `IsPasswordRevealButtonEnabled` (deprecated), `MaxLength`, `Header`, `HeaderTemplate`, `PlaceholderText`, `SelectionHighlightColor`, `PreventKeyboardDisplayOnProgrammaticFocus`, `TextReadingOrder`, `InputScope`, `CanPasteClipboardContent`, `SelectionFlyout`, `Description`, `PasswordChanged`, `PasswordChanging`, `ContextMenuOpening`, `Paste`, `SelectAll()`, `PasteFromClipboard()`. Everything else the `TextBox` base leaked is removed, including `Text` and `TextChanged`.

5. The `this is PasswordBox` tests inside the implementation (`TextBox.cs:1494,1513`, `TextBox.IME.skia.cs:63,76`, `TextBoxView.skia.cs:190-215`) collapse into `_host.IsPassword` / `_host.PasswordChar`, resolving the standing TODO at `TextBoxView.skia.cs:190` ("Inheritance hierarchy is wrong in Uno").

`PasswordChanging`, `TextReadingOrder` and `PreventKeyboardDisplayOnProgrammaticFocus` stay `[NotImplemented]` stubs — out of scope here.

## Pros

- Closes a real confidentiality leak: the password stops being readable through `Text` and stops being announced through `TextChanged`.
- `is TextBox` becomes correct, fixing `TextBox`-targeted styles and converters and removing the ~15 `and not PasswordBox` guards Uno had to sprinkle through `TextCommandBarFlyout`.
- Both controls end up with WinUI's projected hierarchy (`: Control`), and `TextBox`'s base type is not disturbed at all.
- **No new public API.** The shared implementation and its contract are both `internal`.
- Removes ~16 shadow members and the `TextBoxView.skia.cs` inheritance TODO; gives `RichEditBox` a place to plug in when implemented.

## Cons / risks

- **Breaking, source and binary**, for any code treating a `PasswordBox` as a `TextBox` or reading the password via `Text`.
- `PasswordBox` loses `TextChanging`/`BeforeTextChanging`, and `PasswordChanging` remains a stub, so migrating apps have **no** pre-change hook. Migration-note item and follow-up work.
- Composition means every framework touchpoint the implementation needs (`GetTemplateChild`, `VisualStateManager.GoToState`, `SetValue`, `Dispatcher`, `FocusState`, …) routes through `ITextBoxHost.Owner` instead of being inherited, and each `protected override` input handler becomes a thin forward on both controls. More boilerplate than an intermediate base class would need.
- The riskiest category is invisible to the compiler: sites doing a bare `is TextBox` that silently captured `PasswordBox` and now silently stop. `TextControlFlyoutHelper.cs:361` says so in a comment. A green build proves nothing; each site must be walked.
- Touches Uno's most delicate input code (caret, selection, undo history, IME), so the core extraction lands as its own no-behaviour-change commits *before* `PasswordBox` is touched at all.

## Decision

**Share the implementation by composition; both controls derive from `Control`.** The intermediate-base-class alternative was rejected: CS0060 forces it to be `public`, which adds Uno-only public surface to an epic whose purpose is removing exactly that, and it changes `TextBox`'s base type as collateral. The forwarding boilerplate is the accepted cost.

## Affected files (starting set)

New: `src/Uno.UI/UI/Xaml/Controls/TextBoxCore/TextBoxCore{,.skia,.IME.skia,.pointers.skia,.reference}.cs`, `…/TextBoxCore/ITextBoxHost.cs`, `…/Controls/PasswordBox/PasswordBox.reference.cs`.

Modified: `…/Controls/TextBox/TextBox{,.skia,.IME.skia,.pointers.skia,.mux,.reference,.Attached}.cs`, `…/Controls/TextBox/TextBoxView.skia.cs`, `…/Controls/TextBox/Extensions/*.skia.cs`, `…/Controls/PasswordBox/PasswordBox{,.skia}.cs`, `…/Controls/CommandBarFlyout/TextCommandBarFlyout.mux.cs`, `…/Xaml/Internal/TextControlFlyoutHelper.cs`, `…/Xaml/Internal/TextCore.cs`, `…/UI/Text/TextCore.mux.cs`, `…/DirectUI/{TextAdapter,TextRangeAdapter}.cs`, `…/Xaml/Automation/AutomationProperties.uno.cs`, `…/Xaml/Input/FocusManager.mux.cs`, `…/Accessibility/AriaMapper.cs`, the four `Uno.UI.Runtime.Skia.*` TextBox/accessibility folders, `src/Uno.WinAppSDKSyncGenerator/Generator.cs`, `build/PackageDiffIgnore.xml`, `doc/articles/migrating-to-uno-7.md`, `src/Uno.UI.RuntimeTests/…/Given_PasswordBox.cs`, and the `PasswordBox_*` samples.

## Validation strategy

Runtime tests are Skia-only, in `Given_PasswordBox.cs`, each failing before the reparent and passing after:

- `typeof(PasswordBox).GetProperty("Text") is null`, `passwordBox is not TextBox`, `typeof(PasswordBox).BaseType == typeof(Control)`.
- Setting `Password` raises no `TextChanged`; nothing reachable on `PasswordBox` returns cleartext; `GetAccessibilityInnerText()` returns null and the UIA `Value` pattern returns the mask.
- `When_Copy_Cut_Does_Not_Leak_Password` **rewritten, not deleted** — it currently calls `CopySelectionToClipboard()`/`CutSelectionToClipboard()`, Uno-only `TextBox` methods WinUI's `PasswordBox` lacks, so it must drive Ctrl+C/Ctrl+X through the keyboard instead. It encodes a security invariant.
- Behaviour preserved: `PasswordChar` (existing visual-comparison tests), reveal button and `PasswordRevealMode` Peek/Visible/Hidden, `Header`/`PlaceholderText`/`Description` visibility, `MaxLength`, `SelectAll()`, `PasteFromClipboard()`, `ContextMenuOpening`, selection flyout, IME suppression.

Plus a `Given_TextBox*` regression batch (the core extraction must be behaviour-neutral), `/winui-runtime-tests Given_PasswordBox` for parity, the `Uno.UI.Reference` build, and a SamplesApp visual sweep of the `PasswordBox_*` samples — the reveal button and caret/selection are the parts most likely to regress silently.

## Sequencing

Independent of the other Phase 5 items; sibling to BC52 (`RadioMenuFlyoutItem`) and BC73 (`TimePickerFlyoutPresenter`). Own branch and PR onto `feature/breakingchanges`, staged as: impact spec → introduce `TextBoxCore`/`ITextBoxHost` → move the chrome layer → move the Skia engine (one commit) → complete the host contract → **gate on 235/235 with `PasswordBox` still on `TextBox`** → reparent `PasswordBox` → consumer sweeps → tests/samples → sync-gen, package diff, migration note.

The core extraction must land before `PasswordBox` is touched; collapsing the two would make a regression in the input stack indistinguishable from a reparenting bug.

## Validation status

**Baseline (Skia Desktop, runtime):** `Given_TextBox` + `Given_PasswordBox` = **235 total, 235 passed, 0 failed, 4 skipped** at base `47f9d020ac`. The 4 skips are pre-existing: `When_Focus_Immediately`, `When_Paste_While_Pointer_Held`, `When_TextBox_Wrap_Custom_Style`, `When_TextBox_Wrap_Fluent`. Every extraction step is held against this number.

- **Not started** — design recorded, implementation pending.
