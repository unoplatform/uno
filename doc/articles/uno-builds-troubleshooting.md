---
uid: Uno.Development.Troubleshooting
---

# Troubleshooting build issues

## Diagnose environment problems with `uno-check`

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](https://www.nuget.org/packages/Uno.Check) should be your first step. Find installation instructions and other details [here](external/uno.check/doc/using-uno-check.md).

## Multi-targeting considerations

Uno Platform projects use multi-targeting, for which each target framework has to be built individually, for some errors to disappear from the **Error List** window (notice the **Project** column values).
In order to clear the **Error List** window, build the whole solution completely once.
Subsequently, build a specific project and prefer the use of the **Output** tool window (in the menu **View** -> **Output**), taking build messages by order of appearance.

## Troubleshooting build errors using the build Output Window

To troubleshoot build error, you can change the text output log level:

- Go to **Tools**, **Options**, **Projects and Solution**, then **Build and Run**
- Set **MSBuild project build output verbosity** to **Normal** or **Detailed**
- Build your project again and take a look at the additional output

## Generating MSBuild Binary Log files

If you have trouble building your project, you can get additional information using binary log (binlog) files.

**Important: Note that binlog files contain environment variables and csproj files used to build the sources, but not the source files themselves.**
Make sure to review the content of the file for sensitive information before posting it on a public issue, otherwise contact us on Discord for additional information to send us the build logs.**

### From Visual Studio

To use MSBuild binary logs:

- Go to **Tools**, **Options**, **Projects and Solution**, then **Build and Run**
- Set **MSBuild project build log verbosity** to **Detailed** or **Diagnostics**
- Install the [MSBuild log viewer](http://msbuildlog.com/)
- Install the [Project System Tools for VS 2022/2026](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools2022) or [VS 2019](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools) add-in
- Open **View** > **Other Windows** > **Build Logging**, then click the green play button
- Build your project again and right click the **Failed** build entry in the **Build Logging** tool window.
- The binlog viewer tool will expand to the detailed build error

### From the command line

You may be asked to generate a binlog from the command line, as it includes more information (the project structure, but not the source files) that can help troubleshoot issues.

To generate a binlog file from the command line:

- Open a :
  - [Visual Studio Developer command prompt](https://learn.microsoft.com/visualstudio/ide/reference/command-prompt-powershell) on Windows
  - A terminal window on Linux or macOS
- Navigate to the folder of the project head
- For WinUI/iOS/Android/macOS projects, type the following:

  ```bash
  msbuild /r /bl MyProject.csproj
  ```

- For other targets (.NET 5+, WebAssembly, Skia, etc...)

  ```dotnetcli
  dotnet build /bl MyProject.csproj
  ```

- Once the build has finished, a file named `msbuild.binlog` is generated next to the `.csproj` file.

## Common Build errors troubleshooting

You can also browse the [common issues list](xref:Uno.UI.CommonIssues.AllIDEs) for build errors and their resolutions.
