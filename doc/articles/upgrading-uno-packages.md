---
uid: Uno.Development.UpgradeUnoNuget
---
# How to upgarde Uno Platform NuGet Packages

Starting from Uno Platform 5.1 and using the new Uno.Sdk, upgrading NuGet packages starting by `Uno.WinUI.` requires updating the `global.json` file at the root of your solution.

It typically looks like this:
```json
{
  "msbuild-sdks": {
    "Uno.Sdk": "5.1.0",
    "Microsoft.Build.NoTargets": "3.7.56"
  }
}
```

To update the version, pick a stable version from the Uno.WinUI packages either by using [nuget.org](https://www.nuget.org/packages/Uno.WinUI), or by looking at the NuGet Packager's latest stable package version.

Once the version has been chosen, change the global.json to use the newer version of Uno Platform.