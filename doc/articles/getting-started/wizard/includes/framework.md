This setting lets you choose the .NET version to target. The default is .NET 9.0, but you can also choose .NET 8.0!

- #### .NET 8.0

    [.NET 8.0](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8) is the successor to .NET 7. It will be supported for three years (until November 2026) as a long-term support (LTS) release. This is the most stable option for new projects.

    ```dotnetcli
    dotnet new unoapp -tfm net8.0
    ```

- #### .NET 9.0

    [.NET 9.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview) the successor to .NET 8, has a special focus on cloud-native apps and performance. It will be supported for 18 months (until May 2026) as a standard-term support (STS) release. This is the default (for both blank and recommended presets).

    ```dotnetcli
    dotnet new unoapp -tfm net9.0
    ```
