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

[!INCLUDE [VS](include-mainwindow.md)]

[!INCLUDE [VS](include-mainpage-xaml.md)]

[!INCLUDE [VS](include-mainpage-layout.md)]

[!INCLUDE [VS](include-mainpage-image-xaml.md)]

[!INCLUDE [VS](include-mainpage-change-layout.md)]

[!INCLUDE [VS](include-mainpage-elements-xaml.md)]


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




