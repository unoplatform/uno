# SamplesApp.AppiumTests

Appium-driven smoke tests that prove the per-platform automation tree
produced by the Uno SamplesApp is reachable and well-formed:

| Target           | Backend exercised                          | Driver                  |
|------------------|--------------------------------------------|-------------------------|
| Win32 Skia       | UIAutomation (Uno.UI.Runtime.Skia.Win32)   | `appium-windows-driver` |
| macOS Skia       | NSAccessibility (Uno.UI.Runtime.Skia.MacOS)| `appium-mac2-driver`    |
| WebAssembly      | ARIA / `xamlname` DOM attribute (Uno WASM) | Appium chromium driver  |

The tests are intentionally minimal: they validate that the automation
tree is *created and queryable*, not that every control behaves correctly.
That's what `Uno.UI.RuntimeTests` is for. Failures here point at the
runtime accessibility backend, not at control logic.

## Prerequisites

```bash
# Appium 2 + drivers (one-time)
npm i -g appium@2
appium driver install --source=npm appium-windows-driver   # Windows host
appium driver install mac2                                 # macOS host
appium driver install chromium                             # WASM (any host)
```

On Windows, `appium-windows-driver` bundles WinAppDriver; nothing extra.

### macOS permission setup (REQUIRED)

The Mac2 driver runs UI queries through Apple's XCTest framework, which
refuses to operate unless the *invoking process tree* has been granted
Accessibility permission. If the permission is missing you'll see one of:

```
Error Domain=XCTDaemonErrorDomain Code=41
"Not authorized for performing UI testing actions."
```

or `NoSuchElementException` on every `MobileBy.AccessibilityId(...)` lookup
even though the SamplesApp window is clearly visible.

Grant access in **System Settings → Privacy & Security → Accessibility**
to **all** of the following (whichever apply to your setup):

1. The terminal that launches `appium` (Terminal.app, iTerm, etc.) — the
   Appium server's `node` process inherits its permission from its parent.
2. The terminal that runs `dotnet test`.
3. If you launch Appium from VS Code or another IDE, that IDE.
4. `Node.js` itself, if it appears in the list (binary path resolves to
   the `node` you installed, e.g. via Homebrew or nvm).
5. After running once, a `WebDriverAgentRunner-Runner` entry will appear —
   toggle it on too.

After toggling these, fully restart the Appium server (`appium` in its
terminal) so the freshly-permitted process tree picks up the change. A
quick way to verify: with the Appium server running, execute
`tccutil` or simply re-run the test — Code 41 should be gone.

If you also want VoiceOver / Inspect to see what Mac2 sees, leave the
SamplesApp window focused and open `/Applications/Utilities/Inspect.app`
(part of the Xcode Accessibility Tools) — it should show the same
`UNOAccessibilityElement` tree Uno publishes.

## Building the SamplesApp from source

Per `AGENTS.md`, set up cross-targeting first:

```bash
cd src
cp crosstargeting_override.props.sample crosstargeting_override.props
# Edit to net10.0 (or net9.0 for Skia) per the target you're testing
```

Then build the host you want to drive:

```bash
# Skia desktop (works for Win32 and macOS)
dotnet build SamplesApp/SamplesApp.Skia.Generic/SamplesApp.Skia.Generic.csproj -c Release

# WebAssembly
dotnet build SamplesApp/SamplesApp.Wasm/SamplesApp.Wasm.csproj -c Release
```

## Building the test project

```bash
dotnet build src/SamplesApp/SamplesApp.AppiumTests/SamplesApp.AppiumTests.csproj
```

## Running

Start an Appium 2 server in a separate terminal first:

```bash
appium --base-path /wd/hub
```

(The default `appium` start path also works; the tests honor
`UNO_APPIUM_SERVER` if you need to point elsewhere.)

### Windows (Win32 Skia)

```cmd
set UNO_APPIUM_PLATFORM=windows
set UNO_APPIUM_SAMPLESAPP=C:\path\to\uno\src\SamplesApp\SamplesApp.Skia.Generic\bin\Release\net9.0\SamplesApp.Skia.Generic.exe
dotnet test src\SamplesApp\SamplesApp.AppiumTests
```

### macOS (macOS Skia)

> **Run from `Terminal.app`, not from VS Code's integrated terminal.**
>
> macOS LaunchServices refuses to register new GUI apps started from a
> launchd domain that descends from a sandboxed parent (VS Code's
> "Code Helper (Plugin)" process is the canonical culprit). The Mac2
> adapter wraps the SamplesApp dll in a temporary `.app` bundle and asks
> LaunchServices to launch it; from a VS Code-rooted shell that request
> silently exits 0 without launching anything, and the test fixture
> times out waiting for the bundle to appear. From `Terminal.app` (or
> any other unsandboxed terminal), the same call succeeds.
>
> If you absolutely must run from inside VS Code, launch the wrapper
> bundle yourself once from `Terminal.app` and leave it open — the
> adapter will attach to the already-running process via its bundle id
> (`io.platform.uno.SamplesAppAppium`) and skip the launch step.

```bash
export UNO_APPIUM_PLATFORM=mac
export UNO_APPIUM_SAMPLESAPP="$PWD/src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net9.0/SamplesApp.Skia.Generic.dll"
export UNO_APPIUM_DOTNET_PATH="$(which dotnet)"   # optional; auto-resolved if unset
dotnet test src/SamplesApp/SamplesApp.AppiumTests
```

The Mac2 adapter behaviour:

1. If `UNO_APPIUM_SAMPLESAPP` points at a real `.app` bundle, Mac2 launches it directly via its `CFBundleIdentifier`.
2. If it points at a `.dll`, the adapter writes a minimal wrapper `.app` to `$TMPDIR/SamplesAppAppium-*.app` whose `MacOS/SamplesAppAppium` entry shells out to `dotnet <dll> <sampleQuery>`, launches it via AppleScript, and attaches Mac2 to the bundle id `io.platform.uno.SamplesAppAppium`. The wrapper is cleaned up on teardown unless you set `UNO_APPIUM_KEEP_BUNDLE=1`.
3. If a process with that bundle id is already running, the launch step is skipped — useful for the VS Code workaround above.

### WebAssembly

Serve the built app on a port the test process can reach:

```bash
dotnet tool install -g dotnet-serve   # one-time
dotnet-serve -d src/SamplesApp/SamplesApp.Wasm/bin/Release/net10.0/dist -p 8000 &

export UNO_APPIUM_PLATFORM=wasm
export UNO_APPIUM_SAMPLESAPP=http://localhost:8000
dotnet test src/SamplesApp/SamplesApp.AppiumTests
```

The browser-side selectors rely on `AssignDOMXamlName = true`, which
SamplesApp.Wasm already enables in `Program.cs`. AutomationIds are read as
the `xamlname` DOM attribute. If you switch to a host that does *not*
enable `AssignDOMXamlName`, update `WasmAdapter.ByAutomationId` to use
`[name=...]` or `[id=...]`.

## Accessibility-tree baselines (golden-file snapshots)

`AccessibilityBaselineTests` walks the entire platform AX tree for each
sample and compares it against a committed JSON baseline. This catches
silent regressions that the smoke suite can't — a control losing its
Toggle pattern, an AutomationId vanishing on one platform, a heading
level dropping, etc.

Layout:

```
Snapshots/
├── win32/
│   └── Automation_AccessibilityScreenReader.json
├── macos/
│   └── Automation_AccessibilityScreenReader.json
└── wasm/
    └── Automation_AccessibilityScreenReader.json
```

Each file is the canonical tree (normalized roles, AutomationIds, value,
supported patterns, children in tree order) for one sample on one
platform-flavor. Window-chrome and ScrollViewer template parts are
stripped via an allowlist in `Infrastructure/TreeDumper.cs` so the
baseline focuses on the sample under test.

### Workflow

```bash
# (1) Bootstrap baselines from a known-good Uno build, once per platform:
UNO_APPIUM_RECORD_SNAPSHOTS=1 \
UNO_APPIUM_PLATFORM=mac \
UNO_APPIUM_SAMPLESAPP="$PWD/src/SamplesApp/SamplesApp.Skia.Generic/bin/Release/net9.0/SamplesApp.Skia.Generic.dll" \
dotnet test src/SamplesApp/SamplesApp.AppiumTests \
    --filter "Category=Baseline" -c Release --no-build

# (2) Commit the JSON files under Snapshots/ in your PR.

# (3) Subsequent runs verify against the committed baselines:
UNO_APPIUM_PLATFORM=mac \
UNO_APPIUM_SAMPLESAPP="..." \
dotnet test src/SamplesApp/SamplesApp.AppiumTests \
    --filter "Category=Baseline" -c Release --no-build
```

When a test fails, the live tree is written to
`<bin>/snapshot-actual/<flavor>/<sample>.json` and attached to the test
result, alongside a human-readable structural diff. Diff lines look
like:

```
[changed] root.children[3].automation_id: expected 'VisibilityTargetButton' actual ''
[pattern-lost] root.children[5].patterns: expected 'toggle' actual '(absent)'
[added]   root.children[7]: button #NewControl ""
```

To update a baseline after a legit AX-tree change: rerun with
`UNO_APPIUM_RECORD_SNAPSHOTS=1`, review the JSON diff, commit.

### Adding a new sample to the suite

Open `Tests/AccessibilityBaselineTests.cs`, add one entry to `Cases()`:

```csharp
yield return new BaselineCase(
    Sample: "Controls/MyNewSample",
    SnapshotId: "Controls_MyNewSample");
```

Then record baselines on each platform you care about.

## What's covered

The smoke suite (`Tests/AutomationTreeSmokeTests.cs`) navigates the app to
`Windows_UI.Xaml_Automation/AccessibilityScreenReaderPage` and runs:

| Test                                          | What it proves                            |
|-----------------------------------------------|-------------------------------------------|
| `Tree_HasPopulatedAutomationElements`         | The driver sees > 10 elements in the tree |
| `Button_FoundByAutomationId_HasButtonRole`    | UIA/AX/ARIA reports the right role        |
| `Button_FoundByAutomationId_CanBeInvoked`     | `Click()` round-trips to the control      |
| `TextBox_FoundByAutomationId_AcceptsValue`    | The Value pattern reflects typed input    |
| `TextBlock_FoundByAutomationId_HasAccessibleName` | Non-control elements still expose names |
| `Tree_ExposesMoreThanRootElement`             | Multiple distinct roles are surfaced      |

All tests run once per platform leg, controlled by `UNO_APPIUM_PLATFORM`.
There is no cross-platform parallelism by design — each leg owns its
own Appium server.

## Why not extend `SamplesApp.UITests`?

`SamplesApp.UITests` uses the custom `Uno.UITest` abstraction (Selenium
under the hood for WASM, Xamarin.UITest for mobile, in-process for Skia
desktop). None of those paths actually attach to the *OS* automation
backend — they read XAML state through internal hooks. The whole point
of this project is to drive the app through the *real* automation API
that screen readers, assistive technology, and external automation tools
use, which is what Appium provides.
