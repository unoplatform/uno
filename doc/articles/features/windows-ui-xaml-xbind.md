# Uno Support for x:Bind

Uno supports the [`x:Bind`](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension) WinUI feature, which gives the ability to:
- bind to normal fields and properties
- static classes fields
- functions with multiple parameters
- events
- `x:Bind` on _"Plain-old C# Objects"_ (POCO) created in XAML

# Examples
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
  ```
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

- Type casts
  ```xml
  <TextBox FontFamily="{x:Bind (FontFamily)MyComboBox.SelectedValue}" />
  ```

- `x:Load` binding
  ```xml
  <TextBox x:Load="{x:Bind IsMyControlVisible}" />
  ```
  See the [WinUI documentation](https://docs.microsoft.com/en-us/windows/uwp/xaml-platform/x-load-attribute) for more details.
