- #### None

    No CI pipeline will be created. This is the default option for both the blank and recommended presets.

    ```dotnetcli
    dotnet new unoapp -ci none
    ```

- #### Azure Pipelines

    Adds a YAML file that can be used to create a CI pipeline in Azure Pipelines.

    ```dotnetcli
    dotnet new unoapp -ci azure
    ```

- #### GitHub Action  

    Adds a YAML file that can be used to create a CI pipeline in GitHub Actions.

    ```dotnetcli
    dotnet new unoapp -ci github
    ```
