# Troubleshooting Source Generation

Source Generation in Uno is done in one of two ways:
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

## Debugging the XAML Source Generator
It is possible to step into the XAML generator by adding the following property:
```xml
<PropertyGroup>
  <UnoUISourceGeneratorDebuggerBreak>True</UnoUISourceGeneratorDebuggerBreak>
</PropertyGroup>
```
Setting this property will popup a Debugger window allowing the selection of a visual studio instance, giving the ability to set breakpoints and trace the generator.

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
     - .azure-devops-android-tests.yml
     - .azure-devops-macos.yml
     - .azure-devops-wasm-uitests.yml
    
2. At the bottom of the yaml files, you'll find *Publish...* tasks, right above these tasks, copy/paste the following code to create a task which will copy all generated source files and put them in an artifact for you to download:

       - bash: cp -r $(build.sourcesdirectory)<YourProjectDirectory>/obj/Release/g/XamlCodeGenerator/ $(build.artifactstagingdirectory)
            condition: failed()
            displayName: "Copy generated XAML code"

Therefore, in the case of Uno, an example of <YourProjectDirectory> would be */src/SamplesApp/SamplesApp.Droid*.

3. Once the build fails and completes, you can download the corresponding build artifact from the Artifacts menu to view your generated source files.

**Remember that you should never submit this change, this is temporary and only for viewing generated code.**
