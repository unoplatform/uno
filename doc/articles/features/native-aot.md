---
uid: Uno.Features.NativeAOT
---

# Native AOT Support

Uno Platform 6.6 *introduces* support for [.NET Native AOT deployment](https://learn.microsoft.com/dotnet/core/deploying/native-aot) across Android, iOS, Linux, macOS, and Windows. Note that Native AOT support *itself* is experimental on Android.

Enabling Native AOT enables faster app startup and improves performance, typically at the cost of larger app sizes:

| **Sample**                                            | **Environment**   | **Runtime** |          **Publish Size** |     **Startup Times (s)** |
| ----------------------------------------------------- | ----------------- | ----------- | ------------------------: | ------------------------: |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Android, .NET 10  | MonoVM      |   94M                     | 3.711s |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Android, .NET 10  | NativeAOT   |  112M <br> (119% MonoVM)  | 2.586s <br> (70% MonoVM)  |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | iOS, .NET 10      | MonoVM      |  138M                     | 2.333s |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | iOS, .NET 10      | NativeAOT   |  122M <br> (88% MonoVM)   | 1.107s <br> (47% MonoVM)  |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Linux, .NET 10    | CoreCLR     |  451M                     | 3.57s |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Linux, .NET 10    | NativeAOT   |  534M <br> (118% CoreCLR) | 0.78s <br> (22% CoreCLR)  |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | macOS, .NET 10    | CoreCLR     |  458M                     | 3.297s |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | macOS, .NET 10    | NativeAOT   |  645M <br> (141% CoreCLR) | 1.436s <br> (44% CoreCLR) |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Windows, .NET 10  | CoreCLR     |  625M                     | 3.169s |
| [Uno.Chefs][uno-chefs] with Uno.Sdk 6.6.0-dev.168     | Windows, .NET 10  | NativeAOT   |  871M <br> (139% CoreCLR) | 1.122s <br> (35% CoreCLR) |

[!NOTE]
App startup times are provided for comparison purposes.  Actual startup times will vary depending on hardware.

[!NOTE]
.NET support for Native AOT on Android is still experimental.

## Prerequisites

Please see the [.NET SDK Prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites) documentation.

Additionally, some platforms have additional prerequisites.

### [**Android**](#tab/prereqs-Android)

Publishing Android apps with Native AOT requires NDK r27 or later, in addition to the normal .NET for Android SDK requirements.

## Publish Native AOT using the CLI

Publishing apps mirrors the [.NET documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/#publish-native-aot-using-the-cli),
and requires setting the `$(PublishAot)` MSBuild property within `App.csproj` to true:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

It is also recommended to set the `$(IsAotCompatible)` MSBuild property in projects, which enables additional trimmer warnings:

```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

Producing the Native AOT package requires a `dotnet publish` invocation that provides Runtime Identifier (`-r`) option, and if the project is multi-targeted, then the Target Framework (`-f`) option is also required:

```dotnetcli
dotnet publish -f net10.0-ios -r ios-arm64 App.csproj
```

*During testing*, it is often desirable to change `$(PublishAot)` on a per-build basis, so that NativeAOT and non-NativeAOT packages can be produced without editing the `.csproj` file.  Use an *intermediate* MSBuild property which sets `$(PublishAot)` in only App projects:

```xml
<PropertyGroup>
  <PublishAot Condition=" '$(PublishAot)' == '' And '$(TestPublishAot)' != '' ">$(TestPublishAot)</PublishAot>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

This enables:

```dotnetcli
dotnet publish -f net10.0-ios -r ios-arm64 -p:TestPublishAot=true App.csproj
```

The intermediate MSBuild property is useful when you have an `App.csproj` which contains a project reference to a netstandard2.0 project, as using `dotnet publish -p:PublishAot=true App.csproj …` will generate build failures:

```text
…/Microsoft.NET.Sdk.FrameworkReferenceResolution.targets(120,5): error NETSDK1207: Ahead-of-time compilation is not supported for the target framework.
```

## Uno Platform Feature Support

[Uno Platform Features](xref:Uno.Development.SupportedFeatures) work with Native AOT, but some [adaptations](#adaptations) may be required.

### [Animations](xref:Uno.Development.SupportedFeatures#animations)

Animations on Attached Properties may require [reflection adaptations](#xaml-attached-properties).

Binding expressions to Attached properties (Storyboard setters) may require [reflection adaptations](#xaml-attached-properties).

### [Styling](xref:Uno.Development.SupportedFeatures#styling)

Attached Property Style binding may require [reflection adaptations](#xaml-attached-properties).

### [Data Binding](xref:Uno.Development.SupportedFeatures#data-binding)

Attached Properties binding may require [reflection adaptations](#xaml-attached-properties).

<!--
### [Design Fidelity](xref:Uno.Development.SupportedFeatures#design-fidelity)

### [Responsive Design](xref:Uno.Development.SupportedFeatures#responsive-design)
-->

### [Runtime Performance](xref:Uno.Development.SupportedFeatures#runtime-performance)

Expando Binding and DynamicObject Binding likely will not work under Native AOT, as it involves `dynamic`.

<!--
### [ListView](xref:Uno.Development.SupportedFeatures#listview)

### [Command Bar](xref:Uno.Development.SupportedFeatures#command-bar)

-->

### [Others](xref:Uno.Development.SupportedFeatures#others)

AttachedProperty Binding and AttachedProperty Styling may require [reflection adaptations](#xaml-attached-properties).

<!--
### [Media](xref:Uno.Development.SupportedFeatures#media)
-->

## Limitations of Native AOT deployment

Please refer to the [Limitations of Native AOT deployment documentation](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#limitations-of-native-aot-deployment).  In particular:

> * No dynamic loading, for example, `Assembly.LoadFile`.
> * No runtime code generation, for example, `System.Reflection.Emit`.
> * Requires trimming, which has [limitations](https://learn.microsoft.com/dotnet/core/deploying/trimming/incompatibilities).
> * Implies compilation into a single file, which has known [incompatibilities](https://learn.microsoft.com/dotnet/core/deploying/single-file/overview#api-incompatibility).

Some System.Reflection facilities may not work either, such as
[`Type.MakeGenericType()`](https://learn.microsoft.com/dotnet/api/system.type.makegenerictype?view=net-10.0)
and [`MethodInfo.MakeGenericMethod()`](https://learn.microsoft.com/dotnet/api/system.reflection.methodinfo.makegenericmethod?view=net-10.0).

Many Uno Platform features such as XAML Binding expressions may rely upon System.Reflection, often in "fallback" code paths.
Native AOT supports System.Reflection, in that methods such as `object.GetType()` and `Type.GetProperties()` work.
*However*, Native AOT is a [*trimming*](https://learn.microsoft.com/dotnet/core/deploying/trimming/trim-self-contained) environment, meaning that types and properties (and more!) may be *removed* as part of the build process.

Consequently, when a Uno Platform app is built for Native AOT and runs, it is possible that types and members that are required are not present, which often presents as UI bugs such as missing content. The easiest way to obtain actionable information about missing types and members is via [log messages](#log-everything).

<a name="log-everything"></a>

## Log Everything

Broadly speaking, Uno Platform apps write diagnostic log messages via two *separate* mechanisms:

1. via an `ILogger` instance obtained from a `.Log()` extension method; see also [Logging](xref:Uno.Development.Logging)
2. via an `ILogger` instance obtained via Dependency Injection, [when using Uno.Extensions](xref:Uno.Extensions.Logging.Overview)

You need to see messages from *both* sources in order to track down, understand, and fix runtime errors.  This may require changes to your App startup code: if you have an `App.InitializeLogging()` method, then:

1. Ensure that it is called from your relevant `Main()` or startup code, and
2. Ensure it emits output in Release configuration builds, at least during testing.  Native AOT is only enabled in Release configuration builds.

If your app does *not* have an `App.InitializeLogging()` method, then *add one* and call it from your startup code.  See also [unoplatform/uno.extensions#3008](https://github.com/unoplatform/uno.extensions/issues/3008).

Messages will be written to your app's console output.

### [**Android**](#tab/log-Android)

Messages will be written to `adb logcat`.

### [**iOS**](#tab/log-iOS)

Use **Console.app** to view messages from your app.

### [**Linux**](#tab/log-Linux)

Launch the app from within a shell:

```sh
% path/to/your/App
```

Messages are written to stdout.

### [**macOS**](#tab/log-macOS)

Launch the app from within **Terminal.app**:

```sh
% path/to/your/App
```

Messages are written to stdout.

### [**Windows**](#tab/log-Windows)

Launch the app within PowerShell, and pipe output to `Out-Default`:

```PowerShell
Path\To\Your\App.exe | Out-Default
```

Messages are written to stdout.

## Reflection metadata

Native AOT compiles managed code into a single native binary. In order to support System.Reflection, a Reflection metadata "database" is generated at build time. To gain insight into what is *retained* from the trimming process and what is kept in Reflection metadata, add the following options to the `dotnet publish` command:

While Native AOT supports System.Reflection, there are a number of *constraints*, and XAML often uses System.Reflection in "fallback" codepath.  Consequently, apps may not work properly when running in a Native AOT environment.

* `-p:TrimmerSingleWarn=false -p:_ExtraTrimmerArgs=--verbose`: Show all warning messages from the trimmer during the build. Often times you will see "Assembly X has trimmer warnings" with no specifics; these options show all the warnings.
* `-p:IlcGenerateMetadataLog=true`: Emit `$(MSBuildProjectName).metadata.csv` within `$(IntermediateOutputPath)`. This allows viewing the "reflection metadata" that will be accessible to the app at runtime.
* `-p:IlcGenerateMstatFile=true`: Emit `$(MSBuildProjectName).mstat` within `$(IntermediateOutputPath)`. This is an assembly that references all types, fields, non-inlined methods, etc., which make up the resulting native binary.
* `-p:EmitCompilerGeneratedFiles=true "-p:CompilerGeneratedFilesOutputPath=PATH"`: Emit C# source generator output into PATH. It is frequently useful to separately review and search generated output.

The `.mstat` file produced by `-p:IlcGenerateMstatFile=true` is useful as it reference types and members retained *after* trimming. It can be disassembled with ildasm.  (*Note*: it contains type and member *references*. I find using the old Mono SDK `monodis` utility to be helpful, such as `monodis --typeref App.mstat` to view all referenced types.)

*However*, just because the `.mstat` file references a type or property, that does *not* mean that the member is available via Reflection. Only members listed in the `.metadata.csv` file are accessible via Reflection.

There are three ways to *add* types and members to Reflection metadata:

1. [Linker Descriptor XML](https://github.com/dotnet/runtime/blob/main/docs/tools/illink/data-formats.md#descriptor-format) via the
    [`@(TrimmerRootDescriptor)` item group](https://learn.microsoft.com/dotnet/maui/android/linking#preserve-assemblies-types-and-members) and other locations.
2. The [`DynamicallyAccessedMembersAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.dynamicallyaccessedmembersattribute?view=net-10.0) custom attribute
3. The [`DynamicDependencyAttribute`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.dynamicdependencyattribute?view=net-10.0) custom attribute.

<a name="adaptations"></a>

## Adapt an app to Native AOT deployment

An app may require changes in order to run under Native AOT. (It is also possible that Native AOT cannot be used at all, depending on the app dependencies.)

See the [Fixing trim warnings](https://learn.microsoft.com/dotnet/core/deploying/trimming/fixing-warnings) Microsoft documentation.

Additional common problems and their workarounds follow.

### `Type.GetType()`

[`Type.GetType()`](https://learn.microsoft.com/dotnet/api/system.type.gettype?view=net-10.0) only works reliably when given a *string constant*:

```csharp
var t = Type.GetType("System.Int32, System.Runtime");
```

Any other use may mean that the type isn't available at runtime, resulting in an exception at runtime.

*If* `Type.GetType()` must be used, *always use a string constant* containing an [assembly-qualified name](https://learn.microsoft.com/en-us/dotnet/api/system.type.assemblyqualifiedname?view=net-10.0#remarks).

### JSON serialization

Many apps (and templates) rely on reflection-based serialization behavior (for example, `System.Text.Json` default runtime contract discovery). Under Native AOT, this can fail because required members and metadata can be removed.

Prefer [System.Text.Json source generators](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation) for DTOs and commonly serialized types.

When using [Uno.Extensions.Serialization](xref:Uno.Extensions.Serialization.Overview), use the `.AddJsonTypeInfo()` extension method to register the Json Source Generator output:

```csharp
using System.Text.Json.Serialization;

public record Person(string name, int age, double height, double weight);

[JsonSerializable(typeof(Person))]
public partial class PersonJsonContext : JsonSerializerContext
{
}

partial class App
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var appBuilder = this.CreateBuilder(args)
            .Configure(host => {
                host
                .UseSerialization(services =>
                {
                    services
                        .AddJsonTypeInfo(PersonJsonContext.Default);
                });
            });
        …
    }
}
```

### Properties

Properties may be referenced via XAML Binding Expressions, and if not preserved by Native AOT then using them will result in messages such as:

```text
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ListenButtonContent] property getter does not exist on type [Uno.Gallery.Views.Samples.ClipboardSamplePageViewModel]
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [Message] property getter does not exist on type [Uno.Gallery.Views.Samples.ClipboardSamplePageViewModel]
```

There are two general ways to fix this.

The easiest way is to add `Microsoft.UI.Xaml.Data.BindableAttribute` to the declaring type:

```csharp
[Microsoft.UI.Xaml.Data.Bindable]
partial class ClipboardSamplePageViewModel {
}
```

However, this will not work if the type has `init`-only properties *and also* WinRT support is required, as this will hit [microsoft/microsoft-ui-xaml#8723](https://github.com/microsoft/microsoft-ui-xaml/issues/8723).

If WinRT support is required, then `[DynamicDependency]` must be used:

```csharp
partial class ClipboardSamplePageViewModel {
    [DynamicDependency(nameof(ListenButtonContent))]
    [DynamicDependency(nameof(Message))]
    public ClipboardSamplePageViewModel() {…}
}
```

### XAML Attached Properties

XAML binding can involve Reflection, which can include use of attached properties.  If Reflection Metadata doesn't include a required attached property, then the app log will contain messages such as:

```text
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ShowMeTheXAML:XamlDisplayExtensions.Header] property getter does not exist on type [ShowMeTheXAML.XamlDisplay]
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ShowMeTheXAML:XamlDisplayExtensions.Description] property getter does not exist on type [ShowMeTheXAML.XamlDisplay]
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ShowMeTheXAML:XamlDisplayExtensions.Options] property getter does not exist on type [ShowMeTheXAML.XamlDisplay]
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ShowMeTheXAML:XamlDisplayExtensions.PrettyXaml] property getter does not exist on type [ShowMeTheXAML.XamlDisplay]
fail: Uno.UI.DataBinding.BindingPropertyHelper[0]
      The [ShowMeTheXAML:XamlDisplayExtensions.ShowXaml] property getter does not exist on type [ShowMeTheXAML.XamlDisplay]
```

Fix this by using `[DynamicDependency]` on the attached property so that the associated `Get` and `Set` methods are preserved:

```diff
diff --git a/Uno.Gallery/Extensions/XamlDisplayExtensions.cs b/Uno.Gallery/Extensions/XamlDisplayExtensions.cs
index 764886bb..f146a16f 100644
--- a/Uno.Gallery/Extensions/XamlDisplayExtensions.cs
+++ b/Uno.Gallery/Extensions/XamlDisplayExtensions.cs
@@ -1,6 +1,7 @@
 ﻿#pragma warning disable
 using System;
 using System.Collections.Generic;
+using System.Diagnostics.CodeAnalysis;
 using System.IO;
 using System.Linq;
 using System.Text;
@@ -28,7 +29,12 @@ namespace ShowMeTheXAML
 
                #region Property: Header
 
-               public static DependencyProperty HeaderProperty { get; } = DependencyProperty.RegisterAttached(
+               public static DependencyProperty HeaderProperty
+               {
+                       [DynamicDependency(nameof(GetHeader))]
+                       [DynamicDependency(nameof(SetHeader))]
+                       get;
+               } = DependencyProperty.RegisterAttached(
                        "Header",
                        typeof(string),
                        typeof(XamlDisplayExtensions),
```

*All* use of `DependencyProperty.RegisterAttached()` is potentially suspect and should be reviewed.

[uno-chefs]: https://github.com/unoplatform/uno.chefs/commit/d54bceea
