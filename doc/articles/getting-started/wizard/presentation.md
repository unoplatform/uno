---
uid: Uno.GettingStarted.UsingWizard.Presentation
---

### Presentation
This setting allows you to choose a presentation architecture.

- #### None
    Generates a project without any presentation architecture installed. This is the default for the blank preset.

- #### MVVM
    Generates a project optimized for use with the traditional MVVM architecture, using Microsoft's [MVVM Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm).

    ```
    dotnet new unoapp -presentation mvvm
    ```

- #### MVUX
    The **M**odel **V**iew **U**pdate e**X**tended (MVUX) pattern is a new programming architecture by Uno Platform. This is the default for the recommended preset.

    Its main feature is enabling the use of immutable [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object) entities and Models (using [records](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records)) as the presentation layer, making the whole need for implementing property change notification redundant.  

    To learn more about the MVUX pattern, read [this](xref:Uno.Extensions.Mvux.Overview).


    ```
    dotnet new unoapp -presentation mvux
    ```
