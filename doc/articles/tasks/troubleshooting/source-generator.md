# Troubleshooting the Source Generator


## Before you begin

- Install the [MSBuild Binary and Structured Log Viewer](http://msbuildlog.com/). 
- Install the [Microsoft Child Process Debugging Power Tool](https://marketplace.visualstudio.com/items?itemName=vsdbgplat.MicrosoftChildProcessDebuggingPowerTool)

## Debugging the source generators of the Uno solution

- Create a side app
- [Override](https://github.com/unoplatform/uno/blob/7f003e13f34f899a4b9ac04552317920f961247a/src/crosstargeting_override.props.sample#L45) the nuget package cache
- Build once so the `Uno.SourceGenerationHost.exe` spins up
- Attach to that process from the Uno.UI solution
- Rebuild the side app
- Your breakpoints in the source generators will hit
- Output from the generator is stored at `obj\Debug\netstandard2.0\g\XamlCodeGenerator`


