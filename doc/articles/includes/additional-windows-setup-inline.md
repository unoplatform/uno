In order to run Skia+GTK heads, you will need to make sure to install the [GTK3 runtime](https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases).

> [!TIP]
> Once the GTK3 runtime is installed, you will need restart your IDE for the changes to take effect.

To install on CI using Azure Pipelines, use the following step:

```yml
steps:
  - powershell: |
        $source = "https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases/download/2020-07-15/gtk3-runtime-3.24.20-2020-07-15-ts-win64.exe"
        $destination = "gtk3-runtime.exe"
        Invoke-WebRequest $source -OutFile $destination
        Start-Process -FilePath "gtk3-runtime.exe" -Wait -PassThru -ArgumentList /S
        Write-Host "##vso[task.setvariable variable=PATH;]${env:PATH};C:\Program Files\GTK3-Runtime Win64\bin";

    displayName: Install GTK3 runtime
```
