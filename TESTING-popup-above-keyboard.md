# Testing: flyouts above the on-screen keyboard

Manual on-device check for branch `dev/anpi/popup-above-keyboard`.

**What changed:** a flyout used to be placed against the whole window. On mobile web, it ended up under the on-screen keyboard whenever its button sat near the bottom. Now:

- A flyout is placed in the area above the keyboard.
- It moves again when the keyboard shows or hides.
- If it's too tall for that area, it shrinks and its content scrolls, as WinUI's `ResizeToFit` does.

> Remove this file before opening the PR.

## 1. Get the branch

```bash
git fetch https://github.com/ajpinedam/uno dev/anpi/popup-above-keyboard
git checkout FETCH_HEAD
```

## 2. Publish the WebAssembly SamplesApp

You need the .NET 10 SDK and the `wasm-tools` workload (`dotnet workload install wasm-tools`). From the repo root:

```bash
dotnet publish src/SamplesApp/SamplesApp/SamplesApp.csproj -c Release -f net10.0-browserwasm -p:UnoFastDevBuild=true -p:UnoTargetFrameworkOverride=net10.0-browserwasm
```

A cold publish can take around 20 minutes.

## 3. Serve it on the LAN

Do this only on a trusted network. The server lets anyone on that network read the published files.

Bind it to this computer's LAN address rather than `0.0.0.0`, so it isn't reachable through other network interfaces:

```bash
python -m http.server 8000 --bind <this-computer-ip> -d src/SamplesApp/SamplesApp/bin/Release/net10.0-browserwasm/publish/wwwroot
```

- The tablet must be on the same network as this computer.
- On Windows, don't change the network profile. Open the port to your local subnet only, from an elevated PowerShell:

  ```powershell
  New-NetFirewallRule -DisplayName wasm-test-8000 -Direction Inbound -Protocol TCP -LocalPort 8000 -RemoteAddress LocalSubnet -Action Allow
  ```

- When you're done, stop the server (Ctrl+C) and remove the rule:

  ```powershell
  Remove-NetFirewallRule -DisplayName wasm-test-8000
  ```

## 4. Open the sample on the tablet

Open this URL in Safari on the iPad, or Chrome on an Android tablet:

```text
http://<this-computer-ip>:8000/?sample=Windows.UI.ViewManagement/SoftKeyboardFlyoutTests
```

After a re-publish, use a private tab or clear the site data, so the browser doesn't keep the old `.wasm`.

## 5. Steps

The page shows two lines that update live:

- the keyboard rect (`InputPane.OccludedRect`);
- the open flyout's bounds, with "Above keyboard: yes/no".

| # | Do | Expect |
|---|---|---|
| 1 | Tap **Open search flyout**. | The flyout opens above its button. |
| 2 | Tap the search box in the flyout. | When the keyboard is up, the whole flyout sits right above it ("Above keyboard: yes"). |
| 3 | Hide the keyboard. | The flyout returns next to its button. |
| 4 | Close the flyout. Tap **Open tall flyout**, then its search box. | The flyout shrinks to the area above the keyboard (its height is now less than its natural height). Its list still scrolls down to Item 29. |
| 5 | Hide the keyboard. | The tall flyout gets its natural height back, next to its button. |
| 6 | Close the flyout. Tap the top field to bring the keyboard up, then tap either button. The buttons don't take focus, so the keyboard stays up. | The flyout opens above the keyboard. The tall one opens shrunk. |

Run the steps in portrait and in landscape. Landscape leaves the least room above the keyboard, so it is the real test for steps 4 to 6.

## 6. What to report

For each failing step, note:

- the step number and the orientation;
- a screenshot that shows both readout lines;
- the device model, the OS version and the browser.

If the keyboard rect reads empty while the keyboard is visible, or its top doesn't match where the keyboard starts, the browser reports the keyboard differently than the fix expects. Note that too.

## 7. Logs for a failing step

These log categories explain what the fix decided:

| Category | Level | Shows |
|---|---|---|
| `Microsoft.UI.Xaml.Controls.Primitives.PopupPanel` | Debug | the area the flyout was placed in, the side it took, and whether it was resized |
| `Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase` | Debug | each keyboard show or hide, and each presenter resize, reset or skip with its reason |
| `Uno.UI.Runtime.WebAssemblyWindowWrapper` | Trace | each keyboard height the browser reported, and the rect derived from it |

The SamplesApp enables Trace logs for the categories listed in the `UNO_SAMPLES_TRACE` environment variable, separated by `;`, in any configuration. On WebAssembly, set it before publishing, by adding this to `src/SamplesApp/SamplesApp/SamplesApp.csproj`:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-browserwasm'">
  <WasmShellMonoEnvironment Include="UNO_SAMPLES_TRACE" Value="Microsoft.UI.Xaml.Controls.Primitives.PopupPanel;Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase;Uno.UI.Runtime.WebAssemblyWindowWrapper" />
</ItemGroup>
```

The logs go to the browser console. Read them with Safari's Web Inspector from a Mac for an iPad, or with `chrome://inspect` from a computer for an Android tablet. Don't commit that `.csproj` change.
