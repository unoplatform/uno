# Uno Support for Windows.UI.Notifications

## Badge notifications

Badge notifications are supported on iOS and macOS.

macOS supports numeric and textual badges (as opposed to UWP) but does not support glyphs.

iOS allows for numeric badges only. It is also necessary to request user permission prior to executing the badge notification for the first time (this request can be executed when appropriate for your application):

```
#if __IOS__
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