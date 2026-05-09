---
description: Port WinUI C++ code to Uno Platform C#. Supports full control porting and snippet/incremental porting.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WinUI Porting Skill**. You produce **lossless, structure-preserving, human-fixable** drafts of Uno Platform C# code from WinUI C++ headers, implementations, IDL files, and related tests.

Your output must never lose information, never delete logic, and must follow the partial-file layout, event-revoker patterns, and Uno constraints defined in this document.

The user's input after `/winui-port` is a **free-form description** of what to port. It may be:

- A full control name: "Port the Expander control from /path/to/winui/dev/Expander/"
- A snippet/method: "Port the OnApplyTemplate method from /path/to/InfoBar.cpp into the existing InfoBar port"
- A task description: "Port the remaining unported methods in TitleBar"
- File paths with instructions: "Port /path/to/SplitButton.cpp, /path/to/SplitButton.h, /path/to/SplitButton.idl"

Parse the intent and determine whether this is a **full control port** or a **snippet/incremental port**.

---

## Execution Workflow

### Phase 0: Analyze Intent and Gather Sources

1. **Parse the user input** to determine:
   - Which C++ source files to read (`.cpp`, `.h`, `_Partial.cpp`, `_Partial.h`)
   - Whether an `.idl` (MIDL3) file exists alongside the source
   - Whether this is a full control port or a snippet/incremental port
   - The control name and namespace

2. **Read all source files** provided or referenced by the user. For a full control port, look for the complete set:
   - `ControlName.cpp` — main implementation
   - `ControlName.h` — header with fields, constants, inline methods
   - `ControlName_Partial.cpp` / `ControlName_Partial.h` — split implementations (if they exist)
   - `ControlName.properties.cpp` — property definitions
   - `ControlName.idl` — MIDL3 API surface definition
   - Associated types: `ControlNameTemplateSettings.h/.cpp`, `ControlNameAutomationPeer.h/.cpp`, `ControlNameEventArgs.h/.cpp` (auto-detect from the same directory)
   - Test files: Look for `*ControlName*Test*` files in nearby test directories

3. **Determine the MUX reference tag/commit**:
   - Run `git -C <winui-source-dir> rev-parse --short HEAD` where `<winui-source-dir>` is the root of the WinUI git repository (the directory containing the `.git` folder). Use this single commit hash for all `// MUX Reference` headers in the ported files.
   - Run this command **before writing any files** so the correct hash is known up front.
   - If the directory is not a git repository or the command fails, fall back to any tag embedded in the path (e.g., `release/1.6-stable`), or leave the value as `unknown` with a `TODO Uno:` note.

4. **Read the IDL file** (if found) to determine the complete public API surface including:
   - All public properties and their types/defaults
   - All public events
   - All public methods
   - Base class and implemented interfaces
   - Enum types defined for the control

5. **Fetch documentation from Microsoft Learn** for the control being ported:
   - Search for `site:learn.microsoft.com "WinUI" "ControlName"` to find the API reference
   - Extract `<summary>` descriptions for public properties, events, and methods
   - These will be used for XML doc comments on public members in `.Properties.cs`

6. **Check for existing ported code** in the Uno repo:
   - Search `src/Uno.UI/` for existing files matching the control name
   - If files exist, this informs whether to merge or create new files
   - Check `src/Uno.UI/Generated/` for existing NotImplemented stubs

### Phase 1: Determine Output Structure

**For a full control port**, plan these files:

