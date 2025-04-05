---
uid: Uno.Workshops.Counter.DataBinding
---
- Let's add the **`DataContext`** to our page. To do so, add `.DataContext(new MainViewModel(), (page, vm) => page` before `.Background(...)`. Remember to close the **`DataContext`** expression with a `)` at the end of the code. It should look similar to the code below:

    ```csharp
    this.DataContext(new MainViewModel(), (page, vm) => page
        .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
        .Content(
            ...
        )
    );
    ```

- Update the **`TextBlock`** by removing its current text content and replacing it with a binding expression for the **`Countable.Count`** property of the **`MainViewModel`**. Modify the existing **`Text`** property with `() => vm.Countable.Count, txt => $"Counter: {txt}"`. The adjusted code is as follows:

    ```csharp
    new TextBlock()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
        .Text(() => vm.Countable.Count, txt => $"Counter: {txt}")
    ```

- Update the **`TextBox`** by binding the **`Text`** property to the **`Countable.Step`** property of the **MainViewModel**. The **`Mode`** of the binding is set to **`TwoWay`** so that the **`Countable.Step`** property is updated when the user changes the value in the **`TextBox`**.

    ```csharp
    new TextBox()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
        .PlaceholderText("Step Size")
        .Text(x => x.Binding(() => vm.Countable.Step).TwoWay())
    ```

- Update the **`Button`** to add a **`Command`** property that is bound to the **`IncrementCounter`** task of the **`MainViewModel`**.

    ```csharp
    new Button()
        .Margin(12)
        .HorizontalAlignment(HorizontalAlignment.Center)
        .Command(() => vm.IncrementCounter)
        .Content("Increment Counter by Step Size")
    ```

- The final code for **MainPage.cs** should look like this:

    ```csharp
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.DataContext(new MainViewModel(), (page, vm) => page
                .Background(ThemeResource.Get<Brush>("ApplicationPageBackgroundThemeBrush"))
                .Content(
                    new StackPanel()
                        .VerticalAlignment(VerticalAlignment.Center)
                        .Children(
                            new Image()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Width(150)
                                .Height(150)
                                .Source("ms-appx:///Assets/logo.png"),
                            new TextBox()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                                .PlaceholderText("Step Size")
                                .Text(x => x.Binding(() => vm.Countable.Step).TwoWay()),
                            new TextBlock()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .TextAlignment(Microsoft.UI.Xaml.TextAlignment.Center)
                                .Text(() => vm.Countable.Count, txt => $"Counter: {txt}"),
                            new Button()
                                .Margin(12)
                                .HorizontalAlignment(HorizontalAlignment.Center)
                                .Command(() => vm.IncrementCounter)
                                .Content("Increment Counter by Step Size")
                        )
                )
            );
        }
    }
    ```
