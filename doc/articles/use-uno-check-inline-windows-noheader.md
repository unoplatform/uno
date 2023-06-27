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
> Alternatively you can run `uno-check --pre` to install the preview version of the Uno Platform. This is strongly suggested when using Visual Studio **Previews**.
