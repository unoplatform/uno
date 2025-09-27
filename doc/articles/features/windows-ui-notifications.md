---
uid: Uno.Features.WinUINotifications
---

# Badge Notifications

> [!TIP]
> This article covers Uno-specific information for the `Windows.UI.Notifications` namespace. For a full description of the feature and instructions on using it, see [Windows.UI.Notifications Namespace](https://learn.microsoft.com/uwp/api/windows.ui.notifications).

* The `Windows.UI.Notifications` namespace provides classes for creating and managing badge notifications.

Badge notifications are supported on iOS, tvOS, and macOS.

macOS supports numeric and textual badges (as opposed to WinUI) but does not support glyphs.

iOS and tvOS allow for numeric badges only. It is also necessary to request user permission prior to executing the badge notification for the first time (this request can be executed when appropriate for your application):

```csharp
#if __IOS__ || __TVOS__
UNUserNotificationCenter.Current
    .RequestAuthorization(
        UNAuthorizationOptions.Badge,
        (granted, error) => {
            if (granted)
            {
                //now setting badge is possible
            }
        });
#endif
```
