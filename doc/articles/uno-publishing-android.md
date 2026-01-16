---
uid: uno.publishing.android
---

# Publishing Your App for Android

## Preparing For Publish

- [Performance Profiling](xref:Uno.Tutorials.ProfilingApplications)

## Building your app

### Packaging your app using the CLI

To build your app from the CLI, on Windows, Linux, or macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net10.0-android -c Release -o ./publish
  ```

- Once the build is done, the output `.apk` and `.aab` files are located in the `./publish` folder.

## Publishing your Android App

Publishing an Uno Platform app uses the same steps as .NET for Android-based technologies.

Your app can be published:

- Through a market – There are multiple Android marketplaces that exist for distribution, with the most well-known being [Google Play](https://developer.android.com/distribute/googleplay/publish/index.html), or [Amazon](https://www.developer.amazon.com/docs/app-submission/submitting-apps-to-amazon-appstore.html).
- Via a website – An Uno Platform app can be made available for download on a website, from which users may then install the app by clicking on a link.
- Using [`adb`](https://learn.microsoft.com/en-us/dual-screen/android/emulator/adb) on the command line, with `adb install your.apk`. In Visual Studio 2022/2026, you can open a shell with `adb` available in the `PATH` with the **Tools**, **Android**, **Android Adb Command Prompt** menu.

Publishing an Uno Platform also uses the same steps as a MAUI app, which is based on .NET for Android. You can follow these [steps and links](https://learn.microsoft.com/dotnet/maui/android/deployment/?view=net-maui-8.0) to publish your app.
