# Input validation — presentation

**Status**: Partially implemented — §2, §5 and the error presenter plumbing (§4.1) shipped; §4 (templates) and §3/Q10 remain
**Audience**: Internal engineering (Uno Platform maintainers)
**Created**: 2026-09-22

> Second of two. `specs/059-input-validation/spec.md` is the prerequisite: it delivers the transport layer
> and the bindable read model, which is enough for an app to render its own error text. This spec covers the
> *built-in* visuals — control templates, the error presenter and the validation visual state group.
>
> **§2 has been implemented, and not as this document proposed.** The states are WinUI's own, and the
> `ChangeVisualState` route it declared closed was reopened. §2.0 records what shipped; §2.1 and §2.2 are
> kept verbatim below because the audit in them is what made the shipped design possible, and because two of
> their three findings still hold. §3 is **untouched and still blocking**, and is now known to bite: WinUI's
> error states recolour `BorderElement.BorderBrush`, a property `CommonStates` owns.

## 1. Scope

| In | Out |
|---|---|
| The validation visual state groups (§2) — **shipped**, see §2.0 | Everything in spec 059 — the transport layer, the attached properties, the validation-property attribute |
| Visual-state contention with `CommonStates` (§3) | |
| Per-control template changes and the error presenter (§4) | |
| `ErrorTemplate` (§5) — **shipped** as the `Validation.ErrorTemplate` attached property; rendered by §4.1 into any template that has an `ErrorPresenter` | |
| The floated `IsRequired` indicator (§6) | |

The group names collide with nothing: before implementation `grep -rn "ValidationStates" src` returned zero
hits, and the names that shipped are WinUI's `InputValidationEnabledStates` / `InputValidationErrorStates`,
which were equally absent.

## 2. The validation visual state groups — and what drives them

### 2.0 What shipped (supersedes the proposal below)

The proposal in this section was **one** new group, `ValidationStates`, with
`ValidationNone | ValidationError | ValidationSuccess`. That was **withdrawn**. What shipped is a direct port
of WinUI's `CControl::EnsureValidationVisuals` (`Control.cpp:1858`) — **two** groups:

| Group | States |
|---|---|
| `InputValidationEnabledStates` | `CompactValidationEnabled`, `InlineValidationEnabled`, `ValidationDisabled` |
| `InputValidationErrorStates` | `CompactErrors`, `InlineErrors`, `ErrorsCleared` |

Driven by `protected void Control.UpdateValidationStates()`, with the pre-existing
`Control.EnsureValidationVisuals` stub forwarding to it. Fidelity points worth keeping: transitions are always
applied with `useTransitions: false`; a missing group is silent, as WinUI discards every `GoToState` result;
`InputValidationKind.Auto` is never resolved and behaves as `Compact`; and the disabled branch deliberately
leaves the error group where it was.

**Q6 is answered.** Two framework triggers, plus per-control call sites:

1. The changed handlers of the `Validation.Mode` / `Validation.Kind` / `Validation.HasErrors` attached
   properties, which Uno.UI.Extras owns and which call into `Control` (059 §10d). This covers every
   participant, the four built-in ones and any third-party control, with no per-control code.
2. `FrameworkElement.InvokeApplyTemplate`, immediately after `OnApplyTemplate()` — which also answers §2.2's
   open sub-question about re-application after template realization. Measured by mutation, not assumed: with
   it removed, a control without a per-control call site that reported errors before its template existed
   lands in no state at all. `AutoSuggestBox` is that control, and the runtime test for this trigger uses it.
   A `TextBox` masks this, because its own `UpdateVisualState` call site runs after the template applies.

