---
uid: Uno.GettingStarted.UsingWizard.CIPipeline
---

### CI Pipeline

- #### None

    No CI pipeline will be created. This is the default option for both the blank and recommended presets.

    ```
    dotnet new unoapp -ci none
    ```

- #### Azure Pipelines

    Adds a YAML file that can be used to create a CI pipeline in Azure Pipelines.

    ```
    dotnet new unoapp -ci azure
    ```

- #### GitHub Action  

    Adds a YAML file that can be used to create a CI pipeline in GitHub Actions.

    ```
    dotnet new unoapp -ci github
    ```
