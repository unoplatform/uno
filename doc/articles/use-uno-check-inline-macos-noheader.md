1. Open a Terminal.

1. a. Install the tool by running the following command from the command prompt:
    ```
    dotnet tool install -g uno.check
    ```
   b. To update the tool, if you already have an existing one:
    ```
    dotnet tool update -g uno.check
    ```
1. Run the tool from the command prompt with the following command:
    ```
    uno-check
    ```
    If the above command fails, use the following:
    ```
    ~/.dotnet/tools/uno-check
    ```
1. Follow the instructions indicated by the tool

> [!NOTE]
> To build .NET 7 apps, you will need to run `uno-check --pre`.