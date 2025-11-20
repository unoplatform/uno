---
uid: Uno.Development.Create-Repro
---

# How to create a reproduction project (aka "repro")

This documentation describes the steps needed to create a "repro" or reproduction project, which will help the community and maintainers troubleshoot issues that you may find when developing with Uno Platform.

The goal of a repro app is to find the **smallest possible piece of code that demonstrates the problem**, with the least dependencies possible. This is needed to make the resolution as fast as possible, as the Uno Platform team and community members do not have access to your projects sources nor understand your own expertise domain.

Some steps and questions to answer:

1. Make sure to test with the latest Uno Platform pre-release builds, the issue might have already been fixed.
1. Start from a new "blank uno app" from the Uno platform Visual Studio extension, or `dotnet new unoapp --preset=blank` app.
1. Attach a .zip file of that repro to the issue.
   > [!TIP]
   > Watch out for the size of zip created. Check the section below on reducing the sample size.
1. If you can, add a video or screenshots reproducing the issue. Github supports uploading `mp4` files in issues.

## Tips on how to create the simplest repro app

- Find the smallest piece of API used by your app (XAML Control, method, type) and extract that code into the repro app
- If it's impacting a control:
  - Try replicating the interactions as minimally as possible by cutting the ties with the rest of your original app
  - Try altering the properties of the control by either removing the styles, changing the styles, changing modes of the control if there are any.
- If you can't repro in a separate app because there are too many dependencies in your app try, removing as much code as you can around the use of failing API or Control. This may include removing implicit styles, global initializations.
- If the control offers events, try adding logging to Loading/Unloading/PropertyChanged/LayoutUpdated or other available events to determine if the Control or API is interacting with your code in expected ways. Sometimes adding a breakpoint in the handler of those events can show interesting stack traces.
- When debugging data bindings:
  - Show the text version of the binding expression somewhere in the UI, to see the type of the bound data:
    - `<TextBlock Text="{Binding}" />`
    - `<TextBlock Text="{Binding MyProperty}" />`
    - `<TextBlock Text="{Binding Command, ElementName=MyButton}" />`
  - Add an event handler to `DataContextChanged` in the code behind to see if and when the `DataContext` changed.
- Analyze device and app logs for clues about the control's behavior.
  - You may enable [the controls debug logs](https://github.com/unoplatform/uno/blob/master/doc/articles/logging.md), if any.
  - To validate that logs are enabled and in Debug, those starting with `Windows`, `Microsoft`, or `Uno` should be visible in the app's output. If not, make sure to [setup the logging properly](xref:Uno.Development.MigratingFromPreviousReleases).
  - Logs on iOS may need to have the [OSLog logger](https://github.com/unoplatform/uno.extensions.logging) enabled when running on production devices.
- Try on different versions of Visual Studio, iOS, Android, Linux, or browsers
- If available, try the API on Windows (WinUI) and see if it behaves differently than what Uno Platform is doing
- When issues occur, try breaking on all exceptions to check if an exception may be hidden and not reported.
- Update Uno.WinUI or other dependencies to previous or later versions, using a [bisection technique](https://git-scm.com/docs/git-bisect). Knowing which version of a package introduced an issue can help orient the investigations.

## Creating a smaller zip file to upload to github

> Yowza, that’s a big file Try again with a file smaller than 10MB.  
> -- Github

If you get the above message when attempting to upload the zipped sample, thats usually because you have included, beside the source codes, needless build outputs inside `bin` and `obj` folders for each target heads.

You can usually reduce this by performing `Build > Clean Solution` before zipping the entire solution folder. It also helps to manually delete the `bin\` and `obj\` folders under each project heads that you've compiled.

However, sometimes that still may not be enough. In such case, you can leverage the `git` tool and a `.gitignore` file to further reduce the size of the solution.

### [**Windows (Visual Studio)**](#tab/windows-vs)

If you're inside of Visual Studio 2022/2026:

- Open the solution
- At the bottom of the IDE window, click the **Add to Source Control** button, then **git**
- Select **Local only**, then **Create**
- Wait a few seconds for the changes to be committed
- Close visual studio
- Open a command line prompt in your solution folder and type `git clean -fdx`

Once done, you can zip the folder and send it to github in your issue or discussion.

### [**Windows (Console)**](#tab/windows-console)

Using the command prompt:

- Navigate to your sample's root folder
- Type `dotnet new gitignore`
- Type `git init`
- Type `git add .`
- Type `git commit -m "Initial sample commit"`
- Type `git archive HEAD --format zip --output sample.zip`
- Type `explorer /select,sample.zip`

Once done, you can send the `sample.zip` to github in your issue or discussion.

### [**macOS / Linux**](#tab/nix)

Using a terminal:

- Navigate to your sample's root folder
- Type `wget https://raw.githubusercontent.com/github/gitignore/main/VisualStudio.gitignore -O .gitignore`
- Type `git init`
- Type `git add .`
- Type `git commit -m "Initial sample commit"`
- Type `git clean -fdx`

Once done, you can zip the folder and send it to github in your issue or discussion.

---
