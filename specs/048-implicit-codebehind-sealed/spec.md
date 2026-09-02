# Spec 048: Seal the auto-generated XAML code-behind class

**Branch**: `dev/david/implicit-codebehind-sealed`
**Created**: 2026-07-09
**Status**: Draft
**Issue**: unoplatform/uno#23708

Refines [`001-auto-codebehind`](../001-auto-codebehind/spec.md), whose **FR-013** mandates the
generated class be `partial` but is silent on **accessibility** and **`sealed`**.

## Problem

`XamlCodeBehindEmitter` — the shared emitter that produces the auto-generated ("implicit") code-behind
for a XAML file that has an `x:Class` but no user-authored `.xaml.cs` — declares the class as:

```csharp
public partial class {ClassName} : {BaseType}    // XamlCodeBehindEmitter.cs
```

i.e. `public` but **not `sealed`**. This is inconsistent with:

- The **ResourceDictionary** backing class emitted by the same generator, which is
  `public sealed partial class` (`XamlFileGenerator.cs`, `BuildResourceDictionaryBackingClass`).
- Every page shipped by the official **project templates** (`unoplatform/uno.templates`) and the
  `dotnet new` / IDE "Add > Page" item, which are `public sealed partial class X : Page` — the
  conventional shape of a hand-authored code-behind.

### Impact: hot-reload rude edit (ENC0004)

Under hot reload (Roslyn Edit-and-Continue), the class symbol compiled into the EnC **baseline** and
the class symbol in a later edit are compared. When a XAML page transitions between:

- **auto-generated code-behind** (`public partial`, from `XamlCodeBehindEmitter`), and
- a **conventional / user-authored code-behind** (`public sealed partial`, as every template and the
  `dotnet new` page item produce),

…the declared modifiers change (`sealed` is added or removed). EnC classifies a change to an existing
type's modifiers as a **rude edit**:

```
error ENC0004: Updating the modifiers of class requires restarting the application.
```

The reload then fails and the host must restart / rebuild instead of applying a live metadata update.
This is common in tooling that authors a page's `.xaml` and `.xaml.cs` as distinct steps, or that
starts a page as XAML-only and later adds a real code-behind.

## Root cause

`src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindEmitter.cs` — the class
declaration omits `sealed`:

```csharp
public partial class {classInfo.ClassName} : {classInfo.BaseTypeFullName}
```

The emitter is **shared** by both generator entry points, so a single site covers all targets:

- Uno Platform targets: `XamlCodeGeneration.cs` → `XamlCodeBehindEmitter.Emit(...)`.
- WinUI (Uno.Sdk) targets: `Uno.UI.SourceGenerators.WinAppSDK/XamlCodeBehindGenerator.cs` (links the
  same `XamlCodeBehindEmitter.cs` via `<Compile Include=... Link=...>`) → `XamlCodeBehindEmitter.Emit(...)`.

## Solution

Emit the auto-generated code-behind class as `public sealed partial`:

```csharp
public sealed partial class {classInfo.ClassName} : {classInfo.BaseTypeFullName}
```

`sealed` now matches both the ResourceDictionary backing class and the conventional/template
code-behind, so replacing the auto-generated code-behind with a real one (or vice-versa) under hot
reload no longer changes the class modifiers → no `ENC0004`.

### Why this is safe

- The emitter runs **only** when there is no user code-behind (`001-auto-codebehind`, FR-005), so the
  added `sealed` can never conflict with a hand-authored partial at compile time.
- The `InitializeComponent()` partial emitted by `XamlFileGenerator` (`partial class {0} : {1}`, no
  modifiers) is **left unchanged** — it must stay modifier-less to merge with an arbitrary user
  code-behind. Sealing happens on the code-behind partial only.

### Compatibility note (minor, source-level)

Sealing prevents *deriving* from a page/control whose code-behind is auto-generated. In practice a type
meant to be a base class carries its own (user-authored) code-behind, so the emitter does not run for
it; and all first-party templates are already `sealed`. Called out for the maintainers to weigh.

### Known edge

A developer who writes a **non-sealed** code-behind (`public partial class X : Page`, no `sealed`) for a
page that previously had the auto-generated code-behind will still see a `sealed → non-sealed` rude edit
on that first reload. This is unconventional (templates and the item template are sealed) and is out of
scope; the common path is now rude-edit-free.

## Acceptance criteria

- [x] `XamlCodeBehindEmitter.Emit` produces `public sealed partial class …`.
- [x] `XamlCodeBehindGeneratorTests` expected output updated to the sealed declaration. These tests use
      **inline** `SourceText.From("""…""")` expectations — there is no `Out/` snapshot dir for
      `XamlCodeBehindGeneratorTests`.
- [x] The one code-behind golden that *does* live on disk — the ResourceDictionary
      `…/XamlCodeGeneratorTests/Out/…/XamlCodeGenerator_Test.RD.codebehind.g.cs` — regenerated to `sealed`.
- [x] Suppression tests retain coverage for a **non-sealed** `public partial` user code-behind (valid C#),
      so "existing code-behind suppresses generation" is not asserted sealed-only.
- [x] Emitter unit test asserts the `public sealed partial` declaration (the guard expressible as a
      source-generator unit test).
- [ ] **Follow-up**: a real metadata-update / EnC `ENC0004` regression — it needs a live compiler session
      and cannot be asserted by `CSharpSourceGeneratorVerifier`.
- [x] `001-auto-codebehind` FR-013 updated to state the generated class is `public sealed partial`.
- [x] Source generators build clean (the WinAppSDK generator links the same shared emitter).

## Files to change

- `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindEmitter.cs` — add `sealed`.
- `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeBehindGeneratorTests/XamlCodeBehindGeneratorTests.cs` — inline expected output (keeps a non-sealed suppression fixture for coverage).
- `src/SourceGenerators/Uno.UI.SourceGenerators.Tests/XamlCodeGeneratorTests/Out/…/XamlCodeGenerator_Test.RD.codebehind.g.cs` — regenerated RD code-behind golden.
- `specs/001-auto-codebehind/spec.md` — FR-013 → `public sealed partial`.

## Scope of this PR

This PR is **not** documentation-only — the implementation (the one-line emitter change + test and
RD-golden updates) has landed on this branch alongside the spec.
