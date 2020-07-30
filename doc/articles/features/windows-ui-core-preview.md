# Uno Support for Windows.UI.Core.Preview

## `SystemNavigationManagerPreview`

The `CloseRequested` event is supported on macOS. No additional capability needs to be declared in the application manifest as opposed to UWP which requires the [`confirmAppClose` capability](https://docs.microsoft.com/en-us/uwp/api/windows.ui.core.preview.systemnavigationmanagerpreview.closerequested?view=winrt-19041#remarks).

To execute asynchronous logic, get a deferral at the beginning of the event handler and complete it after the logic is finished. 

Example of a close confirmation dialog:

```
private async void App_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
{
    var deferral = e.GetDeferral();
    var dialog = new MessageDialog("Are you sure you want to exit?", "Exit");
    var confirmCommand = new UICommand("Yes");
    var cancelCommand = new UICommand("No");
    dialog.Commands.Add(confirmCommand);
    dialog.Commands.Add(cancelCommand);
    if (await dialog.ShowAsync() == cancelCommand)
    {
        //cancel close by handling the event
        e.Handled = true;
    }
    deferral.Complete();
}
```