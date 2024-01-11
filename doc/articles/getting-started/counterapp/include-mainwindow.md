
Before proceeding, you should select a target platform and run the application. Follow these links for more information on debugging an application for [Visual Studio](xref:Uno.GettingStarted.CreateAnApp.VS2022), [Visual Studio Code](xref:Uno.GettingStarted.CreateAnApp.VSCode) or [Rider](xref:Uno.GettingStarted.CreateAnApp.Rider).

## MainWindow and MainPage

The majority of an Uno Platform application is defined in a class library project, in this case, named **Counter**. This project contains files that define the layout of the application and files that implement the application logic. There are also platform-specific projects, in the **Platforms** folder, that contain the platform-specific code required to run the application on each platform.

The startup logic for the application is contained in the `App.cs` file in the **Counter** project. In the `OnLaunched` method, the `MainWindow` of the application is initialized with a `Frame`, used for navigation between pages, and the `MainPage` is set as the initial page.
