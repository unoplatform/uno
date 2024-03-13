---
uid: Uno.UI.CommonIssues.IosCatalyst
---

# Issues related to iOS/Mac Catalyst projects

## Developing on older Mac hardware

The latest macOS release and Xcode version are required to develop with Uno Platform for iOS and Mac Catalyst. However, if you have an older Mac that does not support the latest macOS release, you can use a third-party tool to upgrade it such as [OpenCore Legacy Patcher](https://dortania.github.io/OpenCore-Legacy-Patcher/). While not ideal, this can extend the use of older hardware by installing the latest macOS release on it. Please note that this method is not required when developing for other targets such as Android, Skia, WebAssembly, or Windows.

## `Don't know how to marshal a return value of type 'System.IntPtr'`

[This issue](https://github.com/unoplatform/uno/issues/9430) may happen for Uno.UI 4.4.20 and later when deploying an application using the iOS Simulator or Mac Catalyst when the application contains a `TextBox`.

In order to fix this, add the following to your csproj (Xamarin, `net6.0-ios`, `net6.0-maccatalyst`):

```xml
<PropertyGroup>
  <MtouchExtraArgs>$(MtouchExtraArgs) --registrar=static</MtouchExtraArgs>
</PropertyGroup>
```
