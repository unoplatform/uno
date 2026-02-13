# Add-In Discovery System Design

> **Parent**: [Main Spec](spec.md) — Section 7
> **Related**: [Discovery Roadmap](spec-appendix-f-discovery-roadmap.md) | [Startup Workflow](spec-appendix-a-startup-workflow.md)

The add-in discovery system must be **forward-compatible** (new add-ins work without code changes) and **backward-compatible** (older SDK versions still work).

---

## 1. Discovery Priority Chain

```
1. devserver-addin.json manifest     (highest priority if present)
2. buildTransitive/*.targets parsing (primary, convention-based)
3. tools/devserver/ directory check  (diagnostic only — see WARNING below)
4. MSBuild dotnet build              (legacy fallback, 10-30s)
```

> **WARNING**: Levels are NOT cumulative. Only levels 1 and 2 produce loadable DLL paths. Level 3 is a **diagnostic signal** that a package might be an add-in but couldn't be resolved, NOT a source of DLL paths. Level 4 (MSBuild) is triggered only when levels 1-2 find zero add-ins for an SDK version that should have them.
>
> **Why not cumulative**: `tools/devserver/` contains the entry point DLL + all its dependencies (dozens of DLLs). Blindly loading all of them via `Assembly.LoadFrom()` would cause load errors, DI conflicts, and non-deterministic behavior. Only the specific entry point DLL identified by manifest, `.targets`, or MSBuild should be loaded.

---

## 2. Convention-Based `.targets` Parsing (Primary)

```
ParseTargetsForAddIns(packagesJsonPath, nugetCachePaths[]):
  1. Parse packages.json -> list of (packageName, version) groups
  2. For each (packageName, version):
     a. Locate package dir in NuGet cache
     b. List files matching: {packageDir}/buildTransitive/*.targets
        If none found, fallback: {packageDir}/build/*.targets
     c. For each .targets file:
        i.  Parse as XML
        ii. Build a property dictionary:
            - $(MSBuildThisFileDirectory) = directory of .targets file (with trailing slash)
            - $(MSBuildThisFile) = filename of .targets file
        iii. Resolve <PropertyGroup> elements:
            - For each property with a simple value containing $(MSBuildThisFileDirectory),
              substitute and store in the property dictionary
            - Evaluate exists() conditions where possible (check disk)
            - Ignore conditions referencing unknown properties ($(Configuration), etc.)
        iv. Find all <UnoRemoteControlAddIns Include="..."/> items
        v.  Substitute property references in Include value
        vi. Normalize path (resolve ..), verify DLL exists on disk
        vii. If valid, add to result set
  3. Return { resolvedPaths[], warnings[] }
```

**Why this works for forward compatibility**: Any new add-in package that follows the convention (`buildTransitive/*.targets` + `UnoRemoteControlAddIns` item + `tools/devserver/` layout) is discovered automatically. No code changes, no hardcoded lists.

**Why this works for backward compatibility**: Existing packages (`uno.ui.app.mcp`, `uno.settings.devserver`) already follow this convention. The `.targets` files have been shipping in NuGet packages for years.

### `.targets` Property Resolution

The parser handles a safe subset of MSBuild properties:

| Property | Resolution | Example |
|----------|-----------|---------|
| `$(MSBuildThisFileDirectory)` | Directory of the `.targets` file (with trailing separator) | `{cache}/pkg/1.0/buildTransitive/` |
| `$(MSBuildThisFile)` | Filename of the `.targets` file | `Uno.UI.App.Mcp.targets` |
| `$({UserProperty})` | Resolved from `<PropertyGroup>` elements in the same file | `_UnoMcpServerProcessorPath` |
| `exists('...')` | Checked on disk | File system check |

Properties referencing external state (`$(Configuration)`, `$(UsingUnoSdk)`, etc.) are ignored — these are only used for local development fallback paths that don't apply to NuGet cache resolution.

---

## 3. Directory Presence Check (Diagnostic Only)

```
CheckDirectoriesForUnresolvedAddIns(packagesJsonPath, nugetCachePaths[], alreadyResolved[]):
  For each (packageName, version) in packages.json:
    packageDir = findInNugetCache(packageName, version)
    devserverDir = {packageDir}/tools/devserver/
    If directory exists AND packageName NOT in alreadyResolved:
      Log warning: "Package {packageName} has tools/devserver/ but no UnoRemoteControlAddIns in .targets"
      Add to diagnostics (surfaced via uno://health)
      Do NOT load any DLLs from this directory
```

This serves as a **diagnostic signal** to detect packages that might be add-ins but couldn't be resolved via `.targets` parsing. It does NOT produce loadable paths. See WARNING in section 1.

---

## 4. Known Add-in Package Patterns

| Package | `.targets` Pattern | DLL Path | Status |
|---------|-------------------|----------|--------|
| `uno.ui.app.mcp` | Property indirection + `exists()` condition | `tools/devserver/Uno.UI.App.Mcp.Server.dll` | Active |
| `uno.settings.devserver` | Direct include | `tools/devserver/Uno.Settings.DevServer.dll` | Active |
| *(future add-ins)* | Should follow either pattern | `tools/devserver/{Name}.dll` | Convention |

---

## 5. DevServer Add-In Author Guide

> **Note**: This guide is for Uno maintainers and internal contributors. It should live in the source code alongside the DevServer Host (e.g., `src/Uno.UI.RemoteControl.Host/DEVSERVER-ADDINS.md`), not in `/doc` which is reserved for public-facing documentation on `docs.platform.uno`.

### Checklist for Creating a DevServer Add-In

