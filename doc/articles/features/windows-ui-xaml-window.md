# Microsoft.UI.Xaml.Window

## Setting the background color for the Window

WinUI and UWP does not support the ability to provide a background color for `Window`, but Uno provides such an API through:

```
#if __HAS_UNO__
Uno.UI.Xaml.WindowHelper.SetBackground(Window.Current, new SolidColorBrush(Colors.Red));
#endif
```

This feature can help blend the background of the window with the background of the rendered app content when the window is resized.

### Supported platforms

|                            | Skia+GTK | Skia+WPF | iOS   | Android | macOS | Catalyst | WebAssembly |
| -------------------------- | :------: | :------: | :---: | :-----: | :---: | :------: | :---------: |
| WindowHelper.SetBackground |   ✔️     |    ✔️    | ❌    |  ❌     |  ❌  |   ❌     |  ❌         |
