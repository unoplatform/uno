# Input validation — presentation

**Status**: Deferred
**Audience**: Internal engineering (Uno Platform maintainers)
**Created**: 2026-09-22

> Second of two, and **not scheduled**. `specs/059-input-validation/spec.md` is the prerequisite: it delivers
> the transport layer and the bindable read model (`Validation.IsEnabled` / `HasErrors` / `Errors`), which is
> enough for an app to render its own error text. This spec covers the *built-in* visuals — control
> templates, the error presenter and the `ValidationStates` visual state group.
>
> It is recorded now rather than later because two of its findings are **blocking**, not merely unfinished:
> §2 shows the obvious mechanism for driving the visual states is closed, and §3 shows a second visual state
> group cannot be given priority over `CommonStates`. Both were discovered while designing 059, and both
> would otherwise be rediscovered at implementation time.

## 1. Scope

| In | Out |
|---|---|
| The `ValidationStates` visual state group (§2) | Everything in spec 059 — the transport layer, the attached properties, the validation-property attribute |
| Visual-state contention with `CommonStates` (§3) | |
| Per-control template changes and the error presenter (§4) | |
| `IInputValidationControl.ErrorTemplate` (§5) | |
| The floated `IsRequired` indicator (§6) | |

`grep -rn "ValidationStates" src` returns **zero hits**, so the group name collides with nothing.

## 2. The `ValidationStates` group — and what drives it

The proposal is one new visual state group on `Control`:
`ValidationNone | ValidationError | ValidationSuccess`.

**What drives the transitions is undecided (Q6).** The obvious route is closed, and the audit that closed it
is the useful part of this section.

### 2.1 `ChangeVisualState` cannot carry it

An earlier draft sketched an override of `Control.ChangeVisualState` that would compute and apply the
validation state for any `IInputValidationControl`. The real declaration
(`UI/Xaml/Controls/Control/Control.cs:82`) is:

```csharp
private protected virtual void ChangeVisualState(bool useTransitions)
{
}
```

Three measured problems:

1. **`private protected`, not `protected`.** A control outside the `Uno.UI` assembly *cannot* override it.
   Since 059's D1 makes `IInputValidationControl` public API, this would ship a public interface whose
   visual-state half no public implementer can reach.
2. **The base-call discipline does not hold.** Across `src/Uno.UI`: **31** overrides of
   `ChangeVisualState`, **12** call `base.ChangeVisualState`, **19** do not — and the non-callers are
   precisely the controls that would participate:

   > `Button`, `CheckBox`, `HyperlinkButton`, `RadioButton`, `CalendarDatePicker`, `CalendarView`,
   > `ComboBox`, `ComboBoxItem`, `DatePicker`, `FlipView`, `MenuFlyoutItem`, `MenuFlyoutSubItem`,
   > `SplitMenuFlyoutItem`, `ToggleMenuFlyoutItem`, `RepeatButton`, `ToggleButton`, `RadioMenuFlyoutItem`,
   > `Slider`, `TimePicker`

   A mechanism that silently does nothing for `CheckBox`, `ComboBox`, `Slider`, `DatePicker` and
   `ToggleButton` is not a mechanism.
3. **The obvious fallback fails the same way.** `Control.UpdateVisualState` (`Control.cs:74`,
   `internal virtual`, calling `ChangeVisualState` at `:78`) has 6 overrides, and `TextBox`/`PasswordBox`
   override it *without* calling base — they delegate to `_core.UpdateVisualStateCore`.

No enforcement mechanism rescues either route. Fixing the discipline across 19 controls is possible but is a
much larger change than validation, and it would not fix the `private protected` half.

### 2.2 The recorded candidate — not adopted

Drive the transition directly from the `Validation.HasErrors` changed callback, via
`VisualStateManager.GoToState(control, …)`:

- `GoToState` is `public static` and takes a `Control`, so **no virtual override and therefore no base-call
  discipline** is required from any control, including third-party ones.
- It no-ops harmlessly on a template that lacks the group.
- It is already how the participating controls drive their states — `TextBoxCore.UpdateVisualStateCore` is a
  straight chain of `VisualStateManager.GoToState(owner, "…", true)` calls with no `ChangeVisualState`
  involvement.

This settles **state selection** only. It does **not** settle property writes — see §3, which is the reason
this is recorded as a candidate rather than adopted.

Open sub-question if that route is taken: **re-application after template realization.** The non-virtual
anchor is `Control.ApplyTemplate()` → `InvokeApplyTemplate`; `FrameworkElement.OnApplyTemplate` is
`protected virtual` and has the same base-call problem as `ChangeVisualState`.

## 3. Visual-state contention — `ValidationStates` vs `CommonStates` (Q10)

A second visual state group is independent for **state selection** and shared for **property writes**. Only
the first half is intuitive, and assuming it is the whole story is the trap.

Every `VisualState` write — setter *or* storyboard — lands at
`DependencyPropertyValuePrecedences.Animations`:

