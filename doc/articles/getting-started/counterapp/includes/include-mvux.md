## MainModel

So far, all the elements we've added to the **MainPage** have had their content set directly. This is fine for static content, but for dynamic content, we need to use data binding. Data binding allows us to connect the UI to the application logic, so that when the application logic changes, the UI is automatically updated.

As part of creating the application, we selected MVUX as the presentation framework. This added a reference to [**MVUX**](https://aka.platform.uno/mvux) which is responsible for managing our Models and generating the necessary bindings.

- Add a new class named `MainModel`.
- Update the `MainModel` class to be a `partial record`.

    ```csharp
    internal partial record MainModel
    {
    }
    ```

- Add a new `partial record` above named `Countable`. This record will be responsible for updating our counter's properties all while ensuring immutability. Learn more about [immutable records](xref:Uno.Extensions.Mvux.Records#how-to-create-immutable-records).

    ```csharp
    internal partial record Countable
    {
    }
    ```

- Add the `Count` and `Step` properties to the `Countable`'s primary constructor.

    ```csharp
    internal partial record Countable(int Count, int Step)
    {
    }
    ```

- Add an `Increment` method to the `Countable` record. The `with` operator allows us to [create a new instance](xref:Uno.Extensions.Mvux.Records#updating-records) of the object.

    ```csharp
    public Countable Increment() => this with
    {
        Count = Count + Step
    };
    ```

- Add the newly created `Countable` as a property in the `MainModel` class. The type must be `IState<Countable>` and we use `=> State.Value(...)` to initialize it.

    ```csharp
    public IState<Countable> Countable => State.Value(this, () => new Countable(0, 1));
    ```

- Add a method named `IncrementCounter` to the `MainModel` that will in turn call the `Countable`'s `Increment` method and therefore update the counter. You can find more information on [commands in MVUX](xref:Uno.Extensions.Mvux.Advanced.Commands).

    ```csharp
    public ValueTask IncrementCounter()
        => Countable.UpdateAsync(c => c?.Increment());
    ```

The final code for the `MainModel` class should look like this:

```csharp

internal partial record Countable(int Count, int Step)
{
    public Countable Increment() => this with
    {
        Count = Count + Step
    };
}

internal partial record MainModel
{
    public IState<Countable> Countable => State.Value(this, () => new Countable(0, 1));

    public ValueTask IncrementCounter()
        => Countable.UpdateAsync(c => c?.Increment());
}
```
