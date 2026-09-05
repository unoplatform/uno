# SamplesApp.AppiumTests

Appium-backed accessibility regression tests for the real SamplesApp automation
surfaces:

| Target | Platform tree exercised | Driver |
| --- | --- | --- |
| Windows Skia | UI Automation | `appium-windows-driver` |
| macOS Skia | NSAccessibility through Mac2 | `appium-mac2-driver` |
| Skia WASM | DOM + ARIA semantic tree | ChromeDriver |

The suite does **not** compare raw trees position-by-position across platforms.
Instead it captures a shared canonical model for a curated set of stable
elements from `Automation/Accessibility_ScreenReader`, with per-platform field
selection where the native surfaces legitimately differ.

## What runs where

Three categories exist:

- `HostIndependent` - pure logic tests for configuration parsing, role
  normalization, snapshot schema/diffing, and baseline-definition integrity.
  These run in ordinary CI and do not need Appium.
- `HostRequired` - real external-driver sessions that validate canonical snapshots and
  representative interactions against the live platform tree.
- `WasmHostRequired` - external semantic-DOM standards checks that run only
  against the Skia-WASM ChromeDriver session.

The unit-test job runs the host-independent gate:

```powershell
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=HostIndependent"
```

`HostRequired` tests are opt-in on machines that actually have the matching
host, app build, and driver available. The Skia-WASM runtime-test lane also
runs all `HostRequired` tests once (matrix group 0) against the published app
with a version-matched ChromeDriver. Windows and macOS host-backed runs remain
manual until those CI lanes provide Appium and the required OS permissions.
If you intentionally select host-backed tests without the required
environment, they fail fast with a configuration error; they never silently
skip or auto-bless baselines.

## Prerequisites

```powershell
npm i -g appium@2
appium driver install --source=npm appium-windows-driver
appium driver install mac2
```

Start Appium separately before running Windows or macOS host-backed tests:

```powershell
appium
```

The default Appium server URI for Windows and macOS is
`http://127.0.0.1:4723/`. If you use a different address or
`--base-path /wd/hub`, set `UNO_APPIUM_SERVER` explicitly. WASM uses a
version-matched ChromeDriver directly; set `UNO_APPIUM_SERVER` to its URL.

## Building the app under test

Per `AGENTS.md`, use a single-target override for local iteration.

### Windows / macOS Skia

```powershell
dotnet build src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj `
  -c Release `
  -p:UnoTargetFrameworkOverride=net10.0 `
  -p:UnoFastDevBuild=true
```

- Windows uses the produced `.exe`
- macOS uses the produced `.dll` or an already-built `.app`

### WASM

```powershell
dotnet publish src\SamplesApp\SamplesApp.Skia.WebAssembly.Browser\SamplesApp.Skia.WebAssembly.Browser.csproj `
  -c Release `
  -f net10.0 `
  -p:UnoTargetFrameworkOverride=net10.0 `
  -p:UnoFastDevBuild=true
```

Serve the built dist folder from any reachable URL, for example:

```powershell
dotnet tool install --global dotnet-serve
dotnet serve --directory src\SamplesApp\SamplesApp.Skia.WebAssembly.Browser\bin\Release\net10.0\publish\wwwroot --port 8000
```

Start a ChromeDriver whose major version matches Chrome/Chromium:

```powershell
chromedriver --port=9515
```

## Building and running this project

```powershell
dotnet build src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj -c Release
```

### Host-independent validation

```powershell
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=HostIndependent"
```

### Windows

```powershell
$env:UNO_APPIUM_PLATFORM = 'windows'
$env:UNO_APPIUM_SAMPLESAPP = 'C:\path\to\SamplesApp.Skia.Generic.exe'
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=HostRequired"
```

### macOS

> Run Appium and the tests from an unsandboxed terminal such as `Terminal.app`.

```bash
export UNO_APPIUM_PLATFORM=mac
export UNO_APPIUM_SAMPLESAPP="$PWD/src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net10.0/SamplesApp.Skia.Generic.dll"
dotnet test --project src/SamplesApp/SamplesApp.AppiumTests/SamplesApp.AppiumTests.csproj \
  -c Release \
  --filter "TestCategory=HostRequired"
```

If `UNO_APPIUM_SAMPLESAPP` points to a `.dll`, the adapter creates a wrapper
`.app` bundle under the test artifacts directory (`mac-bundles\...`) and
launches that bundle through LaunchServices. Set `UNO_APPIUM_KEEP_BUNDLE=1` if
you want to inspect the generated wrapper after the run. If `dotnet` is not on
the default macOS probe paths, set `UNO_APPIUM_DOTNET_PATH`.

### WASM

```powershell
$env:UNO_APPIUM_PLATFORM = 'wasm'
$env:UNO_APPIUM_SAMPLESAPP = 'http://127.0.0.1:8000/'
$env:UNO_APPIUM_SERVER = 'http://127.0.0.1:9515/'
# Optional when Chrome is not installed in a standard location:
$env:UNO_APPIUM_CHROME_BINARY = 'C:\path\to\chrome.exe'
# Optional, pipe-delimited (useful on a headless CI host):
$env:UNO_APPIUM_CHROME_ARGUMENTS = '--headless=new|--disable-gpu'
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=HostRequired|TestCategory=WasmHostRequired"
```

WASM discovery is based on Uno's semantic DOM:

- the suite enables accessibility with `#uno-enable-accessibility`
- elements are found under `#uno-semantics-root`
- AutomationIds come from `xamlautomationid`
- names/descriptions/states come from DOM + ARIA attributes