| Step | Site |
|---|---|
| `Setter` builds its path with `forAnimations: true` | `UI/Xaml/Setter.cs:240` |
| …which selects the `Animations` precedence | `DataBinding/BindingPath.BindingItem.cs:228` |
| …which stores through `ModifiedValue.SetAnimatedValue` | `UI/Xaml/DependencyPropertyDetails.cs:203` |
| …backed by **one** field, with no record of the writer | `UI/Xaml/ModifiedValue.cs` |

There is therefore no group identity to arbitrate on, and **`ValidationStates` cannot be given priority over
`CommonStates`**. Two distinct failure modes follow:

- **Overwrite.** Both groups write `BorderElement.BorderBrush`; the later write wins, whichever group it came
  from.
- **Collateral clear.** A group leaving a state clears every property that state wrote which the next state
  does not (`UI/Xaml/VisualStateGroup.cs:444-462`) — clearing the shared slot regardless of who filled it.
  `PointerOver → Normal` on the Fluent TextBox, where `Normal` is empty, therefore erases a validation brush
  from a group that never changed state.

**Ordering is not a fix.** `GoToState` early-outs on `object.Equals(originalState, state)`
(`UI/Xaml/VisualStateManager.cs:218`), so re-asserting the state a group is already in applies nothing.
"Apply validation last" corrects the first application only.

**This is WinUI behaviour, not an Uno defect.** `ModifiedValue.cs:62` cites
`CModifiedValue::GetEffectiveValue` by name, and the rollback rules in `VisualStateGroup` are recorded as
WinUI behaviour observed on a dated build. Worth confirming against native WinUI with the
`/winui-runtime-tests` skill before any of options A–C is costed, since the parity claim currently rests on
those comments.

### 3.1 When it bites

Only when `ValidationStates` writes a property `CommonStates` already writes **on the same element**. For the
Fluent TextBox (`TextBox_themeresources.xaml:261-317`) that set is
`BorderElement.{BorderBrush, Background, BorderThickness}`, `ContentElement.Foreground`,
`PlaceholderTextContentPresenter.Foreground` and `HeaderContentPresenter.Foreground`.

**The error presenter in §4 is disjoint** — a new element with its own `Visibility` — so the design as
written is unaffected. The question opens only if the error visual is to recolour the border, the header or
the text.

The existing template already shows the discipline: `CommonStates` owns the brushes, `ButtonStates`
(`:318-331`) owns only `DeleteButton.Visibility` — zero overlap, by construction.

### 3.2 Options — undecided

| | Option | Cost | Notes |
|---|---|---|---|
| **0** | **Keep the property sets disjoint.** Error visuals never touch a `CommonStates`-owned property. | none | What §4 currently proposes. Rules out an error-coloured border. |
| **A** | **Re-apply `ValidationStates` whenever another group changes state**, via the public `VisualStateGroup.CurrentStateChanged` (raised after the target state is applied), forcing re-entry `Valid → Invalid` to defeat the early-out. | ~30 lines in the control; **no template cost** | The only option that delivers real priority, and it keeps working on app and third-party retemplates. Needs a re-entrancy guard; lets the policy be conditional (e.g. `Disabled` beats the error colour), which XAML cannot express. Unverified: that the toggle is visually silent, and the behaviour if a group declares a `VisualTransition` with a duration — the event fires at its *end*, the clear at its *start*. |
| **B** | **Give the two groups different elements** — a dedicated error border overlaying `BorderElement`. | +1 template element | No `CommonStates` edit, no state multiplication; but the overlay must track `BorderThickness` / `CornerRadius`, which `Focused` changes. Unverified: whether an `ElementName` binding follows the *animated* value. |
| **C** | **Fold validation into `CommonStates`** as a cross-product (`PointerOverInvalid`, …). | every participating template rewritten | States multiply with each future concern and every app retemplate breaks. Recorded for completeness; **not recommended**. |

Not viable, for the record: a local `SetValue` appears to win
(`ModifiedValue.cs:69`, `LocalValueNewerThanAnimatedValue`, ported from WinUI) but only until the next state
write resets the flag — a trap worth documenting rather than a mechanism. And `Coercion`, the one precedence
above `Animations`, is fixed at DP registration and so is not addressable per instance.

## 4. Per-control template changes

Each participating control's default template needs an extra row hosting the **error presenter**, its
visibility driven by the `ValidationStates` group. That is the whole requirement.

**This is a smaller change than it sounds, because `Description` is already exactly this pattern.**

The live Fluent TextBox template is `src/Uno.UI.FluentTheme/Resources/Priority02/TextBox_themeresources.xaml`
and is **already a 3-row / 2-column `Grid`** (`:333-337`):

| Row | Content |
|---|---|
| 0 | `HeaderContentPresenter` (`:342`) — `Grid.ColumnSpan="2"`, `Visibility="Collapsed"`, `x:DeferLoadStrategy="Lazy"` |
| 1 | `BorderElement` / `ContentElement` / `PlaceholderTextContentPresenter` / `DeleteButton` |
| 2 | `DescriptionPresenter` (`:349`) — `ContentPresenter`, `Grid.ColumnSpan="2"`, **`x:Load="False"`** |

So the error presenter is a **4th row modelled on row 2**, not a restructure.

