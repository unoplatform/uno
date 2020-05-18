# Uno Support for Windows.ApplicationModel.DataTransfer

## `Clipboard`

| Feature        | Android | iOS | macOS | WASM |
|----------------|---------|-----|-------|------|
| SetContent     | ✅      | ✅ | ✅    | ✅  |
| GetContent     |          | ✅ | ✅    | ✅ |
| Flush          | ✅      | ✅ | ✅    | ✅  |
| Clear          | ✅      | ✅ | ✅    | ✅  |
| ContentChanged | ✅      | ✅ |       | ✅   |

### Limitations

`SetContent` and `GetContent` APIs currently support textual data only.

`Flush` operation has an empty implementation. In contrast to UWP, on other platforms data automatically remain in the clipboard even after application is closed.

`ContentChanged` event can observe clipboard changes only when the application is in the foreground.