---
uid: Uno.Development.UpgradeUnoNuget
---
# How to upgrade Uno Platform NuGet Packages

Upgrading packages in your applications is done differently, depending on how your solution has created.

- If your Uno Platform `.csproj` files start with `<Project Sdk="Uno.Sdk"`, your are using the [Uno.Sdk](https://www.nuget.org/packages/uno.sdk) structure introduced in Uno Platform 5.1.
- If not, you are using the original project structure provided before Uno Platform 5.1.

Choose one of the sections below depending on your situation.

## Projects without the Uno.Sdk

To upgrade nuget packages without the Uno.Sdk, you can use the [Nuget Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio) coming from Visual Studio. Choose the latest stable versions of Uno Platform's NuGet packages.

## Projects using the Uno.Sdk

Starting from Uno Platform 5.1 and using the new [Uno.Sdk](https://www.nuget.org/packages/uno.sdk), upgrading NuGet packages starting by `Uno.WinUI.` requires updating the `global.json` file at the root of your solution.

It typically looks similar to this:

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "5.1.0",
    "Microsoft.Build.NoTargets": "3.7.56"
  }
}
```

> [!IMPORTANT]
> At this time the NuGet Manager in Visual Studio 2022 does not parse or manage Sdks provided by NuGet. If you would like to see this feature added, please be sure to provide your [feedback or upvote this issue](https://github.com/NuGet/Home/issues/13127).

To update the version, pick the latest stable build from the [Uno.WinUI](https://www.nuget.org/packages/Uno.WinUI) package with either:
- [Uno.WinUI in Nuget Package Explorer](https://nuget.info)
- [Uno.WinUI in nuget.org](https://www.nuget.org/packages/Uno.WinUI)
- The latest Uno.WinUI package in the Visual Studio 2022 NuGet Package Manager.

Once the version has been chosen, change the `global.json` line with `"Uno.Sdk"` to use the newer version of Uno Platform.