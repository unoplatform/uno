# Material DatePicker and TimePicker on Android

After following the [Getting Started Tutorial](./material-design.md), there are a few follow up steps to properly configure native colors for the `DatePicker` and `TimePicekr` control on Android.

Note: These changes are across the whole application, all `TimePicker` and `DatePicker` controls will have these themes.

In your Android project head, navigate to `YourProject.Droid/Resources/values/Styles.xml`.

Update your `AppTheme` style component to include two new `Item` elements:
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<style name="AppTheme" parent="Theme.AppCompat.Light">

		<!-- Color style for Time/Date Pickers -->
		<item name="android:datePickerDialogTheme">@style/AppCompatDialogStyle</item>
		<item name="android:timePickerDialogTheme">@style/AppCompatDialogStyle</item>
	</style>

	<style name="AppCompatDialogStyle" parent="Theme.AppCompat.Light.Dialog">
		<item name="colorAccent">@color/MaterialPrimaryColor</item>
	</style>
</resources>
```

### Add Light/Dark Color Palettes

1. In your `Styles.xml` file, set the `AppTheme` Style's `parent` attribute to `Theme.Compat.DayNight`
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<style name="AppTheme" parent="Theme.AppCompat.DayNight">

		<!-- Color style for Time/Date Pickers -->
		<item name="android:datePickerDialogTheme">@style/AppCompatDialogStyle</item>
		<item name="android:timePickerDialogTheme">@style/AppCompatDialogStyle</item>
	</style>

	<style name="AppCompatDialogStyle" parent="Theme.AppCompat.DayNight.Dialog">
		<item name="colorAccent">@color/MaterialPrimaryColor</item>
	</style>
</resources>
```

2. In your Android project head, navigate to `YourProject.Droid/Resources/values` and create a file called `colors.xml`. Include your "Light" theme colors here.
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<color name="MaterialPrimaryColor">#5B4CF5</color>
</resources>
```

3. Navigate to `YourProject.Droid/Resources` and create a folder called `values-night`. Inside this folder, create a file called `colors.xml`. Include your "Dark" theme colors here. 
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<color name="MaterialPrimaryColor">#B6A8FB</color>
</resources>
```
