About Uno platform

## What is the Uno Platform?
The Uno Platform is a Universal Windows Platform Bridge to allow UWP based code to run on iOS, Android, and WebAssembly. It provides the full API definitions of the UWP Windows 10 2004 (19041), and the implementation of parts of the UWP API, such as Windows.UI.Xaml, to enable applications to run on these platforms.
This allows the use of the UWP tooling from Windows in Visual Studio, such as XAML Edit and Continue and C# Edit and Continue, to build an application as much as possible on Windows, then validate that the application runs on iOS, Android and WebAssembly.
The XAML User Interface (UI) provides the ability to display the same XAML files on Windows, iOS, Android and WebAssembly platforms. Uno also provides support for the MVVM pattern on all platforms, with binding, styling, control and data-templating features.
As the Uno Platform provides all of the APIs of the complete UWP platform, any UWP library can be compiled on top of Uno (e.g. XamlBehaviors), with the ability to determine which APIs are implemented or not via the IDE using C# Analyzers.

## What does Uno Platform do?

**For users**, it can provide a consistent experience across platforms, particularly between mobile and desktop browsers.

**For developers**, it can provide a consistent development experience across all platforms, using Microsoft's tooling as a base for a more efficient development loop.

**For designers**, XAML can provide a shared way to use pixel-perfect designs and use rich UI interactions.

## Why Uno?
Developing for Windows (phone, desktop, tablet, Xbox), iOS (tablet and phone), Android (tablet and phone) and the Web via WebAssembly at once can be a complex process, especially when it comes to the user interface. Each platform has its own ways of defining dynamic layouts, with some being more efficient, some more verbose, some more elegant, and some more performant than others.
Yet, being able to master all these frameworks at once is a particularly difficult task because of the amount of platform-specific knowledge required to master each platform. Most of the time it boils down to different teams developing the same application multiple times, with each requiring a full development cycle.
With Xamarin, C# comes to all these platforms; however, it only provides transparent translations of the UI frameworks available for iOS and Android. Most non-UI code can be shared, but when it comes to the UI, almost nothing can be shared.
To avoid having to learn the UI-layout techniques and approaches for each platform, Uno.UI mimics the Windows XAML approach of defining UI and layouts. This translates into the ability to share styles, layouts, and data-bindings while retaining the ability to mix XAML-style and native layouts. For instance, a StackPanel can easily contain a RelativeLayout on Android, or a MKMapView on iOS.
Uno.UI provides the ability for developers to reuse known layouts and coding techniques on all platforms, resulting in a gain of overall productivity when creating UI-rich applications.


## Who makes Uno Platform?
Uno Platform was developed by Team nventive over the past 4 years.

## What makes Uno Platform unique?
The Uno Platform is the only implementation of the UWP API that runs across iOS, Android and WebAssembly. 

