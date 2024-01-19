---
uid: Uno.Features.WAMEmail
---

# E-mail

> [!TIP]
> This article covers Uno-specific information for `Windows.ApplicationModel.Email` namespace. For a full description of the feature and instructions on using it, see [Windows.ApplicationModel.Email Namespace](https://learn.microsoft.com/uwp/api/windows.applicationmodel.email).

* The `Windows.ApplicationModel.Email` namespace provides classes for accessing and managing e-mail messages.

## `EmailManager`

The `ShowComposeNewEmailAsync` method is supported.

On Android, it lets the user choose the target e-mail application before composing the e-mail.

On iOS, it uses `MFMailComposeViewController` but falls back to `mailto:` in case user has not set up any e-mail account yet.

On other platforms, the `mailto:` URI is used to try to compose the e-mail. This uses the `Launcher` API to launch the URI, so works on all platforms where `Launcher` is supported.

E-mail attachments are not supported yet.
