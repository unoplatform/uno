# Feature Specification: `OriginalSourceLocation` for `ResourceDictionary`

**Repo**: `uno` (Uno.UI.SourceGenerators — XAML generator)
**Created**: 2026-08-27
**Status**: Implemented (working tree, not committed)
**Related**: `specs/041-hot-design-xaml-sources/spec.md` (the sibling source-mapping channel:
embedded XAML sources)

## Overview

When Hot Reload code generation is enabled (`UnoForceHotReloadCodeGen`, defaulted to `_isDebug`),
the XAML generator stamps the XAML declaration site of the objects it creates so a live object can
be mapped back to the markup that produced it:

- `FrameworkElement`s get it through `FrameworkElementHelper.SetBaseUri(...)` → the
  `FrameworkElement.DebugParseContext` record.
- Everything else gets it through the weak attached property
  `MarkupHelper.SetElementProperty(target, "OriginalSourceLocation", "file:///<path>#L<line>:<pos>")`,
  emitted by `XamlFileGenerator.TrySetOriginalSourceLocation`.

The second channel is opt-in per type, and today only covers:

| Object | Call site (`XamlFileGenerator.cs`) |
|---|---|
| `VisualState`, `AdaptiveTrigger`, `StateTrigger` | `IsNotFrameworkElementButNeedsSourceLocation` → `BuildExtendedProperties` |
| `Style` | `BuildChild` (`isStyle` branch) |
| `DataTemplate` / `ControlTemplate` / … | `BuildChild` (template branch) |
| top-level `<ResourceDictionary x:Class="…">` | `BuildResourceDictionaryBackingClass` |

**`ResourceDictionary` instances are otherwise never stamped.** A `<ResourceDictionary>` written in
markup — a dictionary file without `x:Class`, a merged dictionary, a theme dictionary, or the
explicit dictionary of a `FrameworkElement.Resources` / `Application.Resources` — produces a live
object that carries no pointer back to its declaration, so a designer holding that dictionary
(Hot Design's resource surfaces) cannot tell which file/line declared it.

Objective: every `ResourceDictionary` that the generated code materializes **from a
`<ResourceDictionary>` element of the current file** carries `OriginalSourceLocation`, using the
existing helper and the existing emission patterns — no new runtime API, no new metadata channel.

## Requirements

### R1 — dictionaries created by the generator

`InitializeAndBuildResourceDictionary` is the single place where the generator emits
`new global::Microsoft.UI.Xaml.ResourceDictionary { … }`. The location of the
`<ResourceDictionary>` element it was called for is stamped on that instance.

Because the dictionary is emitted in **expression** position (initializer of a field, entry of a
`MergedDictionaries` collection initializer, return of a `WeakResourceInitializer` lambda), the
stamp is appended through `CreateApplyBlock` — the same mechanism already used for `Style` and the
templates. This single call site covers:

- a top-level dictionary file with no resources (the `new ResourceDictionary` fallback of
  `BuildTopLevelResourceDictionary`), including the file-level dictionary that
  `GlobalStaticResources` exposes for an `x:Class`'d dictionary (a distinct instance from the
  `x:Class` type, which `BuildResourceDictionaryBackingClass` already stamps);
- inline `<ResourceDictionary>` entries of `ResourceDictionary.MergedDictionaries`;
- `<ResourceDictionary x:Key="…">` entries of `ResourceDictionary.ThemeDictionaries`;
- a `<ResourceDictionary>` built as a child through `BuildChild`.

### R2 — the pre-sized top-level dictionary

`BuildTopLevelResourceDictionary` has a fast path for a dictionary file that declares resources: it
calls `ResourceDictionary.CreateWithCapacity(n)` into a local field instead of going through R1.
That path is a **statement** context, so the stamp is emitted directly on the field, next to the
`IsParsing` / `IsSystemDictionary` assignments.

### R3 — the dictionary of an element's `Resources`

