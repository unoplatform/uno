This setting lets you choose the .NET version to target. The default is .NET 10.0, and you can also choose .NET 11.0.

- #### .NET 10.0

    [.NET 10.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview), the successor to .NET 9, includes improvements in performance, C# 14 support, and long-term platform stability.
    As a Long Term Support (LTS) release, it will be supported for **three years (until November 2028)**.
    .NET 10 is the stable and default runtime option for new Uno Platform projects.

    ```dotnetcli
    dotnet new unoapp -tfm net10.0
    ```

- #### .NET 11.0

    [.NET 11.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-11/overview), the successor to .NET 10, includes C# 15 support and runs Android and iOS apps on the CoreCLR runtime.
    As a Standard Term Support (STS) release, it will be supported for **two years (until November 14, 2028)**.
    See [Upgrading from .NET 10 to .NET 11](xref:Uno.Development.MigratingFromNet10ToNet11) to move an existing project.

    ```dotnetcli
    dotnet new unoapp -tfm net11.0
    ```
