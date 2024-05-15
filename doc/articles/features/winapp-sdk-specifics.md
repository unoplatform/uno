---
uid: Uno.Features.WinAppSDK
---

# Specific considerations for WinAppSDK

## Adjusting Windows SDK References

When building a WinUI3 app, you may encounter the following error message:

```output
error NETSDK1148: A referenced assembly was compiled using a newer version of Microsoft.Windows.SDK.NET.dll. Please update to a newer .NET SDK in order to reference this assembly.
```

This indicates that your application, or current .NET SDK is using a version of the Windows .NET SDK which is lower than the SDKs used to compile any of your projects dependent nuget packages.

To fix this, find or add the following block in your Windows `.csproj` file:

```xml
<ItemGroup>
    <!--
    If you encounter this error message:

        error NETSDK1148: A referenced assembly was compiled using a newer version of Microsoft.Windows.SDK.NET.dll. Please update to a newer .NET SDK in order to reference this assembly.

    This means that the two packages below must be aligned with the "build" version number of
    the "Microsoft.Windows.SDK.BuildTools" package above, and the "revision" version number
    must be the highest found in https://www.nuget.org/packages/Microsoft.Windows.SDK.NET.Ref.
    -->

    <FrameworkReference Update="Microsoft.Windows.SDK.NET.Ref" RuntimeFrameworkVersion="10.0.22000.25" />
    <FrameworkReference Update="Microsoft.Windows.SDK.NET.Ref" TargetingPackVersion="10.0.22000.25" />
</ItemGroup>
```

To find the appropriate version:

- The two `FrameworkReference` packages must be aligned with the "build" version number (`22000` in the example above) of the "Microsoft.Windows.SDK.BuildTools" package defined earlier in the project
- The "revision" version number (`25` in the example above) must be the highest found in the versions of https://www.nuget.org/packages/Microsoft.Windows.SDK.NET.Ref.

## Un-packaged application support

By default the **Uno Platform App** Visual Studio template creates a packaged application. If you want to add un-packaged support, you'll need to do the following:

- Add a new entry in the launchSettings.json file:

    ```json
    {
        "profiles": {
            "UnoWinUIQuickStart.Windows (Package)": {
                "commandName": "MsixPackage"
            },
            "UnoWinUIQuickStart.Windows (Unpackaged)": {
                "commandName": "Project"
            }
        }
    }
    ```

- Add this new set of properties in your `.Windows` csproj:

    ```xml
      <PropertyGroup>
        <!-- Bundles the WinAppSDK binaries (Uncomment for unpackaged builds) -->
        <WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>
        <!-- This bundles the .NET Core libraries (Uncomment for packaged builds)  -->
        <!-- <SelfContained>true</SelfContained> -->
    </PropertyGroup>
    ```

    You will need to adjust which property is enabled based on the deployment target that you are choosing. Both properties are not supported (as of WinAppSDK 1.0.3).
