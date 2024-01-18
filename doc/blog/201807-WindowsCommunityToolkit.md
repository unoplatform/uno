---
uid: Uno.Blog.WindowsCommunityToolkit
---

# Uno Platform support for the Windows Community Toolkit

Recent updates to the Uno Platform have allowed for the [Windows Community Toolkit](https://github.com/unoplatform/uno.WindowsCommunityToolkit) to run
on iOS, Android and the Web through WebAssembly.

You can try it live here in your browser: http://windowstoolkit-wasm.platform.uno

Support for the Windows toolkit is an important part of the UWP development experience, as it provides controls and helpers missing
from the UWP base API, such as the [WrapPanel](https://github.com/Microsoft/WindowsCommunityToolkit/tree/master/Microsoft.Toolkit.Uwp.UI.Controls/WrapPanel),
[Headered` TextBlock](https://github.com/Microsoft/WindowsCommunityToolkit/tree/master/Microsoft.Toolkit.Uwp.UI.Controls/HeaderedTextBlock),
[DockPanel](https://github.com/Microsoft/WindowsCommunityToolkit/tree/master/Microsoft.Toolkit.Uwp.UI.Controls/DockPanel), etc...

We're providing an experimental Nuget package named [Uno.WindowsCommunityToolkit](https://github.com/unoplatform/uno.WindowsCommunityToolkit), to
allow for developers to use the same controls on all platforms.

Following the same direction we took for the other libraries ([MVVMLight](https://github.com/unoplatform/uno.mvvmlight),
[ReactiveUI](https://github.com/unoplatform/uno.ReactiveUI), [WindowsStateTriggers](https://github.com/unoplatform/uno.WindowsStateTriggers),
[Prism](https://github.com/unoplatform/uno.Prism), ...) the Uno-compatible packages are forks of the original repositories, in order to demonstrate
the viability of Uno as a target for those libraries. Ultimately, the Uno Platform developers will make pull requests back to the original
repositories, once it makes sense for the original maintainers and that the sources will no longer need significant structural
changes. For some of those libraries, changes are very limited as the UWP target can be adjusted to include iOS/Android/Wasm, where as for others
platform support is bundled in one single nuget package, requiring the extraction of platform specific code into new packages.

## Building support for the Windows Community Toolkit for Uno

To add initial support for the Windows Community Toolkit, some new features had to be added to the Uno Platform code
base, such as support for `x:Bind`, support for code-behind events in `DataTemplate`, and other small updates to have
a first list of available controls.

WCT being a showcase for all Windows features, the initial list of controls available in the sample application [only contains
the non-commented out samples](https://github.com/unoplatform/uno.WindowsCommunityToolkit/blob/uno/Microsoft.Toolkit.Uwp.SampleApp.Shared/SamplePages/samples.json) that the
Uno Platform supports in the current release. This list will grow over time, as more UWP APIs get implemented out of their stubs.

As the Uno Platform provides all APIs UWP definitions, almost all of the code is building without
modifications, with the exception of the Win2D and Gaze related code. Those are windows native dependencies,
and are currently not available for non-Windows targets.

Some notable parts of the toolkit are not working at the moment:

- Windows Composition APIs samples, for which the Uno Platform has very limited support on iOS only.
- Service based samples, such as OneDrive, LinkedIn or Microsoft Graph.

While the latter are built, in some cases the Microsoft-provided nuget packages cannot function properly
with the Uno Platform yet, and there may be some additional updates to do (such as the embedded login features).

## Going forward

If you need a particular control or service for your application to work on the Uno Platform, don't hesitate
to [create an issue](https://github.com/unoplatform/uno.WindowsCommunityToolkit/issues) and have it up-voted for it
to be prioritized by the Uno Platform team.
