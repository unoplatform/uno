---
uid: Uno.UI.CommonIssues.IosCatalyst
---

# Issues related to WebAssembly projects

### `Don't know how to marshal a return value of type 'System.IntPtr'`

[This issue](https://github.com/unoplatform/uno/issues/9430) may happen for Uno.UI 4.4.20 and later when deploying an application using the iOS Simulator or Mac Catalyst when the application contains a `TextBox`.

In order to fix this, add the following to your csproj (Xamarin, `net6.0-ios`, `net6.0-maccatalyst`):

```xml
<PropertyGroup>
  <MtouchExtraArgs>$(MtouchExtraArgs) --registrar=static</MtouchExtraArgs>
</PropertyGroup>
```
