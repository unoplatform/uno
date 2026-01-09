---
uid: Uno.Features.SplashScreen
---

# Splash Screen

## What is a splash screen?

A splash screen is a visual element displayed when an application launches, providing immediate feedback to users while the app initializes. It typically shows branding elements like logos, app names, or icons, creating a polished first impression and a smooth transition into the application experience.

For platform-specific design guidance, see:
- [Windows App Design: Splash screens](https://learn.microsoft.com/windows/apps/design/launch/splash-screens)
- [Apple Human Interface Guidelines: Launch screens](https://developer.apple.com/design/human-interface-guidelines/launching)
- [Android Material Design: Launch screens](https://developer.android.com/develop/ui/views/launch/splash-screen)

## Splash screens in Uno Platform apps

Uno Platform applications include splash screen support by default across all supported platforms. This feature provides a consistent user experience whether your app runs on Windows, WebAssembly, iOS, Android, macOS, or Linux.

### Default behavior

When you create a new Uno Platform app using version 4.8 or later templates, splash screen functionality is automatically configured. The [Uno.Resizetizer](https://www.nuget.org/packages/Uno.Resizetizer) package is included by default and handles:

- **Asset scaling**: Automatically generates platform-specific image sizes from your source assets (SVG or PNG)
- **Cross-platform setup**: Configures splash screens for all target platforms with minimal manual intervention
- **Consistent branding**: Ensures your splash screen appears uniformly across platforms

### Image assets

Splash screens display image assets that represent your application's branding. The recommended approach is to provide:

- **Vector graphics (SVG)**: Uno.Resizetizer can scale SVG images to all required resolutions automatically
- **High-resolution PNG**: If not using SVG, provide high-resolution PNG images that can be scaled appropriately

For detailed information on working with image assets and scaling, see [Assets and image display](xref:Uno.Features.Assets).

### Setup approaches

Depending on when your project was created, you have different options:

#### Modern templates (Uno Platform 4.8+)

Projects created with newer templates include Uno.Resizetizer by default. To configure your splash screen:

1. Add your splash screen image (SVG or PNG) to your project
2. Configure the image in your project file with the `UnoSplashScreen` item

See [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted#unosplashscreen) for complete setup instructions.

#### Legacy templates (Pre-4.8)

For projects created with older templates, you have two options:

1. **Migrate to Uno.Resizetizer** (recommended): Update your project to use Uno.Resizetizer for automatic asset generation and configuration. See [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted#unosplashscreen).

2. **Manual configuration**: Set up splash screens manually for each platform. See [How to manually add a splash screen](xref:Uno.Development.SplashScreen).

## Customization options

Uno Platform enables you to customize various aspects of the splash screen experience to match your app's branding and design requirements.

### Colors

You can customize the background and accent colors of your splash screen:

- **Background color**: The color displayed behind your splash screen image
- **Accent color**: Used for loading indicators and other UI elements during launch

The specific method for setting these colors varies by platform (see platform-specific sections below).

### WebAssembly-specific customization

WebAssembly applications provide the most extensive customization options through the `AppManifest.js` file. You can configure:

- **Splash screen image**: Path to your splash screen asset
- **Background colors**: Separate colors for light and dark themes
- **Accent colors**: Loading indicator colors for different themes
- **Display name**: Application name shown during launch

For a complete list of properties and examples, see:
- [AppManifest for WebAssembly](xref:Uno.Development.WasmAppManifest)
- [WebAssembly splash screen properties](xref:Uno.Development.SplashScreen#5-webassembly)

### Platform-specific customization

Each platform may have additional customization options:

#### Windows

Configure splash screen settings through the `Package.appxmanifest` file in the Visual Assets section. See [Manual setup: Windows](xref:Uno.Development.SplashScreen#2-windows).

#### Android

Customize splash screen appearance through Android resource files (`Styles.xml`, `drawable/splash.xml`). You can set:
- Background colors
- Image positioning and scaling
- Theme integration

See [Manual setup: Android](xref:Uno.Development.SplashScreen#3-android).

#### iOS/macOS

Configure splash screens using a Storyboard file with constraints and layout options. iOS and macOS follow similar patterns for splash screen configuration. See [Manual setup: iOS](xref:Uno.Development.SplashScreen#4-ios) for detailed instructions that apply to both platforms.

#### GTK/Linux

For GTK and Linux desktop platforms, splash screen configuration follows the Skia implementation patterns. When using Uno.Resizetizer, splash screens are automatically configured. For manual setup, refer to the platform-specific documentation in [How to manually add a splash screen](xref:Uno.Development.SplashScreen).

## See also

- [How to manually add a splash screen](xref:Uno.Development.SplashScreen) - Step-by-step tutorial for manual configuration
- [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted) - Automatic asset generation and splash screen setup
- [AppManifest for WebAssembly](xref:Uno.Development.WasmAppManifest) - WebAssembly-specific configuration options
- [Assets and image display](xref:Uno.Features.Assets) - Understanding image assets and qualifiers
- [Completed splash screen sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/SplashScreenSample) - Working example on GitHub
