---
description: Create a SamplesApp sample page with correct theming and attributes. Use when adding UI samples for controls.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **Add Sample Skill**. This skill creates a SamplesApp sample page with correct XAML, code-behind, and attribute setup. Files dropped under `src/SamplesApp/SamplesApp.Samples/` are auto-discovered by glob — no manual project registration is required.

---

## Execution Workflow

### Phase 0: Determine Location & Names

1. Parse user input for:
   - **Control name**: The control being demonstrated (e.g., `Button`, `TreeView`, `NavigationView`)
   - **Scenario**: What the sample demonstrates (e.g., `BasicUsage`, `CustomStyle`, `DataBinding`)

2. Find existing folder under `src/SamplesApp/SamplesApp.Samples/` matching the control's namespace. Folders are a nested hierarchy mirroring the API namespace, same as `src/Uno.UI`:
   - WinUI controls: `Microsoft/UI/Xaml/Controls/` (e.g., `Microsoft/UI/Xaml/Controls/NavigationViewTests/`)
   - XAML framework: `Windows/UI/Xaml/Controls/` (e.g., `Windows/UI/Xaml/Controls/Button/`)
   - Shapes: `Windows/UI/Xaml/Shapes/`
   - Media: `Windows/UI/Xaml/Media/`
   - Search for existing samples of the same control to find the right folder

3. Generate file names: `ControlName_Scenario.xaml` and `ControlName_Scenario.xaml.cs`
   - Follow the naming convention of nearby existing samples
   - Use PascalCase with underscores separating control from scenario

### Phase 1: Create XAML Page

Create the XAML file with:
- Standard `Page` root element
- Correct namespace declarations for the control
- `{ThemeResource ApplicationPageBackgroundThemeBrush}` for Background (**NOT hardcoded colors** — supports light/dark theme)
- The control being demonstrated with meaningful property settings
- Follow patterns from nearby existing samples in the same folder

**Template:**
```xml
<Page x:Class="UITests.FolderNamespace.ControlName_Scenario"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      mc:Ignorable="d"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <StackPanel Spacing="8" Padding="16">
        <!-- Sample content here -->
    </StackPanel>
</Page>
```

### Phase 2: Create Code-Behind

Create the code-behind file with:
- Namespace copied from neighbouring samples in the same folder, e.g. `UITests.Shared.Windows_UI_Xaml_Controls.Button` for `Windows/UI/Xaml/Controls/Button/`. Namespaces keep the legacy underscore form — don't derive them from the nested folder path.
- `[Sample("CategoryName")]` attribute from `Uno.UI.Samples.Controls`
- `sealed partial class` inheriting from `Page`
- `this.InitializeComponent()` in constructor

**Template:**
```csharp
using Uno.UI.Samples.Controls;

namespace UITests.FolderNamespace;

[Sample("CategoryName", Description = "Brief description of what this sample demonstrates")]
public sealed partial class ControlName_Scenario : Page
{
    public ControlName_Scenario()
    {
        this.InitializeComponent();
    }
}
```

**Available `[Sample]` attribute properties:**
| Property | Type | Usage |
|----------|------|-------|
| Constructor parameter | `string` | Category name (required) — displayed in sample browser |
| `Name` | `string` | Display name (defaults to class name) |
| `Description` | `string` | Expected behavior explanation |
| `IsManualTest` | `bool` | For animations, external dependencies |
| `IgnoreInSnapshotTests` | `bool` | Skip in automated screenshot tests |
| `ViewModelType` | `Type` | Auto-set as DataContext |

### Phase 3: Format XAML

Run XamlStyler on the new XAML file to ensure it matches the project's formatting standards:
```bash
dotnet xstyler -f src/SamplesApp/SamplesApp.Samples/FolderName/SampleName.xaml
```

### Phase 4: Verification

1. **Build** to verify compilation:
   ```bash
   dotnet build src/SamplesApp/SamplesApp/SamplesApp.csproj -f net11.0-desktop
   ```

2. Remind the user:
   > "Run SamplesApp and search for 'ControlName_Scenario' in the sample browser to verify the sample appears and renders correctly."

---

## Key File References

- **Sample attribute source:** `src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UITests/Views/Controls/SampleAttribute.cs`
- **Sample folder:** `src/SamplesApp/SamplesApp.Samples/` (XAML and `.cs` are picked up by glob)
- **Example samples:** Browse `src/SamplesApp/SamplesApp.Samples/Windows/UI/Xaml/Controls/` for patterns

## Common Mistakes to Avoid

1. **Hardcoding Background colors** — use `{ThemeResource ApplicationPageBackgroundThemeBrush}` instead
2. **Wrong namespace in code-behind** — copy it from neighbouring samples in the same folder; it won't match the nested folder path verbatim
3. **Missing `InitializeComponent()` call** — XAML won't be loaded
4. **Wrong `x:Class` in XAML** — must match the fully qualified class name in code-behind