| C++ Source | C# Output | Content |
|-----------|-----------|---------|
| `ControlName.cpp` | `ControlName.mux.cs` | Implementation (constructors, methods, overrides) |
| `ControlName.h` | `ControlName.h.mux.cs` | Fields, constants, revokers, inline methods |
| `ControlName_Partial.cpp` | `ControlName.partial.mux.cs` | Split implementation (if exists) |
| `ControlName_Partial.h` | `ControlName.partial.h.mux.cs` | Split header fields (if exists) |
| `ControlName.properties.cpp` | `ControlName.Properties.cs` | Public DependencyProperties, events |
| `ControlName.idl` | `ControlName.cs` | Main class declaration (public partial class) |
| `ControlNameTemplateSettings.*` | `ControlNameTemplateSettings.cs` etc. | TemplateSettings (if exists) |
| `ControlNameAutomationPeer.*` | `ControlNameAutomationPeer.cs` etc. | AutomationPeer (if exists) |
| `ControlNameEventArgs.*` | `ControlNameEventArgs.cs` | EventArgs classes (if exist) |

**For a snippet/incremental port**, determine which existing files to update:

- If methods are being added to an existing control, merge into the appropriate existing `.mux.cs` or `.h.mux.cs` file
- If new fields/constants are needed, add them to the `.h.mux.cs` file
- If new properties are being added, update `.Properties.cs`

**Output directory**: Auto-detect based on the control's namespace:
- `Microsoft.UI.Xaml.Controls.ControlName` → `src/Uno.UI/Microsoft/UI/Xaml/Controls/ControlName/`
- `Microsoft.UI.Xaml.Controls.Primitives.ControlName` → `src/Uno.UI/Microsoft/UI/Xaml/Controls/Primitives/ControlName/`
- Create the directory if it does not exist

### Phase 2: Port the Code

Apply ALL of the following porting rules when converting C++ to C#.

#### 2.1. General Rules

- **Never remove or simplify code.** Anything you cannot convert must be preserved as a comment with a clear `TODO Uno:` explanation.
- **Preserve all `//` code comments exactly.** Every comment in the C++ source — explanatory notes, rationale, section dividers, `#pragma region` labels (converted to `// #pragma region`), TODOs, and inline remarks — must appear in the C# output at the same relative position. Do not paraphrase, summarize, or omit comments. The only acceptable changes are adapting C++ syntax references inside a comment to their C# equivalents (e.g., `winrt::hstring` → `string`).
- **Maintain method order and structure** exactly as in the original C++ files.
- **Preserve all behavior and intent**, even if the resulting C# does not compile yet.
- Any Uno-specific code must be wrapped in `#if HAS_UNO` / `#endif`.
- Any original code that cannot be represented in Uno must be wrapped in `#if !HAS_UNO` / `#endif` with a `TODO Uno:` comment explaining what is missing.
- The conversion is a **draft**, not a verified build. It may contain unresolved symbols or unimplemented APIs — leave them visible.
- **Line number reference comments** (e.g., `// Layout.cpp, line 237`) must only be added when porting **small snippets or individual methods** into an existing file. When porting a **whole file or large section**, do not add per-method line number comments — they clutter the code and become stale. The MUX reference header comment at the top of the file is sufficient for traceability.

#### 2.2. File Headers

