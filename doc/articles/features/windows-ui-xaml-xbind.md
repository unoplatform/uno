---
uid: Uno.Features.WinUIxBind
---

# Uno Support for x:Bind

Uno supports the [`x:Bind`](https://learn.microsoft.com/windows/uwp/xaml-platform/x-bind-markup-extension) WinUI feature, which gives the ability to:

- bind to normal fields and properties
- static classes fields
- functions with multiple parameters
- events
- `x:Bind` on _"Plain-old C# Objects"_ (POCO) created in XAML

## Examples

- Properties
  - Page or control property

    ```xml
    <TextBlock Text="{x:Bind MyProperty}" />
    ```

  - Member function

    ```xml
    <TextBlock Text="{x:Bind MyProperty.ToUpper()}" />
    ```

  - Static types field or properties OneTime binding

    ```xml
    <TextBlock Text="{x:Bind local:StaticType.PropertyIntValue}" />
    ```

  - OneWay local member function with multiple observable parameters

    ```xml
    <TextBlock Text="{x:Bind Multiply(slider1.Value, slider2.Value), Mode=OneWay}" />
    ```

  - OneWay static class function with  multiple observable parameters

    ```xml
    <TextBlock Text="{x:Bind local:StaticType.Add(slider1.Value, slider2.Value), Mode=OneWay}" />
    ```

  - Literal boolean parameters (`x:True`, `x:False`)

    ```xml
    <TextBlock Text="{x:Bind BoolFunc(x:False)}" />
    ```

  - Null parameter (`x:Null`)

    ```xml
    <TextBlock Text="{x:Bind TestString(x:Null)}" />
    ```

  - Quote escaping

    ```xml
    <TextBlock Text="{x:Bind sys:String.Format('{0}, ^'{1}^'', InstanceProperty, StaticProperty)}" />
    ```

  - Literal numeric value

    ```xml
    <TextBlock Text="{x:Bind Add(InstanceProperty, 42.42)}" />
    ```

- Use of system functions (given `xmlns:sys="using:System"`):
  - Single parameter formatting:

    ```xml
    <TextBlock Text="{x:Bind sys:String.Format('Formatted {0}', MyProperty), Mode=OneWay}" />
    ```

  - Multi parameters formatting:

    ```xml
    <TextBlock Text="{x:Bind sys:String.Format(x:Null, 'slider1: {0}, slider2:{1}', slider1.Value, slider2.Value), Mode=OneWay}" />
    ```

  - TimeParsing:

    ```xml
    <CalendarDatePicker Date="{x:Bind sys:DateTime.Parse(TextBlock1.Text)}" />
    ```

- Use of `BindBack`

  ```xml
  <TextBlock Text="{x:Bind sys:String.Format('{0}', MyInteger), BindBack=BindBackMyInteger, Mode=TwoWay}" />
  ```

  where this methods is available in the control:

  ```csharp
  public void BindBackMyInteger(string text)
  {
    MyInteger = int.Parse(text);
  }
  ```

- Bind to events

  ```xml
  <CheckBox Checked="{x:Bind OnCheckedRaised}" Unchecked="{x:Bind OnUncheckedRaised}" />
  ```

  where these methods are available in the code behind:

  ```csharp
  public void OnCheckedRaised() { }
  public void OnUncheckedRaised(object sender, RoutedEventArgs args) { }
  ```

- [Attached Properties](https://learn.microsoft.com/windows/uwp/xaml-platform/x-bind-markup-extension#attached-properties)

  ```xml
  <Button x:Name="Button22" Content="Click me!" Grid.Row="42" />
  <TextBlock Text="{x:Bind Button22.(Grid.Row)}" />
  ```

- Type casts

  - ```xml
    <TextBox FontFamily="{x:Bind (FontFamily)MyComboBox.SelectedValue}" />
    ```

  - ```xml
    <TextBox Text="{x:Bind (x:String)MyObject}" />
    ```

  - ```xml
    <TextBox Text="{x:Bind MyFunction((x:String)MyObject, (x:String)MyObject)}" />
    ```

  - ```xml
    <TextBox Tag="{x:Bind ((x:String)MyObject).Length}" />
    ```

  where this methods is available in the code behind:

  ```csharp
  public void MyFunction(string p1, string p2) { }
  ```

- [Pathless casting](https://learn.microsoft.com/windows/uwp/xaml-platform/x-bind-markup-extension#pathless-casting)

  ```xml
  <Page
    x:Class="AppSample.MainPage"
    ...
    xmlns:local="using:AppSample">

    <Grid>
        <ListView ItemsSource="{x:Bind Songs}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:SongItem">
                    <TextBlock
                        Margin="12"
                        FontSize="40"
                        Text="{x:Bind local:MainPage.GenerateSongTitle((local:SongItem))}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
  </Page>
  ```

- `x:Load` binding

  ```xml
  <TextBox x:Load="{x:Bind IsMyControlVisible}" />
  ```

  See the [{x:Bind} markup extension from WinUI documentation](https://learn.microsoft.com/windows/uwp/xaml-platform/x-bind-markup-extension) for more details.
