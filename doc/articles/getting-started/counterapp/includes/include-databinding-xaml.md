---
uid: Uno.Workshops.Counter.Xaml.DataBinding-Inline
---
- Add a **`DataContext`** element to the **`Page`** element in the **MainPage.xaml** file, between the first `StackPanel` and the `Page` element.

    ```xml
    <Page.DataContext>
        <local:MainViewModel />
    </Page.DataContext>
    ```

- Update the **`TextBlock`** by removing the **`Text`** attribute, replacing it with two **`Run`** elements, and binding the **`Text`** property of the second **`Run`** element to the **`Countable.Count`** property of the **`MainViewModel`**.

    ```xml
    <TextBlock Margin="12"
               HorizontalAlignment="Center"
               TextAlignment="Center">
        <Run Text="Counter: " /><Run Text="{Binding Countable.Count}" />
    </TextBlock>
    ```

- Update the **`TextBox`** by binding the **`Text`** property to the **`Countable.Step`** property of the **MainViewModel**. The **`Mode`** of the binding is set to **`TwoWay`** so that the **`Countable.Step`** property is updated when the user changes the value in the **`TextBox`**.

    ```xml
    <TextBox Margin="12"
             HorizontalAlignment="Center"
             PlaceholderText="Step Size"
             Text="{Binding Countable.Step, Mode=TwoWay}"
             TextAlignment="Center" />
    ```

- Update the **`Button`** to add a **`Command`** attribute that is bound to the **`IncrementCounter`** task of the **`MainViewModel`**.

    ```xml
    <Button Margin="12"
            HorizontalAlignment="Center"
            Command="{Binding IncrementCounter}"
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
             Text="{Binding Countable.Step, Mode=TwoWay}"
             TextAlignment="Center" />

    <TextBlock Margin="12"
               HorizontalAlignment="Center"
               TextAlignment="Center">
        <Run Text="Counter: " /><Run Text="{Binding Countable.Count}" />
    </TextBlock>

    <Button Margin="12"
            HorizontalAlignment="Center"
            Command="{Binding IncrementCounter}"
            Content="Increment Counter by Step Size" />
  </StackPanel>
</Page>
```