# ComboBox in Uno.UI

The `ComboBox` is designed to select a value in a set of items. For more info about its usage, 
[please refer the microsoft documentation](https://docs.microsoft.com/en-us/windows/uwp/design/controls-and-patterns/combo-box)

## Customize the placement of the Drop-Down **UNO ONLY feature**

By default when opening a combo, UWP aligns the drop down in order to keep the selected item at the same location.
So it means that if the currently selected value is the last one in the list, the drop down will appear above the `ComboBox`.
If there isn't any selected item, the drop down will appear centered over the `ComboBox`.

On Uno you can change this behavior.

### Change the default value for all the `ComboBox` in you application

You can change the default placement for all your `ComboBox` by setting in the startip of your app (app.xaml.cs) the feature flag:

```cs
Uno.UI.FeatureConfiguration.ComboBox.DefaultDropDownPreferredPlacement = DropDownPlacement.Below;
```

### Change only for a specific `ComboBox`

```xaml
<Page
	[...]
	xmlns:not_win="using:Uno.UI.Xaml.Controls"
    mc:Ignorable="d not_win">

	<ComboBox
		ItemsSource="12345"
		not_win:ComboBox.DropDownPreferredPlacement="Below" />

```
