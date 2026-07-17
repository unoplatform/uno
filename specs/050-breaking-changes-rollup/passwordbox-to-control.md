# PasswordBox → Control — reparent plan

**Status:** Plan only — not yet implemented. **Effort:** Large (~12 person-days).
**Part of:** breaking-changes catalogue [#8339](https://github.com/unoplatform/uno/issues/8339).

> This is a design/implementation plan for a reparent that is too large to land in a
> single shared session. It is the source-of-truth for the work; implement it as its own
> focused pass, one stabilized PR (or a small ordered series) against `feature/breakingchanges`.

## Goal

Uno's `PasswordBox` derives from `TextBox`. In WinUI, `PasswordBox` and `TextBox` are
**siblings**, both deriving directly from `Control`. 7.0 reparents `PasswordBox` to match
WinUI.

## Why it matters

- **Security / correctness** — the `TextBox` base mirrors the password into the inherited
  `Text` property (and raises `TextChanged`), so the cleartext password is readable via
  `Text` — a confidentiality leak WinUI does not have.
- **WinUI parity** — the `TextBox` base also leaks its whole input API (`SelectedText`,
  `SelectionStart/Length`, `IsReadOnly`, `AcceptsReturn`, `TextWrapping`, `CanUndo/CanRedo`,
  …) onto `PasswordBox`, and `is TextBox` wrongly captures `PasswordBox`. Uno already adds
  ~16 `new`/override shadows to compensate — direct evidence the parent is wrong.

## How the leak works today

Two sinks feed cleartext into the inherited `Text` surface:

1. `PasswordBox.OnPasswordChanged` → `SetValue(TextProperty, (string)e.NewValue)` — pushes
   the cleartext password into the inherited `TextBox.Text` DP.
2. `PasswordBox.OnTextChanged` override calls `base.OnTextChanged`, which raises the public
   `TextChanged` event with the cleartext, then mirrors back into `PasswordProperty`.

`((TextBox)passwordBox).Text` therefore returns the cleartext. After the reparent the
cleartext lives in an internal buffer; `PasswordBox` exposes it **only** as `Password`, and
no inherited `Text`/`TextChanged` member exists on the type — both sinks are structurally
gone, not merely overridden. (`PasswordBoxAutomationPeer` already returns bullet characters,
and `GetAccessibilityInnerText()` already returns `null`, so the automation surface is
already leak-free.)

## Design decision — extract an internal shared base (recommended)

Two approaches produce the **identical public break**; the choice is about internal
maintainability:

- **(a) Relocate text-input machinery onto `Control` / into `PasswordBox`** — *rejected.*
  `Control` is the universal control base; text-input machinery cannot live there.
  "Into `PasswordBox`" duplicates the entire Skia text pipeline (`TextBoxView` +
  `DisplayBlock` rendering, IME composition, caret/selection, undo/redo, pointer handling,
  clipboard, overlay) — thousands of lines of divergent, security-sensitive code.

- **(b) Extract `internal TextBoxBase : Control`** — *recommended.* Mirrors WinUI's own
  design (native `CTextBoxBase` under two `Control`-derived types). `TextBox : TextBoxBase`
  and `PasswordBox : TextBoxBase`. An **internal** base introduces **no new public API**;
  the public base members become the WinUI intersection, the shared machinery stays in one
  place, and the scattered `this is PasswordBox` guards become polymorphic virtual hooks.

## Phased implementation

### Slice 0 — internal base, **zero public break** (the heavy refactor, done first)
Introduce `internal partial class TextBoxBase : Control`. Move the shared machinery down
(DP definitions, the `OnTextChanged`/input pipeline, selection, paste, focus, pointer).
Retype the view layer from `TextBox` to `TextBoxBase`:
- `TextBoxView._textBox` / `TextBoxView.TextBox` / its constructor parameter,
- `TextBlock.OwningTextBox` (drives selection/caret/IME rendering),
- the `TextBoxView` unit-test/reference stub,
- other `TextBox`-typed owners (context-requested, scroll-viewer, accessibility owners).

Keep `TextBox : TextBoxBase` **and** `PasswordBox : TextBox` — **no reparent yet**.
Everything resolves transitively; validate against the existing `Given_TextBox` suite. This
lands the scary refactor with no API break — the honest stopping point for a first session.

### Slice 1 — reparent
`PasswordBox : TextBoxBase` (publicly `Control`). Move masking to a `PasswordBox`-aware
virtual hook on the base (resolves the existing wrong-hierarchy TODO in `TextBoxView`).
Convert the `is PasswordBox` guards (copy/cut-to-clipboard, IME, overlay extension) into
virtual hooks. Delete the now-redundant `new` shadows and the `Password`↔`Text` bridge.

### Slice 2 — API contract + tests
Add the `PackageDiffIgnore.xml` (`baseVersion="6.5"`) removal set for every inherited
`TextBox` member no longer on `PasswordBox`: `Text`, `TextChanged`/`TextChanging`/
`BeforeTextChanging`, `SelectedText`, `SelectionStart`/`SelectionLength`, `AcceptsReturn`,
`TextWrapping`, `IsReadOnly`, `CharacterCasing`, `IsSpellCheckEnabled`,
`IsTextPredictionEnabled`, `TextAlignment`, `SelectionChanged`, `Copy/CutSelectionToClipboard`,
plus the base-type change. Add tests:
- `PasswordBox` is **not** a `TextBox`; it **is** a `Control`.
- the password is **not** retrievable via any public inherited API (reflection sweep);
- entry + reveal (`PasswordRevealMode`) still work end-to-end.

## Risks / notes

- Moving shared DPs into `TextBoxBase` changes each DP's **owner type** — validate it
  doesn't perturb the API-diff owner matching or `TargetType`/style/template resolution
  (`PasswordBox` default style, `DefaultStyleKey`, shared `TextBoxConstants` part names).
- The native `PasswordBox.Android`/`PasswordBox.Apple` partials the original issue
  referenced are **already removed** on `feature/breakingchanges` (native UI rendering layer
  dropped), so the historical "riskiest part" is largely neutralized; residual native risk
  is confined to keeping the reference/unit-test stub and `#if !__SKIA__` paths compiling.

## Definition of done

`PasswordBox : Control` · the password is no longer exposed through `Text`/`TextChanged` ·
the leaked `TextBox` API is gone · runtime + WinUI-parity validation green.
