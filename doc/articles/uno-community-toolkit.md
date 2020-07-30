# Port of  Windows Community Toolkit
The [Windows Community Toolkit](https://docs.microsoft.com/en-us/windows/communitytoolkit/) is a collection of helper functions, custom controls, and app services. It simplifies and demonstrates common developer patterns when building experiences for Windows 10.

Uno has ported the Windows Community Toolkit for use in Uno applications to allow for use on Windows,
Android, iOS, macOS, and WebAssembly.

The following packages are available:
- [Uno.Microsoft.Toolkit](https://www.nuget.org/packages/Uno.Microsoft.Toolkit )
- [Uno.Microsoft.Toolkit.Parsers](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Parsers)
- [Uno.Microsoft.Toolkit.Services](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Services)
- [Uno.Microsoft.Toolkit.Notifications](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Notifications)
- [Uno.Microsoft.Toolkit.UWP](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.UWP)
- [Uno.Microsoft.Toolkit.Uwp.Services](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Uwp.Services)
- [Uno.Microsoft.Toolkit.Uwp.UI.Controls.DataGrid](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.UWP.UI.DataGrid)
- [Uno.Microsoft.Toolkit.Uwp.UI](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.UWP.UI)
- [Uno.Microsoft.Toolkit.Uwp.UI.Animations](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Uwp.UI.Animations)
- [Uno.Microsoft.Toolkit.Uwp.UI.Controls](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Uwp.UI.Controls)
- [Uno.Microsoft.Toolkit.Uwp.UI.Controls.Graph](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.UWP.UI.Controls.Graph)
- [Uno.Microsoft.Toolkit.Uwp.Connectivity](https://www.nuget.org/packages/Uno.Microsoft.Toolkit.Uwp.Connectivity)

## Add Windows Community Toolkit to Uno Project
This tutorial will walk through adding and implementing the DataGrid control but the steps can be followed for any of the Uno ported Windows Community Toolkit controls.  
  
1. Install Nuget package for targeted control  
 ![datagrid-nuget](uno-development/assets/controls/datagrid-nuget.JPG)  
Note: Be aware of which versions of Uno.UI are compatible with the Nuget package of each control.  
      If using version `3.0.0` or higher of `Uno.UI`, use version `6.1.0` or higher of `DataGrid` Nuget package.
      

2. Add a reference to the UWP UI Controls 

   In XAML:  
    ```xmlns:controls="using:Microsoft.Toolkit.Uwp.UI.Controls"```  
  
   In C#:  
    ```using Microsoft.Toolkit.Uwp;```

3. Implement the control.

## Add the DataGrid Control 

This control will create an easily organized grid that will allow you to create flexible columns and rows.

1. Begin by adding the control using the syntax below. Change the `x:Name` to the name of your DataGrid.  
```<controls:DataGrid x:Name="dataGrid"></controls:DataGrid>```

2. Add columns. Similar to how you would configure columns for a XAML `Grid` layout, you can add column definitions within your `DataGrid` control:
   ``` xaml
<controls:DataGrid.Columns>
    <controls:DataGridTextColumn Header="Rank"/>
    <controls:DataGridComboBoxColumn Header="Mountain"/>
</controls:DataGrid.Columns>
    ```

    This will create two columns that can be adjusted by the user  

![datagrid-column-gif](uno-development/assets/controls/datagrid-basic-columns.gif)

Alternatively, you can use the `AutoGenerateColumns` attribute on your `DataGrid` control if you do not know how many columns your data will require.  
``` xml
<controls:DataGrid x:Name="dataGrid" AutoGenerateColumns="True" />
```

3. Format your rows in the same way as your columns or use a `DataTemplate` added as an attribute on the `DataGrid` control  
``` xml
<controls:DataGrid x:Name="dataGrid" RowDetailsTemplate="{StaticResource RowDetailsTemplate}">
```

4. Data can be added with data binding. 

First, add your `ItemsSource` as a property of your `DataGrid` control.  
``` xml
<controls:DataGrid x:Name="dataGrid" ItemsSource="{x:Bind MyViewModel.Customers}" />  
```
Then, set the binding on each column  
``` xml
<controls:DataGrid.Columns>
    <controls:DataGridTextColumn Header="Rank" Binding="{Binding Rank}" Tag="Rank" />
    <controls:DataGridTextColumn Header="Mountain" Binding="{Binding Mountain}" Tag="Mountain" />
</controls:DataGrid.Columns>
```  

![datagrid-full-sample](uno-development/assets/controls/datagrid-full-sample.gif)


For a deeper dive into the code, check out the [full Uno Windows Community Toolkit Sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/UnoWCTDataGridSample) in [Uno.Samples](https://github.com/unoplatform/Uno.Samples).
