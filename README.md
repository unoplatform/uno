# Introduction

Create Uno Platform Projects with Hot Reload and XAML Preview.

# Requirements

## Linux (same for WSL)

- .NET Core 3.1
- .NET 5
- [mono-complete](https://platform.uno/docs/articles/get-started-vscode.html#prerequisites) (required for WASM projects)
- [libgtk-3-dev](https://platform.uno/docs/articles/get-started-with-linux.html#setting-up-for-linux) (required for Skia GTK projects)

##  Windows

- .NET Core 3.1
- .NET 5
- [Mono](https://platform.uno/docs/articles/get-started-vscode.html#prerequisites) (required for WASM projects)
- [GTK 3 runtime](https://platform.uno/docs/articles/get-started-with-linux.html#setting-for-windows-and-wsl) (required for Skia GTK projects)

# Features

## Project

- New Skia Gtk Project
- New WASM Project
- New Shared Skia Gtk/WASM Project

## Debug

- Debug Skia GTK project pressing F5
- Debug WASM project pressing F5
- Debug shared Skia GTK/WASM
    - Select the debug configuration (Skia GTK or WASM)
    - Press F5 do start debug

## XAML

- Create new `.xaml` and automatically create `.xaml.cs`
    - Simply create a new file with a `.xaml` extension
- XAML Preview
    - With a `.xaml` file opened click on the 👁️ icon for open the XAML Preview
    - Edit the `.xaml` file
- XAML Hot Reload
    - Start debug process
    - Edit the `.xaml` file
- XAML Code completion
