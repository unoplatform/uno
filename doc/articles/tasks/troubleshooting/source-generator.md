# Troubleshooting the Source Generator


## Before you begin

- Install the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/). 
- Install the [Microsoft Child Process Debugging Power Tool](https://marketplace.visualstudio.com/items?itemName=vsdbgplat.MicrosoftChildProcessDebuggingPowerTool)

## Debugging the source generators of the Uno solution

- Create a side app and note which version of `Uno.UI` is used as a `PackageReference`
- [Override](https://github.com/unoplatform/uno/blob/7f003e13f34f899a4b9ac04552317920f961247a/src/crosstargeting_override.props.sample#L45) the nuget package cache and use the same version for `Uno.UI` as is used in the side app.
- Build the side application which will start a initial `Uno.SourceGenerationHost.exe` process
- Attach to the `Uno.SourceGenerationHost.exe` processes (there may be many) from the `Uno.UI` solution
- Rebuild the side app
- Your breakpoints in the source generators will hit
- Output from the generator is stored at `obj\Debug\netstandard2.0\g\XamlCodeGenerator`
- If you need to restart debugging after making significant code changes to the source generator then make sure you terminate all existing processes (`taskkill /fi "imagename eq Uno.SourceGeneration.Host.exe" /f /t`) in between development iterations.