`TextBox.Description` is the working analogue, and the pattern is **already replicated across seven
templates** — `TextBox`, `PasswordBox`, `RichEditBox`, `AutoSuggestBox`, `CalendarDatePicker`, `ComboBox`
and `NumberBox`: a `ContentPresenter` with `x:Load="False"`, plus a DP whose changed-callback calls
`FindName` and toggles `Visibility`, as in `TextBoxCore.UpdateDescriptionVisibility`
(`Controls/TextBoxCore/TextBoxCore.cs:785`, first called at `:641`). Copy this.

Existing visual state groups on the Fluent TextBox: exactly two — `CommonStates` (`:261`) and `ButtonStates`
(`:318`).

> **Three file traps when editing templates.**
> - `src/Uno.UI.FluentTheme.v1/` and `.v2/` contain only `bin`/`obj` and are **not tracked source**.
> - `src/Uno.UI/UI/Xaml/Style/mergedstyles.xaml` is an **untracked build artifact** of `Uno.XamlMerge.Task` —
>   do not edit it.
> - The tracked non-Fluent fallback is `src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml` (TextBox
>   `ControlTemplate` at `:236`), which needs the same change.

## 5. `IInputValidationControl.ErrorTemplate`

Held back from spec 059 deliberately: with no templated error presenter it would have had no consumer, and
shipping a public interface with a dead member is worse than adding it here.

```csharp
// sketch — the member 059 omits
public interface IInputValidationControl
{
    DataTemplate ErrorTemplate { get; set; }
    event EventHandler<DataErrorsChangedEventArgs> ErrorChanged;   // shipped in 059
}
```

**Proposed default:** a `TextBlock` whose `DataContext` is the error `IEnumerable`, bound through a
string-joining converter —
`{Binding Converter={StaticResource StringJoinConverter}, ConverterParameter=' '}`.

> **Adding a member to a shipped public interface is a breaking change for implementers.** 059 ships
> `IInputValidationControl` with one member; adding `ErrorTemplate` later must either use a default
> implementation or accept the source break. Decide this when 060 is scheduled — and note it depends on
> Q1 in spec 059 (the element type behind `GetErrors`), since that is what the default template has to
> render.

## 6. Floated, not adopted — `IsRequired`

An `IsRequired` attached property driving a leading `*` indicator has been **raised as a suggestion only**.
It is not part of either spec, and nothing else depends on it. Recorded so the idea and its known objections
are not lost:

- **Q4 — the `*` misaligns labels.** With a plain `bool`, required and non-required fields get different
  leading widths and their labels stop lining up. One candidate: `bool?`, where `true` = show `*`,
  `false` = reserve the space, `null` = occupy no space.
- **Q7 — it would need a dedicated column** in every participating control's template: a per-control cost
  paid for a purely decorative affordance.

## 7. Decisions

| | Decision |
|---|---|
| **D5** | Error placement is fixed by the control template in a first pass; customization means retemplating. |

> **D5 is worth re-opening before implementation.** Weigh it against
> [Avalonia](https://github.com/AvaloniaUI/Avalonia), which reaches configurable placement without an adorner
> layer either: its error presenter is a *control* (`DataValidationErrors`) whose own theme decides
> dock / inline / tooltip. That moves the cost out of every input control's template and makes placement a
> theme swap rather than an API — which also sidesteps §4's per-template edit entirely. Neither WinUI nor Uno
> has the adorner layer WPF's `Validation` depends on, so Avalonia is the closer precedent.

## 8. Open decisions

| | Question |
|---|---|
| **Q6** | What drives the `ValidationStates` transitions. The audit in §2 closes the `ChangeVisualState` and `UpdateVisualState` routes; `GoToState`-from-callback is recorded as a candidate, **not adopted**. Includes the re-application-after-template-realization sub-question. |
| **Q10** | Whether the error visual may recolour the border / header / text at all — and if so, which of options A–C in §3.2 pays for it. Option 0 (disjoint property sets) keeps §4 as written correct. |
| **Q4** | *(only if `IsRequired` is adopted)* the `*` indicator misaligns labels; is `bool?` the answer? |
| **Q7** | *(only if `IsRequired` is adopted)* whether templates need a dedicated column for the indicator. |
| **D5** | Whether placement stays template-fixed, or moves to an Avalonia-style presenter control — see §7. |

Q1, Q3, Q5, Q8, Q9 and Q11 belong to spec 059 and are not restated here.

## 9. References

- [microsoft-ui-xaml-specs#26](https://github.com/microsoft/microsoft-ui-xaml-specs/pull/26) — the withdrawn
  WinUI input-validation spec that `IInputValidationControl` and `ErrorTemplate` borrow from; unmerged
- [WPF `Validation`](https://learn.microsoft.com/dotnet/api/system.windows.controls.validation) — the
  reference implementation, and the source of the attached-property shape; it depends on an adorner layer,
  which neither WinUI nor Uno has
- [Avalonia](https://github.com/AvaloniaUI/Avalonia) — `DataValidationErrors`, a shipping decorator-based
  answer to this layer that works *without* an adorner layer