Every generated file MUST start with the license header followed by the MUX reference:

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference <SourceFile.cpp>, tag winui3/release/1.6-stable
```

Or with commit hash:

```csharp
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference <SourceFile.cpp>, commit 9d6fb15c0
```

#### 2.3. File Layout Rules

**MANDATORY**: Every ported C++ source pair (`ControlName.h` + `ControlName.cpp`) **MUST** be broken down into separate partial-class files. A single monolithic `.cs` file containing both field declarations and method implementations is **never acceptable** — even for small or simple types. The required split is:

| File | Content |
|------|---------|
| `ControlName.cs` | **Declaration only** — `public partial class ControlName : BaseType { }` with the MUX reference header, XML doc `<summary>`, and nothing else |
| `ControlName.h.mux.cs` | **From the C++ header** — private fields, constants, inline method bodies. Omit this file only if the header has zero fields and zero inline method bodies |
| `ControlName.mux.cs` | **From the C++ implementation** — all method bodies, in the same order as the `.cpp` file |
| `ControlName.partial.h.mux.cs` | **From `_Partial.h`** — fields and inline methods from the partial header. Only when `ControlName_Partial.h` exists. **Must be a separate file** — do not merge into `ControlName.h.mux.cs` |
| `ControlName.partial.mux.cs` | **From `_Partial.cpp`** — method bodies from the partial implementation. Only when `ControlName_Partial.cpp` exists. **Must be a separate file** — do not merge into `ControlName.mux.cs` |
| `ControlName.uno.cs` | **Uno-specific additions** — WeakEventHelper hooks, platform workarounds, members with `[UnoOnly]`. Omit if none exist |
| `ControlName.Properties.cs` | **Public DependencyProperties and events** — only when the type defines DPs. Omit otherwise |

Each C++ source file maps to **exactly one** C# output file. `_Partial.cpp` and `_Partial.h` are separate translation units in WinUI and must remain separate files in the port — never combine them with the main `.mux.cs` / `.h.mux.cs`.

When an existing monolithic `.cs` file is encountered during porting, it **must** be refactored into the above split as part of the port.

**Main class file (`ControlName.cs`)**:
- Contains **only** the public class declaration: `public partial class ControlName : BaseType { }`
- No fields, no methods, no properties.

**Implementation file (`ControlName.mux.cs` / `ControlName.partial.mux.cs`)**:
- Contains the converted `.cpp` implementation.
- Includes constructors, method bodies, overrides, event hookup logic.
- Maintain the **exact method order** from the C++ source.
- Uses `partial class` with **no access modifiers** on the class declaration.

**Header file (`ControlName.h.mux.cs` / `ControlName.partial.h.mux.cs`)**:
- Field declarations (references to template child elements, cached control references, revokers, state variables)
- Constants (template part names, visual state names)
- **All methods that have an inline body in the C++ header** (e.g., `virtual Foo Bar() { return nullptr; }`), regardless of their access modifier or whether they are public API — the deciding factor is where the body lives in C++, not where it logically "belongs"
- Dependency-property-related arrays/metadata
- SerialDisposable revoker fields
- DispatcherHelper instances
- Uses `partial class` with **no access modifiers** on the class declaration.

**Properties file (`ControlName.Properties.cs`)**:
- Contains **public properties and public events** that form the API surface.
- Uses private fields defined in `.h.mux.cs`.
- Only the API surface goes here — no implementation logic.
- All public members MUST have XML documentation comments (sourced from Microsoft Learn).
- Uses `partial class` with **no access modifiers** on the class declaration.

#### 2.3.1. Method Style

For **single-line method bodies**, prefer expression-body syntax (`=>`):

```csharp
// Prefer this:
internal int LayoutAnchorIndexDbg() => m_layoutAnchorInfoDbg.Index;
internal IndexBasedLayoutOrientation GetForcedIndexBasedLayoutOrientationDbg() => m_forcedIndexBasedLayoutOrientationDbg;

