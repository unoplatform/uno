
The layout for the **MainPage** is defined in the **MainPage.xaml** file. This file contains the XAML markup that defines the layout of the application.

```xml
<Page x:Class="Counter.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:Counter"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
  <StackPanel HorizontalAlignment="Center"
              VerticalAlignment="Center">
    <TextBlock AutomationProperties.AutomationId="HelloTextBlock"
               Text="Hello Uno Platform"
               HorizontalAlignment="Center" />
  </StackPanel>
</Page>
```
