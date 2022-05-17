# SMS

The `Windows.ApplicationModel.Chat.ChatMessageManager` class allows composing SMS messages and presenting them to the user to send. The class is supported on Windows, iOS, Android, and macOS.

### Limitations

**macOS**
`ShowComposeSmsMessageAsync` method is implemented but due to limitations of iMessage `sms:` scheme implementation, it doesn't support multiple `Recipients` (will only use the first one)
