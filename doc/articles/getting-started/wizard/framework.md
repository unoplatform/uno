---
uid: Uno.GettingStarted.UsingWizard.Framework
---

<<<<<<< HEAD
This setting lets you choose the .NET version to target. The default is .NET 7.0, but you can change it to .NET 8.0 (preview)!

- [**.NET 7**](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-7) is provided as an STS (Standard Term Support) version, this is the most recent release and has introduced a myriad of improvements in performance especially for mobile, as well as other aspects and is the current recommended .NET version.  
- [**.NET 8**](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8) adds further performance improvements as well as other general enhancements.
=======
### Framework (tfm)

This setting lets you choose the .NET version to target. The default is .NET 8.0, but you can change it to .NET 7.0!

- #### .NET 8.0
    [.NET 8.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8) is provided as a LTS (Long Term Support) version. This version adds significant performance improvements, as well as other general enhancements. This is the default (for both blank and recommended presets) and the recommended option for new projects
>>>>>>> c60da5359c (docs: Updating Wizard/Template information (#14580))

    ```
    dotnet new unoapp -tfm 8.0
    ```

- #### .NET 7.0 
    [.NET 7.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-7) is provided as an STS (Standard Term Support) version. This option should only be used in scenarios where .NET 8.0 cannot be used.  

    ```
    dotnet new unoapp -tfm 7.0
    ```
