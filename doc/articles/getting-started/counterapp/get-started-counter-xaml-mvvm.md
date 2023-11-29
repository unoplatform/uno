---
uid: Uno.GettingStarted.Counter.XAML.MVVM
---

[!INCLUDE [Intro](include-intro.md)]

In this tutorial you will learn how to:

- Create a new Project with Uno Platform using Visual Studio Template Wizard or the `dotnet new` command
- Add elements to the XAML file to define the layout of the application
- Add code to the C# file to implement the application logic using the Model-View-ViewModel (MVVM) pattern
- Use data binding to connect the UI to the application logic

To complete this tutorial you don't need any prior knowledge of the Uno Platform, XAML, or C#. 

[!INCLUDE [VS](include-vs.md)]

- Select the `Presentation` tab and choose `MVVM`

- Select the `Markup` tab and choose `XAML`

[!INCLUDE [VS](include-cli.md)]

```
dotnet new unoapp -preset blank -presentation mvvm -markup xaml -o Counter
```

[!INCLUDE [VS](include-cli2.md)]


![Counter Solution](Assets/counter-solution-xaml.png) 

Before proceeding you should select a target platform and run the application. Follow these links for more information on debugging an application for [Visual Studio](xref:Uno.GettingStarted.CreateAnApp.VS2022), [Visual Studio Code](xref:Uno.GettingStarted.CreateAnApp.VSCode) or [Rider](xref:Uno.GettingStarted.CreateAnApp.Rider).

## MainWindow and MainPage

The majority of an Uno Platform application is defined in a class library project, in this case, named `Counter`. This project contains the XAML files that define the layout of the application and the C# files that implement the application logic. There are also platform-specific projects, in the `Platforms` folder, that contain the platform-specific code required to run the application on each platform.

The startup logic for the application is contained in the `app.cs` file in the `Counter` project. In the `OnLaunched` method, the `MainWindow` of the application is initialized with a `Frame`, used for navigation between pages, and the `MainPage` is set as the initial page.

The layout for the `MainPage` is defined in the `MainPage.xaml` file. This file contains the XAML markup that defines the layout of the application.

```xml
<Page x:Class="Counter.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Counter"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
  <StackPanel
        HorizontalAlignment="Center"
        VerticalAlignment="Center">
    <TextBlock AutomationProperties.AutomationId="HelloTextBlock"
          Text="Hello Uno Platform"
          HorizontalAlignment="Center" />
  </StackPanel>
</Page>
```

This defines a page with a background set to the theme resource `ApplicationPageBackgroundThemeBrush`, meaning it will adapt to the theme (`Dark` or `Light`) of the application. 

The page contains a `StackPanel`, which will lay out controls in a vertical stack and is aligned in the center of the page, both horizontally and vertically. The `StackPanel` contains a single `TextBlock` control, which displays the text `Hello Uno Platform` and is aligned in the horizontal center of the `StackPanel`.

## Add a Control

We're going to replace the existing `TextBlock` with an `Image` but before we can do this, we need to add the image file to the application. Download [this SVG image]('Assets/icon.svg') and add it to the `Assets` folder inside the `Counter` project. At this point, you should rebuild the application in order for the image to be included in the application package.

> [!NOTE] 
> If you're working in Visual Studio, select the newly added icon.svg file in the `Solution Explorer`, open the `Properties` tool window, and make sure the `Build Action` property is set to `UnoImage`. For other IDEs, no further action is required as the `unoapp` template automatically sets the `Build Action` to `UnoImage` for all files in the `Assets` folder.

Including SVG files with the `UnoImage` build action will use `Uno.Resizetizer` to convert the SVG file to a PNG file for each platform. The generated PNG files will be included in the application package and used at runtime. For more information on using `Uno.Resizetizer` in Uno Platform, see [Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted). 

Now that we have the image file, we can replace the `TextBlock` with an `Image`. 

- Open the `MainPage.xaml` file in the `Counter` project
- Replace the `TextBlock` with the following `Image` element

    ```xml
    <Image Width="150"
            Height="150"
            Source="Assets/logo.png" />
    ```
- The `Width` and `Height` have been set on the `Image` to ensure the image is displayed at the correct size. The `Source` property has been set to the path of the image file inside the `Counter` project.

Run the application to see the updated `MainPage`. You should see the image displayed in the center of the page. Keep the application running whilst completing the rest of this tutorial. Hot Reload is used to automatically update the running application as you make changes to both XAML and C# code. For more information on Hot Reload, see [Hot Reload](xref:Uno.Features.HotReload).

## Change Layout

The layout of the application uses a `StackPanel` which allows multiple controls to be added as children and will layout them in a vertical stack. An alternative to the `StackPanel` that is often used to control layout within an Uno Platform application is the `Grid`. The `Grid` allows controls to be laid out in rows and columns, and is often used to create more complex layouts.

