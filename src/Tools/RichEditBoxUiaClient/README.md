# RichEditBoxUiaClient

A manual Windows tool that checks what an external UI Automation client (a screen reader, Accessibility Insights) sees of `RichEditBox` on the Skia Win32 host.

It runs out of process: it starts SamplesApp on the `RichEditBox/RichEditBox_UIAutomation` sample, finds the editor by its `RichEditBoxUiaFixture` AutomationId, and checks the Text, Text2 and TextEdit patterns, text attributes, annotations, hyperlink and image children, and the text-changed, structure-changed and conversion-target events. Each check prints `PASS`/`FAIL`, and the exit code is non-zero if any check fails.

In-process runtime tests can't cover this: they never cross the UIA COM boundary that real assistive technology uses.

## Requirements

- Windows.
- `TlbImp.exe` from the .NET Framework 4.8 SDK tools. The build generates the `UIAutomationCore` interop assembly with it. Pass `-p:TlbImpToolPath=<path>` if it isn't at the default Windows SDK location.

The tool isn't part of CI. It's listed in `Uno.UI.slnx` with building disabled, so solution builds don't need TlbImp.

## Usage

Build SamplesApp for desktop, then run the script from the repository root:

```powershell
dotnet build src/SamplesApp/SamplesApp/SamplesApp.csproj -c Release -f net11.0-desktop
./src/Tools/RichEditBoxUiaClient/Run-RichEditBoxUiaClient.ps1
```

`-AppPath <SamplesApp.dll>` points at a different SamplesApp build.

`-NativeInstalled` runs the same checks against the installed WinAppSDK SamplesApp (`unosamplesapp.exe`). Checks where native WinUI legitimately differs print `INFO` lines instead of failing, which makes this the WinUI parity baseline.
