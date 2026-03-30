# Warnings as Errors - Phased Plan

## Current State

`TreatWarningsAsErrors` is **already `true`** in `src/Directory.Build.props:255`, but the build succeeds clean because of extensive `NoWarn` and `WarningsNotAsErrors` entries that hide real issues.

## Full Warning Inventory

All suppressions were removed and SamplesApp.Skia.Generic was built for net10.0. Here's everything that's hiding:

### C# Compiler Warnings (affected by TreatWarningsAsErrors)

| Code | Count | Unique Files | Location | Description |
|------|-------|-------------|----------|-------------|
| **IDE0051** | 144 | 53 | SamplesApp (82), RuntimeTests (62) | Unused private members |
| **CS0436** | 80 | 1 | `Uno.UI/XmlnsDefinitionAttribute.cs` | Type conflict (dotnet/runtime#103205) |
| **UNO0008** | 2 | 1 | `SamplesApp.Shared/App.xaml.cs` | Obsolete `EnableHotReload()` API |

### MSBuild Warnings (NOT affected by TreatWarningsAsErrors)

| Code | Count | Location | Description |
|------|-------|----------|-------------|
| **UNOB0003** | 100 | Uno.UI.Tasks.targets | Resource language detection failures (quz-PE, zh-CN, etc.) |
| **MSB3836** | 8 | Uno.UI.Tasks | Binding redirect conflicts |
| **MSB3026/3027/3021** | 12 | Uno.UI.Tasks | File copy locks (transient) |

### Latent Warnings (don't fire for net10.0 Skia but would for other targets/configs)

| Code | Trigger | Description |
|------|---------|-------------|
| **CA1416** | Platform-specific targets (android, ios) | Cross-platform call sites. Intentionally suppressed - Uno.UI IS the abstraction layer |
| **CS1711/1712/1572/1574/1570** | When `GenerateDocumentationFile=true` | Doc comment issues (tagged "YOUSSEF TODO") |
| **IDE0055** | Release builds, or CI on Linux with UWP | Formatting violations |
| **CS1998** | RuntimeTests only | Async method without await (per-project `NoWarn`) |

### Additional .editorconfig suppressions

- `src/SourceGenerators/.editorconfig`: `IDE0055 = none`
- `src/Uno.UI/Microsoft/.editorconfig`: `CA1805 = none`
- `src/Uno.UI/UI/Xaml/.editorconfig`: `CA1805 = none`

---

## Phased Plan

### Phase 1: Fix IDE0051 - Unused Private Members (53 files, low risk)

**Effort: Medium** | **Risk: Low** | **Impact: High (removes largest suppression)**

- Fix 53 files across SamplesApp and RuntimeTests by removing or using unused private members
- Remove `NoWarn IDE0051` from the `Otherwise` block (line ~307)
- Remove `IDE0051` from `WarningsNotAsErrors` in Debug (line 257)
- Zero instances in core libraries, so no framework risk

### Phase 2: Fix UNO0008 - Obsolete Studio API (1 file, trivial)

**Effort: Trivial** | **Risk: Low** | **Impact: Low**

- Update `SamplesApp.Shared/App.xaml.cs:330` to use `UseStudio()` instead of `EnableHotReload()`
- Remove `NoWarn UNO0008`

### Phase 3: Address UNOB0003 - Resource Language Detection (100 warnings)

**Effort: Medium** | **Risk: Low** | **Impact: Medium (cleans up build output)**

- Fix `Uno.UI.Tasks` resource processing to handle locales like `quz-PE`, `zh-CN`, `zh-TW`, `ca-Es-VALENCIA`, `pa-IN`
- These are MSBuild warnings, so need `MSBuildWarningsAsErrors` or a targeted `WarningsAsErrors` for `UNOB0003`
- Alternative: add these locales to the language detection logic

### Phase 4: Fix MSB3836 - Binding Redirect Conflicts (8 warnings)

**Effort: Low** | **Risk: Low** | **Impact: Low**

- Clean up `Uno.UI.Tasks.csproj` binding redirect configuration
- Remove explicit binding redirects that conflict with auto-generated ones

### Phase 5: Address CS17xx - Doc Comment Warnings (unknown count, potentially large)

**Effort: High** | **Risk: Medium** | **Impact: Medium**

- Enable `GenerateDocumentationFile=true` to discover actual count
- Fix doc comments across the codebase (CS1711, CS1712, CS1572, CS1574, CS1570)
- Remove the `NoWarn` entry (already tagged "YOUSSEF TODO")
- This is likely the largest effort since doc comments span the entire framework

### Phase 6: Tighten IDE0055 - Formatting (Debug config)

**Effort: Low-Medium** | **Risk: Low** | **Impact: Medium**

- Run `dotnet format` to auto-fix formatting issues
- Remove `IDE0055` from `WarningsNotAsErrors` in Debug
- Keep the CI/UWP/Linux conditional suppression (legitimate cross-platform issue)

### Phase 7: Tighten NU* - NuGet Warnings (Debug config)

**Effort: Low** | **Risk: Medium** | **Impact: Low**

- Evaluate which NuGet warnings (NU1202, NU1701, NU1901-1904, NU1604) can be resolved
- Some may be inherent to the multi-targeting setup and need to stay

### Phase 8: Evaluate CA1416 (long-term / may stay suppressed)

**Effort: N/A** | **Risk: High if removed globally** | **Impact: Framework-wide**

- CA1416 is suppressed because Uno.UI provides the cross-platform abstraction itself
- Could selectively enable for SamplesApp/RuntimeTests but not for core libs
- Likely needs to stay globally suppressed for `src/Uno.UI/` and `src/Uno.UWP/`

## Keep Suppressed (permanent/blocked)

| Code | Reason |
|------|--------|
| **CS0436** | Blocked on dotnet/runtime#103205 - cannot fix until upstream resolves |
| **CA1416** | Architectural - Uno.UI IS the platform abstraction, every file would trigger this |
| **MSTEST0001** | Test configuration preference, not a code quality issue |
| **MSB3026/3027/3021** | Transient file-locking during parallel builds, not actionable |

---

**Recommended starting point**: Phase 1 (IDE0051) gives the best effort-to-impact ratio - it's the largest warning count, all in test/sample code, and many can be fixed mechanically by deleting unused members.
