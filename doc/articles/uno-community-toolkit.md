---
uid: Uno.Development.CommunityToolkit
---

# How to use Windows Community Toolkit

The [Windows Community Toolkit](https://learn.microsoft.com/windows/communitytoolkit/) is a collection of helper functions, custom controls, and app services. It simplifies and demonstrates common developer patterns when building experiences for Windows.

Depending on the version of the Windows Community Toolkit that you want to use, these tutorials below will walk you through adding and implementing:

- **[For WCT version 8.x](xref:Uno.Development.CommunityToolkit.v8):** The `SettingsCard` control, but the same steps can be followed for **other\*** Windows Community Toolkit components supported out of the box.
- **[For WCT version 7.x](xref:Uno.Development.CommunityToolkit.v7):** The `DataGrid` control, but the same steps can be followed for **other\*** Uno ported Windows Community Toolkit components.

**\* See the [non-Windows platform compatibility](#non-windows-platform-compatibility) section below for more details.**

> [!IMPORTANT]
> **Here is the [Migration Guide from v7 to v8 for Windows Community Toolkit](https://github.com/CommunityToolkit/Windows/wiki/Migration-Guide-from-v7-to-v8) for additional information on what changed lately between these versions.**
>
> For some controls (`DataGrid`, `Carousel`, ect...) you will need to use **version 7.x** for them as they are no longer available in the latest 8.x version of Windows Community Toolkit. The complete list of changes is available in the [migration guide](https://github.com/CommunityToolkit/Windows/wiki/Migration-Guide-from-v7-to-v8).
>
> For additional information, here are the releases notes for Windows Community Toolkit:
>
> - [Release notes for version 7.x](https://github.com/CommunityToolkit/WindowsCommunityToolkit/releases)
> - [Release notes for version 8.x](https://github.com/CommunityToolkit/Windows/releases)

## Non-Windows platform compatibility

### Overview

While all Windows Community Toolkit packages are supported on WinAppSDK, this is not the case for the other platforms Uno Platform supports.

### Unsupported Components

Some components, like [Media](https://github.com/CommunityToolkit/Windows/tree/main/components/Media/src), rely heavily on Composition APIs that are not yet supported by Uno Platform on all platforms. As a result, the Media package does not have the Uno-powered [MultiTargets](https://github.com/CommunityToolkit/Windows/blob/main/components/Media/src/MultiTarget.props) enabled, as the component would be non-functional out of the box.

In such cases, the Windows Community Toolkit prefers to disable the package for Uno Platform rather than enable it and have it not work. To address these functional gaps, we encourage contributing to Uno Platform to bridge the gaps for the missing supported APIs or to the [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows) by seeking or helping to build Uno-compatible alternatives.

### Partial Support

In limited cases, WCT packages may have partial support for Uno Platform where the TargetFramework is enabled, but not all Toolkit code works out of the box. Currently, the only package in this scenario is Animations. It has a special FrameworkLayer abstraction that enables `AnimationBuilder` and `CustomAnimation` on Uno-powered MultiTargets but does not extend to `ImplicitAnimationSet` or Connected Animations.
See [CommunityToolkit/Windows #319](https://github.com/CommunityToolkit/Windows/issues/319) for tracking.

### Majority Support

The majority of controls are supported on all platforms by default.
If you find a package that doesn't work as expected on Uno Platform, please open an [issue](https://github.com/unoplatform/uno/issues/new/choose) or [discussion](https://github.com/unoplatform/uno/discussions) to let us know.

## Troubleshooting

The features and support for Uno Platform and Windows Community Toolkit components are constantly evolving. Therefore, you may encounter some issues while building your application. We encourage you to report these [issues](https://github.com/unoplatform/uno/issues/new/choose) and engage in [discussions](https://github.com/unoplatform/uno/discussions) to help improve the platform.

[!include[getting-help](includes/getting-help.md)]
