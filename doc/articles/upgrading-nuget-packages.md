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
> In Visual Studio 2022 once the Uno Version is updated, you'll need to close/reopen the solution or restart Visual Studio for the change to take effect.
> 
> At this time the NuGet package Manager does not parse or manage Sdks provided by NuGet. If you would like to see this feature added, please be sure to provide your [feedback or upvote this issue](https://github.com/NuGet/Home/issues/13127).

To find the version to updatet to, pick the latest stable build from the [Uno.WinUI](https://www.nuget.org/packages/Uno.WinUI) package with either:

- When using Visual Studio 2022, use the NuGet Package Manager
- Use [`dotnet outdated`](https://github.com/dotnet-outdated/dotnet-outdated):
  - Install the tool using:
    ```xaml
    dotnet tool install --global dotnet-outdated-tool
    ```
  - Then, at the root of the solution, run the tool with:
    ```xaml
    dotnet outdated
    ```
  - When available, the tool will provide the versions which can be updated.
- [Uno.WinUI in Nuget Package Explorer](https://nuget.info/packages/Uno.WinUI)
- [Uno.WinUI in nuget.org](https://www.nuget.org/packages/Uno.WinUI)

Once the version has been chosen, change the `global.json` line with `"Uno.Sdk"` to use the newer version of Uno Platform. If you're running Visual Studio 2022, make sure to close/reopen the solution or restart the IDE.
