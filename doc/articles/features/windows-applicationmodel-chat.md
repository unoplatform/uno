---
uid: Uno.Features.WAMChat
---

# Chat

> [!TIP]
> This article covers Uno-specific information for the `Windows.ApplicationModel.Chat` namespace. For a full description of the feature and instructions on using it, see [Windows.ApplicationModel.Chat Namespace](https://learn.microsoft.com/uwp/api/windows.applicationmodel.chat).

* The `Windows.ApplicationModel.Chat` namespace provides classes for accessing and managing chat messages.

## `ChatMessageManager`

### Limitations

**macOS**
`ShowComposeSmsMessageAsync` method is implemented but due to limitations of iMessage `sms:` scheme implementation, it doesn't support multiple `Recipients` (will only use the first one).