## Environment variables

| Variable | Meaning |
| --- | --- |
| `UNO_APPIUM_PLATFORM` | `windows`, `mac`, or `wasm` |
| `UNO_APPIUM_SAMPLESAPP` | Absolute `.exe`, absolute `.dll`/`.app`, or absolute `http(s)` URL |
| `UNO_APPIUM_SERVER` | Optional Appium server URI; defaults to `http://127.0.0.1:4723/` |
| `UNO_APPIUM_RECORD_SNAPSHOTS` | `1/0`, `true/false`, `yes/no`; explicitly rewrites committed baselines |
| `UNO_APPIUM_SNAPSHOTS_DIR` | Optional override for the committed baseline root |
| `UNO_APPIUM_ARTIFACTS_DIR` | Optional artifact root for actual snapshots, raw trees, and macOS wrapper bundles |
| `UNO_APPIUM_TIMEOUT_SECONDS` | Per-wait timeout; default `20` |
| `UNO_APPIUM_POLL_INTERVAL_MS` | Poll cadence for bounded waits; default `200` |
| `UNO_APPIUM_KEEP_BUNDLE` | Keeps the generated macOS wrapper bundle |
| `UNO_APPIUM_DOTNET_PATH` | Optional macOS-only `dotnet` override when wrapping a `.dll` |
| `UNO_APPIUM_CHROME_BINARY` | Optional absolute Chrome/Chromium executable used by the WASM ChromeDriver session |
| `UNO_APPIUM_CHROME_ARGUMENTS` | Optional pipe-delimited Chrome arguments, such as `--headless=new|--disable-gpu` |

Validation is strict:

- Windows requires an absolute `.exe`
- macOS requires an absolute `.app` or `.dll`
- WASM requires an absolute `http(s)` URL
- `UNO_APPIUM_SERVER` must be an absolute `http(s)` URL

## Canonical snapshot baselines

Committed, host-verified baselines live under:

```text
Snapshots/
└── wasm/
```

The WASM baseline is recorded and enforced in CI. Windows and macOS baselines
must be recorded on their matching hosts before those snapshot lanes are
enabled; the snapshot test fails clearly when a selected platform has no
baseline and never substitutes values from another platform.

Each file is schema version 2:

```json
{
  "schema": 2,
  "sample": "Automation/Accessibility_ScreenReader",
  "flavor": "win32|macos|wasm",
  "elements": [ ... ]
}
```

Each canonical element is keyed by a stable id and stores only the semantic
fields that matter for that platform: role, name, description, value, patterns,
and selected state fields such as toggle state, selection, required, heading
level, landmark, or live setting.

### Verifying baselines

```powershell
$env:UNO_APPIUM_PLATFORM = 'windows'
$env:UNO_APPIUM_SAMPLESAPP = 'C:\path\to\SamplesApp.Skia.Generic.exe'
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=Snapshot"
```

### Re-recording baselines

Recording is always explicit:

```powershell
$env:UNO_APPIUM_RECORD_SNAPSHOTS = '1'
dotnet test --project src\SamplesApp\SamplesApp.AppiumTests\SamplesApp.AppiumTests.csproj `
  -c Release `
  --filter "TestCategory=Snapshot"
```

Review the JSON diff before committing. The tests never overwrite baselines
unless `UNO_APPIUM_RECORD_SNAPSHOTS` is set.

### Failure artifacts

On a snapshot mismatch, the suite writes:

- `snapshot-actual\<flavor>\<snapshot>.json` - canonical actual snapshot
- `snapshot-actual\<flavor>\<snapshot>.tree.json` - raw captured platform tree,
  when available

The failure message includes the platform/session context plus an element-id
based diff such as:

```text
[changed] FavoriteColorComboBox.value: expected 'Red' actual 'Green'
[changed] EnableNotificationsCheckBox.state.toggleState: expected 'on' actual 'off'
```

## Interaction coverage

The live suite currently checks that the platform tree reflects these actions:

- checkbox invoke -> toggle state changes
- radio selection -> selected state moves
- combobox selection -> value and selected item change
- textbox typing -> value and focus update
- disable button -> enabled/focusable state changes
- live-region update -> accessible text changes, with live setting where exposed

## Why this project exists beside `SamplesApp.UITests`

`SamplesApp.UITests` validates app behavior through Uno's existing UI test
abstractions. `SamplesApp.AppiumTests` validates the external accessibility and
automation contracts that screen readers, inspectors, and Appium itself see.
