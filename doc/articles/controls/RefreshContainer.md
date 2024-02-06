---
uid: Uno.Controls.RefreshContainer
---

# RefreshContainer (Pull to Refresh)

## Summary

`RefreshContainer` is used to provide the pull-to-refresh UI functionality primarily for scrollable content.

The touch-based pull capability is currently available only on Android, iOS and Windows (via WinUI). However, on the other targets you can still manually call the `RequestRefresh()` method to display the refresh UI.

To handle the refresh, subscribe to the `RefreshRequested` event. You can perform any kind of work in the handler. To use `async/await`, make the method `async`, get the deferral in the beginning of the method, and complete it after all required work is finished:

```csharp
refreshContainer.RefreshRequested += OnRefreshRequested;

private async void OnRefreshRequested(
    object sender,
    RefreshRequestedEventArgs e)
{
    var deferral = e.GetDeferral();
    await Task.Delay(3000); // Do some asynchronous work
    deferral.Complete();
}
```

## Android and iOS specifics

On Android and iOS `RefreshContainer` requires a scrollable element as the child of the control. This can be either a `ScrollViewer` or a list-based control like `ListView`:

```xml
<RefreshContainer>
    <ScrollViewer>
        <!-- Your content that should support refresh -->
    </ScrollViewer>
</RefreshContainer>
```
