---
description: Create a SamplesApp sample page with correct registration, theming, and attributes. Use when adding UI samples for controls.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **Add Sample Skill**. This skill creates a SamplesApp sample page with correct XAML, code-behind, attribute setup, and — critically — the manual registration in `UITests.Shared.projitems` that is required because `EnableAutomaticXamlPageInclusion` is `false`.

**The #1 silent failure when adding samples is forgetting the projitems registration.** This skill ensures it never happens.

---

## Execution Workflow

### Phase 0: Determine Location & Names

1. Parse user input for:
   - **Control name**: The control being demonstrated (e.g., `Button`, `TreeView`, `NavigationView`)
   - **Scenario**: What the sample demonstrates (e.g., `BasicUsage`, `CustomStyle`, `DataBinding`)

2. Find existing folder under `src/SamplesApp/UITests.Shared/` matching the control's namespace:
   - WinUI controls: `Microsoft_UI_Xaml_Controls/` (e.g., `Microsoft_UI_Xaml_Controls/NavigationViewTests/`)
   - XAML framework: `Windows_UI_Xaml_Controls/` (e.g., `Windows_UI_Xaml_Controls/Button/`)
   - Shapes: `Windows_UI_Xaml_Shapes/`
   - Media: `Windows_UI_Xaml_Media/`
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
- Namespace matching the folder structure: `UITests.<FolderNamespace>`
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

### Phase 3: Register in projitems (CRITICAL)

**File:** `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`

This step is **MANDATORY** — `EnableAutomaticXamlPageInclusion` is `false`, so without manual registration the sample will silently not appear.

1. Add `<Page>` entry in the appropriate `<ItemGroup>`:
   ```xml
   <Page Include="$(MSBuildThisFileDirectory)FolderName\SampleName.xaml">
     <SubType>Designer</SubType>
     <Generator>MSBuild:Compile</Generator>
   </Page>
   ```

2. Add `<Compile>` entry in the appropriate `<ItemGroup>`:
   ```xml
   <Compile Include="$(MSBuildThisFileDirectory)FolderName\SampleName.xaml.cs">
     <DependentUpon>SampleName.xaml</DependentUpon>
   </Compile>
   ```

3. **Insert in alphabetical order** within the existing `<ItemGroup>` entries for the same folder.

4. **Verify** the entries are correctly placed by checking nearby entries in the projitems file.

### Phase 4: Format XAML

Run XamlStyler on the new XAML file to ensure it matches the project's formatting standards:
```bash
dotnet xstyler -f src/SamplesApp/UITests.Shared/FolderName/SampleName.xaml
```

### Phase 5: Verification

1. **Build** to verify compilation:
   ```bash
   dotnet build src/SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -f net10.0
   ```

2. Remind the user:
   > "Run SamplesApp and search for 'ControlName_Scenario' in the sample browser to verify the sample appears and renders correctly."

---

## Key File References

- **Sample attribute source:** `src/SamplesApp/SamplesApp.UnitTests.Shared/Controls/UITests/Views/Controls/SampleAttribute.cs`
- **Registration file:** `src/SamplesApp/UITests.Shared/UITests.Shared.projitems`
- **Example samples:** Browse `src/SamplesApp/UITests.Shared/Windows_UI_Xaml_Controls/` for patterns

## Common Mistakes to Avoid

1. **Forgetting projitems registration** — the sample will compile but not appear in the browser
2. **Hardcoding Background colors** — use `{ThemeResource ApplicationPageBackgroundThemeBrush}` instead
3. **Wrong namespace in code-behind** — must match the folder structure under `UITests.`
4. **Missing `InitializeComponent()` call** — XAML won't be loaded
5. **Wrong `x:Class` in XAML** — must match the fully qualified class name in code-behind
