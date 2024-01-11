---
uid: Uno.GettingStarted.UsingWizard.Framework
---

### Framework (tfm)

This setting lets you choose the .NET version to target. The default is .NET 8.0, but you can change it to .NET 7.0!

- #### .NET 8.0

    [.NET 8.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8) is provided as a LTS (Long Term Support) version. This version adds significant performance improvements, as well as other general enhancements. This is the default (for both blank and recommended presets) and the recommended option for new projects

    ```
    dotnet new unoapp -tfm 8.0
    ```

- #### .NET 7.0

    [.NET 7.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-7) is provided as an STS (Standard Term Support) version. This option should only be used in scenarios where .NET 8.0 cannot be used.  

    ```
    dotnet new unoapp -tfm 7.0
    ```
