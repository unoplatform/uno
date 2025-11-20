---
uid: Uno.QuickReference
---

# Quick Reference Guide

This guide provides quick access to common commands, patterns, and workflows for Uno Platform development.

## Prerequisites

| Requirement | Installation |
|-------------|--------------|
| .NET SDK | [Download .NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS) or [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) |
| IDE | [Visual Studio 2022/2026](xref:Uno.GetStarted.vs2022), [VS Code](xref:Uno.GetStarted.vscode), or [Rider](xref:Uno.GetStarted.Rider) |
| Uno Platform Extension | Install from marketplace for your IDE |
| Uno.Check | `dotnet tool install -g uno.check` |

## Common Commands

### Project Setup

```bash
# Install/Update uno.check
dotnet tool install -g uno.check
dotnet tool update -g uno.check

# Run uno.check to verify environment
uno-check

# Create new Uno Platform app
dotnet new install Uno.Templates
dotnet new unoapp -o MyApp
```

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build for specific platform
dotnet build -f net10.0                           # WebAssembly/Skia (takes 30-60 seconds)
dotnet build -f net10.0-android                   # Android (takes 1-2 minutes)
dotnet build -f net10.0-ios                       # iOS (takes 2-3 minutes)
dotnet build -f net10.0-windows10.0.19041.0      # Windows (takes 1-2 minutes)

# Run the application
dotnet run -f net10.0                             # WebAssembly/Skia
```

### Package Management

```bash
# Add Uno Platform package
dotnet add package Uno.WinUI

# Add common packages
dotnet add package Uno.Extensions.Navigation
dotnet add package Uno.Extensions.Hosting
dotnet add package Uno.Toolkit.WinUI

# Update all packages
dotnet outdated                                   # List outdated packages
dotnet add package PackageName                    # Update specific package
```

### Clean and Rebuild

```bash
# Clean build artifacts
dotnet clean

# Remove bin/obj folders (deep clean)
find . -name "bin" -o -name "obj" | xargs rm -rf  # macOS/Linux
Get-ChildItem -Include bin,obj -Recurse | Remove-Item -Recurse -Force  # Windows PowerShell

# Full rebuild
dotnet clean && dotnet restore && dotnet build
```

## Project Structure

```text
MyApp/
├── MyApp.sln                           # Solution file
├── MyApp/                              # Shared code
│   ├── App.xaml                        # Application entry point
│   ├── App.xaml.cs                     # Application code-behind
│   ├── MainPage.xaml                   # Main UI page
│   ├── Presentation/                   # MVVM ViewModels
│   ├── Services/                       # Business logic
│   └── Assets/                         # Images, fonts, etc.
├── MyApp.Mobile/                       # iOS/Android head
│   └── MyApp.Mobile.csproj
├── MyApp.Desktop/                      # Windows/macOS/Linux head
│   └── MyApp.Desktop.csproj
├── MyApp.Wasm/                         # WebAssembly head
│   └── MyApp.Wasm.csproj
└── MyApp.Windows/                      # WinAppSDK head
    └── MyApp.Windows.csproj
```

## Common XAML Patterns

### Basic Layout

```xml
<Page x:Class="MyApp.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <StackPanel Spacing="8" Padding="16">
        <TextBlock Text="Welcome to Uno Platform" 
                   Style="{StaticResource TitleTextBlockStyle}" />
        
        <TextBox PlaceholderText="Enter your name" 
                 Text="{x:Bind ViewModel.UserName, Mode=TwoWay}" />
        
        <Button Content="Submit" 
                Command="{x:Bind ViewModel.SubmitCommand}" />
    </StackPanel>
</Page>
```

### Data Binding

```xml
<!-- One-way binding -->
<TextBlock Text="{x:Bind ViewModel.Title}" />

<!-- Two-way binding -->
<TextBox Text="{x:Bind ViewModel.UserInput, Mode=TwoWay}" />

<!-- Command binding -->
<Button Content="Click" Command="{x:Bind ViewModel.ClickCommand}" />

<!-- Collection binding -->
<ListView ItemsSource="{x:Bind ViewModel.Items}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Item">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

### Styling and Theming

```xml
<!-- Use theme resources for adaptive colors -->
<Border Background="{ThemeResource SystemControlBackgroundChromeMediumBrush}">
    <TextBlock Foreground="{ThemeResource SystemControlForegroundBaseHighBrush}" 
               Text="Adapts to light/dark theme" />
</Border>

<!-- Apply styles -->
<TextBlock Text="Title" Style="{StaticResource TitleTextBlockStyle}" />
<TextBlock Text="Subtitle" Style="{StaticResource SubtitleTextBlockStyle}" />
<TextBlock Text="Body" Style="{StaticResource BodyTextBlockStyle}" />
```

## MVVM Pattern

### ViewModel Base

```csharp
public class MainViewModel : ObservableObject
{
    private string _title = "Welcome";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ICommand SubmitCommand { get; }

    public MainViewModel()
    {
        SubmitCommand = new RelayCommand(OnSubmit);
    }

    private void OnSubmit()
    {
        // Handle command
    }
}
```

### Dependency Injection