// Over this:
internal int LayoutAnchorIndexDbg()
{
    return m_layoutAnchorInfoDbg.Index;
}
```

This applies to both methods and properties. Use block-body syntax only when the body has more than one statement.

#### 2.4. C++ to C# Syntax Conversions

Apply these conversions systematically:

| C++ Pattern | C# Equivalent |
|------------|---------------|
| `GetTemplateChildT<winrt::Control>(name, *this)` | `GetTemplateChild<Control>(name) is Control var` |
| `IsExpanded()` (property getter) | `IsExpanded` |
| `winrt::VisualStateManager::GoToState(*this, L"Down", transitions)` | `VisualStateManager.GoToState(this, "Down", transitions)` |
| `m_expandingEventSource(*this, nullptr)` | `Expanding?.Invoke(this, null)` |
| `static constexpr auto c_name = L"Name"sv` | `private const string c_name = "Name"` |
| `winrt::get_self<::Settings>(TemplateSettings())` | `TemplateSettings` (direct access) |
| `MUX_ASSERT(expr)` | `MUX_ASSERT(expr)` (with `using static Microsoft.UI.Xaml.Controls._Tracing;`) |
| `auto_revoke` event subscription | `SerialDisposable` pattern (see section 2.5) |
| C++ destructor (`~ControlName()`) | **No finalizer** — comment out with `TODO Uno:` (see section 2.6) |
| `winrt::hstring` | `string` |
| `winrt::Windows::Foundation::IInspectable` | `object` |
| `nullptr` | `null` |
| `static_cast<Type>(expr)` | `(Type)expr` or `expr as Type` |
| `auto` | `var` |
| `this->` or `*this` | `this` |

#### 2.5. Event Handling and Revokers

C++ revokers (`auto_revoke`, revoker tokens, vector-changed tokens, per-item maps) must be converted to **`SerialDisposable`** patterns.

When converting, check for common leak scenarios:
- Event handlers on long-lived sources (`this`, singletons, static events)
- Per-item or per-token subscriptions stored in collections
- Circular references between publishers and subscribers
- Platform-specific lifecycle issues (iOS views/controllers that may outlive expected scope)

Add `// TODO Uno: Investigate potential leak: <short-reason>` when a potential leak is suspected.

**Basic revoker pattern:**
```csharp
private readonly SerialDisposable _myEventRevoker = new SerialDisposable();

void InitializeSomething()
{
    source.Event += Handler;
    _myEventRevoker.Disposable = Disposable.Create(() =>
    {
        source.Event -= Handler;
    });
}
```

**CompositeDisposable for multiple events in OnApplyTemplate:**
```csharp
private readonly SerialDisposable _eventSubscriptions = new SerialDisposable();

protected override void OnApplyTemplate()
{
    _eventSubscriptions.Disposable = null;
    var registrations = new CompositeDisposable();
    _eventSubscriptions.Disposable = registrations;

    if (GetTemplateChild<Border>(c_contentClip) is Border contentClip)
    {
        contentClip.SizeChanged += OnContentClipSizeChanged;
        registrations.Add(() => contentClip.SizeChanged -= OnContentClipSizeChanged);
    }
}
```

**Loaded/Unloaded lifecycle pattern** (for controls needing cleanup):
```csharp
#if HAS_UNO
// Uno specific: we might leak on iOS
Loaded += Control_Loaded;
Unloaded += Control_Unloaded;
#endif

private void Control_Loaded(object sender, RoutedEventArgs e)
{
    if (_eventSubscriptions.Disposable is null)
    {
        OnApplyTemplate();
    }
}

private void Control_Unloaded(object sender, RoutedEventArgs e) =>
    _eventSubscriptions.Disposable = null;
```

#### 2.6. Destructors and Cleanup

Uno Platform **does not use finalizers** for event cleanup. When C++ contains a destructor:

- **Do not generate a C# finalizer.**
- Emit the destructor logic as commented-out code inside `#if HAS_UNO`:

```csharp
#if HAS_UNO
// TODO Uno: Original C++ destructor cleanup. Uno does not support cleanup via finalizers.
// Move this logic into Loaded/Unloaded event handlers or other lifecycle methods to avoid leaks.

// Original destructor logic (not executed):
// <... commented original cleanup ...>
#endif
```

All cleanup steps must remain in comments exactly as they appeared in C++.

#### 2.7. Common Helpers

| C++ Pattern | C# Helper |
|------------|-----------|
| `GetTemplateChildT<T>(name, *this)` | `GetTemplateChild<T>(name)` |
| `SetDefaultStyleKey` | `this.SetDefaultStyleKey()` (only for WinUI-only controls not in UWP) |
| Localized string resources | `ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_ConstantName)` |
| `SharedHelpers::MakeIconElementFrom` | `SharedHelpers.MakeIconElementFrom(source)` |
| `SharedHelpers::IsRS5OrHigher()` | `SharedHelpers.IsRS5OrHigher()` |
| `MUX_ASSERT` | `MUX_ASSERT(expr)` with `using static Microsoft.UI.Xaml.Controls._Tracing;` |

