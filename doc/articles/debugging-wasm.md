# Using the WebAssembly C# Debugger

Debugging WebAssembly via Google Chrome is experimentally supported by the Uno Platform. We HIGHLY recommend that you use [Google Chrome Canary](https://www.google.com/chrome/canary/).  Step-through debugging (in, out, over), breakpoints, inspection of run-time locals and viewing .NET source code from the developer tools works. Additional capabilities and browser support will become available as Microsoft adds [support for them to mono](https://github.com/mono/mono/tree/master/sdks/wasm).

- Make your WASM project the startup project (right-click **set as startup** in Solution Explorer)
- Make sure you have the following lines defined in your project file which enable the Mono runtime debugger. Please ensure that `DEBUG` constant is defined and debug symbols are emitted and are of the type `portable`:

```xml
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
    <MonoRuntimeDebuggerEnabled>true</MonoRuntimeDebuggerEnabled>
    <DefineConstants>$(DefineConstants);TRACE;DEBUG</DefineConstants>
    <DebugType>portable</DebugType>
    <DebugSymbols>true</DebugSymbols>
</PropertyGroup>
```

- In the debugging toolbar:

   - Select **IIS Express** as the debugging target
   - Select **Chrome** as the Web Browser
   - Make sure script debugging is disabled<br/>
   ![IIS express settings](Assets/quick-start/wasm-debugging-iis-express.png)

- Start the debugging session using <kbd>Ctrl</kbd><kbd>F5</kbd> or _Debug_ > _Start Without Debugging_ from the menu, (<kbd>F5</kbd> will work, but the debugging experience won't be in Visual Studio)
- Once your application has started, press <kbd>Alt</kbd><kbd>Shift</kbd><kbd>D</kbd> (in Chrome, on your application's tab)
- A new tab will open with the debugger or instructions to activate it
![](Assets/quick-start/wasm-debugger-step-01.png)
- You will now get the Chrome DevTools to open listing all the .NET loaded assemblies on the Sources tab:<br/>
![](Assets/quick-start/wasm-debugger-step-02.png)
- You may need to refresh the original tab if you want to debug the entry point (Main) of your application.<br/>
![](Assets/quick-start/wasm-debugger-step-03.png)

> ### Tips for debugging in Chrome
> * You need to launch a new instance of Chrome with right parameters. If Chrome is your main browser
> and you don't want to restart it, install another version of Chrome (_Chrome Side-by-Side_).
> You may simply install _Chrome Beta_ or _Chrome Canary_ and use them instead.
> * Sometimes, you may have a problem removing a breakpoint from code (it's crashing the debugger).
> You can remove them in the _Breakpoints list_ instead.
> * Once _IIS Express_ is launched, no need to press <kbd>Ctrl</kbd><kbd>F5</kbd> again: you simply need to rebuild your
> _WASM_ head and refresh it in the browser.
> * **To refresh an app**, you should use the debugger tab and press the _refresh_ button in the content.
> * **If you have multiple monitors**, you can detach the _debugger tab_ and put it on another window.
> * For breakpoints to work properly, you should not open the debugger tools (<kbd>F12</kbd>) in the app's tab.
> * If you are **debugging a library which is publishing SourceLinks**, you must disable it or you'll
> always see the SourceLink code in the debugger. SourceLink should be activated only on Release build.
> * When debugging in Chrome, <kbd>Ctrl</kbd>+<kbd>O</kbd> brings up a file-search field. That way it's a lot easier to find .cs files versus searching through the whole folder hierarchy.
