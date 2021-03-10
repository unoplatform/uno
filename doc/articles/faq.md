# FAQ

## About Uno Platform

#### What is the Uno Platform?

Uno Platform is a cross-platform application framework which lets you write an application once in XAML and C#, and deploy it to [any target platform](getting-started/requirements.md). Read more [here](what-is-uno.md).

#### Who makes Uno Platform?

Uno Platform is an [open-source project](https://github.com/unoplatform/Uno) with many contributors. It was developed internally by [nventive](https://nventive.com) from 2013-2018, and has been open source since 2018. It's maintained by [nventive](https://nventive.com).

#### Where can I get support?
Support is available through [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform), [Twitter account](https://twitter.com/unoplatform), Discord www.platform.uno/discord - #uno-platform channel and email [info@platform.uno](mailto:info@platform.uno).

#### How can I get involved?
There are lots of ways to contribute to the Uno Platform and we appreciate all the help we get from the community. You can provide feedback, report bugs, give suggestions, contribute code, and participate in the platform discussions. If you're interested, the [contributors' guide](uno-development/contributing-intro.md) is a great place to start.

#### How can I report a bug?
- If you think you've found a bug, please [log a new issue](https://github.com/unoplatform/Uno/issues) in the Uno Platform GitHub issue tracker. When filing issues, please use our bug filing template. The best way to get your bug fixed is to be as detailed as you can be about the problem. Providing a minimal project with steps to reproduce the problem is ideal. Here are questions you can answer before you file a bug to make sure you're not missing any important information.

## Features

#### Is [Control X] supported by Uno Platform?

Consult [the list of supported WinUI controls](implemented-views.md).

#### What do I need to develop Uno Platform applications?

You can develop Uno Platform applications on Windows, macOS, or Linux. Supported IDEs include Visual Studio, Visual Studio Code, and Rider. Consult the [setup guide](get-started.md) for more details.

## Technologies

#### What is Universal Windows Platform (UWP)?

Universal Windows Platform (UWP) is an API created by Microsoft and first introduced in Windows 10. The purpose of this platform is to help develop universal apps that run on Windows 10, Windows 10 Mobile, Xbox One and HoloLens without the need to be re-written for each. It supports Windows app development using C++, C#, VB.NET, and XAML. The API is implemented in C++, and supported in C++, VB.NET, C#, F# and JavaScript. Designed as an extension to the Windows Runtime platform first introduced in Windows Server 2012 and Windows 8, UWP allows developers to create apps that will potentially run on multiple types of devices.

Visit Microsoft's documentation for a primer on UWP : https://docs.microsoft.com/en-us/windows/uwp/get-started/

#### What is WinUI 3?

WinUI 3 is the [next generation of Microsoft's Windows UI library](https://docs.microsoft.com/en-us/windows/apps/winui/), scheduled for stable release in 2021.

From [Microsoft](https://docs.microsoft.com/en-us/windows/apps/winui/):

> WinUI is the path forward for all Windows apps—you can use it as the UI layer on your native UWP or Win32 app, or you can gradually modernize your desktop app, piece by piece, with XAML Islands.
> All new XAML features will eventually ship as part of WinUI. The existing UWP XAML APIs that ship as part of the OS will no longer receive new feature updates. However, they will continue to receive security updates and critical fixes according to the Windows 10 support lifecycle.

Read more about [Uno and WinUI 3](uwp-vs-winui3.md).

#### How is Uno Platform different from Xamarin.Forms or Avalonia?
Multiple techniques can be used to render UI, ranging from rendering pixels in a Frame Buffer (Avalonia), to rendering only using platform-provided controls (Xamarin.Forms). 

While the former provides a high flexibility in terms of rendering fidelity and the ability to add new platforms, it has the drawback of not following the platform native behaviors. For instance, interactions with text boxes, has to be re-implemented completely in order to match the native behavior, and has to be updated regularly to follow platform updates. This approach also makes it very difficult to integrate native UI components "in-canvas", such as Map or Browser controls.

The latter, however, provides full fidelity with the underlying platform, making it blend easily with native applications. While this can be interesting for some kinds of applications, designers usually want to have a branded pixel-perfect look and feel which stays consistent across platforms, where drawing primitives are not available.

The Uno Platform sits in the middle, using the power of XAML to provide the ability to custom draw and animate UI, while reusing key parts of the underlying UI Toolkit (such as chrome-less native text boxes) to provide native system interactions support and native accessibility features.

#### What is XAML?

XAML stands for **eXtensible Application Markup Language**, and is used to provide a declarative way to define user interfaces. Its ability to provide a clear separation of concerns between the UI definition and application logic using data binding provides a good experience for small to very large applications where integrators can easily create UI without having to deal with C# code.