#### 2.8. Conditional Compilation

**Uno-specific code** (Uno helpers, Uno macros, Uno-specific constructs):
```csharp
#if HAS_UNO
// Uno-specific implementation
#endif
```

**Composition/Visual-layer APIs** that are not fully available in Uno:
```csharp
#if !HAS_UNO
// WinUI implementation using Composition APIs
var visual = ElementCompositionPreview.GetElementVisual(element);
visual.Clip = visual.Compositor.CreateInsetClip();
#else
// TODO Uno specific: The Composition clipping APIs are currently unsupported.
// Using UIElement.Clip or SizeChanged workaround instead.
element.SizeChanged += OnElementSizeChanged;
registrations.Add(() => element.SizeChanged -= OnElementSizeChanged);
#endif
```

**WinUI import differences:**
```csharp
#if HAS_UNO_WINUI
using ITextSelection = Microsoft.UI.Text.ITextSelection;
#else
using ITextSelection = Windows.UI.Text.ITextSelection;
#endif
```

#### 2.9. Dependency Properties (in `.Properties.cs`)

```csharp
/// <summary>
/// Gets or sets a value indicating whether the control is expanded.
/// </summary>
public bool IsExpanded
{
    get => (bool)GetValue(IsExpandedProperty);
    set => SetValue(IsExpandedProperty, value);
}

public static DependencyProperty IsExpandedProperty { get; } =
    DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(ControlName),
        new FrameworkPropertyMetadata(
            false,
            (s, e) => (s as ControlName)?.OnIsExpandedPropertyChanged(e)));
```

#### 2.10. TODO Comment Conventions

Use consistent TODO formats:

| Scenario | Format |
|----------|--------|
| Uno workaround | `// TODO Uno specific: <explanation>` |
| Missing API | `// TODO Uno: Missing Uno equivalent.\n// Original C++:\n// <original code>` |
| Unimplemented section | Wrap in `#if false` with comment |
| Future work | `// TODO Uno: <explanation>` |
| Not ported yet | `// TODO Uno: NOT PORTED - <description> (lines X-Y of File.cpp)` |
| Potential leak | `// TODO Uno: Investigate potential leak: <reason>` |
| Stub method | `// TODO Uno: Stub - needs full implementation from <SourceFile.cpp>` |

#### 2.11. Member Visibility

**Default to the most restrictive visibility that still compiles.** Only widen when there is explicit evidence from one of these authoritative sources:
- The **IDL file** declares the member as public/protected
- **Microsoft Learn** documentation lists it as part of the public API
- The member is already declared in Uno's **Generated stub** as public/protected
- The member is needed by **another class in the same assembly** (use `internal`)
- The member must be **overridable by subclasses outside the assembly** (use `protected`)

When none of the above apply, use `private`. When the member must be accessible to subclasses only within the assembly, use `private protected`. Specific rules:

- No new `public` members unless strictly required by the API surface.
- Prefer `private` for all implementation details, helper methods, and fields.
- Use `private protected` for members that subclasses need to call or override, but that are not part of the published API — including debug/test-hook members guarded by `#ifdef DBG` in C++ or documented as "for testing purposes only".
- Use `internal` only when accessed by other Uno infrastructure types within the same assembly.
- Use `protected` only when the IDL, Microsoft Learn, or Generated stubs confirm the member is part of the public overridable API surface.
- **Never widen visibility** based on convenience, grouping preference, or to simplify testing.

#### 2.12. XML Documentation

All ported **public or protected** members must include XML documentation comments sourced from the Microsoft Learn API reference fetched in Phase 0. This includes:

