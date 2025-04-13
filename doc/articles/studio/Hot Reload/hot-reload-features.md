---
uid: Uno.Platform.Studio.HotReload.Features
---

## Key Features of HotReload

- Supported in **Visual Studio 2022** (Windows), **VS Code** (Linux, macOS, Windows, CodeSpaces, and GitPod) and **Rider** (Linux, macOS, Windows).
- XAML and [C# Markup](xref:Uno.Extensions.Markup.Overview) Hot Reload for **iOS, Catalyst, Android, WebAssembly, and Skia (X11, Windows, macOS and FrameBuffer)**.
- All **[C# of Hot Reload](https://learn.microsoft.com/visualstudio/debugger/hot-reload)** in both Visual Studio, VS Code and Rider. See [supported code changes](https://learn.microsoft.com/visualstudio/debugger/supported-code-changes-csharp).
- **Simulator and physical devices** support.
- **Hot Reload Indicator** visuals for an enhanced development experience on Uno Platform targets (not currently supported on WinAppSDK target).
- What can be Hot Reloaded:
  - **XAML files** in the **main project** and **referenced projects libraries**
  - **C# Markup controls**
  - **Bindings**
  - **x:Bind expressions**
  - **App.xaml** and **referenced resource dictionaries**
  - **DataTemplates**
  - **Styles**
  - Extensible [**State restoration**](xref:Uno.Contributing.Internals.HotReload)
  - Support for partial **tree hot reload**, where modifying a `UserControl` instantiated in multiple locations will reload it without reloading its parents.

Hot Reload features are now consistent across platforms and IDEs, but with some debugger-specific variations. You can check below the list of currently supported features.

For existing applications, take this opportunity to update to the [latest **Uno.Sdk** version](https://www.nuget.org/packages/Uno.Sdk/latest) to take advantage of all the latest improvements and support. Refer to our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) for upgrade steps.

> [!IMPORTANT]
> When upgrading to **Uno.Sdk 5.5 or higher**, the `EnableHotReload()` method in `App.xaml.cs` is deprecated and should be replaced with `UseStudio()`.

## Supported features per OS

<!-- Styles applied specifically to the following tables -->
<style>
    /* Center all non-first-column content horizontally */
    table th:not(:first-child),
    table td:not(:first-child) {
        text-align: center !important;
    }

    /* Ensure ALL table cells are vertically centered */
    table th,
    table td {
        vertical-align: middle !important;
        display: table-cell !important;
    }

    /* Keep first column text left-aligned */
    table td:first-child {
        text-align: left;
    }

    /* Specifically center the '🐞 Debugger' text in the first column header */
    table th:first-child {
        text-align: center !important;
    }
</style>

### [**Windows**](#tab/windows)

<table>
    <thead>
        <tr>
            <th></th>
            <th colspan="2">Visual Studio</th>
            <th colspan="2">VS Code</th>
            <th colspan="2">Rider</th>
        </tr>
        <tr>
            <th>🐞 Debugger</th>
            <th>With</th>
            <th>Without</th>
            <th>With</th>
            <th>Without</th>
            <th>With</th>
            <th>Without</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Desktop<br /><small><code>net9.0-desktop</code></code></small></td>
            <td>✅</td><td>✅</td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Desktop - WSL<br /><small><code>net9.0-desktop</code></small></td>
            <td>⌛<sup><a href="#hr-footnotes">[1]</a></sup></td><td>⌛<sup><a href="#hr-footnotes">[1]</a></sup></td>
            <td>🔳</td><td>✅</td>
            <td>🔳<sup><a href="#hr-footnotes">[2]</a></sup></td><td>🔳<sup><a href="#hr-footnotes">[2]</a></sup></td>
        </tr>
        <tr>
            <td>iOS<br /><small><code>net9.0-ios</code></small></td>
            <td>✅</a></sup></td><td>🔳</td>
            <td>🟥</a></td><td>✅🛜</td>
            <td>🔳</a></td><td>✅</td>
        </tr>
        <tr>
            <td>Android<br /><small><code>net9.0-android</code></small></td>
            <td>✅</td><td>🔳</td>
            <td>🟥</a></td><td>✅</td>
            <td>🔳</a></td><td>✅</td>
        </tr>
        </tr>
        <tr>
            <td>WinAppSDK<br /><small><code>net9.0-windows10.x.x</code></small></td>
            <td>✅<sup><a href="#hr-footnotes">[3]</a></sup></td><td>✅<sup><a href="#hr-footnotes">[4]</a></sup></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>WebAssembly<br /><small><code>net9.0-browserwasm</code></small></td>
            <td>✅</td><td>✅</td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Catalyst<br /><small><code>net9.0-maccatalyst</code></small></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>✅🛜</td>
            <td>🔳</td><td>🔳</td>
        </tr>
    </tbody>
</table>

### [**macOS**](#tab/macOS)

<table>
    <thead>
        <tr>
            <th></th>
            <th colspan="2">VS Code</th>
            <th colspan="2">Rider</th>
        </tr>
        <tr>
            <th>🐞 Debugger</th>
            <th>With</th>
            <th>Without</th>
            <th>With</th>
            <th>Without</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Desktop<br /><small><code>net9.0-desktop</code></code></small></td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Desktop - WSL<br /><small><code>net9.0-desktop</code></small></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>iOS<br /><small><code>net9.0-ios</code></small></td>
            <td>🟥</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Android<br /><small><code>net9.0-android</code></small></td>
            <td>🟥</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>WinAppSDK<br /><small><code>net9.0-windows10.x.x</code></small></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>WebAssembly<br /><small><code>net9.0-browserwasm</code></small></td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Catalyst<br /><small><code>net9.0-maccatalyst</code></small></td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
    </tbody>
</table>

### [**Linux**](#tab/linux)

<table>
    <thead>
        <tr>
            <th></th>
            <th colspan="2">VS Code</th>
            <th colspan="2">Rider</th>
        </tr>
        <tr>
            <th>🐞 Debugger</th>
            <th>With</th>
            <th>Without</th>
            <th>With</th>
            <th>Without</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Desktop<br /><small><code>net9.0-desktop</code></code></small></td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Desktop - WSL<br /><small><code>net9.0-desktop</code></small></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>iOS<br /><small><code>net9.0-ios</code></small></td>
            <td>🟥</td><td>✅🛜</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>Android<br /><small><code>net9.0-android</code></small></td>
            <td>🟥</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>WinAppSDK<br /><small><code>net9.0-windows10.x.x</code></small></td>
            <td>🔳</td><td>🔳</td>
            <td>🔳</td><td>🔳</td>
        </tr>
        <tr>
            <td>WebAssembly<br /><small><code>net9.0-browserwasm</code></small></td>
            <td>🔳</td><td>✅</td>
            <td>🔳</td><td>✅</td>
        </tr>
        <tr>
            <td>Catalyst<br /><small><code>net9.0-maccatalyst</code></small></td>
            <td>🔳</td><td>✅🛜</td>
            <td>🔳</td><td>🔳</td>
        </tr>
    </tbody>
</table>

---

Legend:

- ✅ Supported
- 🛜 Supported through [SSH to a Mac](xref:Uno.GettingStarted.CreateAnApp.VSCode#debug-the-app)
- ⌛ Upcoming support
- 🟥 Not supported yet
- 🔳 Not supported by the environment/IDE

### Notes

<a href="hr-footnotes"/>

- [1]: Support is [pending support](https://github.com/dotnet/sdk/pull/40725) in the .NET SDK.
- [2]: Support is [not available](https://youtrack.jetbrains.com/issue/RIDER-53302/launchSettings.json-WSL2-command-support).
- [3]: Unpackaged: C# & XAML / Packaged: XAML only
- [4]: Unpackaged: C# / Packaged: none

## Supported features per Platform

### [**Desktop**](#tab/skia-desktop)

Skia-based targets provide support for full XAML Hot Reload and C# Hot Reload. There are some restrictions that are listed below:

- The Visual Studio 2022 for Windows support is fully available, with and without running under the debugger
- As of VS 2022 17.9 XAML or C# Hot Reload under WSL is not supported
- VS Code
  - With the debugger: The C# Dev Kit is handling hot reload [when enabled](https://code.visualstudio.com/docs/csharp/debugging#_hot-reload). As of December 20th, 2023, C# Dev Kit hot reload does not handle class libraries. To experience the best hot reload, do not use the debugger.
  - Without the debugger: The VS Code Uno Platform extension is handling Hot Reload (C# and XAML).
  - Adding new C# or XAML files to a project is not yet supported.
- Rider
  - Hot Reload is only supported without the debugger.
  - Adding new C# or XAML files to a project is not yet supported.

### [**WebAssembly**](#tab/wasm)

WebAssembly is currently providing full Hot Reload support.

- In Visual Studio Code:
  - Both C# and XAML Hot Reload are fully supported.
  - Adding new C# or XAML files to the project is not yet supported.
  - Hot Reload is not supported when using the debugger.
- In Rider:
  - Both C# and XAML Hot Reload are fully supported.
  - Adding new C# or XAML files to the project is not yet supported.
  - Hot Reload is not supported when using the debugger.

### [**iOS, Android**](#tab/mobile)

Mobile targets now support both XAML and C# Hot Reload. Debugger-specific variations apply depending on the IDE.

- In Visual Studio:
  - The debugger **has** to be attached.
- In VS Code, and Rider:
  - Hot Reload is not supported when using the debugger.
- XAML `x:Bind` Hot Reload is limited to simple expressions and events.

### [**Catalyst**](#tab/catalyst)

Mobile targets now support both XAML and C# Hot Reload.

- XAML `x:Bind` hot reload is limited to simple expressions and events.

### [**WinAppSDK**](#tab/winappsdk)

- Hot Reload is supported by Visual Studio for WinAppSDK and provides support in unpackaged deployment mode.
- Hot Reload is not supported in VS Code and Rider.

---

[!INCLUDES [learn-more-about-hot-reload](includes/learn-more-about-hot-reload-inline.md)]