A `StackPanel` is a good choice for this application as we want the controls to be laid out vertically, one above the other. Let's go ahead and add the remaining controls for the counter.

- Update the `StackPanel` to remove the `HorizontalAlignment` property, as we'll be centering each of the nested elements individually.

    ```xml
    <StackPanel VerticalAlignment="Center">
    ```

- Update the `Image` element to center it horizontally and add a margin.

    ```xml
    <Image Width="150"
            Height="150"
            Margin="12"
            HorizontalAlignment="Center"
            Source="Assets/logo.png" />
    ```

- Add a `TextBox` to allow the user to enter the step size.

    ```xml
    <TextBox Margin="12"
            HorizontalAlignment="Center"
            PlaceholderText="Step Size"
            Text="1"
            TextAlignment="Center" />
    ```

- Add a `TextBlock` to display the current counter value.

    ```xml
    <TextBlock Margin="12"
                HorizontalAlignment="Center"
                TextAlignment="Center"
                Text="Counter: 1" />
    ```

- Add a `Button` to increment the counter.

    ```xml
    <Button Margin="12"
            HorizontalAlignment="Center"
            Content="Increment Counter by Step Size" />
    ```

## ViewModel

So far all the elements we've added to the `MainPage` have had their content set directly in the XAML. This is fine for static content, but for dynamic content, we need to use data binding. Data binding allows us to connect the UI to the application logic, so that when the application logic changes, the UI is automatically updated.

As part of creating the application, we selected MVVM as the presentation framework. This added a reference to the [`CommunityToolkit.Mvvm`](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) package which provides a base class called `ObservableObject` which implements the `INotifyPropertyChanged` interface. This interface is used to notify the UI when a property has changed so that the UI can be updated.

- Add a new class, `MainViewModel`, to the `Counter` project
- Update the `MainViewModel` class to be a `partial` class and inherit from `ObservableObject`.

    ```csharp
    internal partial class MainViewModel : ObservableObject
    {
    }
    ```

- Add fields, `_count` and `_step`, to the `MainViewModel` class. These fields both have the `ObservableProperty` attribute applied, which will generate matching properties, `Count` and `Step`, that will automatically raise the `PropertyChanged` event when their value is changed.

    ```csharp
    [ObservableProperty]
    private int _count = 0;

    [ObservableProperty]
    private int _step = 1;
    ```

- Add a method `Increment` to the `MainViewModel` that will increment the counter by the step size. The `RelayCommand` attribute will generate a matching `ICommand` property, `IncrementCommand`, that will call the `Increment` method when the `ICommand.Execute` method is called.

    ```csharp
    [RelayCommand]
    private void Increment()
        => Count += Step;
    ```

The final code for the `MainViewModel` class should look like this:

```csharp
namespace Counter;

internal partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _count = 0;

    [ObservableProperty]
    private int _step = 1;

    [RelayCommand]
    private void Increment()
        => Count += Step;
}
```

## Data Binding

Now that we have the `MainViewModel` class, we can update the `MainPage` to use data binding to connect the UI to the application logic.

- Add a `DataContext` element to the `Page` element in the `MainPage.xaml` file.

    ```xml
    <Page.DataContext>
        <local:MainViewModel />
    </Page.DataContext>
    ```

- Update the `TextBlock` by removing the `Text` attribute, replacing it with two `Run` elements, and binding the `Text` property of the second `Run` element to the `Count` property of the `MainViewModel`.

    ```xml
    <TextBlock
        Margin="12"
        HorizontalAlignment="Center"
        TextAlignment="Center">
        <Run Text="Counter: " /><Run Text="{Binding Count}" />
    </TextBlock>
    ```
- Update the `TextBox` by binding the `Text` property to the `Step` property of the `MainViewModel`. The Mode of the binding is set to `TwoWay` so that the `Step` property is updated when the user changes the value in the `TextBox`.

    ```xml
    <TextBox Margin="12"
            HorizontalAlignment="Center"
            PlaceholderText="Step Size"
            Text="{Binding Step, Mode=TwoWay}"
            TextAlignment="Center" />
    ```

- Update the `Button` to add a `Command` attribute that is bound to the `IncrementCommand` property of the `MainViewModel`.

    ```xml
    <Button Margin="12"
            HorizontalAlignment="Center"
            Command="{Binding IncrementCommand}"
            Content="Increment Counter by Step Size" />
    ```

The final code for `MainPage.xaml` should look like this:

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

## Wrap Up

At this point, you should have a working counter application. Try changing the step size and clicking the button to increment the counter.

If you want to see the completed application, you can download the source code from [GitHub](https://github.com/unoplatform/Uno.GettingStartedTutorial/tree/master/src/Counter)




