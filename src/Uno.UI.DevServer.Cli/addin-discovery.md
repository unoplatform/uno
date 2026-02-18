# Add-in Discovery

Add-in resolution follows a **priority chain** -- each resolver either returns a result or falls through to the next:

```
Manifest (devserver-addin.json)  -->  .targets parsing  -->  MSBuild fallback
```

## Manifest-First (`ManifestAddInResolver`, instant)

**File:** `Helpers/ManifestAddInResolver.cs`

Looks for `devserver-addin.json` at the root of each NuGet package. Three return states:

| Return | Meaning | Chain behavior |
|--------|---------|----------------|
| `null` | No manifest found, or `version > 1` | Fall through to `.targets` |
| Empty result | Manifest found but parse error | Stop chain for this package |
| Populated result | Success, add-ins resolved | Use result |

Forward-compatibility: when `manifest.Version > 1`, the resolver logs a warning and returns `null` so older CLIs gracefully degrade to `.targets` parsing.

### Data Model

**Files:** `Helpers/AddInManifest.cs`, `Helpers/AddInManifestEntry.cs`

```json
{
  "version": 1,
  "addins": [
    {
      "entryPoint": "lib/net10.0/MyAddIn.dll",
      "minHostVersion": "5.6.0"
    }
  ]
}
```

- `entryPoint` -- relative DLL path (forward slashes normalized to platform separators)
- `minHostVersion` -- optional; skipped if current host version is lower

### Validation

1. JSON parse (catches `JsonException`, `IOException`)
2. `entryPoint` is not null/whitespace
3. Entry point file exists on disk
4. `minHostVersion` semver check (if present)

## `.targets` Parsing (`TargetsAddInResolver`, ~200ms)

**File:** `Helpers/TargetsAddInResolver.cs`

Parses `packages.json` from the Uno SDK and `buildTransitive/*.targets` files from NuGet packages to extract `<UnoRemoteControlAddIns>` items without invoking MSBuild.

## MSBuild Fallback (Host-side, 10-30s)

**File:** `../Uno.UI.RemoteControl.Host/Extensibility/AddIns.cs`

When the CLI fast paths fail, the Host falls back to `dotnet build` evaluation. The `--addins` flag on the Host bypasses this when paths are pre-resolved by the CLI.

## Integration with DevServerMonitor

`DevServerMonitor.RunMonitor()` runs discovery via `UnoToolsLocator.DiscoverAsync()`, which invokes the resolvers. Resolved add-in paths are stored in `LastDiscoveryInfo` and passed to the Host via `--addins` when starting the process.

## Adding a Manifest Field

1. Add the property to `AddInManifest.cs` or `AddInManifestEntry.cs` with `[JsonPropertyName]`
2. Update `ManifestAddInResolver.cs` to read and act on the new field
3. Add test in `ManifestAddInResolverTests.cs` covering the new field
