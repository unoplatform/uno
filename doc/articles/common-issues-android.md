---
uid: Uno.UI.CommonIssues.Android
---

# Issues related to Android projects

## ADB0020 - The package does not support the CPU architecture of this device

This error may occur when deploying an application to a physical device with ARM architecture. To resolve this issue, you will need to add the following to your csproj anywhere inside the `<PropertyGroup>` tag:

```xml
  <RuntimeIdentifiers Condition="$(TargetFramework.Contains('-android'))">android-arm;android-arm64;android-x86;android-x64</RuntimeIdentifiers>
```

## Deploying an Android app takes a long time

Android deployment requires a few considerations:

- Android physical device
  - Make sure to have a good cable (USB 3 or C) to have a good connection
  - Avoid debugging through wifi
- Android Emulators
  - Use an Android x86_64 emulator. If not, [create a new one](https://learn.microsoft.com/dotnet/maui/android/emulator/device-manager).
  - Ensure that you have either Hyper-V or AEHD enabled. (See [Microsoft's documentation](https://learn.microsoft.com/dotnet/maui/android/emulator/hardware-acceleration))
  - Try disabling `Fast Deployment` in your app configuration
        1. Open your project properties
        1. In the Android section, search for `Fast Deployment`
        1. Uncheck all target platforms
- Try setting `<EmbedAssembliesIntoApk>false</EmbedAssembliesIntoApk>` in the debug configuration of your `.csproj`.

## Android Warning XA4218

When building for Android, the following messages may happen:

```text
obj\Debug\net8.0-android\android\AndroidManifest.xml : warning XA4218: Unable to find //manifest/application/uses-library at path: C:\Program Files (x86)\Android\android-sdk\platforms\android-34\optional\androidx.window.extensions.jar
obj\Debug\net8.0-android\android\AndroidManifest.xml : warning XA4218: Unable to find //manifest/application/uses-library at path: C:\Program Files (x86)\Android\android-sdk\platforms\android-34\optional\androidx.window.sidecar.jar
```

Those messages are from a [known .NET for Android issue](https://github.com/xamarin/xamarin-android/issues/6809) and can be ignored as they are not impacting the build output.

## Additional troubleshooting

You can get additional build [troubleshooting information here](xref:Uno.Development.Troubleshooting).
