---
uid: Uno.Features.WAMCalls
---

# Phone calls

> [!TIP]
> This article covers Uno-specific information for `PhoneCallManager`. For a full description of the feature and instructions on using it, see [PhoneCallManager Class](https://learn.microsoft.com/uwp/api/windows.applicationmodel.calls.phonecallmanager).

* `Windows.ApplicationModel.Calls.PhoneCallManager` allows apps to present a phone call UI to the user to allow them to initiate a call.
* The current call state can be checked using `IsCallActive` and `IsCallIncoming` properties.
* `CallStateChanged` event is raised when a call is incoming, accepted or dropped.

## Supported features

| Feature                   | Windows | Android | iOS | Web (WASM) | Desktop (macOS) | Desktop (X11) | Desktop (Windows) |
|---------------------------|---------|---------|-----|------------|-----------------|---------------|-------------------|
| `ShowPhoneCallUI`         | ✔       | ✔       | ✔   | ✔          | ✖               | ✖             | ✖                 |
| `ShowPhoneCallSettingsUI` | ✔       | ✔       | ✖   | ✖          | ✖               | ✖             | ✖                 |
| `CallStateChanged`        | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |
| `IsCallActive`            | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |
| `IsCallActive`            | ✔       | ✔       | ✔   | ✖          | ✖               | ✖             | ✖                 |

## Examples

### Show phone call UI

```csharp
PhoneCallManager.ShowPhoneCallUI("123456789", "Jon Doe");
```

### Observe phone call state

```csharp
PhoneCallManager.CallStateChanged += PhoneCallManager_CallStateChanged;

private void PhoneCallManager_CallStateChanged(object sender, object e)
{
    var isCallActive = PhoneCallManager.IsCallActive;
    var isCallIncoming = PhoneCallManager.IsCallIncoming;
}
```

## Limitations

For the `ShowPhoneCallUI` method, the second parameter (`displayName`) is not utilized on non-Windows targets - only the phone number is displayed to the user.
