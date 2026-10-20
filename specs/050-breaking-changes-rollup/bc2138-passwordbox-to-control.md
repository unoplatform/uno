# #2138 — `PasswordBox` no longer inherits `TextBox`

**Epic:** [#8339](https://github.com/unoplatform/uno/issues/8339) · **Danger:** 3/5 · **Effort:** L · **Phase:** 5 (reparenting track, own PR)

## TL;DR

Uno's `PasswordBox` derives from `TextBox`; WinUI's derives from `Control`. The `TextBox` base mirrors the password into the inherited `Text` property — a confidentiality leak WinUI does not have — and leaks the whole text-input API onto `PasswordBox`, so `is TextBox` wrongly matches a `PasswordBox`. Both controls move to `Control` and share the editing implementation by **composition**: an `internal sealed TextBoxCore` reached through an `internal ITextBoxHost` that each control implements.

## Current state (verified)

- `PasswordBox : TextBox` at `src/Uno.UI/UI/Xaml/Controls/PasswordBox/PasswordBox.cs:12`. The mirroring is explicit: `OnPasswordChanged` does `SetValue(TextProperty, (string)e.NewValue)` (`PasswordBox.cs:112`) and `OnTextChanged` does `SetValue(PasswordProperty, …)` (`PasswordBox.cs:187`). So `passwordBox.Text` returns cleartext and `TextChanged` fires on every keystroke.
- Uno carries ~16 `new`/`override` shadows on `PasswordBox` (`Description`, `SelectionHighlightColor`, `SelectAll`, `PasteFromClipboard`, `Paste`, `SelectionFlyout`, `CanPasteClipboardContent`, `ContextMenuOpening`, …) purely to re-surface or suppress inherited `TextBox` members — direct evidence the parent is wrong.
- The implementation `PasswordBox` genuinely needs all sits on `TextBox`: `TextBox.cs`, plus the input engine (~1,860 lines — selection, caret, undo history, grippers, selection flyout, proofing menu), the IME and pointer partials, and `TextBox.mux.cs`.

  > **Updated after PR #23999 landed on `feature/breakingchanges`.** That PR folded the UI-layer Reference flavor into Skia and merged every `.skia.cs` partial into its base file, so the suffixed files this section originally named (`TextBox.skia.cs`, `TextBox.IME.skia.cs`, `TextBox.pointers.skia.cs`, `TextBox.reference.cs`) no longer exist — the engine now lives inside `TextBox.cs`, which grew to ~3,500 lines. The extraction described below is unchanged in substance; only the source paths moved.

- **The source issue's scope section is stale.** It cites `PasswordBox.Android.cs:8` / `PasswordBox.Apple.cs:7` and says the change "spans Skia plus the native Android/Apple/WASM partials". Those files do not exist, and `TextBox.cs` contains zero `__ANDROID__` / `__APPLE_UIKIT__` / `__WASM__` references. The L·~12-person-day estimate is built on that stale scope. (`src/Uno.UI/` shipped `Uno.UI.Skia.csproj` + `Uno.UI.Reference.csproj` when this was written; post-#23999 it is a single `Uno.UI.csproj` targeting Skia only.)
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

5. The `this is PasswordBox` tests inside the implementation (in `TextBox.cs`, the IME partial and `TextBoxView`) collapse into `_host.IsPassword` / `_host.PasswordChar`, resolving the standing `TextBoxView` TODO ("Inheritance hierarchy is wrong in Uno").

`PasswordChanging`, `TextReadingOrder` and `PreventKeyboardDisplayOnProgrammaticFocus` stay `[NotImplemented]` stubs — out of scope here.

## Pros

- Closes a real confidentiality leak: the password stops being readable through `Text` and stops being announced through `TextChanged`.
- `is TextBox` becomes correct, fixing `TextBox`-targeted styles and converters and removing the **10** `and not PasswordBox` guards Uno had to sprinkle through `TextCommandBarFlyout` (the estimate here was originally "~15"; the actual count is 10).
- Both controls end up with WinUI's projected hierarchy (`: Control`), and `TextBox`'s base type is not disturbed at all.
- **No new public API.** The shared implementation and its contract are both `internal`.
- Removes ~16 shadow members and the `TextBoxView` inheritance TODO; gives `RichEditBox` a place to plug in when implemented.

## Cons / risks

- **Breaking, source and binary**, for any code treating a `PasswordBox` as a `TextBox` or reading the password via `Text`.
- `PasswordBox` loses `TextChanging`/`BeforeTextChanging`, and `PasswordChanging` remains a stub, so migrating apps have **no** pre-change hook. Migration-note item and follow-up work.
- Composition means every framework touchpoint the implementation needs (`GetTemplateChild`, `VisualStateManager.GoToState`, `SetValue`, `Dispatcher`, `FocusState`, …) routes through `ITextBoxHost.Owner` instead of being inherited, and each `protected override` input handler becomes a thin forward on both controls. More boilerplate than an intermediate base class would need.
- The riskiest category is invisible to the compiler: sites doing a bare `is TextBox` that silently captured `PasswordBox` and now silently stop. `TextControlFlyoutHelper.cs:361` says so in a comment. A green build proves nothing; each site must be walked.
- Touches Uno's most delicate input code (caret, selection, undo history, IME), so the core extraction lands as its own no-behaviour-change commits *before* `PasswordBox` is touched at all.

## Decision

**Share the implementation by composition; both controls derive from `Control`.** The intermediate-base-class alternative was rejected: CS0060 forces it to be `public`, which adds Uno-only public surface to an epic whose purpose is removing exactly that, and it changes `TextBox`'s base type as collateral. The forwarding boilerplate is the accepted cost.

## Affected files (as landed)

**New** — `src/Uno.UI/UI/Xaml/Controls/TextBoxCore/`: `ITextBoxHost.cs`, `TextBoxCore.cs`, `TextBoxCore.Input.cs`, `TextBoxCore.IME.cs`, `TextBoxCore.pointers.cs`; plus `…/Controls/TextBox/TextBox.{Host,Overrides,Properties}.cs` and `…/Controls/PasswordBox/PasswordBox.{Host,Overrides,Properties}.cs`.

Two deviations from the plan above, both deliberate:

- **No `.reference.cs` files.** `Uno.UI.csproj` is Skia-only post-#23999, so a `.reference.cs` compiles nowhere. `PasswordBox.reference.cs` was never needed either — it would have carried `SelectionStart`/`SelectionLength`, which WinUI's `PasswordBox` does not have.
- **The engine is `TextBoxCore.Input.cs`, not folded into `TextBoxCore.cs`.** Folding would mirror what #23999 did to `TextBox.skia.cs`, but `TextBoxCore.cs` is `#nullable enable` while the engine is nullable-oblivious; merging puts ~1,880 unannotated lines under an annotated context and produces 40 errors unrelated to this change.

**Modified** — `…/Controls/TextBox/TextBox{,.mux,.Attached}.cs`, `…/Controls/TextBox/TextBoxView.cs`, `…/Controls/PasswordBox/PasswordBox.cs`, `…/Controls/CommandBarFlyout/TextCommandBarFlyout.mux.cs`, `…/Xaml/Internal/TextControlFlyoutHelper.cs`, `…/Xaml/Controls/TextBlock/TextBlock.cs`, `…/Xaml/Controls/ScrollViewer/ScrollViewer.cs`, `…/Xaml/XamlRoot.cs`, `…/Xaml/Automation/AutomationProperties.uno.cs`, `…/Accessibility/AriaMapper.cs`, the four `Uno.UI.Runtime.Skia.*` text-input/accessibility folders, `doc/articles/migrating-to-uno-7.md`, `src/Uno.UI.RuntimeTests/…/Given_PasswordBox.cs` and `…/Given_TextBox.skia.cs`, `SamplesApp.UITests/…/PasswordBoxTests.cs`.

**Not modified, contrary to the original list:**

- **`build/PackageDiffIgnore.xml` — no entry was required.** See *Package-diff outcome* below.
- `src/Uno.WinAppSDKSyncGenerator/Generator.cs` — the generator needed no change; the sync-gen check passes with no drift, because `PasswordBox` now *declares* `Header`/`HeaderTemplate`/`InputScope`/`MaxLength`/`PlaceholderText` itself rather than inheriting them.
- `…/Xaml/Internal/TextCore.cs`, `…/UI/Text/TextCore.mux.cs`, `…/DirectUI/{TextAdapter,TextRangeAdapter}.cs`, `…/Xaml/Input/FocusManager.mux.cs` — walked and verified no-ops. `FocusManager.mux.cs`'s two `is TextBox` sites have **entirely commented-out bodies**; the `TextCore`/`UIElement.ContextRequested` sites already test both types; `TextAdapter` masks via the automation peer regardless.
- The `PasswordBox_*` samples needed **no** changes: no sample sets a `TextBox`-only property or binds to `Text`. The single real fix was a string-keyed DP lookup in `SamplesApp.UITests/…/PasswordBoxTests.cs` reading `GetDependencyPropertyValue<string>("Text")`, now `"Password"` — invisible to the compiler, which is the hazard class this reparent specialises in.

## Package-diff outcome

`Uno.PackageDiff` reported exactly **one** unignored breaking change, and it was **not** the removed `TextBox` surface — that surface is absent from WinUI's `PasswordBox`, so the tool's ignore sets already cover it. The single report was:

```
Removed method …Primitives.FlyoutBase …PasswordBox.get_SelectionFlyout()
```

`SelectionFlyout` was still declared. The break was its **vtable shape**: because the public property implicitly implemented `ITextBoxHost.SelectionFlyout`, the compiler emitted `get_SelectionFlyout` as `virtual final newslot`, whereas the baseline had a plain non-virtual getter. `TextBox` forwards that interface member **explicitly**, keeping its own getter non-virtual; `PasswordBox` now does the same (`FlyoutBase? ITextBoxHost.SelectionFlyout => SelectionFlyout;`). A metadata sweep confirmed it was the only such member (`PasswordBox`: 1 public virtual+newslot method before, 0 after; `TextBox`: 0 both).

**So the fix is a genuine binary-compatibility restoration, not a suppression, and `PackageDiffIgnore.xml` is untouched.** Verified by running `generatepkgdiff` against published `Uno.WinUI 6.6.184`: the unmodified package reports the error and exits with differences; the same package carrying only this fix compares clean.

**Reusable note for future BC items:** any `internal` interface a *public* control implements implicitly will silently make the matching public accessors `virtual`, which the package diff reports as removed methods. Forward such members explicitly.

## Validation strategy

Runtime tests live in `Given_PasswordBox.cs`. **Only some genuinely fail before the reparent** — the distinction matters and is recorded under *Validation status*: 4 of the 7 new tests are true fails-before proofs of the change's claims, and 3 are regression guards that also passed beforehand. The hierarchy and surface assertions are deliberately **not** platform-gated, since they are WinUI-parity claims that hold on native WinUI too, so `/winui-runtime-tests` exercises them as parity checks.

- `typeof(PasswordBox).GetProperty("Text") is null`, `passwordBox is not TextBox`, `typeof(PasswordBox).BaseType == typeof(Control)`.
- Setting `Password` raises no `TextChanged`; nothing reachable on `PasswordBox` returns cleartext; `GetAccessibilityInnerText()` returns null and the UIA `Value` pattern returns the mask.
- `When_Copy_Cut_Does_Not_Leak_Password` **rewritten, not deleted** — it currently calls `CopySelectionToClipboard()`/`CutSelectionToClipboard()`, Uno-only `TextBox` methods WinUI's `PasswordBox` lacks, so it must drive Ctrl+C/Ctrl+X through the keyboard instead. It encodes a security invariant.
- Behaviour preserved: `PasswordChar` (existing visual-comparison tests), reveal button and `PasswordRevealMode` Peek/Visible/Hidden, `Header`/`PlaceholderText`/`Description` visibility, `MaxLength`, `SelectAll()`, `PasteFromClipboard()`, `ContextMenuOpening`, selection flyout, IME suppression.

Plus a `Given_TextBox*` regression batch (the core extraction must be behaviour-neutral) and a SamplesApp visual pass of the `PasswordBox_*` samples — the reveal button and caret/selection are the parts most likely to regress silently.

⚠️ **A four-project build gate is not sufficient, and this item proved it.** Three Skia platform runtimes (`Android`, `WebAssembly.Browser`, `AppleUIKit`) were left **not compiling** for seven consecutive items while four green builds reported success, because the native text-input path was `TextBox`-typed end to end and nothing in the narrow gate reached it. The standing gate for this class of change is **every project the change can reach** — see *Validation status*. (The `Uno.UI.Reference` build named in the original plan no longer exists; Skia and Reference collapsed into one `Uno.UI` in #23999.)

## Sequencing

Independent of the other Phase 5 items; sibling to BC52 (`RadioMenuFlyoutItem`) and BC73 (`TimePickerFlyoutPresenter`). Own branch and PR onto `feature/breakingchanges`, staged as: impact spec → introduce `TextBoxCore`/`ITextBoxHost` → move the implementation into the core → complete the host contract → **gate with `PasswordBox` still on `TextBox`** → reparent `PasswordBox` → consumer sweeps → tests/samples → sync-gen, package diff, migration note.

The core extraction must land before `PasswordBox` is touched; collapsing the two would make a regression in the input stack indistinguishable from a reparenting bug.

**The "chrome layer then engine" split was attempted and abandoned — they are not separable.** `TextBox.cs` declares 33 partial methods and the engine implements 24 of them, so the platform seam runs straight through the chrome layer (`UpdateFontPartial`/`OnForegroundColorChangedPartial` are declared in one and implemented in the other; `UpdateVisualState` drags `_forceFocusedVisualState`; `UpdateButtonStates` reaches `UpdateScrolling`). Splitting produces bridge members deleted one commit later, so the move landed as a single commit.

⚠️ **`partial void` is the silent-failure hazard that governs this sequencing.** A declaration with no implementation is legal C# and calls to it are **silently elided** — so moving an implementation to the core while leaving the declaration on the control keeps the build green and makes the behaviour disappear. The audit found **24 orphans**, of which 8 were real silent regressions. The same hazard appears in `override` clothing: six `protected override` handlers were converted to plain `internal` methods on the core with no forwarding override left on the control, leaving selection-drag, right-tap, double-tap word selection and caret bring-into-view **completely uncalled** while compiling clean. Both audits (declaration-vs-implementation, and `override` name-set diff) must be re-run after any further work of this shape.

## Validation status

**Complete — implemented in PR #24038, awaiting review.**

**Baseline (Skia Desktop, runtime):** `Given_TextBox` + `Given_PasswordBox` = **235 total, 235 passed, 4 skipped** at the fork point. The 4 skips are pre-existing: `When_Focus_Immediately`, `When_Paste_While_Pointer_Held`, `When_TextBox_Wrap_Custom_Style`, `When_TextBox_Wrap_Fluent`. Every extraction step was held against that number; the suite is **243/243 + 4 skipped** once the 7 new tests land.

| Gate | Result |
|---|---|
| Ten builds — `Uno.UI`, `Uno.UI.UnitTests`, Skia runtimes {Win32, X11, MacOS, Browser, AppleUIKit, Android}, `SamplesApp.UITests`, `SamplesApp.Skia.Generic` | ✅ all green |
| Consolidated `SamplesApp` head (net11.0) | ✅ green — needs `allowPrerelease: true` locally, otherwise CI-only |
| `Given_TextBox` + `Given_PasswordBox` (Skia Desktop) | ✅ **243/243**, 4 skipped |
| Blast-radius filter (`AutoSuggestBox`, `ComboBox`, `TextBlock`, `ScrollViewer`, `CommandBarFlyout`) | ✅ 238/239, 9 skipped — the one failure is proven pre-existing at the base commit |
| CI, all platforms | ✅ WASM 0–3, Android 0–4 + NativeAOT, iOS 0–3, Desktop Skia Windows/macOS/Linux/Framebuffer, Snapshot, Screenshot Comparison, WinAppSDK, Unit Tests |
| Package diff · sync-gen · `Analyze (csharp)` | ✅ all pass |

**Which new tests genuinely fail before the change** — 4 of 7 are proofs, 3 are guards:

| Test | Fails before? |
|---|---|
| `When_Derives_From_Control_Not_TextBox` | ✅ `BaseType` was `TextBox` |
| `When_TextBox_Only_Api_Is_Not_Reachable` | ✅ all of it was inherited |
| `When_Password_Is_Not_Reachable_As_Text` | ✅ `Text` returned the cleartext |
| `When_MaxLength_Matches_TextBox` | ✅ catches the old `Password`/`Text` desync |
| `When_WinUI_Surface_Is_Declared` | ❌ guard — `GetProperty` finds *inherited* members |
| `When_Password_Changes_PasswordChanged_Is_Raised` | ❌ guard |
| `When_Automation_Does_Not_Expose_Password` | ❌ guard |

### Behaviour changes found on the way (beyond the reparent itself)

- **`PasswordBox` used to spell-check its own masked text.** It inherited `IsSpellCheckEnabled` (default `true`) and the view pushed that onto the display block. WinUI's `PasswordBox` has no such property. Fixing it **changes rendering** — the squiggly underline is gone — and required rebaselining a pixel test that had been asserting the old parity.
- **`MaxLength` now coerces `Password` directly.** It previously reached a `PasswordBox` only through the `Text` mirror, and the two were desynced: `Password` accepted an over-long value while the mirror rejected it. `PasswordProperty` therefore had to gain `CoerceText`; without it, removing the mirror would have made `MaxLength` stop limiting typed input entirely.
- **`TextBlock.OwningTextBox` was `TextBox`-typed**, so it went null for a reparented `PasswordBox` — silently resetting the display block's selection on every text change, dropping the caret-thickness width adjustment and breaking `ContextRequested` routing. Retyped to the engine. No test covered this; it was found by audit.
- **`RaiseValueAutomationEvents` is deliberately empty on `PasswordBox`.** The shared implementation only raised those when the peer was a `TextBoxAutomationPeer`, which a password box's peer never was, so raising them now would be new behaviour. Notifying listeners of password-length changes is a genuine pre-existing accessibility gap, noted rather than fixed here.
- **`InputScope`'s default stays `Default`.** `Password` is the better value, but the inherited property defaulted to `Default` and changing it is not part of reparenting.

### Visual verification

All samples were rendered in light and dark themes via `SamplesApp.Skia.Generic --auto-screenshots` and inspected. `PasswordBox_Simple` is the highest-value page — its fourth box applies a **custom `ControlTemplate`** unconditionally, so a retemplated `PasswordBox` does reach Skia desktop; it renders correctly. `PasswordBox_Header_PlaceholderText` (which has no automated assertions) renders header and placeholder correctly, and `PasswordBox_PasswordChar` confirms masking for `●`/`*`/`?`/`$` with **no squiggly underline**. `Screenshot Comparison` and `Snapshot Tests` cover these samples automatically and are green. Residual for a reviewer: **interactive states only** — hover/pressed transitions and selection-highlight during a drag.

`Input_PasswordBox` looks like it exercises the custom template but does not: its style is applied as `android:Style=`, so on desktop it renders with the default template.
