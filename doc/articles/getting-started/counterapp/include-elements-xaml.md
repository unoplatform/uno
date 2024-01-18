
- Update the **`StackPanel`** to remove the **`HorizontalAlignment`** property, as we'll be centering each of the nested elements individually.

    ```xml
    <StackPanel VerticalAlignment="Center">
    ```

- Update the **`Image`** element to center it horizontally and add a margin.

    ```xml
    <Image Width="150"
           Height="150"
           Margin="12"
           HorizontalAlignment="Center"
           Source="Assets/logo.png" />
    ```

- Add a **`TextBox`** to allow the user to enter the step size.

    ```xml
    <TextBox Margin="12"
             HorizontalAlignment="Center"
             PlaceholderText="Step Size"
             Text="1"
             TextAlignment="Center" />
    ```

- Add a **`TextBlock`** to display the current counter value.

    ```xml
    <TextBlock Margin="12"
               HorizontalAlignment="Center"
               TextAlignment="Center"
               Text="Counter: 1" />
    ```

- Add a **`Button`** to increment the counter.

    ```xml
    <Button Margin="12"
            HorizontalAlignment="Center"
            Content="Increment Counter by Step Size" />
    ```
