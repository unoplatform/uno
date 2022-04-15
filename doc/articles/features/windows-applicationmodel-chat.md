# Uno Support for Windows.ApplicationModel.Chat

## `ChatMessageManager`

### Limitations

**macOS**
`ShowComposeSmsMessageAsync` method is implemented but due to limitations of iMessage `sms:` scheme implementation, it doesn't support multiple `Recipients` (will only use the first one)

## `ChatMessageStore`

### Limitations

**Android**
Using `ChatMessageStore` on Android is complicated.
Step by step guide:
1) You have to declare either `Android.Manifest.Permission.WriteSms` or `Android.Manifest.Permission.ReadSms` in Android Manifest.
2) As Android doc says, "only the app that receives the SMS_DELIVER_ACTION broadcast (the user-specified default SMS app) is able to write to the SMS Provider" - so, if you want to use `ChatMessageStore` in write mode, you have to get `Android.App.Roles.RoleManager.RoleSms` for your app
For your convenience, Uno adds two methods for you:
a) `ChatMessageStore.SwitchDefaultSMSappAsync(string newAppName)` 
b) `ChatMessageStore.SwitchDefaultSMSappAsync(bool toThisApp)`, with either `true` (to make your app being default app for handling SMS), or `false` to return to return previous default handler.
Both returns true if switch is successful, and both works on Android Kitkat (18) to Q (28).

See also https://stackoverflow.com/questions/21720657/how-to-set-my-sms-app-default-in-android-kitkat, or https://android-developers.googleblog.com/2013/10/getting-your-sms-apps-ready-for-kitkat.html.
