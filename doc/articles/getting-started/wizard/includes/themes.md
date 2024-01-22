Uno platform lets you decide easily which theme or skin to display throughout your app.

This option sets the generated theme or skin to be used in the generated app. The options available are:

- **Material**  
    Material is Google's design system.  
    Learn more about [Material](https://material.io/). This option will result in an application that will have the material theme applied. This is the default for the recommended preset.  

    ```dotnetcli
    dotnet new unoapp -theme material
    ```

- **Fluent**  
    Fluent is an open-source design system that drives Windows and WinUI's default style.  
    Learn more about [Fluent Design System](https://www.microsoft.com/design/fluent/). This is the default for the blank preset.

    ```dotnetcli
    dotnet new unoapp -theme fluent
    ```

The theme in the application can be further customized with the following options:

#### Theme Service  

Includes references to the [Uno.Extensions.Core.WinUI](https://www.nuget.org/packages/Uno.Extensions.Core.WinUI/) package which includes the theme service that can be used to control the theme (Dark, Light or System) of the application. This is included by default in the recommended preset, but not in the blank preset.

```dotnetcli
dotnet new unoapp -theme-Service
```

#### Import DSP

Allows colors in the application to be overridden using a DSP file (Material theme only). This is included by default in the recommended preset, but not in the blank preset.

```dotnetcli
dotnet new unoapp -dsp
```
