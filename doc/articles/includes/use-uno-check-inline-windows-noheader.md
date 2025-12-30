1. Open a command-line prompt, Windows Terminal if you have it installed, or else Command Prompt or Windows Powershell from the Start menu.

1. Setup uno.check by installing or updating the tool with:

    ```bash
    dotnet tool update -g uno.check
    ```

1. Run the tool from the command prompt with the following command:

    ```bash
    uno-check
    ```

    You can optionally add the `--target desktop --target web` (or `ios`, `android`, `windows`) parameters based on your intended development platforms.

1. Follow the instructions indicated by the tool.

You can find additional information about [**uno-check here**](xref:UnoCheck.UsingUnoCheck).
