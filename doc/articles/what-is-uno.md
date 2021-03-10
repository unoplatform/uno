# About the Uno Platform

Uno Platform is a cross-platform application framework which lets you write an application once in XAML and C#, and deploy it to [any target platform](getting-started/requirements.md). 

Uno Platform's application API is compatible with Microsoft's [UWP application API](https://docs.microsoft.com/en-us/windows/uwp/get-started/), and also with the upcoming [WinUI 3 API](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/). In fact, when your application runs on Windows, it's just an ordinary UWP (or WinUI 3) application. 

This means that existing UWP code is compatible with the Uno Platform. Existing UWP libraries can be recompiled for use in Uno Platform applications. A number of [3rd-party libraries](supported-libraries.md) have been ported to Uno Platform.

![High-level architecture diagram - WinUI on Windows, Uno.UI on other platforms](Assets/high-level-architecture-copy.png)

Uno Platform is pixel-perfect by design, delivering consistent visuals on every platform. At the same time, it rests upon the native UI framework on most target platforms, making it easy to [integrate native views](native-views.md) and tap into native platform features. 

Learn more about [how Uno works](how-uno-works.md).