`<Page.Resources><ResourceDictionary>…` does not create a dictionary: the generator populates the
one the owner creates on first access to `Resources`. The location of the `<ResourceDictionary>`
declaration is stamped on that dictionary:

- for every `FrameworkElement` that goes through `BuildExtendedProperties` (the top-level control
  and every child element), inside the apply block that already carries `SetBaseUri`, on
  `<applied>.Resources`. The `FrameworkElement` test matters: `Resources` is declared by
  `FrameworkElement` and `Application` only, so without it a member merely *named* `Resources` on an
  unrelated type would be dereferenced, and a null getter would throw inside `InitializeComponent`;
- for `Application` (App.xaml), which never goes through `BuildExtendedProperties`, in
  `BuildApplicationInitializerBody` right after the resources are built.

**Which dictionary to stamp is decided once**, by `FindResourcesMember` — the same reading of the
`Resources` member that `RegisterAndBuildResources` emits from. The stamp uses its
`OwnDictionaryDeclaration`, which is non-null exactly when the owner keeps the dictionary it creates
itself and the generator populates it in place. Two independent derivations of that rule would drift,
and the drift is not benign: for
`<Page.Resources><ResourceDictionary Source="…"/><ResourceDictionary>…</ResourceDictionary></Page.Resources>`
the owner's `Resources` is *replaced* by the referenced file's shared dictionary, and a predicate that
merely looked for a `Source`-less dictionary element would stamp that shared instance with this
file's location.

**Exactly one generated file ever writes a dictionary's location** — R3 is therefore an
unconditional write, unlike R5. The dictionary reached as `<applied>.Resources` is created by the
owner (`FrameworkElement.Resources`' getter, or `Application`'s) and populated only by the file that
declares it, and the two `Resources`-targeting emissions are mutually exclusive by construction:
`BuildApplicationInitializerBody` runs only for an `Application` root, while the
`BuildExtendedProperties` one is gated on `isFrameworkElement`, and `Application` is not one. Two
distinct objects are never the same instance, and the stamp runs during the owner's construction —
before any code could alias one element's dictionary onto another. The framework agrees that a
dictionary has a single owner: `FrameworkElement.Resources`' setter calls `SetResourceOwner(this)`,
re-owning whatever it is handed. The runtime XAML reader's `Resources` assignment
(`XamlObjectBuilder`) hands over a dictionary it just loaded, and stamps nothing.

The one case where the same instance is written twice is the **same** file re-stamping after a Hot
Reload update, with the declaration's current line — which is why the write must stay unconditional:
a set-if-absent guard there would pin the first location and leave it stale once an edit moves the
declaration. A shared instance can never be reached: a dictionary from a `Source` or a typed subclass
is excluded below, and those instances are stamped by the file that declares them.

Not stamped, as a consequence of using that one decision:

- a `Resources` member with no explicit `<ResourceDictionary>` element
  (`<Page.Resources><Style/></Page.Resources>`) — there is no dictionary element to point at, and the
  owning element already carries its own location;
- a dictionary assigned from a `Source` or from a typed subclass — it is not the owner's own (the
  subclass is stamped by R5 instead);
- an entirely empty `<ResourceDictionary/>` — the generator emits nothing into it, so it has no
  location to describe, and nothing forces the owner to materialize a dictionary it would otherwise
  never create.

### R4 — no behavioural change when Hot Reload codegen is off

Every emission of R1–R3 is inside the existing `_isHotReloadEnabled` guard (`TrySetOriginalSourceLocation`
is itself a no-op otherwise). Release/non-debug output is byte-identical.

### R5 — typed dictionary subclasses, without overwriting their own location

A `<local:MyDictionary/>` (`IsResourceDictionarySubclass`) is stamped at its **use site** too, through
the apply block appended by `BuildTypedResourceDictionary` — which covers both a subclass merged into
another dictionary and one used as a whole `Resources` value.

