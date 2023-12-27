---
uid: Uno.Workshop.Counter.XAML.MVVM
---

# Counter App using XAML and MVVM

[Download the complete XAML + MVVM sample](https://github.com/unoplatform/Uno.GettingStartedTutorial/tree/master/src/Counter/XAML-MVVM)  


[!INCLUDE [Intro](include-intro.md)]

In this tutorial you will learn how to:

- Create a new Project with Uno Platform using Visual Studio Template Wizard or the **dotnet new** command
- Add elements to the XAML file to define the layout of the application
- Add code to the C# file to implement the application logic using the Model-View-ViewModel (MVVM) pattern
- Use data binding to connect the UI to the application logic

To complete this tutorial you don't need any prior knowledge of the Uno Platform, XAML, or C#.

[!INCLUDE [VS](include-create.md)]

# [Visual Studio](#tab/vs)

> [!NOTE]
> If you don't have the **Uno Platform Extension for Visual Studio** installed, follow [these instructions](xref:Uno.GetStarted.vs2022).

- Launch **Visual Studio** and click on **Create new project** on the Start Window. Alternatively, if you're already in Visual Studio, click **New, Project** from the **File** menu.

- Type `Uno Platform` in the search box

- Click **Uno Platform App**, then **Next**

- Name the project `Counter` and click **Create**

At this point you'll enter the **Uno Platform Template Wizard**, giving you options to customize the generated application. For this tutorial, we're only going to configure the markup language and the presentation framework.

- Select **Blank** and click **Customize**

- Select the **Presentation** tab and choose **MVVM**

- Select the **Markup** tab and choose **XAML**

Before completing the wizard, take a look through each of the sections and see what other options are available. You can always come back and create a new project with different options later. For more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

- Click **Create** to complete the wizard

The template will create a new solution with a number of projects. The main project is a class library called **Counter** which contains the application code. The other projects are platform-specific heads that contain the platform-specific code required to run the application on each platform.

# [Command Line](#tab/cli)

> [!NOTE]
> If you don't have the Uno Platform dotnet new templates installed, follow [these instructions](xref:Uno.GetStarted.dotnet-new).

From the command line, run the following command:

```
dotnet new unoapp -preset blank -presentation mvvm -markup xaml -o Counter
```

This will create a new folder called **Counter** containing the new application.

If you want to discover all the options available in the **unoapp** template, run the following command:

```
dotnet new unoapp -h
```

Also, for more information on all the template options, see [Using the Uno Platform Template](xref:Uno.GettingStarted.UsingWizard).

---

[!INCLUDE [Counter Solution](include-solution.md)]

![Counter Solution](Assets/counter-solution-xaml.png)

[!INCLUDE [Main Window](include-mainwindow.md)]

[!INCLUDE [Main Page - XAML](include-mainpage-xaml.md)]

[!INCLUDE [Main Page - Layout](include-mainpage-layout.md)]

[!INCLUDE [Main Page - Image](include-image-xaml.md)]

[!INCLUDE [Main Page - Change Layout](include-mainpage-change-layout.md)]

[!INCLUDE [Main Page - Other Elements](include-elements-xaml.md)]

[!INCLUDE [View Model](include-mvvm.md)]

## Data Binding

Now that we have the **`MainViewModel`** class, we can update the **`MainPage`** to use data binding to connect the UI to the application logic.

- Add a **`DataContext`** element to the **`Page`** element in the **MainPage.xaml** file.

    ```xml
    <Page.DataContext>
        <local:MainViewModel />
    </Page.DataContext>
    ```

- Update the **`TextBlock`** by removing the **`Text`** attribute, replacing it with two **`Run`** elements, and binding the **`Text`** property of the second **`Run`** element to the **`Count`** property of the **`MainViewModel`**.

    ```xml
    <TextBlock Margin="12"
               HorizontalAlignment="Center"
               TextAlignment="Center">
        <Run Text="Counter: " /><Run Text="{Binding Count}" />
    </TextBlock>
    ```

- Update the **`TextBox`** by binding the **`Text`** property to the **`Step`** property of the **MainViewModel**. The **`Mode`** of the binding is set to **`TwoWay`** so that the **`Step`** property is updated when the user changes the value in the **`TextBox`**.

    ```xml
    <TextBox Margin="12"
             HorizontalAlignment="Center"
             PlaceholderText="Step Size"
             Text="{Binding Step, Mode=TwoWay}"
             TextAlignment="Center" />
    ```

- Update the **`Button`** to add a **`Command`** attribute that is bound to the **`IncrementCommand`** property of the **`MainViewModel`**.

    ```xml
    <Button Margin="12"
            HorizontalAlignment="Center"
            Command="{Binding IncrementCommand}"
            Content="Increment Counter by Step Size" />
    ```

The final code for **MainPage.xaml** should look like this:

```xml
<Page x:Class="Counter.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Counter"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
  <Page.DataContext>
    <local:MainViewModel />
  </Page.DataContext>
  <StackPanel VerticalAlignment="Center">
    <Image Width="150"
           Height="150"
           Margin="12"
           HorizontalAlignment="Center"
           Source="Assets/logo.png" />

    <TextBox Margin="12"
             HorizontalAlignment="Center"
             PlaceholderText="Step Size"
             Text="{Binding Step, Mode=TwoWay}"
             TextAlignment="Center" />

    <TextBlock Margin="12"
               HorizontalAlignment="Center"
               TextAlignment="Center">
        <Run Text="Counter: " /><Run Text="{Binding Count}" />
    </TextBlock>

    <Button Margin="12"
            HorizontalAlignment="Center"
            Command="{Binding IncrementCommand}"
            Content="Increment Counter by Step Size" />
  </StackPanel>
</Page>
```

[!INCLUDE [View Model](include-wrap.md)]

If you want to see the completed application, you can download the source code from [GitHub](https://github.com/unoplatform/Uno.GettingStartedTutorial/tree/master/src/Counter/XAML-MVVM).
