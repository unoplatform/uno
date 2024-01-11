---
uid: Uno.Features.AppCloseHandler
---

# App Close Handler

> [!TIP]
> This article covers Uno-specific information for YourFeature. For a full description of the feature and instructions on using it, consult the UWP documentation: https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.preview.systemnavigationmanagerpreview.closerequested

* The `SystemNavigationManagerPreview` API allows your app to handle or prevent users' requests to close it. This only works for standard app closing requests - the user can still kill the application's process by other means.

## Supported features

| Feature          | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `CloseRequested` | ✔       | ✖       | ✖   | ✖          | ✔     | ✔            | ✔            |

## Using App Close Handler with Uno

* On non-supported platforms the `CloseRequested` event is never raised and the application will close directly.
* To use `CloseRequested` on UWP/WinUI the [`confirmAppClose` capability](https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.preview.systemnavigationmanagerpreview.closerequested#remarks) needs to be declared in the application manifest. See [this blog post](https://blog.mzikmund.com/2018/09/app-close-confirmation-in-uwp/) for a full example.
* To execute asynchronous logic, get an event args `Deferral` at the beginning of the event handler and complete it after the logic is finished. See below for an example.

## Example

```csharp
SystemNavigationManagerPreview.CloseRequested += App_CloseRequested;

private async void App_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
{
    var deferral = e.GetDeferral();
    var dialog = new ContentDialog()
    {
        Title = "Exit",
        Content = "Are you sure you want to exit?",
        XamlRoot = this.XamlRoot,
        PrimaryButtonText = "Yes",
        SecondaryButtonText = "No",
        DefaultButton = ContentDialogButton.Secondary
    };

    if (await dialog.ShowAsync() == ContentDialogResult.Secondary)
    {
        //cancel close by handling the event
        e.Handled = true;
    }
    deferral.Complete();
}
```
