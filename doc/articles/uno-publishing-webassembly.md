---
uid: uno.publishing.webassembly
---

# Publishing Your App for WebAssembly

## Preparing For Publish

- [Configure deep linking](xref:UnoWasmBootstrap.Features.DeepLinking)
- [Configure WebAssembly AOT modes](xref:Uno.Wasm.Bootstrap.Runtime.Execution)
- [Profile the memory of your app](xref:Uno.Wasm.Bootstrap.Profiling.Memory)

You can view more on the [WebAssembly Bootstrapper](xref:UnoWasmBootstrap.Overview) options.

## Packaging

### Packaging your app using Visual Studio 2022/2026

To build your app for WebAssembly:

- In the debugger toolbar drop-down, select the `net9.0-browserwasm` target framework
- Once the project has reloaded, right-click on the project and select **Publish**
- Select the appropriate target for your publication, this example will use the **Folder**, then click **Next**
- Choose an output folder for the published output, then click **Close**.
- In the opened editor, on the top right, click the **Publish** button
- Once the build is done, the output is located in the `wwwroot` folder

Once done, you can head over to [publishing section](xref:uno.publishing.webassembly#publishing).

### Packaging your app using the CLI

To build your app from the CLI, on Windows, Linux, or macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net9.0-browserwasm -c Release -o ./publish
  ```

- Once the build is done, the output is located in the `./publish/wwwroot` folder

Once done, you can head over to [publishing section](xref:uno.publishing.webassembly#publishing).

## Publishing your WebAssembly App

Publishing your app can be done to different servers and cloud providers.

- [How to host a WebAssembly App](xref:Uno.Development.HostWebAssemblyApp)
- [Publishing to Azure Static Apps](xref:Uno.Tutorials.AzureStaticWebApps)
- [Server locally using dotnet-serve](https://github.com/natemcmaster/dotnet-serve)