## Is Uno Platform open source?
Yes, the Uno Platform is open source, under the [Apache 2.0 license](https://github.com/unoplatform/uno/blob/master/License.md).

# Getting started
## How can I try Uno platform?
You can try the Uno Platform using the [Uno Platform Playground](https://playground.platform.uno), the [Quick Start GitHub repository](https://github.com/unoplatform/uno.QuickStart), or through our  [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin).

## How experienced do I need to be to use Uno Platform?

## What is the playground?
The [Uno Platform Playground](https://playground.platform.uno) is a convenient way to experiment with the Uno platform, using only a WebAssembly compatible web browser.

## Where can I get support?
Support is available through [Stack Overflow](https://stackoverflow.com/questions/tagged/uno-platform), [Twitter account](https://twitter.com/unoplatform), and email [info@platform.uno](mailto:info@platform.uno).

## How can I get involved?
There are lots of ways to contribute to the Uno Platform and we appreciate the help from the community. You can provide feedback, report bugs, give suggestions, contribute code, and participate in the platform discussions.

## How can I report a bug?
- If you think you've found a bug, please [log a new issue](https://github.com/unoplatform/Uno/issues) in the Uno Platform GitHub issue tracker. When filing issues, please use our bug filing template. The best way to get your bug fixed is to be as detailed as you can be about the problem. Providing a minimal project with steps to reproduce the problem is ideal. Here are questions you can answer before you file a bug to make sure you're not missing any important information.
- Did you read the [documentation](https://github.com/unoplatform/Uno/tree/master/doc/)?
- Did you include the snippet of broken code in the issue?
- What are the EXACT steps to reproduce this problem?
- What specific version or build are you using?
- What operating system are you using?

GitHub supports markdown, so when filing bugs make sure you check the formatting before clicking submit.

# Technology

## What is UWP?

Universal Windows Platform (UWP) is an API created by Microsoft and first introduced in Windows 10. The purpose of this platform is to help develop universal apps that run on Windows 10, Windows 10 Mobile, Xbox One and HoloLens without the need to be re-written for each. It supports Windows app development using C++, C#, VB.NET, and XAML. The API is implemented in C++, and supported in C++, VB.NET, C#, F# and JavaScript. Designed as an extension to the Windows Runtime platform first introduced in Windows Server 2012 and Windows 8, UWP allows developers to create apps that will potentially run on multiple types of devices.

Visit Microsoft's documentation for a primer on UWP : https://docs.microsoft.com/en-us/windows/uwp/get-started/

## How is Uno platform different from Xamarin forms or Avalonia?
Multiple techniques can be used to render UI, ranging from rendering pixels in a Frame Buffer (Avalonia), to rendering only using platform-provided controls (Xamarin.Forms). 

While the former provides a high flexibility in terms of rendering fidelity and the ability to add new platforms, it has the drawback of not following the platform native behaviors. For instance, interactions with text boxes, has to be re-implemented completely in order to match the native behavior, and has to be updated regularly to follow platform updates. This approach also makes it very difficult to integrate native UI components "in-canvas", such as Map or Browser controls.

The latter, however, provides full fidelity with the underlying platform, making it blend easily with native applications. While this can be interesting for some kinds of applications, designers usually want to have a branded pixel-perfect look and feel which stays consistent across platforms, where drawing primitives are not available.

The Uno Platform sits in the middle, using the power of XAML to provide the ability to custom draw and animate UI, while reusing key parts of the underlying UI Toolkit (such as chrome-less native text boxes) to provide native system interactions support and native accessibility features.

## How does my Uno Platform-based application code run on Android or iOS?
Uno Platform-based applications on iOS and Android are no different than any other Xamarin-based applications. See [the details here](https://docs.microsoft.com/en-us/xamarin/cross-platform/app-fundamentals/building-cross-platform-applications/understanding-the-xamarin-mobile-platform).

## How does my Uno platform code run on the web?
On the Web, the application is built using the standard .NET tooling. The application is then transformed into a static website through the [Uno Web Boostrapper](https://github.com/unoplatform/uno.Wasm.Bootstrap), which uses [mono-wasm](https://github.com/mono/mono/tree/master/sdks/wasm) to run the C# code in the browser.

## Is WebAssembly supported in all browsers?
WebAssembly is supported in 4 major browser engines, see the [WebAssembly official site](https://webassembly.org/roadmap/) for more details.

## Where can I deploy my Uno Platform based app?
For iOS and Android, it can be deployed like any Xamarin-based application through the App Store and Play Store, respectively.

For WebAssembly, it can be deployed using [GitHub Pages](https://pages.github.com/), Azure [Web Apps](https://azure.microsoft.com/en-us/services/app-service/web/), [Azure Static Web Sites](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website), or any other static web site hosting provider.

## Does Uno.UI support macOS?
Not yet. We're still waiting for Apple's Marzipan cross macOS-iOS APIs to become available. We’ll see from there. 

## Does Uno.UI support Linux ?
No, not at the moment. Our assumption for now is that the WebAssembly part of Uno.UI will be efficient enough to be a viable alternative. If you find that native support still is a viable scenario, please open a GitHub issue.

## Does Uno.UI support what WPF is calling CustomControls ? 

Yes, those are called Templated Controls in the UWP dialect because they inherit from Control Uno.UI currently handles styles a bit differently from what WPF/UWP is doing and Uno.UI parser does not handle `<Style.Setters>` properly. These should not be impacting as long as you have a resource dictionary file containing your style. See here: https://github.com/unoplatform/uno/blob/master/doc/articles/api-differences.md#styles

## Is the iPhone X supported by Uno.UI ?

Yes, use the [VisibleBoundsPadding](features/VisibleBoundsPadding.md)
behavior to manage the _notch_ correctly.

## What features will Uno Platform support?

The end goal is to implement most features of the UWP API, but the [road map article](roadmap.md) details what is upcoming.

## Why XAML? What is it, how does it work?

XAML stands for **eXtensible Application Markup Language**, and is used to provide a declarative way to define user interfaces. Its ability to provide a clear separation of concerns between the UI definition and application logic using data binding provides a good experience for small to very large applications where integrators can easily create UI without having to deal with C# code.

For a good introduction to the use of XAML and MVVM patterns see [Microsoft's Laurent Bugnion presentation](https://twitter.com/LBugnion).

## What are the different flavors of XAML?
Over the years, Microsoft has been working on different implementations that use XAML for defining User Interfaces, and currently, three main flavors co-exist: WPF, UWP, Xamarin.Forms and the legacy Silverlight.

All these implementations use the same base XAML definition language, with minor differences in terms of the interpretation of the XML namespaces (`clr-namespace` vs. `using:`), and major differences in terms of the APIs used by the XAML parser.

For instance, WPF has [`System.Windows.Controls.StackPanel`](https://docs.microsoft.com/en-us/dotnet/api/system.windows.controls.stackpanel?redirectedfrom=MSDN&view=netframework-4.7.2), UWP
has [`Windows.UI.Xaml.Controls.StackPanel`](https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Controls.StackPanel) and Xamarin.Forms
has [`Xamarin.Forms.StackLayout`](https://docs.microsoft.com/en-us/dotnet/api/Xamarin.Forms.StackLayout?view=xamarin-forms). All three essentially behave in the same way, but have implementation differences and feature differences, though UWP, WPF and Silverlight have very similar behavior.

## Why Mono?
[Mono](https://github.com/mono/mono) is currently the best (and only) mobile-friendly implementation of .NET that targets iOS, Android and WebAssembly. It shares a lot -- and increasing -- of code from the BCL implementation with [.NET core](https://github.com/dotnet/core), making the runtime behavior very similar and in most cases, identical across platforms.

## What do you mean by #UWPeverywhere?

Our ultimate goal is to allow for the UWP api to run on all platforms, using Microsoft's own UWP implementation as a reference.

## What is a bridge?

Our definition of a bridge is the ability to reuse UWP-compatible source code unmodified, and allow it to compile on a different platform, yet behave at runtime the same way.

This definition comes from what Microsoft used to call when working on [Islandwood](https://developer.microsoft.com/en-us/windows/bridges/ios) and Astoria bridges to make iOS and Android code or binaries run on Windows.

## Why .NET?

Microsoft describes it best in its [What is .NET](https://www.microsoft.com/net/learn/what-is-dotnet) page:

> .NET is a free, cross-platform, open source developer platform for building many different types of applications. With .NET, you can use multiple languages, editors, and libraries to build for web, mobile, desktop, gaming, and IoT.

## Why build cross platform apps on the Microsoft stack?

## Will the Uno platform make my app look and run the same way on iOS and Android?

## What are the advantages of using Uno platform over Flutter/React Native/Xamarin Forms?

# Uno.UI Platforms Frequently Asked Questions

## Does Uno.UI support a single .NET Standard 2.0 binary model?
Not at the moment. iOS and Android platform support relies on the underlying APIs being visible through class hierarchy for performance reasons. Also, the .NET Standard model is based on binary sharing, which makes it very difficult to use platform features without jumping through hoops such as Dependency Injection, Inversion of Control or reflection.

## Does Uno.UI support having controls in a class library?
Yes, here's a project sample.  https://github.com/unoplatform/uno.Samples/tree/master/UI/ControlLibrary. It is also possible to create a new Cross-Platform class library using the [Uno Platform Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin).

## How do I update to the latest Uno.UI NuGet package, I don't see any update ?

You may want to try our latest dev builds, and here's how to do it:

- On your solution node, select **Manage NuGet packages for Solution**
- Next to the Search text box, check the **Include prerelease** box
- You should now see the latest Uno.UI package in the **Updates** tab.

## C# Edit and Continue does not work

There's an [issue in Visual Studio](https://developercommunity.visualstudio.com/content/problem/289600/c-edit-and-continue-changes-are-not-allowed-for-cr.html) that
makes the C# edit and continue for the UWP head fail with an error message if the Android, iOS or Wasm
heads are loaded. Temporary unload those projects to use C# edit and continue in the Windows head.

## XAML Edit and Continue does not work

You need to make sure that:
- Your Windows project's 
	- target SDK version is above Fall Creators Update (16299)
	- has been built once successfully
	- the `Microsoft.NETCore.UniversalWindowsPlatform` package version is **above 6.0**
- That you've closed all XAML editor Windows after having build the application
- That the project selector at the left of the XAML editor is selecting your windows project

If you're still having issues, **restart Visual Studio** and/or **unload the iOS/Android and WASM projects**, using right-click on the project, then **unload**.

## Is Uno.UI's Performance on WebAssembly going to improve
Yes! The current performance is limited by the runtime interpreted mode of mono-wasm. The Mono team is working on implementing **AOT** compilation,
which will significantly improve the performance. See [Miguel de Icaza's status update](https://gitter.im/aspnet/Blazor?at=5b1ab670dd54362753f8a168) for more details.

You can subscribe to this [mono issue's updates for progress](https://github.com/mono/mono/issues/10222).

## Why is Chrome slower than Firefox or Microsoft Edge to execute mono-wasm based applications ?

This is a [known Chromium issue](https://bugs.chromium.org/p/v8/issues/detail?id=7838#c7), which should be improving with
the release of [Chrome 69](https://www.chromium.org/developers/calendar).


## I’m getting this error on WebAssembly : Error: System.SystemException: Thread creation failed.

If you're getting this error:
```
Error: System.Threading.Tasks.TaskSchedulerException: An exception was thrown by a TaskScheduler. ---> System.SystemException: Thread creation failed.
at System.Threading.Thread.StartInternal (System.Security.Principal.IPrincipal principal, System.Threading.StackCrawlMark& stackMark) <0x1a68870 + 0x00028> in <bd95bcd953c94273b1c4cdb69e9b2632>:0
	Microsoft.Extensions.Logging.ConsoleLoggerExtensions.AddConsole (Microsoft.Extensions.Logging.ILoggerFactory factory, System.Func`3[T1,T2,TResult] filter, System.Boolean includeScopes)
```

Keep the version 1.1.1 of Microsoft.Extensions.Logging; latest version of Logging Extensions is starting a new thread and it's not supported in Wasm.

## Live updates doesn’t work on UWP project. 

For live update on UWP you need to edit the XAML while the project is debugging, no need for save, it updates on every keystroke (more or less) you need to update to the latest UWP sdk in your project configuration, change target version to latest


## I don't see any "Uno.UI App Solution" from File->Project->New ?

1. Install **Uno.UI Solution Template Visual Studio Extension** https://github.com/unoplatform/Uno/releases
1. Look for Uno.UI App Solution under Visual C#
1. If you still haven't found it, try the Search box


## How to port an existing UWP app to Uno.UI?

First create a shared project and move all .cs and .xaml files into it and reference the project in your UWP head
project.  Ensure everything is still working and add other projects for other platforms referencing the same shared project.

1. Create a project with Uno.UI template.
1. Copying as much code as possible from the existing UWP app to the "My Project.Shared"
1. Add platform specific code using suffixing files in the shared project (ex: ".iOS.cs")
1. iOS/Android specific heads should be relatively empty, only used os specific implementation (push notification handling/deeplinking/permissions)
1. Test, debug and publish.

## How to port an existing UWP library to Uno.UI ? 

This is essentially the same process as porting an app(add steps anyways), but using cross-targeted projects. Use
[this project](https://github.com/unoplatform/uno.Samples/blob/master/UI/ControlLibrary/XamlControlLibrary/XamlControlLibrary.csproj)
as a base for your cross-targeted library.

# Is it possible to make http web requests using the WASM target?

Yes it is possible, but you need to use the provided HttpHandler by Uno.UI like what we did in the Uno.UI Playground:

```csharp
var handler = new Wasm.WasmHttpHandler();
var httpClient = new HttpClient(handler);
```

## How do you choose which APIs are being implemented ?

- If the API is present in .NET Standard, it is generally suggested to use this one instead of the UWP one. (e.g System.IO or System.Net.HttpClient)Missing APIs will be implemented based on the popularity of suggestions in the [Uno.UI issues list](https://github.com/unoplatform/uno/issues). Make sure
to open one for the APIs you need.

## Can I know at runtime which APIs are implemented ?

Yes, through the [`ApiInformation`](https://docs.microsoft.com/en-us/uwp/api/Windows.Foundation.Metadata.ApiInformation) class.

## Wasm-based applications are getting blocked by my firewall/ad-blocker/proxy

Some work has been done to mitigate those issues, but in some instances, the mono-wasm can be heuristically
considered as a crypto-miner, or other type of malware.

If you encounter this type of issue, please open an issue with your setup's relevant installed software and versions.


## Problem when referencing netstandard2.0 projects

I'm getting the following error:
	`System.InvalidOperationException: The project(s) [...] did not provide any metadata reference. `
 
To work around this, you must include all the platforms you want to support in your TargetFrameworks node. See [this
question](https://stackoverflow.com/questions/50608089/referencing-a-netstandard2-0-project-in-a-platform-uno-project).



## My application on iOS/Android/Wasm is not showing icons properly

Uno.UI is making use of the Open-Source WinJS symbols font, which must be installed in your application directly:
- See the [Playground for an actual use example](https://github.com/unoplatform/uno.Playground/tree/master/src/Uno.Playground.iOS/Resources/Fonts).
- See the documentation for adding fonts for [iOS](https://github.com/unoplatform/uno/blob/master/doc/articles/using-uno-ui.md#custom-fonts-on-ios),  [Android](https://github.com/unoplatform/uno/blob/master/doc/articles/using-uno-ui.md#custom-fonts-on-android) and [WebAssembly](https://github.com/unoplatform/uno.Playground/blob/80322aec3d759d009f6a900bca4a07bc63ae6a62/Uno.UI.Demo.WASM/Uno.UI.Demo.WASM.csproj#L29).

## Where is the best place to start in order to implement new controls?

The best way is to create a QuickStart app using the Uno.UI template, take the style from the Microsoft `generic.xaml`,
place it in a resource dictionary and see what the XAML parser is telling.

## How does the navigation in a Uno.UI App compare to how Xamarin.forms "pushes" pages

Navigation is done through [`Frame`](https://docs.microsoft.com/en-us/windows/uwp/design/basics/navigate-between-two-pages)
	
Frame is more or less the same as a NavigationPage in Xamarin.Forms, and MasterDetails page in Xamarin.Forms is a `SplitView` in UWP.

## I never worked with shared project only PCL

Working with a shared project is not much different than working with PCLs, it's simply more flexible as you have access to all platforms, preferably via partial classes.

## Is there a UWP way to tell R# about my datacontext? 

https://docs.microsoft.com/en-us/windows/uwp/data-binding/displaying-data-in-the-designer#sample-data-from-class-and-design-time-attributes

Make sure you also reference libraries in each Head project ( myproject.iOS.csproj, myproject.Android.csproj, myproject.UWP.csproj), sharedprojects don't own references to libraries.

## Error `targets 'netstandard2.0'. It cannot be referenced by a project that targets 'UAP,Version=v10.0.14393'`
	
```
Cannot resolve Assembly or Windows Metadata file 'Type universe cannot resolve assembly: netstandard, Version=2.0.0.0, Culture=neutral, [...]

System.IO.FileLoadException: Could not load file or assembly 'System.Runtime, Version=4.2.1.0, Culture=neutral, PublicKeyToken=[...]'. The located assembly's manifest definition does not match the assembly reference.
```

Make sure to upgrade your SDK version and NuGet packages. You have to target Fall Creators update or later and reference the netcore package to 6.0+.

## when I navigate this ways `_frame.Navigate(typeof(MainPage));` I never call MainPage ctor. How to inject MainPage datacontext then?

The `DataContext` can be passed as a navigation parameter, then set as `DataContext` in the `OnNavigatedTo` method

## I'm trying to ref a net standard library into my iOS project but it says "The project(s) MyApp.Services.Interfaces did not provide any metadata reference".

It's an existing issue, caused by Roslyn. You must add all your TargetFrameworks to your netstd2.0 project, and use Oren's MSBuid.Extras for that [source generation issue](https://github.com/unoplatform/uno.SourceGeneration/issues/2)

# Can Uno.UI convert a UWP control to native Android/iOS and also has the option to use Android native controls within XAML?

That's exactly right. See [this](https://github.com/unoplatform/uno/blob/a69879a3154f61b2d493be433aa08bc3b8aa1b06/src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml#L2843) for the XAML-UWP button and [this](https://github.com/unoplatform/uno/blob/a69879a3154f61b2d493be433aa08bc3b8aa1b06/src/Uno.UI/UI/Xaml/Style/Generic/Generic.Native.xaml#L20) for the iOS native button. When complete XAML is used for rendering, it's not so much a conversion but more of  a vector rendering. For instance, Uno.UI.iOS uses CGLayer for rendering content. It's different because Uno.UI integrates within the layouting system of the platform, which allows for mixed rendering. Uno.UI supports webviews, whereas Flutter does not. Flutter also has to render everything, including what the platform provides by default, such as the Magnifier in the TextBox for accessibility (this is not yet supported either).

## Can I have a screen, use a Map and put markers, using Uno, for Android, and Windows 10?
> And can I access the camera, take a picture and Post to a REST WebAPI? can I access Photos / files in phone?
	
For now, we have internal code to do all of that, but in the meantime, you have to do this by hand on each. Parts of https://docs.microsoft.com/en-us/xamarin/essentials/ allows to do it.

## The PointerReleased event is NOT fired for the Border control on Android

There's a bug on Android where `PointerReleased` is not fired if `PointerPressed` is not handled.

Try this:
```csharp
PointerPressed += (s, e) => { e.Handled = true; };
```

## Do I need a reference of Uno.UI in UWP?

Only Wasm, Android and iOS projects need a reference to Uno.UI. Adding a reference to the Uno.UI package provides access to the
[`VisibleBoundsPadding`](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.Toolkit/VisibleBoundsPadding.md)
attached property for notched devices.

## How can I un-grey the properties view In Edit & Continue?  

You can add them using the add button. Only properties explicitly defined in the XAML will show up at first

## Unknown member 'Clicked' on element 'Button'

The event on button is called Click

## What does the `_UnoSourceGenerator` do in the build?

This is part of the [Uno.SourceGeneration package](https://github.com/unoplatform/uno.SourceGeneration), and it's used to generate code like XAML to C#.

## In the XAML file `error CS0246: The type or namespace name '[...]' could not be found (are you missing a using directive or an assembly reference?)`

`clr-namespace:` is not supported by UWP, `using:` is.

## How to persist data  with wasm? 

For now, persistence is enabled only for `ApplicationData.LocalFolder`, `ApplicationData.RoamingFolder` and `ApplicationData.SharedLocalFolder` folders. 
You can write files in those folders using both `StorageFile` and `File.IO`. 
Files that are located in other directories are going to use the in-memory filesystem of emscripten and won't be persisted.

As `ApplicationData.LocalSettings` and `ApplicationData.RoamingSettings` are persisted in their respective folders, they are also persisted.

## Any pull to refresh on listview?

Not yet. Any plans to?

## Does debugging work for ios through uno? 

Yes, debugging works for iOS and Android, as in any Xamarin native application. Debugging for WebAssembly is not supported yet.

## Does Uno.UI works with MVVM or Prism?

MVVMLight, Prism and ReactiveUI are supported, MvvmCross is coming.

## Does Uno.UI have a XAML AutoComplete?

It's partially implemented but there are parts that are closed source from Microsoft. In the meantime, make sure your Windows head is using the latest Min SDK, at which point you'll be able to use the UWP designer.

## Does intellisense work in XAML editor?
Yes, if you do not see it:
- Make sure you are targeting the latest windows SDK version.
- Choose "XAML Designer" as the default for opening your XAML files (right-click on your XAML file and then "Open With").
- Relaunch your Visual Studio solution.
- Select UWP on the top-left corner of your XAML file.

## `Program does not contain a static 'Main' method suitable for an entry point` when building the UWP project.

This means that the shared project is not referenced by the UWP head, right click references on the UWP project, shared projects, then select it there.

## Does Uno.UI support UWP's media APIs ?

Not yet, but using [XAML conditionals](https://github.com/unoplatform/uno/blob/master/doc/articles/using-uno-ui.md#supporting-multiple-platforms-in-xaml-files) and [XamarinMediaManager](https://t.co/6yQm0RVRMV), it's possible to have a similar experience.

## What apps have been developed using Uno.UI ?

Many of the application developed during the private phase of the Uno.UI Platform require credentials to be used, but here are some of the public ones:

* https://itunes.apple.com/ca/app/jean-coutu/id351461407
* https://itunes.apple.com/ca/app/my-md-mobile/id1144752656
* https://itunes.apple.com/us/app/vca-careclub/id1172429469

## Is Skia supported ?

Yes, as in any other native view integration for iOS and Android.

For WebAssembly the Uno Platform has initial support for Skia via the Uno.SkiaSharp.Views package. [See details here](https://github.com/unoplatform/uno/blob/master/doc/blog/201907-SkiaSharp-for-wasm-support.md)

## Warning `Package Uno.UI.SourceGenerationTasks was restored using `.NETFramework,Version=4.6.1`

This is only a warning that has no effect. If you really want to remove it, [add this](https://github.com/unoplatform/uno/blob/3b1b144fd6d136136b1640ca41847e35e8495b36/src/Uno.UI.Wasm.Shell/Uno.UI.Wasm.Shell.csproj#L15).

## How do I add logging to my application

You can add [logging using this](https://github.com/unoplatform/uno.Playground/blob/80322aec3d759d009f6a900bca4a07bc63ae6a62/Uno.UI.Demo.Shared/App.xaml.cs#L46)

## Are XML Serializers supported in Xamarin projects ?

Yes, make sure to use the following project definition:

```xml
<ItemGroup>
    <Reference Include="System.Runtime.Serialization" />
</ItemGroup>
```

## Does Uno offer a TreeView control?

It's in the UWP API, but [not implemented yet](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/tree-view)
We have an open Github [issue.](https://github.com/unoplatform/Uno/issues/3)

## Is there a table of all UWP controls and their features compared to what's offered under Uno?

https://github.com/unoplatform/uno/blob/master/doc/articles/supported-features.md

##  Is there an Uno template that is based on a portable class library?

No, but use the [Cross-Platform library template](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) instead to achieve a similar goal.

## Is there a Visual Studio template for Uno that incorporates the Prism library?

No, but this sample can serve as a base: https://github.com/unoplatform/uno.Prism/tree/uno/Sandbox/Windows10/HelloWorld

## I get errors when serializing Json in Uno Wasm

If you are using JSON.NET, you need [this](https://github.com/unoplatform/uno.Playground/blob/master/src/Uno.Playground.WASM/LinkerConfig.xml) 
This file is referenced in the .csproj like [that](https://github.com/unoplatform/uno.Playground/blob/master/src/Uno.Playground.WASM/Uno.Playground.WASM.csproj#L43)

## Is NavigationView supported in Uno?

Yes, Uno now supports NavigationView , see [unoplatform/uno#4](https://github.com/unoplatform/Uno/issues/4) for more

## Is there any particular reason that Uno uses a shared project? and is it possible to use a netstandard project instead?

The view layer needs to be in a shared project because it has native dependencies.
For your view models and business logic, it's fine to use a separate netstandard project.
Shared projects are also used to work around the fact that Visual Studio is not able to selectively compile a single Target Framework when building a cross-targeted library.
Using a shared project improves the build performance when debugging a single platform.

## Are there any Visual Studio project creation templates for Uno yet?

Yes. Here are the [templates](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin).

## How do I add the NuGet package if there's no Packages node for Shared Projects?

Go to 'Manage NuGet packages for solution...'
Find the ReactiveUI package
Select all your platform heads (.Wasm, .Android etc) and hit Install

## Is RichEditBox supported in Uno.Platform?

Not yet.

## Is there a way to use local css/js libraries and not those on a CDN?

you can specify a custom HTML template like [this](https://github.com/unoplatform/uno.Wasm.Bootstrap#indexhtml-content-override)

## Debugging a published NuGet package breaks the Xamarin iOS and Android debugger

This has been fixed starting from Visual Studio 15.9 Preview 3
Please see this [Developer Community thread.](https://developercommunity.visualstudio.com/content/problem/306764/debugging-a-published-nuget-package-break-the-xama.html)

## Does Uno offer an `AutoSuggestBox`?

[Yes](https://github.com/unoplatform/uno/blob/master/src/Uno.UI/UI/Xaml/Controls/AutoSuggestBox/AutoSuggestBox.cs)

## Is there a database that works on all platforms?

Yes, and you can use this [WebAssembly SQLite provider](https://github.com/unoplatform/uno.SQLitePCLRaw.Wasm)

## Are Popups/RichEditbox implemented in Uno?

No. You can use Conditional XAML to work around it: https://github.com/unoplatform/uno/blob/master/doc/articles/using-uno-ui.md#supporting-multiple-platforms-in-xaml-files

## Does Uno support all UWP animations?

We've implemented parts of the supported animations, there are others that are still stubbed.

## Visual Studio is requiring Android API v26 but I want to test on an older device.

The target API does not affect the min API. You just need to have the API day installed in your Android SDK manager. The min SDK is specified in the `AndroidManifest.xml` file.

## Is there a workaround for ScrollViewer.ScrollToVerticalOffset method, since it isn't implemented in Uno?

You can use ChangeView instead

## I am having issues running a Wasm project head

Follow the instructions on how to run the WASM head [here](https://github.com/unoplatform/uno.QuickStart#create-an-application-from-the-solution-template)
