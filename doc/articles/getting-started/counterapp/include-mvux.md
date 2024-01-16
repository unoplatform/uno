
## ViewModel

So far all the elements we've added to the **MainPage** have had their content set directly. This is fine for static content, but for dynamic content, we need to use data binding. Data binding allows us to connect the UI to the application logic, so that when the application logic changes, the UI is automatically updated.

As part of creating the application, we selected MVUX as the presentation framework. This added a reference to [**MVUX**](https://aka.platform.uno/mvux) which is responsible for dealing with our Models and generating the ViewModels.

- Add a new class, `MainModel`, to the **Counter** project.
- Update the `MainModel` class to be a `partial record`.

    ```csharp
    internal partial record MainModel
    {
    }
    ```

- Add the `Count` and `Step` properties, to the `MainModel` class. These fields both must be of type `IState<int>` and to initialize them we use `=> State.Value(...)`.

    ```csharp
    public IState<int> Count 
        => State.Value(this, () => 0);

    public IState<int> Step 
        => State.Value(this, () => 1);
    ```

- Add a method called `IncrementCommand` to the `MainModel` that will increment the counter by the step size. The generated ViewModel of our `MainModel` will automatically re-expose as `ICommand` the `IncrementCommand` method.

    ```csharp
    public ValueTask IncrementCommand(int Step)
        => Count.Update(c => c + Step, CancellationToken.None);
    ```

The final code for the `MainModel` class should look like this:

```csharp
namespace Counter;

internal partial record MainModel
{
    public IState<int> Count => State.Value(this, () => 0);

    public IState<int> Step => State.Value(this, () => 1);

    public ValueTask IncrementCommand(int Step)
        => Count.Update(c => c + Step, CancellationToken.None);
}
```
