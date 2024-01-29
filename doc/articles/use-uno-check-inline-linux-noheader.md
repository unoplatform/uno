1. Open a Terminal
1. If `dotnet --version` returns `command not found`:
    - Follow the [official directions](https://learn.microsoft.com/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website#packages) for installing .NET.
      > [!IMPORTANT]
      > If your distribution is Ubuntu and you want to develop for Android, make sure to use the directions to install the Microsoft feed and not the Ubuntu official feed. The latter does not contain Android workloads.
1. Then, setup uno.check by:
    - Installing the tool:

        ```bash
        dotnet tool install -g uno.check
        ```

    - Updating the tool, if you previously installed it:

        ```bash
        dotnet tool update -g uno.check
        ```

1. Run the tool from the command prompt with the following command:

    ```bash
    uno-check
    ```

    If the above command fails, use the following:

    ```bash
    ~/.dotnet/tools/uno-check
    ```

1. Follow the instructions indicated by the tool
