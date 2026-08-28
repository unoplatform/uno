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

- for every object that goes through `BuildExtendedProperties` (the top-level control and every
  child element), inside the apply block that already carries `SetBaseUri`, on
  `<applied>.Resources`;
- for `Application` (App.xaml), which never goes through `BuildExtendedProperties`, in
  `BuildApplicationInitializerBody` right after the resources are built.

A `Resources` member with **no** explicit `<ResourceDictionary>` element
(`<Page.Resources><Style/></Page.Resources>`) is not stamped: there is no dictionary element in the
markup to point at, and the owning element already carries its own location.

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

- **Empty explicit dictionary** — `<Grid.Resources><ResourceDictionary/></Grid.Resources>` emits no
  resource population at all, so the R3 stamp is the first access to `Resources` and thus creates an
  empty dictionary that would otherwise not exist. Accepted: it only happens in Hot-Reload-enabled
  (development) builds, and the dictionary genuinely exists in the markup.
- **`x:Class`'d dictionary file** — two instances exist (the `x:Class` type, and the file-level
  dictionary held by `GlobalStaticResources`). Both are stamped with the same root-element location:
  the first by the pre-existing code, the second by R1.
- **Theme dictionaries** are wrapped in `WeakResourceInitializer`, so their stamp runs lazily on
  first materialization — the same laziness as their content.

## Test plan

Snapshot tests in `Uno.UI.SourceGenerators.Tests` (`Given_HotReloadEnabledInBuild`, the only suite
that turns `UnoForceHotReloadCodeGen` on), with committed generated output under
`XamlCodeGeneratorTests/Out/Given_HotReloadEnabledInBuild/`:

1. `SetOriginalSourceLocationInOutputForResourceDictionaryFile` — a dictionary file declaring a
   resource (R2 fast path), an inline merged dictionary and a themed dictionary (R1), and a merged
   dictionary with a `Source` (non-goal: not stamped, and the referenced file gets its own R2 stamp).
2. `SetOriginalSourceLocationInOutputForExplicitResourceDictionaries` — a page with an explicit
   `<Page.Resources><ResourceDictionary>` holding a theme dictionary, and a `Grid` with its own
   explicit dictionary: R3 on both the top-level control (`useGenericApply`) and a child built in an
   object initializer, plus R1 for the theme dictionary.
3. `SetOriginalSourceLocationInOutputForTypedResourceDictionaries` — a page merging a **code-defined**
   `ResourceDictionary` subclass and using one as a whole `Grid.Resources`: both use sites emit the
   set-if-absent stamp (R5).
4. `ResourceDictionaryCodeBehind` (pre-existing) — regenerated snapshot shows the second (file-level)
   instance of an `x:Class`'d dictionary is now stamped, with the `x:Class` stamp unchanged.

**Application (R3) is not covered by a snapshot test**: the XAML-generator test harness cannot
process an `Application` root at all — a test with an App.xaml root fails on master with
`UXAML0001 Unsupported resource type for …/Uno.dll` + `Processing failed for an unknown reason
(BuildApplicationInitializerBody@466)`, with or without this change. It is instead validated by
compiling `Uno.UI.UnitTests`, whose `App/App.xaml` declares an explicit
`<Application.Resources><ResourceDictionary>` with nested merged dictionaries (see Validation).

Regression guard: the whole `Uno.UI.SourceGenerators.Tests` suite must stay green — a changed
snapshot in a non-Hot-Reload test would be an R4 violation.

## Validation performed

- **Compile** — `dotnet build src/Uno.UI.UnitTests/Uno.UI.UnitTests.csproj -c Debug
  -p:UnoForceHotReloadCodeGen=true -p:EmitCompilerGeneratedFiles=true`: succeeded, 0 warnings /
  0 errors. The emitted `App_*.cs` contains
  `MarkupHelper.SetElementProperty(Resources, "OriginalSourceLocation", "…/App/App.xaml#L12:4")`,
  proving R3 for `Application` generates valid code.
- **Generator tests** — `dotnet test --project src/SourceGenerators/Uno.UI.SourceGenerators.Tests/…`:
  484 total, 470 passed, 14 skipped (pre-existing `Assert.Inconclusive` cases tracked by
  [#24085](https://github.com/unoplatform/uno/pull/24085)), 0 failed.
- **Runtime** — `dotnet test --project src/Uno.UI.UnitTests/… -p:UnoForceHotReloadCodeGen=true`
  (which materializes the app dictionaries, merged and themed, through the new emissions):
  3949 passed / 137 failed / 23 skipped, and the 137 failures are **byte-identical** to the
  baseline captured with this change stashed (Linux-only path/UNC/case-sensitivity expectations).
- **Runtime, R5 in particular** — a throwaway page in `Uno.UI.UnitTests` merging a code-defined
  subclass and the project's `x:Class` `Subclassed_Dictionary`, dumping
  `MarkupHelper.GetElementProperty<string>(d, "OriginalSourceLocation")` for each dictionary:

  | Dictionary | Location read back |
  |---|---|
  | the page's `<Page.Resources><ResourceDictionary>` | `Temp_SourceLocation.xaml#L6:4` (R3) |
  | merged **code-defined** subclass | `Temp_SourceLocation.xaml#L8:6` — the declaring line (R5) |
  | merged **`x:Class`** subclass | `Subclassed_Dictionary.xaml#L1:2` — **its own** file, not the use site (R5 guard) |
  | `Test_Page_Other`'s `Resources = new Subclassed_Dictionary()` | `Subclassed_Dictionary.xaml#L1:2` |
  | `Application.Current.Resources` | `App/App.xaml#L12:4` (R3) |

## Implementation

`src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs` only:

| Change | Requirement |
|---|---|
| `InitializeAndBuildResourceDictionary`: apply block + `TrySetOriginalSourceLocation` after the initializer | R1 |
| `BuildTopLevelResourceDictionary`: `TrySetOriginalSourceLocation` on the `CreateWithCapacity` field | R2 |
| `BuildExtendedProperties`: stamp `<applied>.Resources` from `FindResourcesDictionaryDeclaration` | R3 |
| `BuildApplicationInitializerBody`: same, on `Resources` | R3 |
| new `FindResourcesDictionaryDeclaration` helper (explicit `<ResourceDictionary>`, no `Source`) | R3 |
| `BuildTypedResourceDictionary`: apply block + set-if-absent stamp | R5 |
| `TrySetOriginalSourceLocation`: `preserveExisting` option, emitting the `GetElementProperty … is null` guard | R5 |