The stamp is emitted **set-if-absent** — `if (MarkupHelper.GetElementProperty<string>(d,
"OriginalSourceLocation") is null)` — rather than the use site being skipped by type:

- a subclass generated from an `x:Class` dictionary file stamps its own declaration site in the
  `InitializeComponent` its constructor runs, which happens before the apply block, so the guard
  preserves it — the declaration site keeps winning over the reference site;
- a subclass **defined in code** (a library theme dictionary such as Uno.Themes' `BaseTheme` family,
  whose constructor points `Source` at the library's style bundle) has no `InitializeComponent` and
  therefore no location of its own; the declaring markup is the only source location it can be given,
  and excluding it by type would leave it stamped by nobody.

The type test is deliberately avoided: whether a subclass has a generated backing class is not
reliably knowable from the generator for a dictionary of the *same* compilation, whose generated
`InitializeComponent` does not exist yet in the symbol table.

## Non-goals

- **Dictionaries with a `Source`** (`<ResourceDictionary Source="ms-appx:///…"/>`, as a merged
  dictionary, a theme dictionary, or a whole `Resources` value). The instance retrieved belongs to
  the *referenced* file and is shared; the referenced file's own generated code is what stamps it.
  Stamping it here would attribute another file's dictionary to the referencing one, and would
  overwrite a correct value on a shared instance.
- **Overwriting a location a dictionary already carries.** A typed subclass is stamped at its use
  site (R5), but only when it has none of its own — the declaration site always wins over the
  reference site.
- Exposing the value as a public API, or promoting it to a typed record like
  `FrameworkElement.DebugParseContext`. Consumers read it with
  `MarkupHelper.GetElementProperty<string>(dictionary, "OriginalSourceLocation")`, as they do for
  `Style` and the templates today.
- Locations for individual resources by key (a `RESOURCE::<key>` scheme existed in 2023 and was
  removed); each resource object is stamped as its own object where its type opts in.

## Edge cases

- **File paths are emitted as C# literals**, through `SymbolDisplay.FormatLiteral`, not interpolated
  into one: a file name may contain a quote or a newline on unix-like systems, which would otherwise
  close the literal and inject the rest of the name into the generated code. `SetBaseUri`'s own URI
  argument goes through the same helper.
- **A `#` in a project path** still makes the `#L<line>:<pos>` fragment ambiguous for a consumer
  parsing the value. Left as-is: escaping it would change the format every existing consumer of
  `OriginalSourceLocation` already reads.
- **`x:Class`'d dictionary file** — two instances exist (the `x:Class` type, and the file-level
  dictionary held by `GlobalStaticResources`). Both are stamped with the same root-element location:
  the first by the pre-existing code, the second by R1.
- **Theme dictionaries** are wrapped in `WeakResourceInitializer`, so their stamp runs lazily on
  first materialization — the same laziness as their content.

## Test plan

### Snapshot tests — `Given_HotReloadEnabledInBuild` (the only suite that turns `UnoForceHotReloadCodeGen` on)

1. `SetOriginalSourceLocationInOutputForResourceDictionaryFile` — a dictionary file declaring a
   resource (R2 fast path), an inline merged dictionary and a themed dictionary (R1), and a merged
   dictionary with a `Source` (not stamped; the referenced file gets its own R2 stamp).
2. `SetOriginalSourceLocationInOutputForExplicitResourceDictionaries` — a page with an explicit
   `<Page.Resources><ResourceDictionary>` holding a theme dictionary, and a `Grid` with its own
   explicit dictionary: R3 on the top-level control (`useGenericApply`) and on a child built in an
   object initializer, plus R1 for the theme dictionary.
3. `SetOriginalSourceLocationInOutputForTypedResourceDictionaries` — a page merging a **code-defined**
   `ResourceDictionary` subclass and using one as a whole `Grid.Resources`: both use sites emit the
   set-if-absent stamp (R5).
