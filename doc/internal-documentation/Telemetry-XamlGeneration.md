# XAML Generation Telemetry

**Event Name Prefix:** `uno/generation`

XAML code generation telemetry tracks the performance and behavior of the Uno Platform source generator that processes XAML files into C# code.

## Events

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `generate-xaml` | `IsWasm` (bool), `IsDebug` (bool), `TargetFramework` (string), `UnoRuntime` (string), `IsBuildingUnoSolution` (bool), `IsUiAutomationMappingEnabled` (bool), `DefaultLanguage` (string), `BuildingInsideVisualStudio` (string), `IDE` (string) | `FileCount` (double) | XAML generation started |
| `generate-xaml-done` | - | `Duration` (double) | XAML generation completed successfully |
| `generate-xaml-failed` | `ExceptionType` (string) | `Duration` (double) | XAML generation failed with an exception |

## Property Details

### Generation Start Properties

- **IsWasm**: Whether the build target is WebAssembly
- **IsDebug**: Whether this is a debug build
- **TargetFramework**: The target framework being used (e.g., "net10.0", "net10.0-windows10.0.19041.0")
- **UnoRuntime**: The Uno Platform runtime being targeted (WebAssembly, Skia, Tizen, Reference, Unknown)
- **IsBuildingUnoSolution**: Whether the Uno.UI solution itself is being built
- **IsUiAutomationMappingEnabled**: Whether UI automation mapping is enabled
- **DefaultLanguage**: The default language for the project
- **BuildingInsideVisualStudio**: Whether building inside Visual Studio
- **IDE**: The IDE being used (vswin, vscode, rider, unknown)

### Failure Properties

- **ExceptionType**: The type of exception that caused the generation to fail

## Measurements

- **FileCount**: Number of XAML files being processed
- **Duration**: Time taken for generation in seconds

## Property Value Examples

### Generation Start (`generate-xaml`)

```json
{
  "IsWasm": "true",
  "IsDebug": "true",
  "TargetFramework": "net10.0",
  "UnoRuntime": "WebAssembly",
  "IsBuildingUnoSolution": "false",
  "IsUiAutomationMappingEnabled": "false",
  "DefaultLanguage": "en-US",
  "BuildingInsideVisualStudio": "true",
  "IDE": "vswin",
  "FileCount": 42.0
}
```

### Generation Done (`generate-xaml-done`)

```json
{
  "Duration": 3.456
}
```

### Generation Failed (`generate-xaml-failed`)

```json
{
  "ExceptionType": "System.Xml.XmlException",
  "Duration": 1.234
}
```

## UnoRuntime Values

The UnoRuntime property can have the following values:

- **WebAssembly**: WebAssembly/browser target
- **Skia**: Skia rendering backend (desktop/mobile)
- **Tizen**: Tizen platform
- **Reference**: Reference API mode
- **Unknown**: Could not determine runtime

## IDE Detection

The IDE property is determined through the following logic:

- **vswin**: Building inside Visual Studio (BuildingInsideVisualStudio is true)
- **vscode**: VS Code detected via environment variables (VSCODE_CWD or TERM_PROGRAM=vscode)
- **rider**: Rider detected via IDEA_INITIAL_DIRECTORY environment variable
- **unknown**: Could not determine IDE or UnoPlatformIDE MSBuild property is set

## Privacy Notes

- No personally identifiable information (PII) is collected
- Only project configuration and build statistics are tracked
- Exception types are logged without stack traces or error messages
- Generation is skipped for design-time builds to reduce noise
- Telemetry can be disabled via `UnoPlatformTelemetryOptOut` MSBuild property

## Instrumentation Keys

- **Production**: 9a44058e-1913-4721-a979-9582ab8bedce

## Disabling Telemetry

Add the following to your project file (.csproj):

```xml
<PropertyGroup>
  <UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
</PropertyGroup>
```

Or set the environment variable:

```bash
export UnoPlatformTelemetryOptOut=true
```

## Reference

For detailed implementation, see: [XamlCodeGeneration.Telemetry.cs](https://github.com/unoplatform/uno/blob/master/src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.Telemetry.cs)
