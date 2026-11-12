# Win32 UI Automation structure-event regression

Build the desktop SamplesApp, then run this separate native UIA client on Windows:

```powershell
dotnet run --project build/test-scripts/Win32UiaEventProbe -- <absolute-path-to-SamplesApp.dll> <absolute-path-to-results.xml>
```

The client runs `Given_Win32ProviderLifetime`, subscribes to the test parent's
structure events, and checks `ChildAdded` followed by `ChildRemoved` carrying the
same runtime ID obtained from the added child. This exercises both the null and
non-null runtime-ID arguments of `UiaRaiseStructureChangedEvent`. The process
returns nonzero for a native crash, missing or incorrect events, failed tests,
skipped tests, or timeout. App output is saved alongside the results as `.log`.

The native-client test is inconclusive in a regular runtime-test run because it
requires this external process. The other provider lifetime tests run normally.
CI runs this probe after the Windows Skia runtime tests
(`build/ci/tests/.azure-devops-tests-desktop-windows.yml`).
The probe needs a .NET 10 or newer Windows Desktop runtime; the SamplesApp can
use a different supported .NET version.