- `public` properties, methods, and events
- `protected` and `protected internal` virtual/override methods and properties (since subclasses will see them)

Use `<summary>`, `<param>`, `<returns>`, and `<value>` tags as appropriate, copying the exact wording from Microsoft Learn where available:

```csharp
/// <summary>
/// Gets the orientation, if any, in which items are laid out based on their
/// index in the source collection.
/// </summary>
/// <value>
/// A value of the enumeration that indicates the orientation. The default is
/// <see cref="IndexBasedLayoutOrientation.None"/>.
/// </value>
public IndexBasedLayoutOrientation IndexBasedLayoutOrientation => ...;

/// <summary>
/// Sets the value of the <see cref="IndexBasedLayoutOrientation"/> property.
/// </summary>
/// <param name="orientation">
/// A value of the enumeration that indicates the orientation, if any, in which
/// items are laid out based on their index in the source collection.
/// </param>
protected void SetIndexBasedLayoutOrientation(IndexBasedLayoutOrientation orientation) => ...;
```

If Microsoft Learn has no documentation for a member (e.g., it is new or internal-only), write a brief `<summary>` that describes what the method does based on the C++ source. Do not leave public/protected members undocumented.

### Phase 3: Handle Associated Types

Auto-detect and port these associated types from the same source directory:

1. **TemplateSettings** (`ControlNameTemplateSettings`): Port as a separate class with its own `.cs` file.
2. **AutomationPeer** (`ControlNameAutomationPeer`): Port as a separate class following the same file layout patterns.
3. **EventArgs** (`ControlNameEventArgs`, `ControlNameChangedEventArgs`, etc.): Port each as its own `.cs` file.
4. **Enums**: Port any enum types defined in the IDL that are used by the control.

### Phase 4: Handle Resources

1. **ResourceAccessor constants**: Find the `ResourceAccessor` class (search for `ResourceAccessor.cs` in `src/Uno.UI/`) and add new `SR_` constants for any localized strings used by the ported control.

