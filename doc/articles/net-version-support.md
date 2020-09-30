# .NET version support

This page lists [supported .NET versions](https://docs.microsoft.com/en-us/dotnet/standard/net-standard#net-implementation-support) and [C# language versions](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version) for different target platforms.

### Table of supported versions

| Platform        | Default .NET version | Default C# version |  Max .NET version | Max C# version |
|-----------------|:--------------------:|--------------------|:-----------------:|----------------|
| Android         |   .NET Standard 2.1  | 8                  | .NET Standard 2.1 | 8              |
| iOS             |   .NET Standard 2.1  | 7.3                | .NET Standard 2.1 | 8              |
| macOS           |   .NET Standard 2.1  | 7.3                | .NET Standard 2.1 | 8              |
| WebAssembly     |   .NET Standard 2.0  | 7.3                |       .NET 5      | 9              |
| Linux           |   .NET Standard 2.1  | 8                  |       .NET 5      | 9              |
| UWP             |   .NET Standard 2.0  | 7.3                | .NET Standard 2.0 | 7.3            |
| WinUI 3 (Win32) |        .NET 5        | 9                  |       .NET 5      | 9              |

### Notes

For Android, iOS, and macOS, the supported versions depend on the version of Xamarin installed, which is generally tied to the Visual Studio version if you are using Visual Studio.

You can force a higher version of C# using `LangVersion` in the platform `csproj` (eg `<LangVersion>9.0</LangVersion>`), but some language features may not work properly, such as those that depend on compiler-checked types (eg array slicing, `init`-only properties) or on runtime support (eg default interface implementations).