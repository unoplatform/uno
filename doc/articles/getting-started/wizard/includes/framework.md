This setting lets you choose the .NET version to target. The default is .NET 10.0, and .NET 9.0 remains supported.

- #### .NET 9.0

    [.NET 9.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9/overview), the successor to .NET 8, has a special focus on cloud-native apps and performance.
    As a Standard Term Support (STS) release, it is now supported for **24 months (until November 10, 2026)**. See the [STS latest announcement](https://devblogs.microsoft.com/dotnet/dotnet-sts-releases-supported-for-24-months/) for more details.

    > [!NOTE]
    > For **mobile workloads**, there is **no change yet**, support remains at **18 months (until May 12, 2026)**. See [MAUI support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/maui) for more details.

    ```dotnetcli
    dotnet new unoapp -tfm net9.0
    ```

- #### .NET 10.0

    [.NET 10.0](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview), the successor to .NET 9, includes improvements in performance, C# 14 support, and long-term platform stability.
    As a Long Term Support (LTS) release, it will be supported for **three years (until November 2028)**.
    .NET 10 is now the stable and default runtime option for new Uno Platform projects, while .NET 9 remains supported.

    ```dotnetcli
    dotnet new unoapp -tfm net10.0
    ```
