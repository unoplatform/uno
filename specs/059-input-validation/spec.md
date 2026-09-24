# Input validation — transport and read model

**Status**: Implemented — see §11 for what the code corrected about this document
**Audience**: Internal engineering (Uno Platform maintainers)
**Created**: 2026-09-22

> First of two. This one closes the **transport** gap — errors travelling from an `INotifyDataErrorInfo`
> source to the control that bound to it — and defines the **bindable read model** an app renders them with.
> `specs/060-input-validation-presentation/spec.md` covers the built-in visuals: control templates, the error
> presenter and the `ValidationStates` visual state group. It is deferred, and 059 does not depend on it.

## 1. Why

Neither WinUI nor Uno has an input-validation story. The canonical WinUI request
([microsoft-ui-xaml#179](https://github.com/microsoft/microsoft-ui-xaml/issues/179)) has been open since
2019; the spec that would have answered it
([microsoft-ui-xaml-specs#26](https://github.com/microsoft/microsoft-ui-xaml-specs/pull/26)) was never merged
and its preview implementation was withdrawn before GA; the Uno-side request
([unoplatform/uno#4839](https://github.com/unoplatform/uno/issues/4839)) is closed as `blocked/missing-api`.
Apps hand-build the entire presentation layer today.

Validation splits into three layers, and it is worth separating them because only one is actually missing
anything hard:

| Layer | What it does | Status | Proposed work |
|---|---|---|---|
| **1. Production** | the model decides what is invalid | ✅ ships in the BCL, works on Uno today | **none** |
| **2. Transport** | errors travel from model to control | ❌ missing | this spec, §3 |
| **3. Presentation** | the control shows them | ❌ missing | §4 (read model) here; visuals in spec 060 |

**The guiding constraint: the core depends on `INotifyDataErrorInfo` and nothing else.** No base class, no
attribute set and no MVVM toolkit is required of the app author. Everything in §6 is *one* example of how to
satisfy that interface, not a prescription.

### What this slice actually delivers

Nothing appears on screen by itself. That is deliberate, and it should not be mistaken for the slice doing
nothing: after 059, an app binds to a view model that implements `INotifyDataErrorInfo`, sets one attached
property on the control, and its errors arrive on `Validation.HasErrors` / `Validation.Errors` where its own
markup can render them:

```xml
<!-- xmlns:uno="using:Uno.UI.Xaml.Controls" -->
<StackPanel>
    <TextBox x:Name="UserNameBox" Text="{Binding UserName}" uno:Validation.IsEnabled="True" />
    <TextBlock Foreground="Red"
               Visibility="{Binding (uno:Validation.HasErrors), ElementName=UserNameBox,
                                    Converter={StaticResource BoolToVisibilityConverter}}"
               Text="{Binding (uno:Validation.Errors), ElementName=UserNameBox,
                              Converter={StaticResource StringJoinConverter}}" />
</StackPanel>
```

**Both converters are the app's**, not the framework's — 059 ships no converters, and `Validation.HasErrors`
is a `bool` while `Validation.Errors` is an `IEnumerable` snapshot of `GetErrors` with its element type
untouched (see [Q1](#9-open-decisions)). Supplying them is the cost of this slice rendering nothing on its
own.

That is the independently testable MVP. Spec 060 replaces the hand-written `TextBlock` with a templated
error presenter, and supplies a default template so the converters are no longer the app's problem; it does
not change anything 059 establishes.

### Two related static classes, up front

Two unrelated static classes in this document sit next to each other, and are deliberately named apart:

- **`Uno.UI.FeatureConfiguration.InputValidation`** — the global switch and the public validation-property
  map (§3.4).
- **`Validation`** — the attached-property owner holding `IsEnabled` / `HasErrors` / `Errors` (§4.1). Which
  namespace *this* one lands in is [Q3](#9-open-decisions).

They are named for the same feature and are otherwise unrelated — the `InputValidation` prefix on the first
keeps the two from reading as one type in two places.

## 2. Layer 1 — Production

Already shipped with .NET, already functional on Uno. The contract:

```csharp
public interface INotifyDataErrorInfo
{
    bool HasErrors { get; }
    IEnumerable GetErrors(string? propertyName);
    event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
}

public class DataErrorsChangedEventArgs(string? propertyName) : EventArgs;
```

**No work is proposed at this layer.** It does, however, leave one question that this spec cannot dodge:
`GetErrors` returns the **non-generic** `IEnumerable`, so the element type is unconstrained. Because 059
ships no default error template, whatever lands in `Validation.Errors` *is* the app-facing contract — see
[Q1](#9-open-decisions), which is first on the list for that reason.

`grep -rn "INotifyDataErrorInfo" src` returns **zero hits**. There is no existing support to conflict with.

## 3. Layer 2 — Transport

Nothing in WinUI or Uno currently notices that a binding source implements `INotifyDataErrorInfo`. This
layer closes that gap. It has four parts: where participation is decided, where the source is resolved, how
a control declares which of its properties is the validation property, and where the global state lives.

### 3.1 Participation — `DependencyPropertyDetailsCollection.SetBinding`

XAML-declared bindings are not a separate path; they compile into `SetBinding` calls:

| Binding kind | What reaches the runtime |
|---|---|
| `{Binding}` on a dependency property | `SetBinding(<Owner>.<Name>Property, new Binding() { … })` |
| `{Binding}` on a POCO / non-DP | `GetDependencyObjectForXBind().SetBinding("<Name>", new Binding() { … })` |
| `{x:Bind}` | the same `new Binding()`, decorated by `BindingHelper.SetBindingXBindProvider(…)` — which returns the `Binding`, passed to the same `SetBinding` |
| Runtime / dynamic XAML / manual | the app calls `SetBinding` itself |

There are **two sibling `SetBindingInternal` overloads and the string one does not delegate to the
`DependencyProperty` one** — `UI/Xaml/DependencyObject.Binder.cs:320` and `:360`:

| | DP overload (`:320`) | string overload (`:360`) |
|---|---|---|
| Resolves the DP | already has it | itself, at `:377-379`, `DependencyProperty.GetProperty` ?? `FindStandardProperty` |
| Accepts `ResourceBinding` | ✅ `:338-342` | ❌ throws `NotSupportedException` `:388` |
| Next call | `_properties.SetBinding(…)` | `_properties.SetBinding(…)` — **directly** |
| Null DP | throws | silently no-ops |

So `SetBinding` is *not* a single chokepoint. Both overloads meet one level down, at
`UI/Xaml/DependencyPropertyDetailsCollection.Bindings.cs:148`:

```csharp
internal void SetBinding(DependencyProperty dependencyProperty, Binding binding, ManagedWeakReference target)
{
    if (GetPropertyDetails(dependencyProperty) is DependencyPropertyDetails details)
    {
        details.ClearBinding();
        var bindingExpression = new BindingExpression(viewReference: target, targetPropertyDetails: details, binding: binding);
        details.SetBinding(bindingExpression);
        _bindings = _bindings.Add(bindingExpression);
        …
    }
}
```

This holds, simultaneously, the `DependencyProperty`, the concrete `Binding`, the owner weak-ref, the
`DependencyPropertyDetails` and the new `BindingExpression`. It also **structurally excludes
`ResourceBinding`**: those divert at `DependencyObject.Binder.cs:338-342` into a disjoint
`_resourceBindings` collection (`DependencyObject.Store.cs:61`, read via `GetResourceBindingsForProperty`,
`:2264`) and never produce a `BindingExpression` at all. No filtering is needed — it is impossible by
construction.

> **Why this matters beyond tidiness.** WinUI's preview implementation applied validation for `{x:Bind}` but
> not `{Binding}` ([microsoft-ui-xaml#4642](https://github.com/microsoft/microsoft-ui-xaml/issues/4642)).
> Both kinds converge here, so that class of asymmetry is structurally impossible rather than merely
> tested-for.

### 3.2 Resolution — `BindingExpression.OnValueChanged`

Registration and resolution are two jobs with different lifetimes, and conflating them is the trap.

**The source is the binding's leaf, not the `DataContext`.** For `{Binding Customer.Name}` the
`INotifyDataErrorInfo` is `Customer`, not the page's view model. Uno already distinguishes the two:

| | Accessor | Meaning |
|---|---|---|
| Root | `BindingExpression.DataContext` (`DataBinding/BindingExpression.cs:52`, **public**) | the page VM |
| **Leaf** | `BindingExpression.DataItem` (`:87`, **public**) → `BindingPath.DataItem` | ✅ the `INotifyDataErrorInfo` |
| Leaf property name | `BindingPath.LeafPropertyName` (`DataBinding/BindingPath.cs:223`, `internal`) | what to filter `ErrorsChanged.PropertyName` on |

`BindingPath.GetTargetContextAndPropertyName()` (`:134`) returns both as a tuple, and `GetPathItems()`
(`:129`) exposes the chain as public `IBindingItem`.

> **Correction — those two accessors are `{Binding}`-only.** For `{x:Bind}`, `ParentBinding.Path` is null and
> `ApplyBinding` takes the `_updateSources` branch (`DataBinding/BindingExpression.cs:576-600`), which never
> calls `_bindingPath.SetWeakDataContext`. `_bindingPath` therefore stays empty and both `DataItem` and
> `LeafPropertyName` are **null** for a compiled binding. Reading them alone would have reproduced
> [microsoft-ui-xaml#4642](https://github.com/microsoft/microsoft-ui-xaml/issues/4642) *in reverse* — the
> asymmetry §3.1 claims is structurally impossible — so leaf resolution reads `_updateSources` when present
> and falls back to `_bindingPath`. `OnValueChanged` itself does fire for both binding kinds, so the funnel
> below is sound; only the leaf accessors needed the extra branch. Several update sources have no single
> leaf, and validation is skipped there.

**The leaf is not resolvable at binding-set time.** Generated XAML calls `SetBinding` inside the `XamlApply`
lambda, which is the collection-initializer element expression for `Children` — C# evaluates it fully
*before* `Children.Add(…)`. At that moment the control is parentless, outside the visual tree, and its
`DataContext` is `null`. `{x:Bind}`'s `ApplyXBind` runs later still, on `Loading`. The timeline for one
element:

> ctor → **`SetBinding`** (parentless, DC null) → `CreationComplete()` → `Children.Add(…)` → DataContext
> inheritance → `ApplyDataContext` → **leaf resolves** → `Loading` → `ApplyXBind()` /
> `ApplyElementNameBindings()` → `ApplyTemplate`

The leaf can also change again later — `Customer` gets replaced. So resolution needs a funnel that fires
every time the path re-resolves, and that funnel is `BindingExpression.OnValueChanged(object)`
(`DataBinding/BindingExpression.cs:637`, `internal`), which every path re-resolution passes through:

- initial resolution and `DataContext` arrival — `BindingPath.OnDataContextChanged` →
  `_chain.SetWeakDataContext(…)` → `BindingItem.OnDataContextChanged`
- mid-chain replacement — `BindingItem.OnPropertyChanged` → `Next.DataContext = newValue` → the same cascade
- `RaiseValueChanged` → `Path?.OnValueChanged(newValue)` → `Expression?.OnValueChanged(o)`

**Despite its name, `OnValueChanged` does not have "the value actually changed" semantics.**
`BindingItem.OnDataContextChanged` raises unconditionally on the non-null branch, and `RaiseValueChanged(null)`
on the null branch; only the downstream DP write is elided when equal. The source says so, at
`DataBinding/BindingPath.BindingItem.cs:200-203`:

```csharp
// We should call RaiseValueChanged even if oldValue == newValue.
// It's the responsibility of the user to only raise PropertyChanged event when needed.
// Not calling RaiseValueChanged when oldValue == newValue is a bug because a sub-property could
// have changed, and it can have an effect when applying the binding, for converters for example.
```

That property is what makes this hook survive the case that defeats a pure-pull design: if the view model's
initial value equals the DP default — `TextBox.Text` defaults to `string.Empty` — no value change ever
fires, so a control polling `GetBindingExpression(…)` from its own DP-changed callback would **never
subscribe at all**. The null branch doubles as the unsubscribe signal.

**No push notification exists today.** No event, virtual or callback fires when a binding is set or when its
resolved source changes, anywhere in `DependencyObject`, `DependencyPropertyDetailsCollection`,
`BindingExpression`, `BindingPath` or `BindingItem`. `BindingExpression` declares zero events and zero
virtuals. Adding one is the work of this layer.

**Getting the `BindingExpression` needs no new API.**
`FrameworkElement.GetBindingExpression(DependencyProperty)` is **public in Uno**
(`UI/Xaml/FrameworkElement.mux.cs:68`), unlike WinUI which omits it entirely. Uno's own code already uses
this shape for `UpdateSourceTrigger` in `Controls/TextBox/TextBox.Host.cs`.

#### Sketch

```csharp
// sketch — the control owns the subscription, and the returned handle is the unsubscribe
private IDisposable RegisterValidation(BindingExpression expression);
//   1. resolve the INotifyDataErrorInfo from the binding's LEAF source (BindingExpression.DataItem)
//   2. remap its state onto the Validation.* attached properties
//   3. subscribe to ErrorsChanged, filtered on BindingPath.LeafPropertyName
//   4. run once immediately, so initial state is correct
```

> **Design note — the control syncs, not the binding engine** ([D4](#7-decisions-locked)).
> `Validation.HasErrors` and `Validation.Errors` are populated *by the control*, which owns the resolution.
> This keeps the read model bindable from XAML without teaching the binding engine about validation, and it
> leaves each control to decide which of its properties is the validation property.

#### Lifecycle hazards any implementation must handle

1. **Rebinding silently replaces.** `Bindings.cs:151` `details.ClearBinding()` disposes the prior
   `BindingExpression` with no notification. **Never cache the expression object.**
2. ~~**Setting a local value clears the binding**~~ — **it does not.** `TryClearBinding`
   (`DependencyObject.Store.cs:785-797`) only acts on `UnsetValue`, i.e. `ClearValue`, not `SetValue`; and a
   `TemplatedParent` binding survives even that.
3. ~~**Template recycling suspends and resumes bindings**~~ — **it does not.** `SuspendBindings` /
   `ResumeBindings` have **zero production callers** (only `PhaseBindingTests.cs`). `FrameworkTemplatePool`
   uses `IsRecycling` as a *write-suppression* guard (`BindingExpression.cs:249`, `:278`); the live
   suspend/resume path is the compiled-binding one, `SuspendCompiledSource` / `ApplyCompiledSource`.
4. **`GetBindingExpression` materializes a details entry** as a side effect of `GetPropertyDetails`.
5. **The binding collection is append-only.** `_bindings.Add` is its only mutation; `ClearBinding()` disposes
   the replaced expression but leaves it in the list, still iterated by `ApplyDataContext`. A stale
   expression can therefore still reach the hook, so teardown must verify the expression that established
   the subscription still owns it.
6. **Teardown is silent.** `ClearBinding()` disposes the expression, whose subscription nulls
   `_bindingPath.Expression` first, so `OnValueChanged` can never signal the removal. Rebinding survives only
   because `SetBinding` re-registers immediately; `ClearValue` has no follow-up and would orphan the
   subscription. Both funnel through `DependencyPropertyDetails.ClearBinding()`, which is where the teardown
   hook belongs.

### 3.3 Declaring the validation property — by attribute

A control has many dependency properties and at most one of them is "the input". Something has to say which.

**Decided ([D8](#7-decisions-locked)): a new Uno attribute, `InputValidationPropertyAttribute`, on the control
type, resolved once per type and cached.** The lookup answers one question at the hook site — *is this
`DependencyProperty` the validation property of this target's type?* — and the machinery for both halves
already exists.

#### Why not `InputPropertyAttribute` — it already means something else

It is **shipped WinUI API that Uno already mirrors**, hand-written at
`UI/Xaml/Controls/InputPropertyAttribute.cs`:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public partial class InputPropertyAttribute : Attribute
{
    public string Name;      // a public FIELD, not a property — a WinRT projection artifact
}
```

Its own documentation carries the semantic: it indicates "which property of a type is the XAML **input
property**", used by "a XAML processor … when processing XAML **child elements**". That is markup-parsing
metadata — where a nested element lands — not *the property the user types into*. The two coincide on
`TextBox`, which is what makes the name inviting, and they diverge on precisely the control that already
carries it: `Controls/ComboBox/ComboBox.cs:13` declares `[InputProperty(Name = "Text")]`, correct as XAML
metadata and wrong as a validation annotation for a non-editable `ComboBox`.

So it is **not reused, not extended, and not consulted as a fallback** ([D9](#7-decisions-locked)) — it is
left untouched for WinUI metadata parity, still applied exactly once and still read by nothing. (The
generated file `Generated/3.0.0.0/Microsoft.UI.Xaml.Controls/InputPropertyAttribute.cs` does exist; the
SyncGenerator skip-list suppresses its *members*, because they are hand-declared, not the type.)

#### The new attribute — `InputValidationPropertyAttribute`

```csharp
// sketch
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class InputValidationPropertyAttribute : Attribute
{
    public InputValidationPropertyAttribute(string name) => Name = name;

    public string Name { get; }
}
```

Applied as `[InputValidationProperty(nameof(Text))]`. `grep -rn "InputValidationProperty" src` returns
**zero hits**, so the name collides with nothing in the repo. Which namespace it lands in is the
surviving half of [Q11](#9-open-decisions).

Two deliberate departures from the attribute it is *not* reusing:

- **`Name` is a get-only property set through the constructor, not a public field.** The field on
  `InputPropertyAttribute` exists because that type is a WinRT projection; a Uno-only attribute has no
  reason to inherit the artifact.
- **`AllowMultiple = false` — one validation property per type** ([D9](#7-decisions-locked)). This is a
  "for now", not a claim that no control will ever need two: `ComboBox` is the known candidate, since an
  editable one arguably validates `Text` *and* `SelectedItem`. Widening `false → true` later is source- and
  binary-compatible for every type that applied the attribute once — and until that happens, §3.4's singular
  `Type → DependencyProperty?` cache is the honest shape rather than a list silently truncated to its first
  entry. **The widening is not free on the resolution side, though**, and the spec should not pretend
  otherwise: `AllowMultiple = true` turns the override semantics described below into additive ones. Measured
  on the same probe that backs the shadowing claim below —
  `GetCustomAttribute<T>(inherit: true)` *throws* `AmbiguousMatchException` on a derived
  type that re-declares, and `GetCustomAttributes<T>(inherit: true)` returns base **and** derived (2, not 1).
  So a future widening must also stop using `GetCustomAttribute` and pick the most-derived declaring type
  itself. Reversible, with that string attached.

**`Inherited = true`, under a convention: apply it to the type that owns the input, never to a shared base
that has non-validating subclasses.** Inheritance is what lets a third-party `MyTextBox : TextBox` validate
without re-declaring anything. The convention is what keeps the argument below intact — the attribute must
not sit on `RangeBase` (that would drag in `ProgressBar`) or on `Selector` (that would drag in `FlipView`).
A derived type that *does* re-declare it shadows its base: with `AllowMultiple = false`,
`Type.GetCustomAttribute<T>(inherit: true)` returns the most-derived instance and exactly one — measured on
a .NET 10 scratch probe over a three-type hierarchy, not assumed.

There is deliberately **no opt-out sentinel** for "my base declares one and I have none". An empty `Name` is
the escape hatch if a future base type ever forces the question, but the reason it is unlikely to is
[D2](#7-decisions-locked): `Validation.IsEnabled` defaults to `false` and gates registration and sync, so an
over-reaching attribute costs a control that never opted in exactly nothing. The attribute answers *which
property, if you validate*; `IsEnabled` answers *whether*.

#### Why an attribute, and not a metadata flag

An earlier draft of this design proposed a `FrameworkPropertyMetadataOptions.IsValidationInput` bit hoisted
into `DependencyProperty._flags`. **That route is considered and rejected**, and the `FrameworkPropertyMetadataOptions`
enum is left untouched. The decisive reason is granularity, and it runs the opposite way to first intuition:

**A `DependencyProperty` is registered once for a whole inheritance branch, so a per-DP flag is
per-DP-global.** Flagging `Selector.SelectedItemProperty` would flag `ComboBox`, `ListBox`, `ListView`,
`GridView` **and** `FlipView` together; `RangeBase.ValueProperty` would flag `Slider` **and**
`ProgressBar`. Uno has no
`DependencyProperty.OverrideMetadata`, so there is no per-type escape: `DependencyProperty` holds a single
`_ownerTypeMetadata` and computes `_flags` once in its constructor, and the only per-type divergence in
`GetMetadata(Type)` is `CloneWithOverwrittenDefaultValue` — **default value only**.

An attribute on the **type** resolves per type. `Slider` declares `Value`; `ProgressBar` declares nothing.
Class-level granularity is the attribute's *strength* on the axis that actually matters here.

**This rests on `DependencyProperty.GetProperty` walking the base-type chain, which it does.**
`InternalGetProperty` (`UI/Xaml/DependencyProperty.cs:408`) loops
`do { _registry.TryGetValue(type, name, …); type = type.BaseType; } while (…)`, commented *"Dependency
properties are inherited"* (`:418-430`). So `GetProperty(typeof(Slider), "Value")` resolves
`RangeBase.ValueProperty` even though the attribute sits on `Slider` and the registration on `RangeBase`.
Without that walk this whole argument would collapse.

Two further costs the flag was supposed to avoid turn out not to exist:

- **The name → DP half needs no new code and has no lazy-registration hazard.**
  `DependencyProperty.GetProperty(Type, string)` (`:371`) is already memoised in `_getPropertyCache`, and
  its miss path already calls `ForceInitializeTypeConstructor` (`:473`, `RuntimeHelpers.RunClassConstructor`)
  before looking up the registry — so a DP whose static constructor has not run yet cannot produce a
  spurious "no validation property" answer that then gets cached. The string `SetBindingInternal` overload
  already uses exactly this call at `:377`.
- **The AOT/trimming objection largely dissolves.** Retrieving a custom attribute from a `Type` is
  NativeAOT-safe and needs no `DynamicallyAccessedMembers` annotation, and the name resolution goes through
  the cached `GetProperty` rather than `Type.GetProperty`.

#### Per-control cost: one attribute per participating type

No `DependencyProperty.Register` call is edited at all. The registration sites below are cited to name the
properties, and to show the inheritance problem the per-type attribute solves:

| Property | Registration site | Declared once, used by |
|---|---|---|
| `TextBox.Text` | `Controls/TextBox/TextBox.cs:137` | `TextBox` |
| `ToggleButton.IsChecked` | `Controls/Primitives/ToggleButton.cs:53` | `CheckBox`, `RadioButton` — all validating, so the attribute can sit on `ToggleButton` itself |
| `Selector.SelectedItem` | `Controls/Primitives/Selector.cs:89` | `ComboBox`, `ListBox`, `ListView`, `GridView`, `FlipView` ❌ — leaf types only |
| `RangeBase.Value` | `Controls/Primitives/RangeBase.Properties.cs:115` | `Slider` ✅, `ProgressBar` ❌ — leaf types only |

`ToggleSwitch` is **not** in that second row: it derives from `Control`, not `ToggleButton`
(`Controls/ToggleSwitch/ToggleSwitch.cs:12`), and declares its own `IsOn` (`:108`). It is a separate
annotation, not an inheritance case.

Which of these participate in the first release is [Q8](#9-open-decisions).

### 3.4 Global state — `FeatureConfiguration.InputValidation`

A new nested class in `src/Uno.UI/FeatureConfiguration.cs`. That file already hosts ~30 such classes,
including `BindingExpression` (`:259`) and `DependencyProperty` (`:634`), so this is the established shape —
and it keeps the global switch inside `Uno.UI`, with no dependency on the app builder, which lives outside
this repo.

It holds two things:

**1. The app-wide switch.** The registration hook sits on a path every binding in the app passes through, so
an off position means apps that never validate pay nothing for the machinery. See [Q9](#9-open-decisions)
for whether a chainable `UseInputValidation()` is *also* offered as a convenience over it.

**2. The validation-property map.** Four constraints on its shape:

- **Key it `Type → DependencyProperty?`, not `HashSet<DependencyProperty>`.** A set of DPs would
  reintroduce exactly the per-DP-global problem §3.3 exists to fix — `RangeBase.ValueProperty` in the set
  means `ProgressBar` participates. **Cache the negative answer too**, or every non-participating control
  re-walks its attributes on every value change. The *singular* value is `AllowMultiple = false`
  ([D9](#7-decisions-locked)) showing through: if the attribute is ever widened, this widens to a list with
  it — and since the map is public (third bullet), that widening is a binary break where a method over
  an internal map would have absorbed it. That is the accepted price of the extensibility.
- **UI-thread-affine, not concurrent.** `GetProperty` *throws* off the UI thread unless
  `FeatureConfiguration.DependencyProperty.DisableThreadingCheck` is set (`DependencyProperty.cs:373-376`),
  and its own `_getPropertyCache` is guarded by a shared mutable `_searchPropertyCacheEntry` static — the DP
  system already assumes single-threaded access, and the new cache should follow that discipline rather than
  invent a concurrent one. **The distinction that matters: DP resolution is UI-thread-bound, but
  `ErrorsChanged` is not** — a view model may raise it from a background thread, so the error *sync* needs a
  dispatch hop that the *resolution* does not.
- **Expose the map; do not wrap it in a method.** A control that cannot carry the attribute — sealed, or
  from a library that does not reference Uno — still has to be able to take part, and the map *is* that
  registration entry point rather than something bolted next to it:
  `FeatureConfiguration.InputValidation.ValidationProperties[typeof(X)] = X.TextProperty`, with `null` as the
  explicit opt-out and `Remove` as the undo. A write supersedes an already memoized answer, so a registration
  is not hostage to whether something read the type first. The type stays sealed with an internal constructor
  and offers no `Clear()` — a process-wide registration surface that any library could wipe is not worth the
  convenience.
- **Do not pin types across a collectible ALC unload.** A plain `static Dictionary<Type, …>` in `Uno.UI`
  roots every key type and its assembly, which is the class of leak `specs/044-alc-memory-leak-fixes` and
  `specs/048-hotreload-collectible-alc-agent-teardown` exist to prevent. Either key weakly, or clear on ALC
  teardown.

### 3.5 Existing stubs to build on

`UI/Xaml/Controls/Control/Control.mux.cs:251` and `:256` already reserve the shape, both empty:

```csharp
private protected void EnsureValidationVisuals()                              { /* TODO Uno: Not supported yet #4839 */ }
private protected void InvokeValidationCommand(object control, string value)  { /* TODO Uno: Not supported yet #4839 */ }
```

and `ComboBox.partial.mux.cs:369` / `:1388` already call them. They are the only pre-existing footprint.

## 4. Layer 3 — the public read model

The half of the presentation layer that ships in 059. The visuals are spec 060.

### 4.1 The `Validation` attached-property owner — superseded

> **Superseded, see [§10b](#10b-superseded-by-the-winui-alignment-pass).** This read model never shipped as
> attached properties: every member of `IInputValidationControl` is a dependency property the participating
> control registers itself, and the `Validation` class named below no longer exists — its transport folded
> into `Control`. The section is kept because §10b's decisions are written against it.

A static class owning attached dependency properties, scoped to `Control`.

| Property | Role |
|---|---|
| `IsEnabled` | **defaults to `false`** — validation is opt-in, per control ([D2](#7-decisions-locked)) |
| `HasErrors` | read model, synced from `INotifyDataErrorInfo` by the control |
| `Errors` | read model, synced from `INotifyDataErrorInfo` by the control |

**In this slice, `Validation.IsEnabled` gates registration and sync — not visuals.** There are no built-in
visuals yet, so turning it on costs a subscription and populates two attached properties; nothing more.
Whether the class lives in the default xmlns or behind a prefix is [Q3](#9-open-decisions).

Per-control granularity comes from `IsEnabled`, which is why §3.3's per-type attribute is sufficient: the
attribute says *which property could be validated*, `IsEnabled` says *whether this instance does*.

### 4.2 `IInputValidationControl`

Concept borrowed from the withdrawn WinUI spec
([microsoft-ui-xaml-specs#26](https://github.com/microsoft/microsoft-ui-xaml-specs/pull/26)). **Public API**
([D1](#7-decisions-locked)), so third-party controls can participate.

```csharp
// sketch
public interface IInputValidationControl
{
    event EventHandler<DataErrorsChangedEventArgs> ErrorChanged;
}
```

**The event reuses the BCL handler type** — `EventHandler<DataErrorsChangedEventArgs>`, exactly as on
`INotifyDataErrorInfo` ([D7](#7-decisions-locked)). No new delegate or args type is introduced. The control
re-broadcasts the shape it consumed, and `DataErrorsChangedEventArgs.PropertyName` survives the hop, which
is what lets a consumer tell *which* property changed when one source feeds several controls. The spelling
`ErrorChanged` vs `ErrorsChanged` is [Q5](#9-open-decisions); the handler type is settled.

> **`DataTemplate ErrorTemplate` is deliberately absent.** The withdrawn WinUI spec put it on this
> interface, and spec 060 adds it. It is held back here because with no templated error presenter it would
> have no consumer, and shipping a public interface with a dead member is worse than adding the member
> later — adding one is source- and binary-compatible for implementers only if it is defaulted, so 060 must
> either default it or accept the break. Called out so the omission reads as a decision rather than an
> oversight.

## 5. Out of scope — deferred to spec 060

Everything template- and visual-shaped, per the split at the top of this document:

- Per-control template changes: the extra row hosting the error presenter, in
  `src/Uno.UI.FluentTheme/Resources/Priority02/*.xaml` and `src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml`.
- The `ValidationStates` visual state group, and what drives it (`Control.ChangeVisualState` is
  `private protected`, so the obvious route is closed to public implementers).
- Visual-state contention between `ValidationStates` and `CommonStates`, which share one animated-value slot
  per property.
- `IInputValidationControl.ErrorTemplate` and its proposed default.
- `IsRequired` and the `*` indicator — floated as a suggestion, not adopted by either spec.
- Error-placement policy (the deferred **D5**).

`grep -rn "ValidationStates" src` returns **zero hits**, so the group name collides with nothing when 060
picks it up.

## 6. Consuming it

> Everything below is **one** way to satisfy `INotifyDataErrorInfo` ([D3](#7-decisions-locked)). The proposed
> API knows nothing about these shapes. An app could equally implement the interface through a source
> generator, a decorator, attributes, a helper class, a base class such as CommunityToolkit's
> `ObservableValidator`, or entirely by hand.

### Hand-rolled

```csharp
// sketch
class SignUpViewModel : INotifyDataErrorInfo
{
    Dictionary<string, List<?>> ValidationErrors;   // element type: see Q1

    public bool HasErrors => ValidationErrors.Any(kvp => kvp.Value.Any());

    public IEnumerable GetErrors(string? propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? ValidationErrors.SelectMany(kvp => kvp.Value)
            : ValidationErrors.GetValueSafe(propertyName);

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    void Validate([CallerMemberName] string? propertyName = null)
    {
        // ...rules; on change: ErrorsChanged?.Invoke(this, new(propertyName));
    }

    public string Property1 { get; set => { field = value; Validate(); } }
}
```

### DataAnnotations

```csharp
// sketch
class SignUpViewModel2 : /* ObservableValidator, or any INotifyDataErrorInfo impl */
{
    [Required]
    [Length(5, 10)]
    [RegularExpression(@"[a-zA-Z0-9#@\-]+")]
    public string UserName { get; set; }
}
```

### The page

```xml
<TextBox Text="{Binding Property1}" uno:Validation.IsEnabled="True" />
```

That is the whole opt-in: one attached property on the control, and a binding whose source implements
`INotifyDataErrorInfo`. Q3 resolved in favour of the prefixed spelling, over
`xmlns:uno="using:Uno.UI.Xaml.Controls"`. Rendering the errors is the app's job in this slice — see §1.

## 7. Decisions (locked)

| | Decision |
|---|---|
| **D1** | `IInputValidationControl` is **public** API, not internal — minus `ErrorTemplate`, which is deferred to spec 060 (§4.2). |
| **D2** | `Validation.IsEnabled` defaults to `false` — validation is opt-in, per control. In this slice it gates registration and sync, not visuals. |
| **D3** | The core depends on `INotifyDataErrorInfo` **only**. No base class or attribute set is mandated of the app author. |
| **D4** | The **control** syncs `HasErrors` / `Errors` from `INotifyDataErrorInfo`, for ease of binding — not the binding engine. |
| **D6a** | **Participation** is decided at `DependencyPropertyDetailsCollection.SetBinding` (`Bindings.cs:148`) — the point where both `SetBindingInternal` overloads converge, and where `ResourceBinding` is already structurally excluded. |
| **D6b** | **Resolution** is a separate job with a different lifetime. The `INotifyDataErrorInfo` is the binding's **leaf** (`BindingExpression.DataItem`), not the root `DataContext`, and it is not resolvable at binding-set time. It is (re)resolved through `BindingExpression.OnValueChanged`. |
| **D7** | `IInputValidationControl`'s event reuses the BCL handler type, `EventHandler<DataErrorsChangedEventArgs>` — no new delegate or args type. |
| **D8** | The validation property is declared by **`[InputValidationProperty]` on the control type**, resolved through the existing `DependencyProperty.GetProperty(Type, string)` and memoized in `FeatureConfiguration.InputValidation.ValidationProperties`, a **public, writable** `Type → DependencyProperty?` map that doubles as the registration entry point for controls which cannot carry the attribute (§3.4). A `FrameworkPropertyMetadataOptions` flag was **considered and rejected** (§3.3): a DP is registered once per inheritance branch and Uno has no `OverrideMetadata`, so a per-DP flag cannot distinguish `Slider` from `ProgressBar`. The enum is left untouched. |
| **D9** | That attribute is **new and Uno-owned** — `InputValidationPropertyAttribute`, `AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)`, with `Name` as a constructor-set property. WinUI's `InputPropertyAttribute` is **neither reused nor honoured as a fallback**: it carries a different meaning (XAML child-element processing), and `ComboBox` already applies it for that meaning, which a fallback would misread as a validation opt-in (§3.3). `AllowMultiple = false` is a *for now* — `ComboBox` (`Text` **and** `SelectedItem`) is the case that would trigger a revisit, and widening `false → true` later is source- and binary-compatible for every type that applied it once. `Inherited = true` so that a third-party `MyTextBox : TextBox` participates unchanged, under the convention that the attribute is applied to the type owning the input and never to a shared base with non-validating subclasses. |
| **D5** | *Deferred to spec 060* — error placement fixed by the control template in a first pass. |

## 8. Suggested sequencing

1. `FeatureConfiguration.InputValidation` — the switch and the map (§3.4). Self-contained, no behaviour change.
2. `InputValidationPropertyAttribute` and its resolution (§3.3), with unit tests over the `Slider`/`ProgressBar`
   and `ComboBox`/`FlipView` inheritance cases — those are what the flag route could not express, so they
   are the tests that justify the decision — plus one that a derived type's own attribute shadows its
   base's, which is what `Inherited = true` rests on.
3. The `Validation` attached properties (§4.1) — inert until step 4 populates them.
4. The registration hook and `RegisterValidation` (§3.1, §3.2), with the four lifecycle hazards covered by
   runtime tests: rebinding, local-value set, template recycling, and the initial-value-equals-default case
   that motivates the `OnValueChanged` funnel.
5. `IInputValidationControl` (§4.2) and per-control opt-in for whichever set Q8 settles on.

Steps 1–3 are independently mergeable and observable only through tests. Step 4 is the first one an app can
see.

## 9. Open decisions — resolved

| | Resolution |
|---|---|
| **Q1** | `Validation.Errors` is a **fresh snapshot** materialised from `GetErrors` on every synchronization, element type untouched. A new instance each time is load-bearing rather than stylistic: the dependency property change is what refreshes the application's binding, and sources commonly hand back the same collection instance. The no-error case reuses `Array.Empty<object>()`, so repeated clean syncs raise nothing. |
| **Q3 / Q11** | `Uno.UI.Xaml.Controls`, spelled `uno:Validation.IsEnabled="True"`. Reversible — an unprefixed alias can be added later, the reverse is a break — and it needs no generator change, since `Uno.UI.Xaml.Controls` is absent from the hardcoded `PresentationNamespaces` an unprefixed spelling would have had to join. microsoft-ui-xaml#179 is still open, so WinUI could yet claim the name. |
| **Q5** | `ErrorChanged`, after the withdrawn WinUI spec `IInputValidationControl` is borrowed from. |
| **Q8** | `TextBox`→`Text`, `PasswordBox`→`Password`, `NumberBox`→`Value`, `AutoSuggestBox`→`Text`, `ToggleSwitch`→`IsOn`, `ToggleButton`→`IsChecked` (so `CheckBox` and `RadioButton`), `Slider`→`Value`, `ComboBox`→`SelectedItem`. **`RichEditBox` is left out**: its content is an `ITextDocument` with no dependency property to bind. |
| **Q9** | **De-scoped, not deferred.** §3.4 records that the app builder lives outside this repo, so `UseInputValidation()` cannot be implemented here. `FeatureConfiguration.InputValidation.IsEnabled` is the mechanism. |

The questions as originally posed:

| | Question | Layer |
|---|---|---|
| **Q1** | Element type behind `IEnumerable GetErrors(string?)`. **Most load-bearing**, because with no default error template whatever lands in `Validation.Errors` *is* the app-facing contract: `string`? an error object carrying a message? anything, via `ToString()`? | 1 |
| **Q3** | Does `Validation` — the attached-property owner — belong in the default xmlns (unprefixed `Validation.IsEnabled`), or behind a prefix (`uno:Validation.IsEnabled`)? Unprefixed is the friendlier spelling but claims a generic name in a namespace that mirrors the WinUI contract. | 3 |
| **Q11** | **Which namespace** `InputValidationPropertyAttribute` lives in — [D9](#7-decisions-locked) settles the attribute in every other respect. It travels with Q3, and one thing is not open: it is Uno-only API, so it does not belong in `Microsoft.UI.Xaml.Controls` beside the WinUI attribute it deliberately does not extend. `Uno.UI.Xaml.Controls` is the established home for Uno-only public types in this area. | 3 |
| **Q5** | `ErrorChanged` vs `ErrorsChanged` — naming only; the handler type is settled as `EventHandler<DataErrorsChangedEventArgs>`. Reusing the BCL type is an argument for reusing its name. | 3 |
| **Q8** | Which controls participate in the first release. The per-control cost is now one attribute per type and **zero** DP registration edits (§3.3). | 2/3 |
| **Q9** | Whether a chainable `UseInputValidation()` app-builder method is *also* offered. Largely settled by D8: `FeatureConfiguration.InputValidation` has to exist anyway to host the cache, so it is the mechanism and a builder method would be an optional convenience over it. Remaining detail: the switch must be read before the first binding is registered, which for XAML-declared bindings means before `InitializeComponent` runs on the first page. | 2 |

Q6 and Q10 from the original design — what drives the visual states, and visual-state contention — belong to
spec 060 and are not open questions for this slice.

## 10. What implementation corrected

Recorded so the next reader does not re-derive it. The three lifecycle corrections are inline in §3.2; these
are the rest.

- **A pre-existing bug blocked the read model, and had to be fixed first.**
  `DependencyProperty.InternalGetProperty` forced the static constructor of the *queried* type, but
  `DependencyPropertyDescriptor.Parse` then redirects the lookup to the attached property's **owner**, whose
  registration only happens in *its* static constructor. An uninitialized owner produced a miss, and the null
  was negatively cached — so `{Binding (Owner.Property)}` read its initial value once and never subscribed to
  changes. This affected any attached-property binding path, `Canvas.Left` included; regression tests are in
  `Uno.UI.UnitTests/DependencyProperty/Given_DependencyProperty.AttachedPath.cs`.
- **Owner type matters for an attached property meant to be reachable from a binding path.** The transport's
  four attached properties register with `typeof(Control)`, which is free precisely because none of them is
  reachable from a binding path — they are read and written through `GetValue`/`SetValue` directly. `ComboBox.Uno.cs` instead registers
  `DropDownPreferredPlacement` with the *MUX* `ComboBox` as owner while the path names the Uno static class,
  so the registry lookup misses and that property cannot be observed through a binding path. Pre-existing,
  out of scope here, worth its own issue.
- **Ordering: neither `SetBinding` nor `InputValidationMode` can be assumed to come first.** Generated XAML
  emits the two as sequential assignments in document order, so the attribute order in the markup decides —
  and XamlStyler reorders attributes, so it is not even stable per file. Either way the element is parentless
  with a null `DataContext` at that point, so the leaf is unresolvable regardless. Registration therefore
  cannot be gated on `InputValidationMode`: it marks the expression unconditionally, the *sync* is gated,
  and the changed callback pulls the current expression. Both orders are covered by tests.
- **No read-only dependency property exists in Uno** — no `DependencyPropertyKey`, no
  `RegisterAttachedReadOnly`. `HasValidationErrors` and `ValidationErrors` expose a getter only, but the
  underlying property stays technically settable from XAML through `SetValue`.
- **`FeatureConfiguration` hosts no other cache**, so `InputValidation` is a new shape there. The map is
  **public and writable** — the registration entry point for controls that cannot carry the attribute — and
  backed by a `ConditionalWeakTable<Type, …>`, the established repo answer for a `Type`-keyed cache that
  must not pin a collectible `AssemblyLoadContext`; no teardown hook is needed with weak keys.
- **The naming collision §1 flags reaches the code.** `Uno.UI.Xaml.Controls` also holds static `ComboBox` and
  `ScrollViewer` classes, so the participating controls alias the namespace rather than importing it.
- **Most of the transport is unit-testable.** `Uno.UI.UnitTests` references the real Skia `Uno.UI` and needs
  no visual tree to exercise bindings, so the four lifecycle hazards, the leaf-not-root case, the
  initial-value-equals-default case and the compiled-binding path all live there rather than in runtime tests.

## 10b. Superseded by the WinUI alignment pass

Spec 060's visuals were implemented against **WinUI's own state machine** rather than the `ValidationStates`
group 060 §2 proposed, and porting that machine required reshaping parts of this spec. Recorded here rather
than edited into the decisions above, so the reasoning that produced them stays legible.

- **D2 — `Validation.IsEnabled` no longer exists.** Participation is now
  `InputValidationMode != InputValidationMode.Disabled`, which is exactly WinUI's
  `CControl::IsValidationEnabled`. The property is still opt-in per control and still gates registration and
  sync, so the *decision* stands; only its spelling changed. Its default is `Disabled` rather than the enum's
  zero value `Auto`, a deliberate divergence: WinUI's equivalent only gates visuals, whereas here it also
  gates the `INotifyDataErrorInfo` subscription. `InputValidationKind` was added beside it.

- **Every property of `IInputValidationControl` is a dependency property registered by the control itself**,
  and none of them is attached any more. WinUI registers them per control too — `TextBox_InputValidationMode`,
  `TextBox_HasValidationErrors`, `TextBox_ValidationErrors`, `TextBox_ErrorTemplate` and friends are all
  per-control entries in its property index. That is what lets a `Style` `Setter` target them and a `Binding`
  drive them, neither of which works against a property forwarding to an attached value.

  The consequence for this spec is that **§4.1's attached read model is gone**: `Validation.HasErrors` and
  `Validation.Errors` are replaced by `HasValidationErrors` and `ValidationErrors` on the control, so an app
  binds `{Binding HasValidationErrors, ElementName=…}` rather than the parenthesized attached path. `Validation`
  is gone entirely — see D3 — and `IInputValidationControl` is the API.

  Framework code reaches the properties two ways. Reads go through the interface, which is the C# equivalent
  of WinUI's four `switch (GetTypeIndex())` helpers, fallback included: a control that does not implement it
  is never enabled, exactly as an unknown property index means `Disabled` there. The one write — the
  framework setting `HasValidationErrors` — resolves the control's own property by name through
  `DependencyProperty.GetProperty`, which is already memoized and walks the base-type chain, so `CheckBox`
  finds what `ToggleButton` registered. Resolving by name rather than by a closed type switch is what keeps
  third-party controls working.
- **Q1 — resolved.** The element type behind `ValidationErrors` is `InputValidationError`, carrying the source error's
  `ToString()` as `ErrorMessage`, which is what WinUI's `DefaultInputValidationErrorTemplate` binds against.
  The property is now a `ValidationErrorsCollection` **mutated in place** rather than a fresh snapshot per
  synchronization — so the note above about a fresh instance being load-bearing is superseded: identity is
  now what a binding depends on, and `VectorChanged` is what notifies. The collection is created on first
  read, so a control that never reports an error never allocates one.
- **D1 — `IInputValidationControl` is WinUI's full interface**, moved to `Microsoft.UI.Xaml.Controls` with
  the enums and error types. `ValidationContext` is the one member not ported, commented out in place.
  `ErrorTemplate`, held back here for want of a consumer, ships with it — as a dependency property, so a
  theme's style can set it once §4 of spec 060 gives it something to render.
- **D7 — `ErrorChanged` survives** beside WinUI's `HasValidationErrorsChanged` and `ValidationError`, because
  it is the only one of the three that carries the source's `DataErrorsChangedEventArgs` unchanged.
- **D3 — the `Validation` class no longer exists.** Left with no public members, it was a public type that
  was pure plumbing. Its transport folded onto `Control`, which is where WinUI keeps the equivalent
  (`CControl::IsValidationEnabled`, `CControl::EnsureValidationVisuals`) and where every entry point already
  pointed — participation was always `Control`-typed, so it always required deriving from `Control`.

  The fold also fixed a defect the attached model had hidden: the participation members were `internal`, so
  the "third-party controls can participate" the interface promises was not actually reachable. A control
  outside Uno.UI got the transport for free but could not hook the three changed callbacks or back its three
  events, which would therefore never fire. They are now `protected`, and `Uno.UI.Tests.ViewLibrary` — a
  project deliberately outside `InternalsVisibleTo` — carries a participating control so that the contract
  fails at compile time if it ever regresses. One sharp edge worth recording: `FrameworkPropertyMetadata`'s
  `(value, callback)` overload is `internal`, so such a control registers with `PropertyMetadata`.
- **Q3 / Q11 — partially reversed.** Only `InputValidationPropertyAttribute` and `InputValidationPropertyMap`
  remain in `Uno.UI.Xaml.Controls`; the interface, enums and error types took their WinUI names in
  `Microsoft.UI.Xaml.Controls`. That is a
  **deliberate parity risk**: every one of those types is `PrivateApiContract` / `Feature_InputValidation` in
  WinUI's private IDL and has never shipped publicly, so if microsoft-ui-xaml#179 ever ships with a different
  shape, the names collide. `HasValidationErrorsChangedEventArgs` is the exception — public `WinUIContract`,
  and it already existed in Uno as a `[Uno.NotImplemented]` generated stub, now hand-written.

## 11. References

- [`INotifyDataErrorInfo`](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo)
  — the layer-1 contract, unchanged since .NET 4.5 and already functional on Uno
- [unoplatform/uno#4839](https://github.com/unoplatform/uno/issues/4839) — "Add support for WinUI 3 Input
  Validation", closed as not planned, `blocked/missing-api`
- [microsoft-ui-xaml#179](https://github.com/microsoft/microsoft-ui-xaml/issues/179) — the canonical WinUI
  request, open since 2019
- [microsoft-ui-xaml-specs#26](https://github.com/microsoft/microsoft-ui-xaml-specs/pull/26) — the withdrawn
  WinUI input-validation spec that `IInputValidationControl` borrows from; unmerged
- [microsoft-ui-xaml#4642](https://github.com/microsoft/microsoft-ui-xaml/issues/4642) — the preview bug
  where validation applied to `{x:Bind}` but not `{Binding}`
- [WPF `Validation`](https://learn.microsoft.com/dotnet/api/system.windows.controls.validation) — the
  reference implementation of layers 2–3, and the source of the `Validation.*` attached-property shape; note
  it depends on an adorner layer, which neither WinUI nor Uno has
- [Avalonia](https://github.com/AvaloniaUI/Avalonia) — `DataValidationErrors`, a shipping decorator-based
  answer to layer 3 that works *without* an adorner layer; its `enableDataValidation` is the direct analogue
  of the rejected metadata-flag route, and it is per-property-per-type only because Avalonia *has*
  `OverrideMetadata`
- [CommunityToolkit `ObservableValidator`](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/observablevalidator)
  — covers layer 1 via DataAnnotations; contributes nothing to layers 2–3
