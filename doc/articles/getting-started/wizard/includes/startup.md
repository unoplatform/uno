### Preset

The `unoapp` template comes with two preset options: *Blank* and *Recommended* (*Default* in Visual Studio Wizard). These represent two different sets of pre-configured values for the options that follow. The *Blank* option is the most minimal template, while the *Recommended* option comes with a few more features pre-configured.

Both presets include the following platforms: Windows, Android, iOS, Mac Catalyst, GTK, Wasm.

- #### Blank

    The *Blank* preset option represents the simplest Uno Platform application. Think of this as the "Hello World" of Uno Platform applications. The application has a single page, with a single `TextBlock`. The only additional package reference it includes is to [Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted), which is used to make it easy define the application icon and splash screen for all target platforms.

    Create an application with the *Blank* preset:

    ```dotnetcli
    dotnet new unoapp -preset blank
    ```

    The *Blank* preset can be augmented by clicking `Customize` in the Visual Studio Wizard, or by specifying additional command line parameters to the `dotnet new` command. For example, this command will create a *Blank* application with the Uno.Toolkit.

    ```dotnetcli
    dotnet new unoapp -preset blank -toolkit
    ```

- #### Recommended

    The **Recommended** preset option represents a more complete Uno Platform application. It uses `Uno.Extensions` to initialize the application by creating an `IHost`, using the `IHostBuilder` pattern established by the Microsoft.Extensions libraries. The IHost provides a number of services that are useful in Uno Platform applications, including Dependency Injection, Logging, and Configuration.

    The *Recommended* option also includes a number of additional packages that are useful in Uno Platform applications, including `Uno.UITest`, `Uno.Extensions.Http`, `Uno.Extensions.Navigation`, `Uno.Extensions.Serialization`, and `Uno.Extensions.Logging`.

    Create an application with the *Recommended* preset:

    ```dotnetcli
    dotnet new unoapp -preset recommended
    ```

    The *Recommended* preset can be augmented by clicking `Customize` in the Visual Studio Wizard, or by specifying additional command line parameters to the `dotnet new` command. For example, this command will create a *Blank* application with out the Uno.Toolkit.

    ```dotnetcli
    dotnet new unoapp -preset blank -toolkit false
    ```

For each of the options that follow, we'll define the values that are set for each preset option.
