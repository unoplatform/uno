---
uid: Uno.Development.ToolchainTelemetry
---

# Uno Platform Build tools telemetry

The [Uno Platform SDK](https://github.com/unoplatform/uno) includes a [telemetry feature](https://github.com/unoplatform/uno.devtools.telemetry)
that collects usage information. It is important that the Uno Platform Team understands how the build tools are used so they can be improved.

The collected data is anonymous.

The telemetry behavior is based on the [.NET Core telemetry feature](https://learn.microsoft.com/dotnet/core/tools/telemetry).

## Scope

The build tooling is used to generate XAML from code during the compilation of an
Uno Platform project. This step collects telemetry.

The application resulting of that build **does not collect telemetry**.

## How to opt out

The Uno Platform SDK telemetry feature is enabled by default. Opt out of the telemetry feature by
setting a global environment variable `UNO_PLATFORM_TELEMETRY_OPTOUT` set to `1` or `true`, or with
an msbuild property named `UnoPlatformTelemetryOptOut` set to `1` or `true`.

## Data points

The feature collects the following data:

* Timestamp of invocation
* The step invoked
* The duration of the step
* The exception type if the generation fails
* If the build is running under a CI (Travis, Azure Devops, AppVeyor, Jenkins, GitHub Actions, BitBucket, Build kite, Codebuild, Drone, MyGet, Space, TeamCity)
* Operating system name, version, kernel version, and architecture
* The current culture
* The current Uno Platform nuget package version
* Target frameworks
* Hashed (SHA256) current working directory
* Hashed (SHA256) MAC address: a cryptographically anonymous and unique ID for a machine.

The feature doesn't collect personal data, such as usernames or email addresses. It doesn't scan your code and doesn't extract
sensitive project-level data, such as name, repo, or author. The data is sent securely to Microsoft servers using Microsoft Azure
Application Insights technology, held under restricted access, and published under strict security controls from secure Azure Storage systems.

The Uno Platform team wants to know how the build tools are used and if they're working well, not what you're
building with the Uno Platform. If you suspect that the telemetry is collecting sensitive data or that the
data is being insecurely or inappropriately handled, file an issue in the [unoplatform/uno](https://github.com/unoplatform/uno/issues)
repository for investigation.
