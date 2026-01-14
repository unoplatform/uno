---
uid: Uno.Development.ToolchainTelemetry
---

# Uno Platform Build Tools Telemetry

The [Uno Platform SDK](https://github.com/unoplatform/uno) includes a [telemetry feature](https://github.com/unoplatform/uno.devtools.telemetry)
that collects usage information during XAML code generation. It is important that the Uno Platform Team understands how the build tools are used so they can be improved.

The collected data is anonymous and does not contain personal identifiable information.

The telemetry behavior is based on the [.NET Core telemetry feature](https://learn.microsoft.com/dotnet/core/tools/telemetry).

> [!TIP]
> For a complete overview of all Uno Platform telemetry, see the [Uno Platform Telemetry](xref:Uno.Development.Telemetry) documentation.

## Scope

The build tooling is used to generate code from XAML during the compilation of an
Uno Platform project. This step collects telemetry.

**Important:** The application resulting from that build **does not collect telemetry**. Only the build-time XAML code generation process collects telemetry.

## How to Opt Out

The Uno Platform SDK telemetry feature is enabled by default. You can opt out using either of the following methods:

### Environment Variable

Set the global environment variable `UNO_PLATFORM_TELEMETRY_OPTOUT` to `1` or `true`:

```bash
# Windows (PowerShell)
$env:UNO_PLATFORM_TELEMETRY_OPTOUT = "true"

# macOS/Linux
export UNO_PLATFORM_TELEMETRY_OPTOUT=true
```

### MSBuild Property

Add the MSBuild property `UnoPlatformTelemetryOptOut` to your project file:

```xml
<PropertyGroup>
  <UnoPlatformTelemetryOptOut>true</UnoPlatformTelemetryOptOut>
</PropertyGroup>
```

## Telemetry Events

The build tools emit the following telemetry events with the prefix `uno/generation`:

| Event Name | Properties | Measurements |
|------------|-----------|--------------|
| **generate-xaml-done** | None | Duration (seconds) |
| **generate-xaml-failed** | ExceptionType | Duration (seconds) |

## Data Collected

The telemetry feature collects the following data:

* **Timestamp** of generation invocation
* **Duration** of the XAML code generation step
* **Exception type** if the generation fails (e.g., `System.InvalidOperationException`)
* **CI environment detection** - whether the build is running under a CI system:
  * Travis CI
  * Azure DevOps
  * AppVeyor
  * Jenkins
  * GitHub Actions
  * BitBucket Pipelines
  * Buildkite
  * AWS CodeBuild
  * Drone CI
  * MyGet
  * JetBrains Space
  * TeamCity
* **Operating system** name, version, kernel version, and architecture
* **Current culture** (e.g., `en-US`, `fr-FR`)
* **Uno Platform NuGet package version**
* **Target frameworks** (e.g., `net9.0`, `net9.0-android`, `net9.0-ios`)
* **Hashed (SHA256) current working directory** - anonymized project location
* **Hashed (SHA256) MAC address** - a cryptographically anonymous and unique ID for the build machine

## Privacy and Security

The telemetry feature **does not collect**:
* Personal data such as usernames or email addresses
* Your code or XAML content
* Sensitive project-level data such as project name, repository name, or author information
* Directory paths (only hashed values are transmitted)

The data is sent securely to Microsoft servers using **Microsoft Azure Application Insights** technology, held under restricted access, and published under strict security controls from secure Azure Storage systems.

## Purpose

The Uno Platform team wants to know how the build tools are used and if they're working well, not what you're
building with the Uno Platform.

## Reporting Issues

If you suspect that the telemetry is collecting sensitive data or that the
data is being insecurely or inappropriately handled, please file an issue in the [unoplatform/uno](https://github.com/unoplatform/uno/issues)
repository for investigation.

## See Also

- [Uno Platform Telemetry Overview](xref:Uno.Development.Telemetry)
- [.NET Core Telemetry](https://learn.microsoft.com/dotnet/core/tools/telemetry)