**On §2.1's three objections.** Objection 1 (`private protected`) is sidestepped rather than solved: nothing
overrides `ChangeVisualState`: controls *call* a `protected`, non-virtual helper, which a third-party control
can also call from wherever it drives its own states. Objection 2 (19 of 31 overrides skip the base call) is
moot for the same reason — every call site is explicit. Objection 3 holds exactly as written: `TextBox` and
`PasswordBox` take the call in their `UpdateVisualState` override instead, which is also where WinUI puts it
(`CTextBoxBase::UpdateVisualState:3591`). `ComboBox` takes it in `ChangeVisualState`, through the ported
`EnsureValidationVisuals`; `AutoSuggestBox` has no call site of its own and relies on the two triggers above.

The participants are WinUI's: the four controls in `CControl`'s type-index switches — `TextBox`,
`PasswordBox`, `AutoSuggestBox` and `ComboBox`. An earlier pass also opted in `NumberBox`, `Slider`,
`ToggleSwitch` and `ToggleButton` (so `CheckBox` and `RadioButton`); those were removed to match WinUI, and
059 §10b records it.

`ErrorTemplate` (§5) shipped with this, now as the `Validation.ErrorTemplate` attached property (059 §10d).
§4.1 renders it.

### 2.1–2.2 The original proposal, as written

The proposal was one new visual state group on `Control`:
`ValidationNone | ValidationError | ValidationSuccess`.

**What drives the transitions was undecided (Q6).** The obvious route looked closed, and the audit that
closed it is the useful part of this section — and is what the shipped design in §2.0 is built on.

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

## 3. Visual-state contention — the validation groups vs `CommonStates` (Q10)

> Written before §2.0 shipped, so it says `ValidationStates` throughout. The argument is unchanged and
> applies to whichever group writes second — read it as `InputValidationErrorStates`, which is the one
> that carries setters in WinUI's templates.

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
visibility driven by the `InputValidationErrorStates` group. That is the whole requirement.

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

### 4.1 What shipped — the ErrorPresenter plumbing

A port of `CControl::EnsureErrors` / `CControl::DeferErrors` (`Control.cpp:1712`). On the first error the
template's `ErrorPresenter` (`ContentPresenter`, normally `x:Load="False"`) is realized and given the loaded
`ErrorTemplate`, whose `DataContext` is the control. `Compact` (and `Auto`) wraps it in the tooltip of
`DefaultCompactErrorIconTemplate`, now in `Style/Generic/SystemResources.xaml` as in WinUI's `generic.xaml`.

Triggers, as in WinUI: the first error (WinUI's `RaiseValidationErrorEvent` check, here the `Validation.HasErrors`
changed handler), `Validation.Mode` changed, and `Validation.ErrorTemplate` changed. Changing `Validation.Kind`
only refreshes the states, as in WinUI, so the content keeps its shape until the next first error.

Deviations:

- **Template application also calls it**, from the same `InvokeApplyTemplate` anchor as §2.0. WinUI does not,
  so errors reported before the template exists never reach its presenter there. Proven by mutation.
- **`DeferErrors` does not defer.** Uno has no `TryDefer`, and in Uno a name lookup realizes a stub, so it only
  re-applies the states; the `InputValidationErrorStates` group is what hides the presenter.

None of the built-in templates has an `ErrorPresenter` yet, so this is inert in the shipping styles until the
template work above lands.

## 5. `IInputValidationControl.ErrorTemplate`

> **Shipped differently.** There is no `IInputValidationControl` any more: `ErrorTemplate` is an attached
> property on `Uno.Extras.Input.Validation`, alongside the rest of the surface (059 §10d). The sketch below is
> the original proposal.

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
| ~~**Q6**~~ | **Answered — see §2.0.** `Control.UpdateValidationStates`, called from three framework triggers plus explicit per-control call sites. The re-application sub-question is answered by the `InvokeApplyTemplate` anchor. |
| **Q10** | Whether the error visual may recolour the border / header / text at all — and if so, which of options A–C in §3.2 pays for it. **Now a live question, not a hypothetical**: WinUI's own `CompactErrors` and `InlineErrors` both set `BorderElement.BorderBrush`, which `CommonStates` also writes, so porting its templates verbatim walks straight into §3. Option 0 is no longer available if parity is the goal. |
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
