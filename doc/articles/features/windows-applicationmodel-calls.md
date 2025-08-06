---
uid: Uno.Features.WAMCalls
---

# Phone calls

> [!TIP]
> This article covers Uno-specific information for `PhoneCallManager` and `PhoneCallHistoryManager`. For a full description of the features and instructions on using them, see [PhoneCallManager Class](https://learn.microsoft.com/uwp/api/windows.applicationmodel.calls.phonecallmanager) and [PhoneCallHistoryManager Class](https://learn.microsoft.com/uwp/api/windows.applicationmodel.calls.phonecallhistorymanager).

## PhoneCallManager

* `Windows.ApplicationModel.Calls.PhoneCallManager` allows apps to present a phone call UI to the user to allow them to initiate a call.
* The current call state can be checked using `IsCallActive` and `IsCallIncoming` properties.
* `CallStateChanged` event is raised when a call is incoming, accepted or dropped.

## PhoneCallHistoryManager

* `Windows.ApplicationModel.Calls.PhoneCallHistoryManager` provides access to the device's call history.
* Apps can read call history entries including information about incoming/outgoing calls, duration, timestamps, and contact information.
* On Android, requires appropriate permissions to access call logs and contacts.

## Supported features

### PhoneCallManager

| Feature                   | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|---------------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `ShowPhoneCallUI`         | ✔       | ✔       | ✔   | ✔          | ✖     | ✖            | ✖            |
| `ShowPhoneCallSettingsUI` | ✔       | ✔       | ✖   | ✖          | ✖     | ✖            | ✖            |
| `CallStateChanged`        | ✔       | ✔       | ✔   | ✖          | ✖     | ✖            | ✖            |
| `IsCallActive`            | ✔       | ✔       | ✔   | ✖          | ✖     | ✖            | ✖            |
| `IsCallIncoming`          | ✔       | ✔       | ✔   | ✖          | ✖     | ✖            | ✖            |

### PhoneCallHistoryManager

| Feature                    | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|----------------------------|---------|---------|-----|------------|-------|--------------|--------------|
| `RequestStoreAsync`        | ✔       | ✔       | ✖   | ✖          | ✖     | ✖            | ✖            |
| `PhoneCallHistoryStore`    | ✔       | ✔       | ✖   | ✖          | ✖     | ✖            | ✖            |
| `GetEntryReader`           | ✔       | ✔       | ✖   | ✖          | ✖     | ✖            | ✖            |
| `ReadBatchAsync`           | ✔       | ✔       | ✖   | ✖          | ✖     | ✖            | ✖            |

## Examples

### PhoneCallManager

#### Show phone call UI

```csharp
PhoneCallManager.ShowPhoneCallUI("123456789", "Jon Doe");
```

#### Observe phone call state

```csharp
PhoneCallManager.CallStateChanged += PhoneCallManager_CallStateChanged;

private void PhoneCallManager_CallStateChanged(object sender, object e)
{
    var isCallActive = PhoneCallManager.IsCallActive;
    var isCallIncoming = PhoneCallManager.IsCallIncoming;
}
```

### PhoneCallHistoryManager

#### Request access to call history

```csharp
try
{
    var store = await PhoneCallHistoryManager.RequestStoreAsync(
        PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
    
    if (store != null)
    {
        // Access granted - you can now read call history
        var reader = store.GetEntryReader();
        var entries = await reader.ReadBatchAsync();
        
        foreach (var entry in entries)
        {
            var phoneNumber = entry.Address.RawAddress;
            var displayName = entry.Address.DisplayName;
            var startTime = entry.StartTime;
            var duration = entry.Duration;
            var isIncoming = entry.IsIncoming;
            var isMissed = entry.IsMissed;
            var isVoicemail = entry.IsVoicemail;
        }
    }
    else
    {
        // Access denied or permissions not granted
    }
}
catch (Exception ex)
{
    // Handle permission errors or other exceptions
}
```

#### Read call history in batches

```csharp
var store = await PhoneCallHistoryManager.RequestStoreAsync(
    PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);

if (store != null)
{
    var reader = store.GetEntryReader();
    
    // Read first batch (up to 100 entries)
    var firstBatch = await reader.ReadBatchAsync();
    
    // Read next batch if available
    var secondBatch = await reader.ReadBatchAsync();
    
    // Continue reading until empty batch is returned
}
```

## Android permissions for PhoneCallHistoryManager

To use `PhoneCallHistoryManager` on Android, you need to declare the following permissions in your `AndroidManifest.xml`:

### Required permissions

```xml
<uses-permission android:name="android.permission.READ_CALL_LOG" />
<uses-permission android:name="android.permission.READ_CONTACTS" />
```

### Optional permissions

For write access (when using `PhoneCallHistoryStoreAccessType.AllEntriesReadWrite`):

```xml
<uses-permission android:name="android.permission.WRITE_CALL_LOG" />
```

The application will automatically request these permissions at runtime when calling `RequestStoreAsync()`. If the user denies the permissions, the method will return `null`.

## Limitations

### PhoneCallManager limitations

For the `ShowPhoneCallUI` method, the second parameter (`displayName`) is not utilized on non-Windows targets - only the phone number is displayed to the user.

### PhoneCallHistoryManager limitations

- **Android only**: PhoneCallHistory APIs are only supported on Android. iOS does not provide access to call logs for security and privacy reasons.
- **Permission requirements**: Requires sensitive permissions that users may deny.
- **Access type limitations**: `PhoneCallHistoryStoreAccessType.AppEntriesReadWrite` is not implemented - only system-wide call history access is supported.
- **Batch reading**: Call history entries are read in batches of up to 100 entries at a time.
- **Contact information**: Display names are only available if the app has `READ_CONTACTS` permission and the contact exists in the address book.
