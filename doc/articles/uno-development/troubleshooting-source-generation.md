---
uid: Uno.Contributing.SourceGeneration
---

# Troubleshooting Source Generation

Before Uno 5.0, Source Generation in Uno was done in one of two ways:

- Using [C# 9.0 source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/)
- Using [Uno.SourceGeneration](https://github.com/unoplatform/uno.sourcegeneration) generators

C# 9.0 generators are enabled automatically when using .NET 5 and up projects, otherwise Uno.SourceGeneration generators are used.

It is possible to enable C# 9.0 generators for non .NET 5+ projects by adding this:

```xml
<PropertyGroup>
  <LangVersion>9.0</LangVersion>
  <UnoUIUseRoslynSourceGenerators>true</UnoUIUseRoslynSourceGenerators>
</PropertyGroup>
```

Conversely, Uno.SourceGeneration generators can be used by setting this:

```xml
<PropertyGroup>
  <UnoUIUseRoslynSourceGenerators>false</UnoUIUseRoslynSourceGenerators>
</PropertyGroup>
```

Starting with Uno 5.0, the only way for source generation is using Roslyn source generators, and setting `UnoUIUseRoslynSourceGenerators` has no effect.

## Adding generated files C# pragma

In some cases, the generated code may use patterns that cause C# to raise warnings, and in order to silence those warnings, the generated code can contain a set of custom C# pragma.

The MSBuild `XamlGeneratorAnalyzerSuppressions` item is used to configure suppressions, with the format `CATEGORY-CODE_TEXT`:

- If `CATEGORY` is `csharp`, the generator will create a `#pragma warning CODE_TEXT` entry at the top of the generated files
- If `CATEGORY` is anything else, `SuppressMessageAttribute` attributes will be applied on generated classes.

To define a pragma, in your csproj add the following:

```xml
<ItemGroup>
 <XamlGeneratorAnalyzerSuppressions Include="csharp-618 // Ignore obsolete members warnings" />
</ItemGroup>
```

It is also possible to have `SuppressMessageAttribute` specified in the code, by using the following syntax:

```xml
<ItemGroup>
    <XamlGeneratorAnalyzerSuppressions Include="mycategory.subcategory-CAT0042" />
</ItemGroup>
```

## Debugging the XAML Source Generator

It is possible to step into the XAML generator by adding the following property:

```xml
<PropertyGroup>
  <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak>
</PropertyGroup>
```

Setting this property will popup a Debugger window allowing the selection of a visual studio instance, giving the ability to set breakpoints and trace the generator.

## Dumping the XAML Source Generator state

In the context of hot reload or otherwise, troubleshooting the generator's state may be useful.

Using this feature, the generator will dump the following for **each** run:

- CommandLine arguments
- MSBuild properties
- MSBuild items
- Generated source files

To enable this feature, add the following property:

```xml
<PropertyGroup>
  <XamlSourceGeneratorTracingFolder>path_to_folder</XamlSourceGeneratorTracingFolder>
</PropertyGroup>
```

> [!WARNING]
> A lot of data may be generated in some cases, make sure to disable this feature once it is no longer needed and don't forget to cleanup.

## Troubleshooting Uno.SourceGeneration based generation

When building, if you're having build error messages that looks like one of those:

- `the targets [Microsoft.Build.Execution.TargetResult] failed to execute.`
- `error : Project has no references.`

There may be issues with the analysis of the project's source or configuration.

**Security notice: That `binlog` files produced below should never be published in a public location, as they may contain private information, such as source files. Make sure to provide those in private channels after review.**

The Source Generation tooling diagnostics can be enabled as follows:

- In the project file that fails to build, in the first `PropertyGroup` node, add the following content:

    ```xml
    <UnoSourceGeneratorUnsecureBinLogEnabled>true</UnoSourceGeneratorUnsecureBinLogEnabled>
    ```

- Make to update or add the `Uno.SourceGenerationTasks` to the latest version
- When building, in the inner `obj` folders, a set of `.binlog` files are generated that can be opened with the [msbuild log viewer](http://msbuildlog.com/) and help the troubleshooting of the generation errors.
- Once you've reviewed the files, you may provide those as a reference for troubleshooting to the Uno maintainers.
- The best way to provide those file for troubleshooting is to make a zip archive of the whole solution folder without cleaning it, so it contains the proper diagnostics `.binlog` files.

**Make sure to remove the `UnoSourceGeneratorUnsecureBinLogEnabled` property once done.**

If ever the need arises to view the generated source code of a *failing* CI build, you can perform the following steps:

1. In your local branch, locate the one of build yaml files (located in the root Uno folder):
     - `.azure-devops-android-tests.yml`
     - `.azure-devops-macos.yml`
     - `.azure-devops-wasm-uitests.yml`

2. At the bottom of the yaml files, you'll find *Publish...* tasks, right above these tasks, copy/paste the following code to create a task which will copy all generated source files and put them in an artifact for you to download:

    ```yml
    - bash: cp -r $(build.sourcesdirectory)<YourProjectDirectory>/obj/Release/g/XamlCodeGenerator/ $(build.artifactstagingdirectory)
        condition: failed()
        displayName: "Copy generated XAML code"
    ```

    Therefore, in the case of Uno, an example of <YourProjectDirectory> would be */src/SamplesApp/SamplesApp.Droid*.

3. Once the build fails and completes, you can download the corresponding build artifact from the Artifacts menu to view your generated source files.

**Remember that you should never submit this change, this is temporary and only for viewing generated code.**

## Xaml fuzzy matching

The XAML source generator used to do fuzzy matching for types. This doesn't match Windows behavior and can cause performance issues. The MSBuild property `UnoEnableXamlFuzzyMatching` controls whether fuzzy matching is enabled or not. The property was introduced in Uno 4.8 and defaulted to `true`. Starting with Uno 5.0, this property defaults to `false`. It is planned that fuzzy matching will be removed completely Uno 6.

Note that for XAML-included namespaces (for example `android`, `ios`, etc.), The default namespaces are used (it's considered as if it is `http://schemas.microsoft.com/winfx/2006/xaml/presentation`).
