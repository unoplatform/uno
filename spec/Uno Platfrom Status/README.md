# Uno Platform status

a.k.a. UDEI Uno Development Environment Indicator

Well known places to update:
* VSIX:
  * Components: https://github.com/unoplatform/uno.studio/blob/main/src/Uno.Studio/Studio.Extensions/Tools/_IDEChannel/DevelopmentEnvironmentComponent.WellKnown.cs
  * Messages: https://github.com/unoplatform/uno.studio/blob/main/src/Uno.Studio/Studio.Extensions/Tools/_IDEChannel/DevelopmentEnvironmentStatusIdeMessage.WellKnown.cs
* VS.RC + Dev-Server: 
  * Messages: https://github.com/unoplatform/uno/blob/master/src/Uno.UI.RemoteControl.Messaging/IDEChannel/UDEI/DevelopmentEnvironmentStatusIdeMessage.WellKnown.cs
* Rider:
  * Components: __TBD__
  * Messages: https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentStatusMessages.cs  
* VS Code: __TBD__

> [!IMPORTANT]
> This only defines the places for the **well-known core components**.
> The indicator is extensible and any component can report it's own state by resolving the [IUnoDevelopmentEnvironmentIndicator](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.RemoteControl.VS/VSIXChannel/IUnoDevelopmentEnvironmentIndicator.cs) from the IDEChannel.

# Components

| Component ID | Display name | **Displayed** description |
| --- | --- | --- |
| uno.solution | Solution | Load of the solution, resolution of nuget packages and validation of uno's SDK version. | 
| uno.check | Uno Check* | Validates all external dependencies has been installed on the computer. |
| uno.dev_server | Dev Server* | The local server that allows the application to interact with the IDE and the file-system. |

(*: Update doc + update command in VS Code, not part of this issue)

# Solution

## States
| Kind | Display text | Technical description  | Code link | Actions | 
| --- | --- | --- | --- | --- |
| 🔄️ | Loading.. | a solution file has just been opened, processing it to determine if it's an uno solution | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L13)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L13)<br/>• ${\color{red} VSCode}$ | `generic` |
| 🔄️ | NuGet restore... | a solution file has been opened, we are waiting for the nuget restore to complete to determine if it's an uno solution | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L19)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L25)<br/>• ${\color{red} VSCode}$ | `nuget` |
| 🟥 | Uno SDK not found | solution file has been opened, unable to find uno package while we was expecting to find it | •&nbsp; ${\color{red} \textsf{VS (VSIX)}}$<br/>• ${\color{red} Rider}$ <br/>• ${\color{red} VSCode}$ | `nuget` |
| 🟥 | Entry point not found | solution file has been opened, nuget restore completed, found uno package, but unable to version specific entry-point | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/DevServerLauncher.cs#L277) | `troubleshoot` |
| 🟥 | Entry point failed | solution file has been opened, nuget restore completed, found uno package, but an error occurred while initializing version specific entry-point | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/DevServerLauncher.cs#L385) | `troubleshoot` |
| 🟩 | Loaded | solution file has been opened, nuget restore completed, found uno package, loaded version specific entry-point from package | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L37)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L37)<br/>• ${\color{red} VSCode}$ | `generic` |
| ⬛ | Uno solution not found | solution file has been opened, nuget restore completed, but didn't find uno packages | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L37)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L49)<br/>• ${\color{red} VSCode}$ | `generic` |
| ⬛ | Closed | solution file is being closed | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L49)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L85)<br/>• ${\color{red} VSCode}$ | `generic` |

## Actions
| # | Display text | Command ID |  Effect
| --- | --- | --- | --- |
| `generic` | Learn More | `ide.open_browser` | https://aka.platform.uno/uno-platform-status <br /> _Explain what is UDEI and why there is an icon in status bar_ |
| `nuget` | Learn More| `ide.open_browser` | https://aka.platform.uno/uno-platform-status-nuget <br /> _Explain what is UDEI, why we are waiting for nuget restore and what to do if it fails (fix the solution!)_ |
| `troubleshoot` | Learn More | `ide.open_browser` | https://aka.platform.uno/uno-platform-status-troubleshooting <br /> _Explain how to get logs from the IDE extension_ |

# Uno Check

