# Uno.Check Telemetry

**Event Name Prefix:** `uno/uno-check`

## Overview

Uno.Check is an environment verification tool that validates .NET SDK installations, workloads, and dependencies required for Uno Platform development. It tracks check execution events to monitor tool usage, success rates, and common issues.

**Reference:** [unoplatform/uno.check](https://github.com/unoplatform/uno.check)

## Events

| Event Name | Properties | Measurements | Description |
|------------|-----------|--------------|-------------|
| `check-start` | `RequestedFrameworks` (string) | - | Check process started. Records which frameworks were requested for validation (e.g., "net8.0,net8.0-android,net8.0-ios"). Max 10 frameworks, each truncated to 32 characters |
| `check-success` | - | `Duration` (double) | Check completed successfully with all validations passing. Duration in seconds |
| `check-warn` | `ReportedChecks` (string) | `Duration` (double) | Check completed with warnings. Some checks reported issues but process continued. Duration in seconds |
| `check-fail` | `ReportedChecks` (string) | `Duration` (double) | Check failed. One or more checks detected missing or incorrect dependencies. Duration in seconds. `~aaaaaa` means that the user explicitly skipped the check |

> **check-fail** and **check-warn** are non-catastrophic, meaning that uno-check did not fail but rather one of its checks did either because of an issue with the environment or because the user explicitly skipped the check.

## Properties

### RequestedFrameworks

- **Type**: String (comma-separated list)
- **Description**: Target frameworks requested for validation
- **Format**: Comma-separated list of .NET TFMs matching pattern `net\d+(\.0)(?:-[a-zA-Z0-9.]+)*$`
- **Examples**:
  - `"net8.0"` - Single framework
  - `"net8.0,net8.0-android,net8.0-ios"` - Multiple mobile frameworks
  - `"net8.0-android,net8.0-ios,net8.0-maccatalyst,net8.0-windows"`
  - - All platforms
- **Notes**:
  - Only frameworks matching .NET TFM pattern are included
  - Maximum of 10 frameworks tracked
  - Each framework name truncated to 32 characters
  - Alphabetically sorted

### ReportedChecks

- **Type**: String
- **Description**: Names or identifiers of checks that failed or warned
- **Examples**:
  - `"AndroidSDK"` - Android SDK check
  - `"OpenJDK,AndroidSDK"` - Multiple checks
  - `"DotNetWorkloads"` - .NET workload validation

## Measurements

### Duration

- **Type**: Double (seconds)
- **Description**: Total execution time for check process
- **Examples**:
  - `2.5` - Quick check (2.5 seconds)
  - `15.3` - Standard check with installations (15.3 seconds)
  - `120.7` - Long check with multiple workload installations (2+ minutes)

## Instrumentation Keys

The tool uses the standard Uno Platform Application Insights keys:

| Environment | Key |
|-------------|-----|
| **Production** | `9a44058e-1913-4721-a979-9582ab8bedce` |
| **Development** | `81286976-e3a4-49fb-b03b-30315092dbc4` |

See [Telemetry-Privacy.md](Telemetry-Privacy.md) for details on instrumentation key usage and rotation policy.

## Privacy Notes

- **No PII**: No personally identifiable information is collected
- **Framework Names Only**: Only framework target monikers are recorded, not project paths or names
- **Anonymous**: Uses hashed machine/user identifiers (see [Global Properties](Telemetry-GlobalProperties.md))
- **Check Names Only**: Failed/warning checks recorded by name only, no file paths or configuration details
- **Opt-Out Available**: Set `UNO_PLATFORM_TELEMETRY_OPTOUT=true` environment variable to disable

## Example Telemetry Flow

1. **User runs**: `uno-check --target net8.0-android --target net8.0-ios`
2. **check-start** event: `RequestedFrameworks="net8.0-android,net8.0-ios"`
3. **Tool validates**: Android SDK, iOS workloads, OpenJDK, etc.
4. **Outcome (one of)**:
   - **check-success**: All checks pass → `Duration=12.3`
   - **check-warn**: Minor issues → `ReportedChecks="AndroidSDK"`, `Duration=15.8`
   - **check-fail**: Critical failures → `ReportedChecks="OpenJDK,AndroidSDK"`, `Duration=8.2`

## Reference

- **Source Code**: [unoplatform/uno.check](https://github.com/unoplatform/uno.check)
- **Telemetry Implementation**: [TelemetryClient.cs](https://github.com/unoplatform/uno.check/blob/main/UnoCheck/Telemetry/TelemetryClient.cs)
- **Search**: [uno.check telemetry events](https://github.com/search?q=repo%3Aunoplatform%2Funo.check+TrackEvent&type=code)
