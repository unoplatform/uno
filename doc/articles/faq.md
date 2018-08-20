About Uno platform
**What is UNO Platform?**
The Uno Platform is a Universal Windows Platform Bridge to allow UWP based code to run on iOS, Android, and WebAssembly. It provides the full API definitions of the UWP Spring Creators Update (17134), and the implementation of parts of the UWP API, such as Windows.UI.Xaml, to enable applications to run on these platforms.
This allows the use the UWP tooling from Windows in Visual Studio, such as Xaml Edit and Continue and C# Edit and Continue, to build an application as much as possible on Windows, then validate that the application runs on iOS, Android and WebAssembly.
The XAML User Interface (UI) provides the ability to display the same XAML files on Windows, iOS, Android and WebAssembly platforms. Uno also provides support for the MVVM pattern on all platforms, with binding, styling, control and data-templating features.
As the Uno Platform provides all of the APIs of the complete UWP platform, any UWP library can be compiled on top of Uno (e.g. XamlBehaviors), with the ability to determine which APIs are implemented or not via the IDE using C# Analyzers.

**What does UNO Platform do?**
For users?
For developers?
For designers?

**Why Uno ?**
Developing for Windows (phone, desktop, tablet, XBox), iOS (tablet and phone), Android (tablet and Phone) and WebAssembly at once can be a complex process, especially when it comes to the user interface. Each platform has its own ways of defining dynamic layouts, with some being more efficient, some more verbose, some more elegant, and some more performant than others.
Yet, being able to master all these frameworks at once is a particularly difficult task, because of the amount of platform-specific knowledge required to master each platform. Most of the time it boils down to different teams developing the same application multiple times, with each requiring a full development cycle.
With Xamarin, C# comes to all these platforms; however, it only provides transparent translations of the UI frameworks available for iOS and Android. Most non-UI code can be shared, but when it comes to the UI, almost nothing can be shared.
To avoid having to learn the UI-layout techniques and approaches for each platform, Uno.UI mimics the Windows XAML approach of defining UI and layouts. This translates into the ability to share styles, layouts, and data-bindings while retaining the ability to mix XAML-style and native layouts. For instance, a StackPanel can easily contain a RelativeLayout on Android, or a MKMapView on iOS.
Uno.UI provides the ability for developers to reuse known layout and coding techniques on all platforms, resulting in a gain of overall productivity when creating UI-rich applications.


**Who makes UNO platform?**
Uno Platform was created by Team Nventive, add more stuff here

-What makes UNO Platform unique?

-Where does the name Uno Platform come from? 

-Is Uno Platform open source?

 **Getting started**
**How can I try Uno platform?**
Playground, quick start

**How experienced do I need to be to use Uno Platform?**


**What is the playground?**

**Where can I get support?**

**How can I get involved?**
There are lots of ways to contribute to the Uno Platform and we appreciate the help from the community. You can provide feedback, report bugs, give suggestions, contribute code, and participate in the platform discussions.

**How can I report a bug?**
-If you think you've found a bug, please log a new issue in the Uno Platform GitHub issue tracker. When filing issues, please use our bug filing template. The best way to get your bug fixed is to be as detailed as you can be about the problem. Providing a minimal project with steps to reproduce the problem is ideal. Here are questions you can answer before you file a bug to make sure you're not missing any important information.
-Did you read the documentation?
-Did you include the snippet of broken code in the issue?
What are the EXACT steps to reproduce this problem?
What specific version or build are you using?
What operating system are you using?
GitHub supports markdown, so when filing bugs make sure you check the formatting before clicking submit.

**What is UWP?** 
Universal Windows Platform (UWP) is an API created by Microsoft and first introduced in Windows 10. The purpose of this platform is to help develop universal apps that run on Windows 10, Windows 10 Mobile, Xbox One and HoloLens without the need to be re-written for each. It supports Windows app development using C++, C#, VB.NET, and XAML. The API is implemented in C++, and supported in C++, VB.NET, C#, F# and JavaScript. Designed as an extension to the Windows Runtime platform first introduced in Windows Server 2012 and Windows 8, UWP allows developers to create apps that will potentially run on multiple types of devices.

