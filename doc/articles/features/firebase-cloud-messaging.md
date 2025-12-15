---
uid: Uno.Features.FirebaseCloudMessaging
---

# Implementing Firebase Cloud Messaging (FCM) in Uno Platform Apps

This guide walks through the process of implementing Firebase Cloud Messaging (FCM) for push notifications in an Uno Platform application targeting Android.

## Platform Support

| Platform         | Supported | Notes                                 |
|-----------------|-----------|---------------------------------------|
| Android         | ✅ Yes    | Full support via Firebase SDK         |
| Windows         | ❌ No     | Not supported                         |
| iOS             | ❌ No     | Not supported (use APNs instead)      |
| WebAssembly     | ❌ No     | Not supported                         |
| macOS (Catalyst)| ❌ No     | Not supported                         |
| Linux (Skia)    | ❌ No     | Not supported                         |

> [!NOTE]
> Firebase Cloud Messaging is only supported on Android. For push notifications on other platforms, use the platform's native notification services (e.g., APNs for iOS, Windows Push Notification Services for Windows).
## Step 1: Set Up Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or use an existing one
3. Add an Android app to your Firebase project:
   - Package name should match your application ID in the .csproj file (e.g., `com.companyname.UnoFCM`)
   - Download the `google-services.json` file

## Step 2: Add Required NuGet Packages

Add the following NuGet packages to your project, using the `<ItemGroup>` with a condition to ensure they're only added for the Android target:

```xml
<!-- Update 'net10.0-android' to match your target framework version if needed -->
<ItemGroup Condition="'$(TargetFramework)'=='net10.0-android'">
    <PackageReference Include="Xamarin.Firebase.Messaging" />
    <PackageReference Include="Xamarin.GooglePlayServices.Base" />
    <PackageReference Include="Xamarin.Google.Dagger" />
    <PackageReference Include="Xamarin.AndroidX.Core" />
</ItemGroup>
```

## Step 3: Add google-services.json to Your Project

1. Place the downloaded `google-services.json` file in the `Platforms/Android` folder
2. Add the following to your project file (.csproj):

```xml
<ItemGroup>
  <None Remove="Platforms\Android\google-services.json" />
</ItemGroup>
<ItemGroup>
  <GoogleServicesJson Include="Platforms\Android\google-services.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </GoogleServicesJson>
</ItemGroup>
```

## Step 4: Update AndroidManifest.xml

Add the necessary permissions and receivers to your `AndroidManifest.xml` in the `Platforms/Android` folder:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
    <uses-permission android:name="android.permission.WAKE_LOCK" />

    <application>
        <receiver android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver"
                  android:exported="false" />
        <receiver android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver"
                  android:exported="true"
                  android:permission="com.google.android.c2dm.permission.SEND">
            <intent-filter>
                <action android:name="com.google.android.c2dm.intent.RECEIVE" />
                <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
                <category android:name="${applicationId}" />
            </intent-filter>
        </receiver>
    </application>
</manifest>
```

## Step 5: Create a Firebase Messaging Service

Create a custom service class in the `Platforms/Android` directory called `MyFirebaseMessagingService.cs`:

```csharp
using Android.App;
using Android.Content;
using Firebase.Messaging;
using AndroidX.Core.App;
using Android.OS;
using Android.Media;
using Android.Graphics;
using System.Net.Http;

namespace YourAppNamespace.Platforms.Android
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);
            var notificationBody = message.GetNotification()?.Body;
            var notificationTitle = message.GetNotification()?.Title;
            var imageUrl = message.GetNotification()?.ImageUrl?.ToString();
            var dataPayload = message.Data;
            SendNotification(notificationTitle, notificationBody, imageUrl, dataPayload);
        }

        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            System.Diagnostics.Debug.WriteLine($"FCM Token: {token}");
            // Store or send this token to your server
        }

        private async void SendNotification(string title, string body, string imageUrl, IDictionary<string, string> data)
        {
            var notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
            var channelId = "DefaultChannel";

            // Create notification channel for Android 8.0 (API level 26) and higher
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    channelId,
                    "Default",
                    NotificationImportance.Max)
                {
                    Description = "Default notifications channel"
                };
                channel.SetShowBadge(true);
                channel.EnableLights(true);
                channel.EnableVibration(true);
                channel.SetBypassDnd(true);
                channel.LockscreenVisibility = NotificationVisibility.Public;
                notificationManager.CreateNotificationChannel(channel);
            }

            // Create intent that will open the app when notification is tapped
            var intent = PackageManager.GetLaunchIntentForPackage(PackageName);
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent,
                PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);

            // Build the notification
            var defaultSoundUri = RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            var notificationBuilder = new NotificationCompat.Builder(this, channelId)
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogAlert) // Replace with your own icon
                .SetContentTitle(title)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetSound(defaultSoundUri)
                .SetPriority(NotificationCompat.PriorityMax)
                .SetDefaults(NotificationCompat.DefaultAll)
                .SetVisibility(NotificationCompat.VisibilityPublic)
                .SetContentIntent(pendingIntent);

            // Add image if available
            if (!string.IsNullOrEmpty(imageUrl))
            {
                try
                {
                    using var client = new HttpClient();
                    var bitmap = await client.GetByteArrayAsync(imageUrl);
                    var image = BitmapFactory.DecodeByteArray(bitmap, 0, bitmap.Length);
                    notificationBuilder.SetLargeIcon(image)
                        .SetStyle(new NotificationCompat.BigPictureStyle()
                            .BigPicture(image)
                            .BigLargeIcon((Bitmap)null));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
                }
            }

            // Show the notification
            var notification = notificationBuilder.Build();
            notificationManager.Notify(_random.Next(), notification);
        }
    }
}
```

## Step 6: Initialize Firebase in MainActivity

Update your `MainActivity.Android.cs` to initialize Firebase and request notification permissions:

```csharp
using Android.App;
using Android.OS;
using Android.Content.PM;
using Firebase;

