# How to migrate existing UWP code to Uno

There are two separate paths to use an existing UWP codebase on top of uno:
- An existing UWP application
- An existing UWP library

## Use an existing application

To use an existing application, the easiest path is to create an Uno QuickStart application from [the solution template](using-uno-ui.md), then adjust to project structure for the source code to be shared.

1. In the existing application, create a shared project
1. In this new shared project, move all the Assets and Xaml and C# Files
1. Add a reference to the shared project in the existing UWP project
1. On the side, create a new Uno Quick Start project using a name matching your project
1. Copy the iOS, Android and WebAssembly project over to your existing source tree
1. Adjust the shared project references to use your existing shared project
1. Building your project

> Note: You may need to adjust your code considering the [current API differences](api-differences.md).

## Use an existing library

*TBD*