---
name: Bug Report
about: Report a bug encountered while developing with Uno
labels: kind/bug, triage/untriaged, difficulty/tbd
---

<!-- Please use this template while reporting a bug and provide as much info as possible. Not doing so may result in your bug not being addressed in a timely manner. Thanks!

If the matter is security related, please disclose it privately via https://github.com/unoplatform/Uno/security/
-->

## Current behavior

<!-- Describe how the issue manifests. -->

## Expected behavior

<!-- Describe what the desired behavior would be. -->

## How to reproduce it (as minimally and precisely as possible)

<!-- 
Please provide a **MINIMAL REPRO PROJECT** and the **STEPS TO REPRODUCE**

To create a minimal reproduction project:
 - Create an Uno app through:
   - `dotnet new unoapp --install Uno.ProjectTemplates.Dotnet` and `dotnet new unoapp`
   - or through the [Visual Studio extension](https://platform.uno/docs/articles/get-started-vs.html)
 - Make sure to add the least code possible to demonstrate the issue
 - Keep all project heads, even if the platforms are seemingly not relevant to your issue
 - Remove all the `obj/bin` folders and zip the folder.
 - Attach the zip file to the issue

If the issue is visible on WebAssembly and uses only XAML:
- Visit https://playground.platform.uno
- Add your code and data context as needed
- Create a link and paste it here
-->

## Workaround

<!-- Please provide steps to workaround this problem if possible -->

## Works on UWP/WinUI
<!--
Yes / No

To make sure this is an Uno Platform specific issue, try running your sample application 
on Windows using the UWP or WinUI project. If it does not work as well, it may be a 
Windows issue or it may be a documentation issue. In this case, open a discussion instead:
https://github.com/unoplatform/uno/discussions
-->

## Environment

<!-- For bug reports Check one or more of the following options with "x" -->

Nuget Package:
<!-- Please open issues on the project's repo if any, for instance:
       Uno.Material:       https://github.com/unoplatform/uno.material/issues
       Uno.Wasm.Bootstrap: https://github.com/unoplatform/uno.wasm.bootstrap/issues 
 -->
- [ ] Uno.UI / Uno.UI.WebAssembly / Uno.UI.Skia
- [ ] Uno.WinUI / Uno.WinUI.WebAssembly / Uno.WinUI.Skia
- [ ] Uno.SourceGenerationTasks
- [ ] Uno.UI.RemoteControl / Uno.WinUI.RemoteControl
- [ ] Other: <!-- Please specify -->

Nuget Package Version(s):

Affected platform(s):

- [ ] iOS
- [ ] Android
- [ ] WebAssembly
- [ ] WebAssembly renderers for Xamarin.Forms
- [ ] macOS
- [ ] Skia
  - [ ] WPF
  - [ ] GTK (Linux)
  - [ ] Tizen
- [ ] Windows
- [ ] Build tasks
- [ ] Solution Templates

IDE:

- [ ] Visual Studio 2017 (version: )
- [ ] Visual Studio 2019 (version: )
- [ ] Visual Studio for Mac (version: )
- [ ] Rider Windows (version: )
- [ ] Rider macOS (version: )
- [ ] Visual Studio Code (version: )

Relevant plugins:

- [ ] Resharper (version: )

## Anything else we need to know?

<!-- We would love to know of any friction, apart from knowledge, that prevented you from sending in a pull-request -->
