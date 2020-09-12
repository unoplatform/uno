# Uno Support for Windows.ApplicationModel.DataTransfer

## `Clipboard`

| Feature        | Android | iOS | macOS | WASM |
|----------------|---------|-----|-------|------|
| SetContent     | ✅      | ✅ | ✅    | ✅ |
| GetContent     | ✅      | ✅ | ✅    | ✅ |
| Flush          | ✅      | ✅ | ✅    | ✅ |
| Clear          | ✅      | ✅ | ✅    | ✅ |
| ContentChanged | ✅      | ✅ | ✅    | ✅ |

### Limitations

`SetContent` and `GetContent` APIs currently support textual data on all platforms. On Android, they also support URI and HTML formats, but clipboard can hold only one item. Setting multiple items at once does not work reliably.

`Flush` operation has an empty implementation. In contrast to UWP, on other platforms data automatically remain in the clipboard even after application is closed.

`ContentChanged` event can observe clipboard changes only when the application is in the foreground.

On macOS, the `ContentChanged` event checks for clipboard changes by polling the current `NSPasteboard` change count in 1 second intervals. The polling starts only after first subscriber attaches to `ContentChanged` event and stops after the last subscriber unsubscribes.