## States 
| Kind | Display text | Technical description  | Code link | Actions |
| --- | --- | --- | --- | --- |
| ⬛ | Pending | Waiting for the solution to be opened and uno version found | •&nbsp;VS(VSIX)<br/>•&nbsp;Rider<br/>•&nbsp;VSCode | `doc` |
| 🔄️ | Running... | uno-check is running | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L55)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L97)<br/>• ${\color{red} VSCode}$ | `doc` |
| 🟩 | Success | uno-check has run and didn't reported any problem | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L61)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L109)<br/>• ${\color{red} VSCode}$ |  `doc` |
| 🟧 | Reported a problem | uno-check completed and reported a problem | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L67)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L121)<br/>• ${\color{red} VSCode}$ |  `fix`, `doc` |
| 🟥 | Failed | failed to start uno-check | •&nbsp;[VS(VSIX)](https://github.com/unoplatform/uno.studio/blob/08cbaf4b8d87ed5b9f534cc6280fb1362c3432ee/src/Uno.Studio/Studio.Extensions/Tools/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L73)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L133)<br/>• ${\color{red} VSCode}$   |  `doc` |
| ⬛ | Disabled | uno-check has been disabled for this version of uno | •&nbsp; ${\color{red} \textsf{VS (VSIX)}}$<br/>• ${\color{red} Rider}$ <br/>• ${\color{red} VSCode}$ | `doc` |
| ⬛ | Uno solution not found | uno-check has not been started as no solution opened or not a uno solution | •&nbsp; ${\color{red} \textsf{VS (VSIX)}}$<br/>• ${\color{red} Rider}$ <br/>• ${\color{red} VSCode}$ | `doc` |

## Actions
| # | Display text | Command ID |  Effect |
| --- | --- | --- | --- |
| `doc` | Learn More | `ide.open_browser` | https://aka.platform.uno/uno-check <br /> _Explain what is uno-check and what it does (existing doc)_ |
| `fix` | Fix | `uno.check.fix` | Re-run uno-check with the fix flag |
| `ignore` | Ignore | `uno.check.ignore` | Ignores the uno-check errors for teh current version of uno |

# Dev Server

## States
| Kind | Display text | Technical description  | Code link | Actions |
| --- | --- | --- | --- | --- |
| ⬛ | Pending | Waiting for the solution to be opened and uno version found | •&nbsp;VS(VSIX)<br/>•&nbsp;Rider<br/>•&nbsp;VSCode | `doc` |
| 🔄️ | Starting... | dev-server is starting| •&nbsp;[VS(RC.VS)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.VS/VSIXChannel/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L13)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L145)<br/>•&nbsp;_VSCode_  | `doc` |
| 🟥 | Failed | dev-server is not running (and will not restart by its own) _Failed to start process_ | •&nbsp;[VS(RC.VS)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.VS/VSIXChannel/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L19)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L157)<br/>•&nbsp;_VSCode_  | `restart`, `doc` |
| 🟧 | Restarting... | dev-server is restarting (and will not restart by its own) _Failed to start process_ | •&nbsp;[VS(RC.VS)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.VS/VSIXChannel/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L25)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L169)<br/>•&nbsp;_VSCode_  | `doc` |
| 🟥 | Timeout | dev-server didn't connected back to IDE in the given delay | •&nbsp;[VS(RC.VS)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.VS/VSIXChannel/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L19)<br/>•&nbsp;[Rider](https://github.com/unoplatform/uno.rider/blob/main/src/dotnet/uno.rider/UDEI/UnoDevelopmentEnvironmentIndicatorExtensions.cs#L188)<br/>•&nbsp;_VSCode_  | `restart`, `doc` |
| ⬛ | Uno solution not found | dev-server has not been started as no solution opened or not a uno solution | • ${\color{red} \textsf{VS (VSIX)}}$<br/>• ${\color{red} Rider}$ <br/>• ${\color{red} VSCode}$ | `doc` |
| 🟧 | Studio unavailable | dev-server started, but was not able to discover add-ins (licensing) | •&nbsp;[**All** (DevSrv)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.Host/UnoDevEnvironmentService.cs#L23) | `troubleshoot`, `doc` |
| 🟧 | Unable to load Studio | dev-server started, discovered add-ins (licensing), but failed to load (at least one) | •&nbsp;[**All** (DevSrv)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.Host/UnoDevEnvironmentService.cs#L34) | `troubleshoot`, `doc` |
| 🟩 | Ready | dev-server started, discovered and loaded add-ins, waiting for connections from apps | •&nbsp;[**All** (DevSrv)](https://github.com/unoplatform/uno/blob/d7fb5e64a2b426d89deab597299f94853621a9b0/src/Uno.UI.RemoteControl.Host/UnoDevEnvironmentService.cs#L44) | `doc` |

## Actions
| # | Display text | Command ID |  Effect |
| --- | --- | --- | --- |
| `doc` | Learn More | `ide.open_browser` | https://aka.platform.uno/dev-server <br /> _Explain what is the dev-server and why we need it_ |
| `restart` | Restart | `uno.dev_server.restart` | Request to start the dev_server |
| `troubleshoot` | Troubleshoot | `ide.open_browser` | https://aka.platform.uno/dev-server-troubleshooting <br /> _Explain how to get logs of the dev-server_ |

# Generic statements
* We will keep the UDEI icon (i.e. uno icon) in the IDE's status bar, even the solution is not a "uno solution" (like what we currently have in VS Code)
* We keep only the icon in the status bar, remove the text
* We will change the title in the flyout to "Uno Platform Status"
* To support theme from the IDE
   * We will use IDE's default colors and fonts
   * If we have font size provided by the IDE (e.g. Rider) we will use them, otherwise we will use our own custom size
   * For other UI elements (like the buttons), we will use our own design (which anyway somehow match current design from all IDEs)
* We will make sure to always have at least one action button (to open the documentation of the feature)
* Links (like documentation on uno's website) will be opened in external browser
* [VS_CODE] As we are not able to have dynamic content as a "tool-tip", we will move content to a "full page" (common behavior in VS Code). This means we will need updated design from @NVLudwig to have something more responsive.
* [RIDER] We need to align icon size with other icons in the status bar
* Analytics: @Jen-Uno will determine which events should be fired from the UDEI (e.g. status flyout opened, action button clicked, etc.)
* We will **NOT** alter current notifications from other components (like if uno-check fails)
* Need to add support for the "Disabled" state of Uno-check
* Status is not expected to self-update is uno-check is re-run/fixed on the side.

# vNext
* Auto-open the flyout if an error is being reported?
    * Will conflict with the notifications from other components, like uno-check in case of failure, would require to remove them.
    * Would be disruptive in VS code where we will have a "full page".
* Add button to open the "Studio" app?

