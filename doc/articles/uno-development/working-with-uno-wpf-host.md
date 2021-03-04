# The Uno.UI WebAssembly WPF Host

The Uno Platform provides the ability to run UWP and .NET code through the Mono runtime. While WebAssembly makes it very easy to deploy on the Web, it currently is, as of October 2018, not very easy to debug an application running in this context.

In order to ease the debugging of such an application, the Uno Platform provides the [Uno.UI.WpfHost package](https://www.nuget.org/packages/Uno.UI.WpfHost). It supports the Uno Platform inside of a WPF application, using a Chromium WebView.

This mode is replaces the WebAssembly runtime with the Desktop .NET Framework. It communicates with the WebView via Javascript `eval()` calls on the WebView control. This enables easier troubleshooting of the .NET code, because all the C# code is running in a Visual Studio-supported scenario. In this scenario, C# edit-and-continue and all the debugger features are available.

## Using the Uno.UI.WpfHost package

- Create an Uno Platform app using the [Visual Studio extension](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin#overview) named `MyApp`
- Add a WPF application head
- Reference the [Uno.UI.WpfHost package](https://www.nuget.org/packages/Uno.UI.WpfHost)
- In the new WPF head, reference the `MyApp.Wasm` project
- The constructor of the MainWindow, add the following code:
```csharp
UnoHostView.Init(() => MyApp.Wasm.Program.Main(new string[0]), $@"..\..\..\..\MyApp.Wasm\bin\{configuration}\netstandard2.0\dist");
```

- In the `MainWindow.xaml` file:

```xml
<Window x:Class="SamplesApp.WPF.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:SamplesApp.WPF"
		xmlns:unoHost="clr-namespace:Uno.UI.WpfHost;assembly=Uno.UI.WpfHost"
		mc:Ignorable="d"
        Title="MainWindow" Height="450" Width="800">
    <Grid>
        <unoHost:UnoHostView />
    </Grid>
</Window>
```

Running the application will then execute the Wasm head code inside of the WPF application, allowing the debugger to attach to the C# code.