Visit Microsoft's documentation for a primer on UWP : https://docs.microsoft.com/en-us/windows/uwp/get-started/

**Technology**
**How is Uno platform different from Xamarin forms?**

**How does my Uno platform code run on Android?**

**How does my Uno platform code run on iOS?**

**How does my Uno platform code run on the web?**

**Is WebAssembly supported in all browers?**

**Where can I deploy my Uno Platform app?**

**Does Uno.UI support macOS?**
Not yet. We're still waiting for Apple's Marzipan cross macOS-iOS APIs to become available. We’ll see from there. 

**Does Uno.UI support Linux ?**
No, not at the moment. Our assumption for now is that the WebAssembly part of Uno.UI will be efficient enough to be a viable alternative. If you find that native support still is a viable scenario, please open a GitHub issue.

## Does Uno.UI support wpf CustomControls 

Yes, those are called Templated Controls, because they inherit from Control Uno.UI currently handles styles a bit differently from what WPF/UWP is doing and Uno.UI parser does not handle `<Style.Setters>` properly. These should not be impacting as long as you have a resource dictionary file containing your style. See here: https://github.com/nventive/Uno/blob/master/doc/articles/api-differences.md#styles

## Is the iPhone X supported by Uno.UI ?

Yes, use the [VisibleBoundsPadding](https://github.com/nventive/Uno/blob/master/src/Uno.UI.Toolkit/VisibleBoundsPadding.md)
behavior to manage the _notch_ correctly.



# Features

**What features will UNO Platform support?**

**Why XAML? What is it, how does it work?**

**What is the difference between the two types of XAML?**

**Why Mono?**

**What do you mean by #UWPeverywhere?**

**What is a bridge?**

**Why .NET?**
.NET provides a consistent, first-class development experience across the desktop, Windows Phone, and Windows Store. It offers a higher level of coding and productivity. .NET allows you to keep developing foundational apps on the desktop and add exciting new experiences, all while using your existing skills and reusing code between devices. If you are looking to create more tailored, platform-appropriate experiences on any device, Visual Studio Industry Partners (VSIP) provide solutions that enable re-using C# skills and code with non-Windows devices.
In addition to features you expect such as files/streams and network communication, here are a few highlights of what .NET offers to make building applications very productive:
Runtime productivity and safety such as automatic memory management, type safety, exception handling, and thread management
GUI technologies see below
Rich collection data types
Data modeling (ADO, LINQ, WCF data services)
Language Integrated Query (LINQ)
Date and time library
Serialization
Web services
Security and cryptography
Parallel programming library
https://stackify.com/net-core-csharp-next-programming-language/


**Why build cross platform apps on the Microsoft stack?**

**How can you run .NET in a web browser?**

**Will UNO platform make my app look and run the same way on iOS and Android?** 

**What are the advantages of using Uno platform over Flutter/React Native/Xamarin Forms?**

**Does Uno.UI support a single .NET Standard 2.0 binary model?**
Not at the moment. iOS and Android platform support relies on the underlying APIs being visible through class hierarchy for performance reasons. Also, the .NET Standard model is based on binary sharing, which makes very difficult to use platform features without jumping through hoops such as Dependency Injection, Inversion of Control or reflection.

**Does Uno.UI support having controls in a class library?**
Yes, here's a project sample.  https://github.com/nventive/Uno.Samples/tree/master/UI/ControlLibrary





# Uno.UI Platorms Frequently Asked Questions

## How do I update the to latest Uno.UI nuget package, I don't see any update ?

You may want to try our latest dev builds, and here's how to do it:

- On your solution node, select **Manage Nuget packages for Solution**
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
Yes! The current performance are limited to the runtime interpreted mode of mono-wasm. The Mono is working on implementing **AOT** compilation,
which will improve significantly the performance. See [Miguel de Icaza's status update](https://gitter.im/aspnet/Blazor?at=5b1ab670dd54362753f8a168) for more details.

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

For live update on UWP you need to edit the xaml while the project is debugging, no need for save, it updates on every keystroke (more or less) you need to update to the latest UWP sdk in your project configuration, change target version to latest


## I don't see any "Uno.UI App Solution" from File->Project->New ?

1. Install **Uno.UI Solution Template Visual Studio Extension** https://github.com/nventive/Uno/releases
2. Look for Uno.UI App Solution under Visual C#
3. if you still havent found it, try and use the Search box


## How to port an existing UWP app to Uno.UI?

First create a shared project and move all .cs and .xaml files into it and reference the project in your UWP head
project.  Ensure everything is still working and add other projects for other platforms referencing the same shared project.

1. Create a project with Uno.UI template.
2. Copying as much code as possible from the existing UWP app to the "My Project.Shared"
3. Add platform specific code using suffixing files in the shared project (ex: ".iOS.cs")
4. ios/android specific heads should be relatively empty, only used os specific implementation (push notification handling/deeplinking/permissions)
5. Test, debug and publish.

## How to port an existing UWP library to Uno.UI ? 

This is essentially the same process as porting an app(add steps anyways), but using cross-targeted projects. Use
[this project](https://github.com/nventive/Uno.Samples/blob/master/UI/ControlLibrary/XamlControlLibrary/XamlControlLibrary.csproj)
as a base for your cross-targeted library.

# Is it possible to make http web requests using the WASM target?

Yes it is possible, but you need to use the provided HttpHandler by Uno.UI like what we did in the Uno.UI Playground:

```csharp
var handler = new Wasm.WasmHttpHandler();
var httpClient = new HttpClient(handler);
```

## How do you choose which APIs are being implemented ?

- If the API is present in .NET Standard, it is generally suggested to use this one instead of the UWP one. (e.g System.IO or System.Net.HttpClient)Missing APIs will be implemented based on the popularity of suggestions in the [Uno.UI issues list](https://github.com/nventive/Uno/issues). Make sure
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
- See the [Playground for an actual use example](https://github.com/nventive/Uno.Playground/tree/master/Uno.UI.Demo.iOS/Resources/Fonts).
- See the documentation for adding fonts for [iOS](https://github.com/nventive/Uno/blob/master/doc/articles/using-uno-ui.md#custom-fonts-on-ios),  [Android](https://github.com/nventive/Uno/blob/master/doc/articles/using-uno-ui.md#custom-fonts-on-android) and [WebAssembly](https://github.com/nventive/Uno.Playground/blob/80322aec3d759d009f6a900bca4a07bc63ae6a62/Uno.UI.Demo.WASM/Uno.UI.Demo.WASM.csproj#L29).

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

Make sure to upgrade your SDK version and nuget packages. You have to target Fall Creators update or later and reference the netcore package to 6.0+.

## when I navigate this ways `_frame.Navigate(typeof(MainPage));` I never call MainPage ctor. How to inject MainPage datacontext then?

The `DataContext` can be passed as a navigation parameter, then set as `DataContext` in the `OnNavigatedTo` method

## I'm trying to ref a net standard library into my iOS project but it says "The project(s) MyApp.Services.Interfaces did not provide any metadata reference".

It's an existing issue, caused by Roslyn. You must add all your TargetFrameworks to your netstd2.0 project, and use Oren's MSBuid.Extras for that [source generation issue](https://github.com/nventive/Uno.SourceGeneration/issues/2)

# Can Uno.UI convert a UWP control to native android/ios and also has the option to use android native controls within xaml?

That's exactly right, see [this](https://github.com/nventive/Uno/blob/a69879a3154f61b2d493be433aa08bc3b8aa1b06/src/Uno.UI/UI/Xaml/Style/Generic/Generic.xaml#L2843) for the XAML-UWP button, and [this](https://github.com/nventive/Uno/blob/a69879a3154f61b2d493be433aa08bc3b8aa1b06/src/Uno.UI/UI/Xaml/Style/Generic/Generic.Native.xaml#L20) for the iOS native button When complete XAML is used for rendering, it's not so much a conversion but more of  a vector rendering. For instance, Uno.UI.iOS uses CGLayer for rendering content it's different because Uno.UI integrates within the layouting system of the platform, which allows for mixed rendering. Uno.UI supports webviews, whereas Flutter does not. Flutter also has to render everything, including what the platform provides by default, such as the Magnifier in the TextBox for accessibility (this is not yet supported either).

## Can I have a screen, use a Map and put markers, using UNO, for Android, and Windows 10?
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
[`VisibleBoundsPadding`](https://github.com/nventive/Uno/blob/master/src/Uno.UI.Toolkit/VisibleBoundsPadding.md)
attached property for notch'ed devices.

## How can I un-grey the properties view In Edit & Continue?  

You can add them using the add button. Only properties explicitly defined in the xaml will show up at first

## Unknown member 'Clicked' on element 'Button'

The event on button is called Click

## What does the `_UnoSourceGenerator` do in the build?

This is part of the [Uno.SourceGeneration package](https://github.com/nventive/Uno.SourceGeneration), and it's used to generate code like XAML to C#.

## In the XAML file `error CS0246: The type or namespace name '[...]' could not be found (are you missing a using directive or an assembly reference?)`

`clr-namespace:` is not supported by UWP, `using:` is.

## How to persist data  with wasm? 

For now, persistence is not yet configured, it's using the in-memory filesystem of emscripten, so this has to be worked on. For now, you can use standard File IO, but it won't persist yet

## Any pull to refresh on listview?

Not yet. Any plans to?

## Does debugging work for ios through uno? 

Yes, debugging works for iOS and Android, as in any Xamarin native application. Debugging for WebAssembly is not supported yet.

## Does Uno.UI works with MVVM or Prism?

MVVMLight, Prism and ReactiveUI are supported, MvvmCross is coming.

## Does Uno.UI have an XAML AutoComplete?

It's partially implemented but there are parts that are closed source from Microsoft. In the meantime, make sure your Windows head is using the latest Min SDK, at which point you'll be able to use the UWP designer.

## Does intellisense work in XAML editor?
Yes, if you do not see it; make sure you are targetting the latest windows SDK version

## `Program does not contain a static 'Main' method suitable for an entry point` when building the UWP project.

This means that the shared project is not referenced by the UWP head, right click references on the UWP project, shared projects, then select it there.

## Does Uno.UI support UWP's media APIs ?

Not yet, but using [XAML conditionals](https://github.com/nventive/Uno/blob/master/doc/articles/using-uno-ui.md#supporting-multiple-platforms-in-xaml-files) and [XamarinMediaManager](https://t.co/6yQm0RVRMV), it's possible to have a similar experience.

## What apps have been developed using Uno.UI ?

Many of the application developed during the private phase of the Uno.UI Platform require credentials to be used, but here are some of the public ones:

* https://itunes.apple.com/ca/app/jean-coutu/id351461407
* https://itunes.apple.com/ca/app/my-md-mobile/id1144752656
* https://itunes.apple.com/us/app/vca-careclub/id1172429469

## Is Skia supported ?

Yes, as in any other native view integration for iOS and Android. It is not yet supported for WebAssembly.

## Warning `Package Uno.UI.SourceGenerationTasks was restored using `.NETFramework,Version=4.6.1`

This is only a warning that has no effect. If you really want to remove it, [add this](https://github.com/nventive/Uno/blob/3b1b144fd6d136136b1640ca41847e35e8495b36/src/Uno.UI.Wasm.Shell/Uno.UI.Wasm.Shell.csproj#L15).

## How do I add logging to my application

You can add [logging using this](https://github.com/nventive/Uno.Playground/blob/80322aec3d759d009f6a900bca4a07bc63ae6a62/Uno.UI.Demo.Shared/App.xaml.cs#L46)

## Are XML Serializers supported in Xamarin projects ?

Yes, make sure to use the following project definition:

```xml
<ItemGroup>
    <Reference Include="System.Runtime.Serialization" />
</ItemGroup>
```
