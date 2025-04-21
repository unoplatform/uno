---
uid: Uno.UI.CommonIssues.Android
---

# Issues related to Android projects

## ADB0020 - The package does not support the CPU architecture of this device

This error may occur when deploying an application to a physical device with ARM architecture. To resolve this issue, you will need to add the following to your csproj anywhere inside the `<PropertyGroup>` tag:

```xml
  <RuntimeIdentifiers Condition="$(TargetFramework.Contains('-android'))">android-arm;android-arm64;android-x86;android-x64</RuntimeIdentifiers>
```