4. `SetOriginalSourceLocationNotSetForResourcesFromSource` — `<Page.Resources>` whose first
   declaration carries a `Source`, followed by a second, inline one. The page's `Resources` is the
   referenced file's shared dictionary, so the generated file must contain **no**
   `OriginalSourceLocation` at all.
5. `ResourceDictionaryCodeBehind` (pre-existing) — regenerated: the file-level instance of an
   `x:Class`'d dictionary is now stamped, with the `x:Class` stamp unchanged.

### Runtime test — `Uno.UI.UnitTests`

`Given_ResourceDictionarySourceLocation`, over the fixture
`App/Xaml/ResourceDictionarySourceLocation.xaml`. The project sets `UnoForceHotReloadCodeGen`, so the
locations are always generated there; the test reads them back with
`MarkupHelper.GetElementProperty<string>(d, "OriginalSourceLocation")` and asserts the **semantics** a
consumer depends on, which golden text cannot:

| Case | Assertion |
|---|---|
| the page's `<Page.Resources><ResourceDictionary>` | its declaration, `…#L6:4` (R3) |
| the `Grid`'s own explicit dictionary | its declaration, `…#L18:5` (R3, initializer path) |
| merged **code-defined** subclass | the declaring markup, `…#L8:6` (R5 fallback) |
| merged **`x:Class`** subclass | `Subclassed_Dictionary.xaml`, *not* this file (R5 precedence) |
| merged dictionary from a `Source` | not attributed to this file (non-goal) |
| `Application.Current.Resources` | `App.xaml` (R3 for `Application`) |

The exact line:column expectations are deliberate: they are what catches a stamp landing on the
wrong element, which the snapshot tests cannot distinguish from a correct one.

Regression guard: the whole `Uno.UI.SourceGenerators.Tests` suite must stay green — a changed
snapshot in a non-Hot-Reload test would be an R4 violation.

## Validation performed

- **Generator tests** — 486 total, 472 passed, 14 skipped (pre-existing `Assert.Inconclusive` cases
  tracked by [#24085](https://github.com/unoplatform/uno/pull/24085)), 0 failed. Regenerating the
  snapshots after the R1–R5 fixes changed **no** pre-existing golden other than
  `ResourceDictionaryCodeBehind`.
- **Runtime** — `dotnet test --project src/Uno.UI.UnitTests/…`: 4114 total, 3954 passed, 23 skipped,
  including the six new assertions above. The 137 failures are **byte-identical** to the baseline
  captured with this change stashed (Linux-only path/UNC/case-sensitivity expectations).
- **Compile** — the same project also compiles `App/App.xaml`, whose
  `<Application.Resources><ResourceDictionary>` exercises R3 for `Application` and whose merged
  `x:Class` dictionaries exercise R5: 0 warnings / 0 errors.

## Implementation

All of the production change is in
`src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`:

| Change | Requirement |
|---|---|
| `InitializeAndBuildResourceDictionary`: apply block + `TrySetOriginalSourceLocation` after the initializer | R1 |
| `BuildTopLevelResourceDictionary`: `TrySetOriginalSourceLocation` on the `CreateWithCapacity` field | R2 |
| `BuildExtendedProperties`: stamp `<applied>.Resources`, gated on `isFrameworkElement`, from `FindResourcesMember` | R3 |
| `BuildApplicationInitializerBody`: same, on `Resources` | R3 |
| `BuildTypedResourceDictionary`: apply block + set-if-absent stamp | R5 |
| `TrySetOriginalSourceLocation`: `preserveExisting` option, emitting the `GetElementProperty … is null` guard | R5 |
| new `ResourcesMember` record + `FindResourcesMember`, which `RegisterAndBuildResources` now emits from and the R3 stamps read | R3 |
| `FileUri` / `FileUriLiteral` / `GetSourceLocationLiteral`: the emitted URIs go through `SymbolDisplay.FormatLiteral` | R1–R5, `SetBaseUri` |
