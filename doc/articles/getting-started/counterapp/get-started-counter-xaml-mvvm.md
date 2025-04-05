---
uid: Uno.Workshop.Counter.XAML.MVVM
---

# Counter App using XAML and MVVM

[Download the complete XAML + MVVM sample](https://github.com/unoplatform/Uno.Samples/tree/master/reference/Counter/XAML-MVVM)

[!INCLUDE [Intro](xref:Uno.Workshops.Counter.Intro-Inline)]

In this tutorial you will learn how to:

- Create a new Project with Uno Platform using Visual Studio Template Wizard or the **dotnet new** command
- Add elements to the XAML file to define the layout of the application
- Add code to the C# file to implement the application logic using the Model-View-ViewModel (MVVM) pattern
- Use data binding to connect the UI to the application logic

To complete this tutorial you don't need any prior knowledge of the Uno Platform, XAML, or C#.

[!INCLUDE [VS](xref:Uno.Workshops.Counter.Create-Inline)]

## [Visual Studio](#tab/vs)

> [!NOTE]
> If you don't have the **Uno Platform Extension for Visual Studio** installed, follow [these instructions](xref:Uno.GetStarted.vs2022).

- Launch **Visual Studio** and click on **Create new project** on the Start Window. Alternatively, if you're already in Visual Studio, click **New, Project** from the **File** menu.

- Type `Uno Platform` in the search box

- Click **Uno Platform App**, then **Next**

- Name the project `Counter` and click **Create**

At this point you'll enter the **Uno Platform Template Wizard**, giving you options to customize the generated application. For this tutorial, we're only going to configure the markup language and the presentation framework.

- Select **Blank** in **Presets** selection

- Select the **Presentation** tab and choose **MVVM**

- Select the **Markup** tab and choose **XAML**

Before completing the wizard, take a look through each of the sections and see what other options are available. You can always come back and create a new project with different options later. For more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

- Click **Create** to complete the wizard

The template will create a solution with a single cross-platform project, named `Counter`, ready to run.

## [Command Line](#tab/cli)

> [!NOTE]
> If you don't have the Uno Platform dotnet new templates installed, follow [dotnet new templates for Uno Platform](xref:Uno.GetStarted.dotnet-new).

From the command line, run the following command:

```dotnetcli
dotnet new unoapp -preset blank -presentation mvvm -markup xaml -o Counter
```

This will create a new folder called **Counter** containing the new application.

If you want to discover all the options available in the **unoapp** template, run the following command:

```dotnetcli
dotnet new unoapp -h
```

Also, for more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

---

[!INCLUDE [Counter Solution](xref:Uno.Workshops.Counter.Solution-Inline)]

![Counter Solution](Assets/counter-solution-xaml.png)

[!INCLUDE [Main Window](xref:Uno.Workshops.Counter.MainWindow-Inline)]

[!INCLUDE [Main Page - XAML](xref:Uno.Workshops.Counter.Xaml.MainPage-Inline)]

[!INCLUDE [Main Page - Layout](xref:Uno.Workshops.Counter.Mainpage-Layout-Inline)]

[!INCLUDE [Main Page - Image Xaml](xref:Uno.Workshops.Counter.Xaml.Image-Inline)]

[!INCLUDE [Main Page - Change Layout](xref:Uno.Workshops.Counter.Mainpage-Change-Layout-Inline)]

[!INCLUDE [Main Page - Other Elements Xaml](xref:Uno.Workshops.Counter.Xaml.Elements-Inline)]

[!INCLUDE [View Model](xref:Uno.Workshops.Counter.MVVM)]

## Data Binding

Now that we have the **`MainViewModel`** class, we can update the **`MainPage`** to use data binding to connect the UI to the application logic.

[!INCLUDE [Main Page - Data Binding Xaml](xref:Uno.Workshops.Counter.Xaml.DataBinding-Inline)]

[!INCLUDE [Wrap Up](xref:Uno.Workshops.Counter.WrapUp-Inline)]

If you want to see the completed application, you can download the source code from [GitHub](https://github.com/unoplatform/Uno.Samples/tree/master/reference/Counter/XAML-MVVM).
