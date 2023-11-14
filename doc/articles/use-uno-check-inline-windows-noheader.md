1. Open a command-line prompt, Windows Terminal if you have it installed, or else Command Prompt or Windows Powershell from the Start menu.

1. a. Install the tool by running the following command from the command prompt:

    ```bash
    dotnet tool install -g uno.check
    ```

   b. To update the tool, if you already have an existing one:

    ```bash
    dotnet tool update -g uno.check
    ```

1. Run the tool from the command prompt with the following command:

    ```bash
    uno-check
    ```

1. Follow the instructions indicated by the tool

> [!NOTE]
> Until the release of [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) ([November 14th, 2023](https://www.dotnetconf.net/)) and Visual Studio 17.8, running `uno-check --pre-major` is required. Uno Platform supports [Visual Studio 17.8 Preview 5](https://visualstudio.microsoft.com/vs/preview/) and .NET 8 RC2 until this date.