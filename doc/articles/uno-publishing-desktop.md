---
uid: uno.publishing.desktop
---

# Publishing Your App for Desktop

## Preparing For Publish

- [Profile your app with VS 2022](https://learn.microsoft.com/en-us/visualstudio/profiling/profiling-feature-tour?view=vs-2022)
- [Profile using dotnet-trace and SpeedScope](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)

## Publish your app

### Using Visual Studio 2022

To publish your app with Visual Studio 2022:

- In the debugger toolbar drop-down, select the `net8.0-desktop` target framework
- Once the project has reloaded, right-click on the project and select **Publish**
- Select the appropriate target for your publication, this example will use the **Folder**, then click **Next**
- Choose an output folder for the published output, then click **Close**.
- In the opened editor, click the **Configuration** "pen" to edit the configuration
- In the opened popup, ensure that **Target Framework** is set to `net8.0-desktop`, then click **Save**
- On the top right, click the **Publish** button
- Once the build is done, the output is located in the publish folder

Once done, you can head over to the [publishing section](xref:uno.publishing.webassembly#publishing).

### Using the Command Line

To build your app from the CLI, on Windows, Linux, or macOS:

- Open a terminal, command line, or powershell
- Navigate to your `csproj` folder
- Publish the app using:

  ```shell
  dotnet publish -f net8.0-desktop -c Release -o ./publish
  ```

- Once the build is done, the output is located in the `./publish` folder

Once done, you can head over to [publishing section](xref:uno.publishing.webassembly#publishing).

## Publishing

> [!NOTE]
> Work still in progress for publishing to some targets.

Publishing your app can be done through different means:

- [ClickOnce](https://learn.microsoft.com/visualstudio/deployment/quickstart-deploy-using-clickonce-folder?view=vs-2022) on Windows
- Using a Zip file, then running the app using `dotnet [yourapp].dll`
