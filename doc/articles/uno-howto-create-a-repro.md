---
uid: Uno.Development.Create-Repro
---

# How to create a reproduction project (aka "repro")

This documentation describes the steps needed to create a "repro" or reproduction project, which will help the community and maintainers troubleshoot issues that you may find when developing with Uno Platform.

The goal of a repro app is to find the **smallest possible piece of code that demonstrates the problem**, with the least dependencies possible. This is needed to make the resolution as fast as possible, as the Uno Platform team and community members do not have access to your projects sources nor understand your own expertise domain.

Some steps and questions to answer:

1. Make sure to test with the latest Uno Platform pre-release builds, the issue might have already been fixed.
2. Start from a new "blank uno app" from the Uno platform Visual Studio extension, or `dotnet new unoapp --preset=blank` app.
3. Attach a .zip file of that repro to the issue.
   > [!TIP]
   > Watch out for the size of zip created. Check the section below on reducing the sample size.
4. If you can, add a video or screenshots reproducing the issue. Github supports uploading `mp4` files in issues.

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
- Analyze device and app logs for clues about the control's behavior. For Android-specific logging, see [Android Debugging with Logcat](#android-debugging-with-logcat) below.
  - You may enable [the controls debug logs](https://github.com/unoplatform/uno/blob/master/doc/articles/logging.md), if any.
  - To validate that logs are enabled and in Debug, those starting with `Windows`, `Microsoft`, or `Uno` should be visible in the app's output. If not, make sure to [setup the logging properly](xref:Uno.Development.MigratingFromPreviousReleases).
  - Logs on iOS may need to have the [OSLog logger](https://github.com/unoplatform/uno.extensions.logging) enabled when running on production devices.
- Try on different versions of Visual Studio, iOS, Android, Linux, or browsers
- If available, try the API on Windows (WinUI) and see if it behaves differently than what Uno Platform is doing
- When issues occur, try breaking on all exceptions to check if an exception may be hidden and not reported.
- Update Uno.WinUI or other dependencies to previous or later versions, using a [bisection technique](https://git-scm.com/docs/git-bisect). Knowing which version of a package introduced an issue can help orient the investigations.

## Android Debugging with Logcat

When debugging Android-specific issues, Logcat provides essential device and application logs. Here's how to access Logcat in different development environments:

### [**Visual Studio**](#tab/vs)

Visual Studio provides built-in Logcat support through the **Device Log** window.

**Accessing Logcat:**

1. Deploy your app to an Android device or emulator
2. Navigate to **View** → **Other Windows** → **Device Log**
3. Select your target device from the dropdown menu
4. The logs will automatically stream from the selected device

**Filtering Logs:**

- Filter by **Process Name**: Enter your app's package name
- Filter by **Log Level**: Choose from Verbose, Debug, Info, Warn, Error, or Fatal
- Use the **Search box** to find specific terms like "Uno", "Exception", or error messages
- Click the **Filter** dropdown to apply multiple filters simultaneously

**Useful Tips:**

- Right-click in the Device Log window and select **Clear All** before reproducing an issue for cleaner logs
- Use the **Pause** button in the toolbar to freeze log output for easier reading
- Select relevant log entries, right-click, and choose **Copy** to share specific logs
- Save logs via the toolbar save button for later analysis

### [**JetBrains Rider**](#tab/rider)

Rider provides Logcat access through the **Logcat** tool window with advanced filtering capabilities.

**Accessing Logcat:**

1. Deploy your app to an Android device or emulator
2. Navigate to **View** → **Tool Windows** → **Logcat**
   - Alternatively, press **Alt+6** (Windows/Linux) or **⌘6** (macOS)
3. Select your device from the device dropdown in the toolbar
4. Select your app's process from the process filter dropdown

**Filtering and Searching:**

- Use the **log level filter** buttons in the toolbar (Verbose, Debug, Info, Warn, Error, Assert)
- Enter search terms in the **filter bar** at the top
- Use **regex patterns** for advanced filtering (e.g., `Uno.*Exception`)
- Click the **filter settings** icon to configure column visibility and formatting

**Useful Tips:**

- Right-click in the Logcat window and select **Clear** to remove previous logs
- Use **Ctrl+F** (Windows/Linux) or **⌘F** (macOS) to open the find dialog for searching within logs
- Export logs using **Right-click** → **Export to Text File**
- Configure log colors in **File** → **Settings** → **Editor** → **Color Scheme** → **Logcat**

**Prerequisites:**

- Ensure Android SDK is configured: **File** → **Settings** → **Appearance & Behavior** → **System Settings** → **Android SDK**
- Verify ADB connection by checking if your device appears in the device dropdown
- If the device doesn't appear, run `adb devices` in the terminal to troubleshoot

### [**VS Code**](#tab/vscode)

VS Code requires manual ADB setup or extensions for Logcat viewing.

**Prerequisites:**

1. **Install Android SDK** (if not already installed):
   - Windows: Typically located at `C:\Users\<username>\AppData\Local\Android\Sdk`
   - macOS: `~/Library/Android/sdk`
   - Linux: `~/Android/Sdk`

2. **Verify ADB Installation:**
3. 
   - ADB is located in `<Android-SDK>/platform-tools/adb`
   - Add ADB to your system PATH or note its full path
   - Test by running: `adb version`

**Option 1: Using Terminal (Manual ADB)**

1. Open VS Code's integrated terminal: **View** → **Terminal** or press **Ctrl+`** (Windows/Linux) or **⌘`** (macOS)
2. Ensure your device or emulator is running and connected
3. Verify device connection:
   
   ```bash
   adb devices
   ```
   
5. Start viewing logs:
   
   ```bash
   # View all logs (verbose)
   adb logcat
   
   # Clear previous logs first
   adb logcat -c
   
   # Filter by your app's package name
   adb logcat | grep "com.yourcompany.yourapp"
   
   # Filter by log priority (E=Error, W=Warn, I=Info, D=Debug, V=Verbose)
   adb logcat *:E
   
   # Filter by tag (show only Uno logs)
   adb logcat Uno:* *:S
   
   # Combination: Show Uno debug logs and all errors
   adb logcat Uno:D *:E
   
   # Save logs to file
   adb logcat > android-logs.txt
   
   # Dump existing logs without continuous streaming
   adb logcat -d
   ```

**Option 2: Using VS Code Extensions**

1. Open the Extensions view: **View** → **Extensions** or press **Ctrl+Shift+X** (Windows/Linux) or **⌘⇧X** (macOS)
2. Search for and install one of these extensions:
   
   - **"Android"** by adelphes
   - **"ADB Interface for VSCode"** by vincenthage
   - **"Android iOS Emulator"** by DiemasMichiels
     
4. Follow the extension-specific instructions to:
   
   - Configure the Android SDK path in VS Code settings
   - Open the Logcat view (usually in the sidebar or via command palette)
   - Connect to your device/emulator

**Useful ADB Commands:**

```bash
# List connected devices
adb devices

# Kill and restart ADB server (if device not detected)
adb kill-server
adb start-server

# Filter logs by multiple tags
adb logcat -s Uno:D Microsoft:D AndroidRuntime:E

# View logs with timestamps
adb logcat -v time

# View logs with thread IDs
adb logcat -v threadtime

# Follow logs in real-time with color (Linux/macOS)
adb logcat -v color

# Clear logs and start fresh
adb logcat -c && adb logcat
```

**Tips for Effective Filtering:**

- Use `grep` (Linux/macOS) or `findstr`/`Select-String` (Windows) to filter output:
  ```bash
  # Linux/macOS
  adb logcat | grep -i "exception"
  
  # Windows Command Prompt
  adb logcat | findstr "exception"
  
  # Windows PowerShell
  adb logcat | Select-String "exception"
  ```

---

### Common Logcat Tips (All IDEs)

**Enable Verbose Uno Logging:**

Add this code to your `App.xaml.cs` constructor to increase Uno-specific logging detail:

```csharp
using Microsoft.Extensions.Logging;

public App()
{
#if __ANDROID__
    var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Trace);
        builder.AddFilter("Uno", LogLevel.Trace);
        builder.AddFilter("Windows", LogLevel.Trace);
        builder.AddFilter("Microsoft", LogLevel.Trace);
    });
    
    Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;
    global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
    
    this.InitializeComponent();
}
```

**Key Search Terms for Logcat:**

When analyzing logs, search for these keywords to quickly identify issues:

- `Exception` - Catch any exceptions thrown
- `Error` - Application errors
- `Crash` - Fatal crashes
- `AndroidRuntime` - Native Android runtime errors
- `FATAL EXCEPTION` - Critical app crashes
- `Uno` - Uno Platform-specific logs
- `Microsoft.UI.Xaml` - WinUI/XAML-related logs
- `mono-rt` - Mono runtime messages

**Uno-Specific Log Tags:**

Filter for these tags to focus on Uno Platform logs:

- `Uno.*` - All Uno-related logs
- `Windows.UI.Xaml` - XAML framework logs
- `Microsoft.UI.Xaml` - WinUI framework logs
- `UnoViewGroup` - Android view hierarchy logs
- `Uno.UI.Controls` - Control-specific logs

**Best Practices When Reporting Issues:**

1. **Clear old logs:**
   
   ```bash
   adb logcat -c
   ```

3. **Reproduce the issue** immediately after clearing logs

4. **Capture logs** during the issue:
   
   ```bash
   adb logcat > issue-reproduction.txt
   ```

6. **Filter to relevant logs** before sharing:
   
   - Focus on the timeframe when the issue occurred
   - Include 10-20 lines before and after the error
   - Remove sensitive information (API keys, user data, etc.)

8. **Include essential context:**
   
   - Uno Platform version
   - Android version and device/emulator details
   - Steps to reproduce
   - Expected vs actual behavior

**Additional Resources:**

- [Android Logcat Documentation](https://developer.android.com/studio/command-line/logcat)
- [Uno Platform Logging Guide](xref:Uno.Development.Logging)
- [ADB Command Reference](https://developer.android.com/studio/command-line/adb)

---

## Creating a smaller zip file to upload to github

> Yowza, that’s a big file Try again with a file smaller than 10MB.  
> -- Github

If you get the above message when attempting to upload the zipped sample, thats usually because you have included, beside the source codes, needless build outputs inside `bin` and `obj` folders for each target heads.

You can usually reduce this by performing `Build > Clean Solution` before zipping the entire solution folder. It also helps to manually delete the `bin\` and `obj\` folders under each project heads that you've compiled.

However, sometimes that still may not be enough. In such case, you can leverage the `git` tool and a `.gitignore` file to further reduce the size of the solution.

### [**Windows (Visual Studio)**](#tab/windows-vs)

If you're inside of Visual Studio:

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