namespace YourAppNamespace.Platforms.Android
{
    [Activity(MainLauncher = true)]
    public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialize Firebase
            FirebaseApp.InitializeApp(this);

            // Request notification permissions for Android 13+
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                RequestNotificationPermission();
            }
        }

        private void RequestNotificationPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                this.RequestPermissions(
                    new[] { global::Android.Manifest.Permission.PostNotifications },
                    1001);
            }
        }
    }
}
```

## Step 7: Handling FCM Token

To get and handle the FCM token for sending notifications to specific devices:

1. The token is generated in the `OnNewToken` method of your Firebase Messaging Service
1. You can log it for development purposes or send it to your server
1. To manually retrieve the current token, you can add a method like this:

```csharp
public static async Task<string> GetFCMTokenAsync()
{
    var instanceId = FirebaseMessaging.Instance;
    var token = await instanceId.GetToken().ConfigureAwait(false);
    return token?.ToString();
}
```

## Step 8: Testing Push Notifications

You can test your implementation using the Firebase Console:

1. Go to your Firebase project console
1. Navigate to "Messaging" in the left sidebar
1. Click "Create your first campaign" or "Send your first message"
1. Choose "Test on Android"
1. Add your FCM token (that was printed in the debug output)
1. Fill in notification details and send

## Advanced Usage

### Handling Data Messages

Data messages allow you to send custom data payloads. They are handled in the `OnMessageReceived` method:

```csharp
public override void OnMessageReceived(RemoteMessage message)
{
    base.OnMessageReceived(message);
    
    // Check if the message contains a data payload
    if (message.Data.Count > 0)
    {
        // Handle the data payload
        var customData = message.Data;
        // Process the data as needed
    }

    // Check if the message contains a notification payload
    if (message.GetNotification() != null)
    {
        var notificationBody = message.GetNotification().Body;
        var notificationTitle = message.GetNotification().Title;
        // Display notification
    }
}
```

### Handling Notification Taps

To handle taps on notifications and perform specific actions:

1. Add data to the pending intent:

```csharp
var intent = PackageManager.GetLaunchIntentForPackage(PackageName);
intent.AddFlags(ActivityFlags.ClearTop);

// Add data payload to the intent
foreach (var item in data)
{
    intent.PutExtra(item.Key, item.Value);
}

var pendingIntent = PendingIntent.GetActivity(this, 0, intent,
    PendingIntentFlags.OneShot | PendingIntentFlags.Immutable);
```

2. In your MainActivity, handle the intent data:

```csharp
protected override void OnCreate(Bundle savedInstanceState)
{
    base.OnCreate(savedInstanceState);
    FirebaseApp.InitializeApp(this);
    
    // Check if opened from notification
    if (Intent?.Extras != null)
    {
        foreach (var key in Intent.Extras.KeySet())
        {
            var value = Intent.Extras.GetString(key);
            // Handle notification data
        }
    }
}
```

## Common Issues and Troubleshooting

1. **Notifications not showing**:
   - Ensure you have correct permissions in the manifest
   - Check if notification channels are properly set up for Android 8.0+
   - Verify FCM token is correctly received and used

1. **Firebase initialization issues**:
   - Make sure `google-services.json` has the correct package name
   - Ensure Firebase is initialized before any Firebase-related operations

1. **Permission issues on Android 13+**:
   - Make sure to explicitly request `POST_NOTIFICATIONS` permission

1. **Token not generated**:
   - Ensure Google Play Services are up to date on the test device
   - Check internet connectivity

## Resources

- [Firebase Documentation](https://firebase.google.com/docs/cloud-messaging)
- [Uno Platform Documentation](https://platform.uno/docs/)
- [Android Notification Channels](https://developer.android.com/develop/ui/views/notifications/channels)
