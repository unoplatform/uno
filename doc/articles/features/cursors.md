---
uid: Uno.Features.Cursors
---

# Using pointer cursors

You can change the pointer cursor when the pointer hovers certain elements in your application at runtime on WebAssembly, macOS, or Skia Desktop by making a subclass of the `UIElement` of interest and setting its protected `ProtectedCursor` property, for example by adding a method in the the subclass.

```csharp
public void ChangeCursor(InputCursor cursor)
{
    this.ProtectedCursor = cursor;
}
```

Note that you must perform this action on the UI thread. For more details on how to use `ProtectedCursor`, follow this [discussion](https://github.com/microsoft/WindowsAppSDK/discussions/1816).

Note that the legacy `CoreWindow.PointerCursor` API is no longer supported.
