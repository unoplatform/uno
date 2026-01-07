---
uid: Uno.Development.MigratingBeforeYouStart
---

# Migrating WinUI/UWP-only code - Checklist

Before you start migrating a WinUI/UWP-only application or library to be Uno compatible, it's a good idea to assess the degree of effort involved in getting the port fully functional. Depending on the code in question, it may be as easy as copying the files, or it may involve significant extra effort.

The key questions to ask are:

- what framework APIs is the code using?
- what third-party dependencies does the code rely upon?
- what general .NET functionality is used that may not be supported on specific platforms?

## Controls and framework APIs

The controls supported by Uno and those currently unsupported are [listed here](implemented-views.md). You can see supported non-UI APIs under the 'Features and Controls' section of the docs. Note that API coverage varies by the target platform.

Once you migrate your code to Uno, unsupported API usages will be highlighted with a warning by Uno's included Roslyn analyzer.

## Third-party dependencies

Look at the NuGet packages referenced by the code. For each package, ask:

- is it supported on each platform you wish to target?
- if not, is there an acceptable alternative?

Third-party dependencies typically fall into one of a few categories:

### Platform-independent

Fully platform-independent dependencies will typically be packaged as .NET Standard binaries. They should be compatible with any target.

### Platform-dependent

These include dependencies that may be calling platform-specific functionality (eg push notifications), or that may depend on unmanaged binaries that must be separately compiled for each platform.

You can check if there is a version of the package for your target platform by visiting the package page on nuget.org and opening the 'Dependencies' expander. Typically you'll see `UAP 10.0.x` listed as a target. If you want to target Android and iOS with Uno, you should check that `MonoAndroid` and `Xamarin.iOS` are listed as supported targets. If you want to target macOS, check that `Xamarin.Mac` is listed.

Unfortunately, it's less easy to check if a platform-dependent package supports WebAssembly or Linux. Uno builds .NETStandard binaries for these targets, but the fact that a .NETStandard version exists for any given dependency doesn't guarantee that it will function as expected on those platforms, if platform-specific APIs are involved. You'll generally have to do some additional research.

### Depends on WinUI/UWP

Libraries that depend on WinUI/UWP itself, such as the [Windows Community Toolkit](https://learn.microsoft.com/windows/communitytoolkit/), must be recompiled against Uno Platform in order to be used. A number of popular WinUI/UWP libraries have already been retargeted to Uno; a partial list is given in the [Uno features section](https://github.com/unoplatform/Uno#uno-features).

## .NET runtime features

On certain target platforms, support for some .NET functionality is limited or unavailable.

### iOS

.NET code must be Ahead-Of-Time (AOT) compiled to run on iOS, as a fundamental platform limitation. As a result, certain APIs that require runtime code generation (eg `System.Reflection.Emit`) will not work. This includes code that uses the `dynamic` keyword. See the [Xamarin.iOS documentation](https://learn.microsoft.com/xamarin/ios/internals/limitations) for more details.

### WASM

Currently, WebAssembly code in the browser executes on a single thread (much like JavaScript code in the browser). This limitation is expected to be lifted in the future, but for now, code that expects additional threads to be available may not function as expected.

[This issue](https://github.com/unoplatform/uno/issues/2302) tracks support for multi-threading on WebAssembly in Uno Platform.
