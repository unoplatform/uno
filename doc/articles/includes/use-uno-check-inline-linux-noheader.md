1. Open a Terminal
1. If `dotnet --version` returns `command not found`:
    - Follow the [official directions](https://learn.microsoft.com/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website#packages) for installing .NET.
      > [!IMPORTANT]
      > On Ubuntu 24.04 and later, install .NET from the Ubuntu package feed. The Microsoft package repository no longer publishes .NET packages for Ubuntu. On Ubuntu 22.04, .NET is available from both feeds and you should use only one of them. Refer to the directions from Microsoft on [installing .NET on Ubuntu](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install) and make sure to select your Ubuntu version.
1. Then, setup uno.check by installing or updating the tool with:

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

    You can optionally add the `--target desktop --target web` (or `ios`, `android`, `windows`) parameters based on your intended development platforms.

1. Follow the instructions indicated by the tool

You can find additional information about [**uno-check here**](xref:UnoCheck.UsingUnoCheck).
