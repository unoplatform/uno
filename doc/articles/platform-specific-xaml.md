# Platform-specific XAML markup in Uno

Uno allows you to reuse views and business logic across platforms. Sometimes though you may want to write different code per platform, either because you need to access platform-specific native APIs and 3rd-party libraries, or because you want your app to look and behave differently depending on the platform. 

This guide covers multiple approaches to managing per-platform markup in XAML. See [this guide for managing per-platform C#](platform-specific-csharp.md).

## Project structure

There are two ways to restrict code or XAML markup to be used only on a specific platform:
 * Use conditionals within a shared file
 * Place the code in a file which is only included in the desired platform head.
 
 The structure of an Uno app created with the default [Visual Studio template](https://marketplace.visualstudio.com/items?itemName=nventivecorp.uno-platform-addin) is [explained in more detail here](uno-app-solution-structure.md). The key point to understand is that files in a shared project referenced from a platform head **are treated in exactly the same way** as files included directly under the head, and are compiled together into a single assembly.

## XAML conditional prefixes

The Uno platform uses pre-defined prefixes to include or exclude parts of XAML markup depending on the platform. These prefixes can be applied to XAML objects or to individual object properties.

Conditional prefixes you wish to use in XAML file must be defined at the top of the file, like other XAML prefixes. They can be then applied to any object or property within the body of the file.

For prefixes which will be excluded on Windows (e.g. `android`, `ios`), the actual namespace is arbitrary, since the Uno parser ignores it. The prefix should be put in the `mc:Ignorable` list. For prefixes which will be included on Windows (e.g. `win`, `not_android`) the namespace should be `http://schemas.microsoft.com/winfx/2006/xaml/presentation` and the prefix should not be put in the `mc:Ignorable` list.

### Example

```xaml
<Page x:Class="HelloWorld.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	  xmlns:android="http://uno.ui/android"
	  xmlns:ios="http://uno.ui/ios"
	  xmlns:wasm="http://uno.ui/wasm"
	  xmlns:win="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:not_android="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	  mc:Ignorable="d android ios wasm">

	<StackPanel Margin="20,70,0,0">
		<TextBlock Text="This text will be large on Windows, and pink on WASM"
				   win:FontSize="24"
				   wasm:Foreground="DeepPink"
				   TextWrapping="Wrap"/>
		<TextBlock android:Text="This version will be used on Android"
				   not_android:Text="This version will be used on every other platform" />
		<ios:TextBlock Text="This TextBlock will only be created on iOS" />
	</StackPanel>
</Page>
```

This results in:

![Visual output](Assets/platform-specific-xaml.png)

### Available prefixes

The pre-defined prefixes are listed below:

| Prefix        | Included platforms           | Excluded platforms           | Namespace                                                   | Put in `mc:Ignorable`? |
|---------------|------------------------------|------------------------------|-------------------------------------------------------------|------------------------|
| `win`         | Windows                      | Android, iOS, web, macOS     | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | no                     |
| `xamarin`     | Android, iOS, web, macOS     | Windows                      | `http:/uno.ui/xamarin`                | yes                    |
| `not_win`     | Android, iOS, web, macOS     | Windows                      | `http:/uno.ui/not_win`                | yes                    |
| `android`     | Android                      | Windows, iOS, web, macOS     | `http:/uno.ui/android`                | yes                    |
| `ios`         | iOS                          | Windows, Android, web, macOS | `http:/uno.ui/ios`                    | yes                    |
| `wasm`        | web                          | Windows, Android, iOS, macOS | `http:/uno.ui/wasm`                   | yes                    |
| `macos`       | macOS                        | Windows, Android, iOS, web   | `http:/uno.ui/macos`                  | yes                    |
| `not_android` | Windows, iOS, web, macOS     | Android                      | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | no                     |
| `not_ios`     | Windows, Android, web, macOS | iOS                          | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | no                     |
| `not_wasm`    | Windows, Android, iOS, macOS | web                          | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | no                     |
| `not_macos`   | Windows, Android, iOS, web   | macOS                        | `http://schemas.microsoft.com/winfx/2006/xaml/presentation` | no                     |