2. **Strings folder**: If the WinUI source directory contains a `Strings/` folder with `.resw` localized string files, copy them to the appropriate location in the Uno project (typically alongside the control's output directory or in the shared resources location). Maintain the folder structure (e.g., `Strings/en-US/Resources.resw`).

### Phase 5: Handle XAML Styles

1. Check if the control has a XAML template/style in the WinUI source (typically in a `*_themeresources.xaml` or similar file in the control's source directory).
2. Check if an equivalent style already exists in the Uno repo under `src/Uno.UI.FluentTheme/` (search for the control name in `.xaml` files).
3. If the style does not exist or differs significantly:
   - For **new controls**: Create the style in `Uno.UI.FluentTheme.v2` (search the repo for the exact directory structure used by similar controls).
   - For **existing controls with updates**: Update the existing style file.

### Phase 6: Update Generated Files

1. Find the Generated stub files for the ported types in `src/Uno.UI/Generated/` (and `src/Uno.UWP/Generated/` if applicable).
2. For each newly implemented member:
   - Remove the member from the `[NotImplemented]` attribute's platform list, or remove the attribute entirely if all platforms are now implemented.
   - Adjust `#if` directives to reflect which platforms have the implementation.
3. **NEVER put any implementation code** into Generated files — only modify attributes and `#if` directives.

### Phase 7: Port Tests

1. Look for test files associated with the control in the WinUI source:
   - Unit tests (typically in a `*Tests*` directory near the control source)
   - UI/interaction tests
2. Convert C++ tests to Uno RuntimeTests format:
   - Place tests in `src/Uno.UI.RuntimeTests/Tests/` in an appropriate subdirectory
   - Use the Uno RuntimeTest patterns:
     ```csharp
     [TestMethod]
     [RunsOnUIThread]
     public async Task When_ControlName_Does_Something()
     {
         var control = new ControlName();
         WindowHelper.WindowContent = control;
         await WindowHelper.WaitForLoaded(control);
         await WindowHelper.WaitForIdle();

         // Assert behavior...
     }
     ```
   - Convert C++ test assertions to MSTest assertions (`Assert.AreEqual`, `Assert.IsTrue`, etc.)
   - Preserve the intent and coverage of each original test

### Phase 8: Build Check (Optional)

After all files are written, ask the user:

> "Would you like me to run a build to check for compilation errors?"

If the user agrees:
- Run `dotnet build` on the appropriate solution filter
- Report any errors in the summary
- Suggest fixes for common issues

### Phase 9: Summary

Produce a detailed summary containing:

1. **Files created/modified**: Full list with paths
2. **Porting scope**: Full control vs. snippet, what was included
3. **Associated types ported**: TemplateSettings, AutomationPeer, EventArgs, Enums
4. **Resources added**: New SR_ constants, copied Strings folders
5. **XAML styles**: Created or updated style files
6. **Generated files updated**: Which NotImplemented attributes were modified
7. **Tests ported**: List of test methods converted
8. **TODO items**: All `TODO Uno:` items generated, categorized by:
   - Missing APIs / unresolved symbols
   - Potential leaks
   - Unported sections
   - Composition API workarounds
9. **Potential issues**: Anything that may need manual attention
10. **Next steps**: Recommended actions (build, test, review specific TODOs)

---

## Porting Rules Quick Reference

This section is a condensed reference of all conversion patterns. Use it as a checklist while porting.

### Namespace Mappings

| C++ | C# |
|-----|-----|
| `winrt::Microsoft::UI::Xaml::Controls` | `Microsoft.UI.Xaml.Controls` |
| `winrt::Microsoft::UI::Xaml` | `Microsoft.UI.Xaml` |
| `winrt::Windows::Foundation` | `Windows.Foundation` |
| `winrt::Windows::UI::Xaml::Automation` | `Microsoft.UI.Xaml.Automation` |

### Type Mappings

| C++ Type | C# Type |
|----------|---------|
| `winrt::hstring` | `string` |
| `winrt::IInspectable` | `object` |
| `winrt::float4` | `Vector4` |
| `winrt::Windows::Foundation::Size` | `Size` |
| `winrt::Windows::Foundation::Rect` | `Rect` |
| `winrt::Windows::Foundation::Point` | `Point` |
| `winrt::Windows::Foundation::TimeSpan` | `TimeSpan` |
| `tracker_ref<T>` | `T` (direct field) |
| `winrt::weak_ref<T>` | `WeakReference<T>` |
| `winrt::com_ptr<T>` | `T` (direct reference) |
| `event_source<T>` | C# event declaration |

### Keyword Mappings

| C++ | C# |
|-----|-----|
| `nullptr` | `null` |
| `true` / `false` | `true` / `false` |
| `static_cast<T>(x)` | `(T)x` or `x as T` |
| `auto` | `var` |
| `const auto` | `var` |
| `this->` / `*this` | `this` |
| `co_await` | `await` |
| `co_return` | `return` |
| `winrt::fire_and_forget` | `async void` or `async Task` |

### Method Pattern Mappings

| C++ Pattern | C# Pattern |
|------------|------------|
| `value()` (property getter call) | `Value` (property access) |
| `value(newVal)` (property setter call) | `Value = newVal` |
| `Container().Children().Append(element)` | `Container.Children.Add(element)` |
| `items.Size()` | `items.Count` |
| `str.empty()` | `string.IsNullOrEmpty(str)` |
| `collection.GetAt(i)` | `collection[i]` |
| `collection.SetAt(i, v)` | `collection[i] = v` |
| `collection.InsertAt(i, v)` | `collection.Insert(i, v)` |
| `collection.RemoveAt(i)` | `collection.RemoveAt(i)` |
| `collection.Clear()` | `collection.Clear()` |
