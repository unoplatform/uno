---
uid: Uno.Studio.HotReload.Controls
---

# Controls in the Hot Reload UI

This article will give you a Overview about the Controls and Icons you will see and work with, when using Hot Reload in your specific IDE.

## Hot Reload Indicator

> [!NOTE]
> The **Hot Reload indicator** is currently not supported for the **WinAppSDK** target.

Hot Reload displays a visual indicator to help you further monitor changes while developing. It displays new information every time Hot Reload is triggered. The indicator is enabled by default within the `UseStudio()` method which is located in the root `App.xaml.cs` file. This displays an overlay that hosts the visual indicator. If you wish to disable it, you simply have to provide the following boolean: `UseStudio(showHotReloadIndicator: false)`, removing the overlay from the view.

To start using the **Hot Reload indicator** using the **latest stable 5.5 Uno.Sdk version or higher**, ensure you are signed in with your Uno Platform account. Follow [these instructions](xref:Uno.GetStarted.Licensing) to register and sign in.

<p align="center">
  <img src="~/articles/Assets/features/hotreload/indicator-not-connected-with-flyout.png" alt="The Hot Reload indicator is not connected. A flyout message states that Hot Reload is available only to registered users and prompts the user to sign in via the Uno Platform Settings button." />
</p>

For existing applications, take this opportunity to update to the [latest **Uno.Sdk** version](https://www.nuget.org/packages/Uno.Sdk/latest) to take advantage of all the latest improvements and support. Refer to our [migration guide](xref:Uno.Development.MigratingFromPreviousReleases) for upgrade steps.

> [!IMPORTANT]
> When upgrading to **Uno.Sdk 5.5 or higher**, the `EnableHotReload()` method in `App.xaml.cs` is deprecated and should be replaced with `UseStudio()`.

<p align="center">
  <img src="~/articles/Assets/features/hotreload/indicator.png" alt="A hot reload visual indicator" />
</p>

> [!TIP]
> The overlay can be moved by using the anchor on the left-hand side.

The indicator displays the current connection status. Clicking on it will open a flyout containing all events or changes that were applied by Hot Reload. These events display more details about Hot Reload changes, such as its status and impacted files.

<p align="center">
  <img src="~/articles/Assets/features/hotreload/indicator-flyout.png" alt="A window showing events from Hot Reload" />
</p>

## Statuses

Here's a summary of the Hot Reload connection statuses and their corresponding icons:

### Connection

- ![The icon indicating that the user is not signed in](Assets/status-connection-not-signed-in.png) **Not Signed In**  
  _User needs to sign in to enable Hot Reload._

- ![The icon indicating an ongoing connection attempt](Assets/status-connection-connecting.png) **Connecting**  
  _Establishing a connection._

- ![The icon indicating a successful connection](Assets/status-connection-connected.png) **Connected**  
  _Connection established._

- ![The icon indicating a connection issue](Assets/status-connection-warning.png) **Warning**  
  _Usually indicates an issue that can be resolved by restarting your IDE._

- ![The icon indicating a failed connection](Assets/status-connection-failed.png) **Connection Failed**  
  _A connection error occurred. Refer to the [troubleshooting documentation](#troubleshooting) for possible solutions._

- ![The icon indicating the server is unreachable](Assets/status-connection-server-unreachable.png) **Server Unreachable**  
  _Hot Reload could not connect to the server. Check the [troubleshooting documentation](#troubleshooting) for guidance._

#### Operation

- ![The icon shown when Hot Reload succeeds](Assets/status-hr-success.png) **Success**  
  _The Hot Reload changes have been applied successfully._

- ![The icon shown when Hot Reload fails](Assets/status-hr-failed.png) **Failed**  
  _Hot Reload encountered an error and could not apply the changes._

- ![The icon shown when Hot Reload is in progress](Assets/status-hr-processing.png) **Processing**  
  _Hot Reload is applying changes or initializing._

---

[!INCLUDES [learn-more-about-hot-reload](includes/learn-more-about-hot-reload-inline.md)]
