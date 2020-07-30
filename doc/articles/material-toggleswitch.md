# Material ToggleSwitch on Android

After following the [Getting Started Tutorial](./material-design.md), there are a few follow up steps to properly configure native colors for the `ToggleSwitch` control on Android.
This will allow you to display native Android shadow styles when the `ToggleSwitch` control is set to `Off` and native Android shadow colors when the `ToggleSwitch` control is clicked.

Note: These changes are across the whole application, all `ToggleSwitch` controls will have these themes.

In your Android platform head, navigate to your `Styles` file `yourProject.Droid/Resources/values/Styles.xml`. In the `AppTheme` stlye, add the following `Item` elements:
``` xaml
<item name="colorControlActivated">@color/MaterialPrimaryColor</item>
<item name="colorSwitchThumbNormal">@color/MaterialSurfaceVariantColor</item>
```

### Add Light/Dark Color Palettes

1. In your `Styles.xml` file, set the `AppTheme` Style's `parent` attribute to `Theme.Compat.DayNight`
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<style name="AppTheme" parent="Theme.AppCompat.DayNight">

		<!-- Color style for toggle switch -->
		<item name="colorControlActivated">@color/MaterialPrimaryColor</item>
		<item name="colorSwitchThumbNormal">@color/MaterialSurfaceVariantColor</item>
	</style>
</resources>
```

2. In your Android project head, navigate to `YourProject.Droid/Resources/values` and create a file called `colors.xml`. Include your "Light" theme colors here.
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<color name="MaterialPrimaryColor">#5B4CF5</color>
	<!-- SurfaceColor -->
	<color name="MaterialSurfaceVariantColor">#FFFFFF</color>
</resources>
```

3. Navigate to `YourProject.Droid/Resources` and create a folder called `values-night`. Inside this folder, create a file called `colors.xml`. Include your "Dark" theme colors here. 
``` xaml
<?xml version="1.0" encoding="utf-8" ?>
<resources>
	<color name="MaterialPrimaryColor">#B6A8FB</color>
	<!-- A variant of OnSurfaceMediumColor without alpha opacity (can't use alphas with android colors)  -->
	 <color name="MaterialSurfaceVariantColor">#808080</color>
</resources>
```

4. (Optional) If you have updated the `material color palette` (step 1), you will need to update two further `color` attributes to apply the native `ToggleSwitch` `Disabled` colors.

   `PrimaryVariantDisabledThumbColor` and `SurfaceVariantLightColor` can be overridden in the `colors.xaml` file.

   `PrimaryVariantDisabledThumbColor` is a non-transparent version of the ("Light") `PrimaryDisabled` color in the "Light" palette, and a non-transparent version of ("Dark") `PrimaryMedium` color in the "Dark" palette.

   `SurfaceVariantLightColor` is the Surface color, however, in "Light" Palette it is an off-white color in order to be visible on light backgrounds.
``` xaml
<!-- Variant Colors: Needed for android thumbtints. If a thumbtint color contains opacity, it will actually turn the thumb transparent. (Unwanted behavior) -->
	<ResourceDictionary.ThemeDictionaries>

		<!-- Light Theme -->
		<ResourceDictionary x:Key="Light">
			<!-- Non-opaque/transparent primary disabled color -->
			<Color x:Key="PrimaryVariantDisabledThumbColor">#E9E5FA</Color>
			<!-- Non-opaque/transparent white color that shows on white surfaces -->
			<Color x:Key="SurfaceVariantLightColor">#F7F7F7</Color>
		</ResourceDictionary>

		<!-- Dark Theme -->
		<ResourceDictionary x:Key="Dark">
			<!-- Non-opaque/transparent primary medium color -->
			<Color x:Key="PrimaryVariantDisabledThumbColor">#57507C</Color>
			<Color x:Key="SurfaceVariantLightColor">#121212</Color>
		</ResourceDictionary>
	</ResourceDictionary.ThemeDictionaries>
```


