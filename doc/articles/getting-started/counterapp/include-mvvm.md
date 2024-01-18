
## ViewModel

So far all the elements we've added to the **MainPage** have had their content set directly. This is fine for static content, but for dynamic content, we need to use data binding. Data binding allows us to connect the UI to the application logic, so that when the application logic changes, the UI is automatically updated.

As part of creating the application, we selected MVVM as the presentation framework. This added a reference to the [**MVVM Toolkit**](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) package which provides a base class called `ObservableObject` which implements the `INotifyPropertyChanged` interface. This interface is used to notify the UI when a property has changed so that the UI can be updated.

- Add a new class, `MainViewModel`, to the **Counter** project.
- Update the **MainViewModel** class to be a `partial` class and inherit from **ObservableObject**.

    ```csharp
    internal partial class MainViewModel : ObservableObject
    {
    }
    ```

- Add the `_count` and `_step` fields to the **MainViewModel** class. These fields both have the `ObservableProperty` attribute applied, which will generate matching properties, `Count` and `Step`, that will automatically raise the `PropertyChanged` event when their value is changed.

    ```csharp
    [ObservableProperty]
    private int _count = 0;

    [ObservableProperty]
    private int _step = 1;
    ```

- Add a method called `Increment` to the `MainViewModel` that will increment the counter by the step size. The `RelayCommand` attribute will generate a matching `ICommand` property, `IncrementCommand`, that will call the `Increment` method when the `ICommand.Execute` method is called.

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
