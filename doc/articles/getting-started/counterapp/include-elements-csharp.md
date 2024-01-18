
- Update the **`StackPanel`** to remove the **`HorizontalAlignment`** property, as we'll be centering each of the nested elements individually.

    ```csharp
    new StackPanel()
        .VerticalAlignment(VerticalAlignment.Center)
    ```

- Update the **`Image`** element to center it horizontally and add a margin.

    ```csharp
    new Image()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .Width(150)
        .Height(150)
        .Source("ms-appx:///Counter/Assets/logo.png")
    ```

- Add a **`TextBox`** to allow the user to enter the step size.

    ```csharp
    new TextBox()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
        .PlaceholderText("Step Size")
        .Text("1")
    ```

- Add a **`TextBlock`** to display the current counter value.

    ```csharp
    new TextBlock()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
        .Text("Counter: 1"),
    ```

- Add a **`Button`** to increment the counter.

    ```csharp
    new Button()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .Content("Increment Counter by Step Size")
    ```