```csharp
// In App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .Configure(host => host
            .UseNavigation()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IDataService, DataService>();
                services.AddTransient<MainViewModel>();
            })
        );
    
    MainWindow = builder.Window;
}
```

## Navigation

### Basic Navigation

```csharp
// Navigate to a page
await Navigator.NavigateViewModelAsync<SecondViewModel>(this);

// Navigate with parameters
await Navigator.NavigateViewModelAsync<DetailViewModel>(
    this, 
    data: new Dictionary<string, object> { ["ItemId"] = 123 }
);

// Go back
await Navigator.NavigateBackAsync(this);
```

### Route-based Navigation

```csharp
// Define routes
.ConfigureServices(services =>
{
    services.AddSingleton<IRoutes, Routes>();
})

// Routes.cs
public class Routes : IRoutes
{
    public void Configure(IRouteRegistry routes)
    {
        routes.Register(
            new RouteMap("", View: typeof(MainPage)),
            new RouteMap("Detail", View: typeof(DetailPage))
        );
    }
}

// Navigate by route
await Navigator.NavigateRouteAsync(this, "Detail");
```

## Platform-Specific Code

### Using Preprocessor Directives

```csharp
#if __ANDROID__
    // Android-specific code
#elif __IOS__
    // iOS-specific code
#elif __WASM__
    // WebAssembly-specific code
#elif WINDOWS
    // Windows-specific code
#elif __SKIA__
    // Skia (Desktop) specific code
#endif
```

### Using Platform Helpers

```csharp
// Check current platform
if (OperatingSystem.IsAndroid()) { /* Android code */ }
if (OperatingSystem.IsIOS()) { /* iOS code */ }
if (OperatingSystem.IsBrowser()) { /* WASM code */ }
if (OperatingSystem.IsWindows()) { /* Windows code */ }
```

## Debugging

### Enable Detailed Logging

```csharp
// In App.xaml.cs constructor
#if DEBUG
    this.ConfigureFilters(LogExtensionPoint.AmbientLoggerFactory.WithFilter(
        new FilterLoggerSettings
        {
            { "Uno", LogLevel.Warning },
            { "Windows", LogLevel.Warning },
            { "Microsoft", LogLevel.Information },
            { "YourApp", LogLevel.Debug }
        }
    ));
#endif
```

### Hot Reload

Hot Reload requires:

- **Visual Studio 2022/2026**: Built-in support
- **VS Code**: Uno Platform extension + `UseStudio()` in App.xaml.cs
- **.NET 10.0 or .NET 9.0**: Target framework in csproj
- **Debug mode**: Run with debugger attached


```csharp
// Enable in App.xaml.cs
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var builder = this.CreateBuilder(args)
        .UseStudio() // Enables Hot Reload and other Studio features
        .Configure(/* ... */);
}
```

## Performance Tips

1. **Use `x:Bind` instead of `Binding`** - Compile-time binding is faster
2. **Virtualize long lists** - Use `ListView` or `GridView` for collections
3. **Lazy-load images** - Set `IsLazyLoadingEnabled="True"` on images
4. **Profile with Uno.UI.RemoteControl** - Monitor performance metrics
5. **Use Hot Design<sup>®</sup> Agent** - AI-powered UI optimization suggestions

## Common Issues

### Build Errors

```bash
# Clear package cache
dotnet nuget locals all --clear

# Remove and restore packages
rm -rf ~/.nuget/packages/uno.*
dotnet restore

# Check environment setup
uno-check
```

### XAML Errors

- **"The name does not exist in the namespace"**: Add proper xmlns declaration
- **"Property does not exist"**: Check WinUI compatibility and API availability
- **"x:Bind path cannot be resolved"**: Ensure ViewModel property exists and is public

### Runtime Errors

- **Hot Reload not working**: Ensure `UseStudio()` is called and debugger is attached
- **Navigation fails**: Verify routes are registered and ViewModels are configured
- **Platform-specific API fails**: Check platform availability with conditional compilation

## Resources

### Documentation

- [Getting Started](xref:Uno.GetStarted)
- [Features Overview](xref:Uno.Features.Overview)
- [Troubleshooting](xref:Uno.UI.CommonIssues)
- [API Reference](xref:Uno.Reference.Overview)

### Tools

- [Uno Platform Studio](xref:Uno.Platform.Studio.Overview) - Hot Design<sup>®</sup>, Hot Reload, Design-to-Code, Hot Design<sup>®</sup> Agent
- [Uno MCPs](xref:Uno.Features.Uno.MCPs) - AI agent integration for documentation and live app queries

### Community

- [Discord](https://www.platform.uno/discord) - Chat with the community
- [GitHub](https://github.com/unoplatform/uno) - File issues and contribute
- [Blog](https://platform.uno/blog/) - Latest updates and tutorials

## Next Steps

- [Explore samples and tutorials](xref:Uno.Tutorials.Intro)
- [Learn about Uno Extensions](xref:Uno.Extensions.Overview.ExtensionsOverview)
- [Use Hot Design<sup>®</sup> Agent for UI development](xref:Uno.HotDesign.Agent)
- [Integrate with AI agents using MCPs](xref:Uno.Features.Uno.MCPs)