1. **Create a .NET class library** targeting `net9.0` (or current .NET version)
2. **Reference `Uno.WinUI.DevServer.Messaging`** for `IIdeChannel` and messaging contracts
3. **Register services** via one of:
   - `[assembly: ServerProcessor(typeof(MyProcessor))]` for `IServerProcessor` implementations
   - `[assembly: ServiceCollectionExtension(typeof(MyRegistration))]` for DI-based registration
4. **Package the DLL** under `tools/devserver/` in your NuGet package:
   ```xml
   <!-- In your .csproj -->
   <Target Name="_PackAddIn" AfterTargets="CopyFilesToOutputDirectory">
     <ItemGroup>
       <Content Include="$(OutputPath)/**/*.*">
         <Pack>true</Pack>
         <PackagePath>tools/devserver</PackagePath>
       </Content>
     </ItemGroup>
   </Target>
   ```
5. **Create a `.targets` file** in `buildTransitive/`:
   ```xml
   <!-- buildTransitive/MyPackage.targets -->
   <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
     <ItemGroup>
       <UnoRemoteControlAddIns
         Include="$(MSBuildThisFileDirectory)../tools/devserver/MyAddin.dll" />
     </ItemGroup>
   </Project>
   ```
6. **Pack the `.targets` file**:
   ```xml
   <ItemGroup>
     <Content Include="buildTransitive/**">
       <Pack>true</Pack>
       <PackagePath>buildTransitive</PackagePath>
     </Content>
   </ItemGroup>
   ```
7. **(Optional) Add a `devserver-addin.json` manifest** at the package root for explicit declaration (see section 6).
8. **Test**: Verify your add-in is discovered by `dnx uno.devserver disco --json`

### Convention Rules (for fast discovery)

- `.targets` file MUST be in `buildTransitive/` (not `build/`)
- `UnoRemoteControlAddIns` Include path MUST use `$(MSBuildThisFileDirectory)` as the base
- DLL MUST be under `tools/devserver/` relative to package root
- Avoid complex MSBuild conditions on the `UnoRemoteControlAddIns` item (simple `exists()` is OK)
- If your `.targets` has conditional logic beyond `exists()`, also provide `devserver-addin.json`

---

## 6. Manifest-First Discovery (Updated Priority Chain)

With the `devserver-addin.json` manifest defined (see main spec section 5), the per-package discovery priority chain becomes:

```
For each package in packages.json:
  1. Check {packageRoot}/devserver-addin.json
     → If found and version == 1: use entryPoint paths (DONE for this package)
     → If found and version > 1: warn, fall through to step 2
     → If not found: fall through to step 2

  2. Parse buildTransitive/*.targets for UnoRemoteControlAddIns
     → If UnoRemoteControlAddIns found: resolve paths (DONE for this package)
     → If not found: fall through to step 3

  3. Check if tools/devserver/ directory exists
     → If yes: log diagnostic warning (entry point unknown, do NOT load DLLs)
     → If no: skip (not an add-in package)
```

**After all packages processed**:
- If zero add-ins resolved and SDK version suggests add-ins should exist → trigger MSBuild fallback
- If **fewer add-ins resolved than expected** (e.g., 1 of 2 known add-ins for current SDK) → log diagnostic warning via `uno://health`, but do NOT automatically fall back to MSBuild (partial success is better than 10-30s delay). The warning includes the unresolved package names so the user/agent can investigate.
- MSBuild fallback can also be forced via `--force-msbuild-discovery`

**Migration path for add-in authors**:
1. **Today**: Ship `buildTransitive/*.targets` with `UnoRemoteControlAddIns` (works with fast discovery)
2. **Next**: Add `devserver-addin.json` to package root (takes priority, simpler, no XML parsing needed)
3. **Future**: Remove `.targets` `UnoRemoteControlAddIns` entry once minimum supported DevServer version supports manifests

---

## 7. Package Layout Reference

```
{packageId}/
├── buildTransitive/
│   └── {PackageId}.targets          ← REQUIRED: registers UnoRemoteControlAddIns
├── tools/
│   └── devserver/
│       ├── {AddInName}.dll          ← Add-in entry point
│       ├── {AddInName}.deps.json
│       └── (dependencies)
├── devserver-addin.json             ← OPTIONAL: explicit manifest (takes priority over .targets)
└── (other package content)
```

### Example: `uno.settings.devserver/1.2.3/`

```
uno.settings.devserver/1.2.3/
├── buildTransitive/
│   └── Uno.Settings.DevServer.targets
│       → <UnoRemoteControlAddIns Include="$(MSBuildThisFileDirectory)../tools/devserver/Uno.Settings.DevServer.dll"/>
├── tools/
│   ├── devserver/
│   │   ├── Uno.Settings.DevServer.dll
│   │   └── (dependencies)
│   └── manager/
│       └── Uno.Settings.dll
└── (other content)
```

### Example: `uno.ui.app.mcp/6.5.100/`

```
uno.ui.app.mcp/6.5.100/
├── buildTransitive/
│   └── Uno.UI.App.Mcp.targets
│       → <_UnoMcpServerProcessorPath>$(MSBuildThisFileDirectory)../tools/devserver/Uno.UI.App.Mcp.Server.dll</>
│       → <UnoRemoteControlAddIns Include="$(_UnoMcpServerProcessorPath)"/>
├── tools/
│   └── devserver/
│       ├── Uno.UI.App.Mcp.Server.dll
│       └── (dependencies)
├── lib/
│   └── net9.0/
│       └── Uno.UI.App.Mcp.Client.dll
└── (other content)
```
