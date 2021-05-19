# Troubleshooting build issues

## Diagnose environment problems with `uno-check`

If you're not sure whether your environment is correctly configured for Uno Platform development, running the [`uno-check` command-line tool](https://www.nuget.org/packages/Uno.Check) should be your first step. Find installation instructions and other details [here](uno-check.md).

## Multi-projects structure considerations
Uno uses a multi-project structure, for which each project has to be build individually for errors to disapear from the **Error List** window (notice the **Project** column values).
In order to clear the **Error List** window, build the whole solution completely once.
Subsequently, build a specific project and prefer the use of the **Output** tool window (in the menu **View** -> **Output**), taking build messages by order of appearance.

## Troubleshooting build errors using the build Output Window
To troubleshoot build error, you can change the text output log level:
  - Go to **Tools**, **Options**, **Projects and Solution**, then **Build and Run**
  - Set **MSBuild project build output verbosity** to **Normal** or **Detailed**
  - Build your project again and take a look at the additional output

## Generating MSBuild Binary Log files
If you have trouble building your project, you can get additional information using binary log (binlog) files.

To use MSBuild binary logs:
  - Go to **Tools**, **Options**, **Projects and SOlution**, then **Build and Run**
  - Set **MSBuild project build log verbosity** to **Detailed** or **Diagnostics**
  - Install the [MSBuild log viewer](http://msbuildlog.com/)
  - Install the [Project System Tools](https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.ProjectSystemTools) addin
  - Open **View** > **Other Windows** > **Build Logging**, then click the green play button
  - Build your project again and right click the **Failed** build entry in the **Build Logging** tool window.
  - The binlog viewer tool will expand to the detailed build error
    
**Important: Note that binlog files contain environment variables and csproj files used to build the sources, but not the source files themselves.**
Make sure to review the content of the file for sensitve information before posting it on a public issue, otherwise contact us on Discord for additional information to send us the build logs.**
