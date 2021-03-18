# Uno Support for Windows.ApplicationModel.Chat

## `ChatMessageManager`

### Limitations

**macOS**
`ShowComposeSmsMessageAsync` method is implemented but due to limitations of iMessage `sms:` scheme implementation, it doesn't support multiple `Recipients` (will only use the first one)
