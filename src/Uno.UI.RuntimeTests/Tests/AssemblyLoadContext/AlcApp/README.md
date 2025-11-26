# ALC Test App

This is a simple test application designed to be loaded into a secondary AssemblyLoadContext (ALC) for testing purposes.

## Purpose

This app is used to test the following scenarios:

1. Loading an Uno application into a secondary ALC
2. Window.ContentHostOverride functionality
3. Resource inheritance from the secondary ALC's Application.Current.Resources

## Structure

This app uses the **Uno.Sdk** (version 6.1.23) with a proper desktop app structure:

- **Uno.UI.RuntimeTests.AlcApp.csproj**: Uno.Sdk-based project file targeting `net10.0-desktop`
- **Platforms/Desktop/Program.cs**: Entry point with `Main()` method using `UnoPlatformHostBuilder`
- **App.xaml** / **App.cs**: The main application class (partial) with XAML resources
- **MainPage.xaml** / **MainPage.xaml.cs**: A simple test page with styled content
- **AppResources.xaml**: Resource dictionary with test resources
- **GlobalUsings.cs**: Common namespace imports (implicit usings from Uno.Sdk)

## Building

The app is built automatically by the test infrastructure using:

```bash
dotnet build Uno.UI.RuntimeTests.AlcApp.csproj -c Debug -f net10.0-desktop
```

## Usage in Tests

For ALC testing, the test:

1. Loads the compiled DLL into a secondary AssemblyLoadContext
2. Calls `App.InitializeLogging()` static method
3. Creates an instance of `App` (which calls `InitializeComponent()` to load XAML resources)
4. Calls `OnLaunched()` to initialize the app content

Note: The `Program.Main()` entry point is available for standalone execution but is not used in ALC tests as it would create a conflicting UI host.

## Usage

The app is loaded dynamically during runtime tests and its content is hosted through Window.ContentHostOverride.
