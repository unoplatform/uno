# Uno Support for Windows.ApplicationModel.Email

## `EmailManager`

The `ShowComposeNewEmailAsync` method is supported.

On Android, it lets the user choose the target e-mail application before composing the e-mail.

On iOS, it uses `MFMailComposeViewController` but falls back to `mailto:` in case user has not set up any e-mail account yet.

On other platforms, the `mailto:` URI is used to try to compose the e-mail. This uses the `Launcher` API to launch the URI, so works on all platforms where `Launcher` is supported.

E-mail attachments are not supported yet.