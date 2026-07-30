---
description: Conventions for adding/editing SamplesApp samples (auto-discovery, attributes, theming, XAML formatting). Auto-loaded under src/SamplesApp. The /add-sample skill scaffolds correctly.
paths:
  - "src/SamplesApp/**"
---

# SamplesApp samples

Scaffold new samples with the **`/add-sample`** skill — it gets attributes, namespace, and theming right. Key rules it follows:

- **Auto-discovered by glob** (`SamplesApp.Samples.props` includes `**/*.xaml` + `**/*.xaml.cs`) — no manual project registration. `SamplesApp.Samples.csproj` is `NoBuild=true`; samples compile into each head (Skia/Wasm/Windows…), so they must compile on every head.
- **Mark the code-behind** with `[Sample("Category")]` (category required; optional `Name`, `Description`, `IsManualTest`, `IgnoreInSnapshotTests`, `ViewModelType`, `UsesFrame`). Multiple categories: `[Sample("A", "B")]`.
- **Class is `sealed partial`** deriving from `Page` (full-page sample) or `UserControl` (component), constructor calls `this.InitializeComponent()` first. Wire a ViewModel via the `Loading` event; clean up in `Unloaded`.
- **`x:Class` must match the fully-qualified code-behind class exactly** (case-sensitive). Namespace + folder must mirror each other: `Windows_UI_Xaml_Controls/Button/Button_Events.xaml` → namespace `UITests.Shared.Windows_UI_Xaml_Controls.Button`. File naming is PascalCase `Control_Scenario.xaml`. Co-locate supporting `.cs` (ViewModels) in the same folder.
- **Theme-safe backgrounds**: `Background="{ThemeResource ApplicationPageBackgroundThemeBrush}"`, and `{ThemeResource}` for all fg/bg — hardcoded colors break dark theme and theme CI variants. (Always write "Uno Platform" in any user-facing sample copy, never just "Uno".)
- **Format before pushing**: `dotnet tool restore` then `dotnet xstyler -d src/SamplesApp -r` (config `Settings.XamlStyler`: one attribute per line, attribute reordering on). CI (`xaml-style-check.yml`) fails the PR on drift.
- **Disable a broken sample** by `<Page Remove>` + `<Compile Remove>` in `ItemExclusions.props` (keep the file on disk) — don't delete.
