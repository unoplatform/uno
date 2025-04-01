---
uid: Uno.Contributing.XamlResourceTrimming
---

# XAML Resource Trimming

This document provides technical details about the [XAML Resource trimming phase](xref:Uno.Features.ResourcesTrimming).

## Technical Details

In Uno, XAML is generating C# code in order to speed up the creation of the UI. This allows for compile-time optimizations, such as type conversions or `x:Bind` integration.

The drawback of this approach is that code may be bundled with an app even if it's not used. In common use cases, the IL Linker is able to remove code not referenced, but in the case of XAML resources, this code is conditionally referenced through string cases only. This makes it impossible for the linker to remove that code through out-of-the-box means.

In order to determine what is actually used by the application, the Uno Platform tooling runs a sequence of IL Linker passes and substitutions.

In order to prepare the linking pass:

- The tooling determines the presence of the `UnoXamlResourcesTrimming` msbuild property
- During the source generation, the tooling generates a `__LinkerHints` class, which contains a set of properties for all `DependencyObject` inheriting classes.
- The source generation creates XAML Resources and Bindable Metadata code that conditionally uses those classes behind `LinkerHints` properties.
- The tooling also embeds an ILLinker substitution file allowing the linker to unconditionally remove the code that conditionally references those properties. For instance, for `LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available`, any block of `if (LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available)` will be removed when the `--feature Is_Windows_UI_Xaml_Controls_Border_Available false` parameter is provided to the linker.

Then the multiple passes of IL Linker are done:

- The first pass runs the IL Linker with all XAML resources and Binding Metadata disabled by setting all `__LinkerHints` properties to false. This removes all code directly associated to those Bindable Metadata and XAML Resources. This has the effect of only keeping framework code which is directly referenced from user code.
- The tooling then reads the result of the linker to determine which types in the `LinkerHints` are still available in the assemblies.
- The subsequent passes run the IL Linker with `__LinkerHints` enabled only for types detected to be used during the first pass. This will enable types indirectly referenced by XAML Resources (e.g. a ScrollBar inside a ScrollViewer style) to be kept by the linker.
- The tooling then reads again the result of the linker to determine which types in the `__LinkerHints` are still available in the assemblies.
- The tooling re-runs this last pass until the available types list stops changing.

The resulting `__LinkerHints` types are now passed as features to the final linker pass (the one bundled with the .NET tooling) to generate the final binary, containing only the used types.

As of Uno 3.9, the Uno.UI WebAssembly assembly is 7.5MB, trimmed down to 3.1MB for a `dotnet new unoapp` template.

## Using the `AdditionalLinkerHintAttribute` attribute

In some scenarios (e.g. [ExpandoObject](https://learn.microsoft.com/en-us/dotnet/api/system.dynamic.expandoobject?view=net-9.0)), it may be needed to generate linker hints for non-DependencyObject types.

To get a `__LinkerHints` property for an additional type, add the following in AssemblyInfo.cs:

```csharp
[assembly: AdditionalLinkerHint("System.Dynamic.ExpandoObject")]
```

Then make the code using `ExpandoObject` conditional:

```csharp
if (__LinkerHints.Is_System_Dynamic_ExpandoObject_Available
 && type == typeof(System.Dynamic.ExpandoObject))
{
    ...
}
```

The linker will automatically remove the rest of the conditional block if the linker hint is considered false, based on the use of `ExpandoObject` in the rest of the app (except the current assembly).
