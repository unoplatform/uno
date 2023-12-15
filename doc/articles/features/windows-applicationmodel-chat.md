---
uid: Uno.Features.WAMChat
---

# Chat

> [!TIP]
> This article covers Uno-specific information for `Windows.ApplicationModel.Chat` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.chat

* The `Windows.ApplicationModel.Chat.ChatMessageManager` namespace provides classes for accessing and managing chat messages.

## `ChatMessageManager`

### Limitations

**macOS**
`ShowComposeSmsMessageAsync` method is implemented but due to limitations of iMessage `sms:` scheme implementation, it doesn't support multiple `Recipients` (will only use the first one)
