---
uid: Uno.Development.UpgradeUnoNuget
---
# How to upgrade Uno Platform NuGet Packages

Upgrading packages in your applications is done differently, depending on how your solution has been created.

- If your Uno Platform `.csproj` files start with `<Project Sdk="Uno.Sdk"`, you are using the [Uno.Sdk](xref:Uno.Features.Uno.Sdk) structure introduced in Uno Platform 5.1.
- If not, you are using the original project structure provided before Uno Platform 5.1.

Choose one of the sections below depending on your situation.

## Projects using the Uno.Sdk

The latest version of the Uno.Sdk is [![NuGet](https://img.shields.io/nuget/v/uno.sdk.svg)](https://www.nuget.org/packages/uno.sdk/).

To upgrade the Uno.Sdk, you'll need to open the `global.json` file located at the root of the solution, which typically looks like this:

```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "xx.yy.zz",
  }
}
```

Update the `xx.yy.zz` property to the latest Uno.Sdk version, then save the file.

> [!IMPORTANT]
> In Visual Studio 2022/2026, once the Uno Version is updated, a banner will ask to restart the IDE. Once the solution is reopened the changes will take effect.
>
> At this time, the NuGet package Manager does not parse or manage Sdks provided by NuGet. If you would like to see this feature added, please be sure to provide your [feedback or upvote this issue](https://github.com/NuGet/Home/issues/13127).

You can also browse the available versions of the Uno.Sdk using [Nuget Package Explorer](https://nuget.info).

## Projects without the Uno.Sdk

To upgrade NuGet packages without the Uno.Sdk, you can use the [Nuget Package Manager](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio) coming from Visual Studio. Choose the latest stable versions of Uno Platform's NuGet packages.
