# Uno.Material Controls: Extra setup

## Table of Contents
- [ToggleSwitch](#toggleswitch)
- [DatePicker & TimePicker](#datepickers-and-timepickers)

## ToggleSwitch

> In order to get the proper native colors on android, there is some modification needed.
The reasoning for this is to apply the native android shadowing on the off value of the ToggleSwitch, and proper focus shadow colors when `ToggleSwitch`es are clicked.

1. Open `Resources/values/Styles.xml` inside the `.Droid` project.
    And, add two `<item>`s to under the `AppTheme`:
    - colorControlActivated: the on color for your ToggleSwitches thumb
    - colorSwitchThumbNormal: the off color for your ToggleSwitches thumb)
    > you may add your colors here directly, for example #ffffff, or by files (see our example code below)
    ```xml
    <?xml version="1.0" encoding="utf-8" ?>
    <resources>
        <style name="AppTheme" parent="Theme.AppCompat.Light">

            <!-- Color style for toggle switch -->
            <item name="colorControlActivated">@color/MaterialPrimaryColor</item>
            <item name="colorSwitchThumbNormal">@color/MaterialSurfaceVariantColor</item>
        </style>
    </resources>
    ```

1. (_optional_) If your application uses Light/Dark color palettes.
    1. Inside the `Styles.xml` file change the `AppTheme`'s parent to `Theme.Compat.DayNight`:
        ```diff
        -<style name="AppTheme" parent="Theme.AppCompat.Light">
        +<style name="AppTheme" parent="Theme.AppCompat.DayNight">
        ```
    1. Create a file named `colors.xml` under `Resources/values`.
        And, include your "Light" theme colors:
        ```xml
        <?xml version="1.0" encoding="utf-8" ?>
        <resources>
            <color name="MaterialPrimaryColor">#5B4CF5</color>
            <!-- SurfaceColor -->
            <color name="MaterialSurfaceVariantColor">#FFFFFF</color>
        </resources>
        ```
    1. Create a folder named `values-night` under `Resources/`
    1. Create a file named `colors.xml` under `Resources/values-night`
        And, include your "Dark" theme colors:
        ```xml
        <?xml version="1.0" encoding="utf-8" ?>
        <resources>
            <color name="MaterialPrimaryColor">#B6A8FB</color>
            <!-- A variant of OnSurfaceMediumColor without alpha opacity (can't use alphas with android colors)  -->
            <color name="MaterialSurfaceVariantColor">#808080</color>
        </resources>
        ```

1. (_optional_) If you have changed the material color palette for your application.
    Make sure these colors are updated as well in `ColorPaletteOverride.xaml`:
    - `MaterialToggleSwitchButtonColor`: knob color when unchecked (IsOn=false)
    - `MaterialSurfaceVariantLightColor`: knob color when checked (IsOn=true)
    - `MaterialToggleSwitchBackgroundColor`: Rail fill color when checked
    - `MaterialPrimaryVariantLowThumbColor`: Knob color when the control is disabled

## DatePickers and TimePickers

> If your application uses `DatePicker` and/or `TimePicker` (these are native components).
To apply your material colors to these android components, do the following (this will affect every `DatePicker`/`TimePicker` in the application).

1. Open `Resources/values/Styles.xml` inside the `.Droid` project.
    And, add two `<item>`s to under the `AppTheme`:
    - datePickerDialogTheme
    - timePickerDialogTheme

    And add a new `<style>` as shown below:
    ```xml
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

2. (_optional_) If your application uses Light/Dark color palettes.
    1. Inside the `Styles.xml` file change the `AppTheme`'s parent to `Theme.Compat.DayNight`:
        ```diff
        -<style name="AppTheme" parent="Theme.AppCompat.Light">
        +<style name="AppTheme" parent="Theme.AppCompat.DayNight">
        ```
    1. Create a file named `colors.xml` under `Resources/values`.
        And, include your "Light" theme colors:
        ```xml
        <?xml version="1.0" encoding="utf-8" ?>
        <resources>
            <color name="MaterialPrimaryColor">#5B4CF5</color>
        </resources>
        ```
    1. Create a folder named `values-night` under `Resources/`
    1. Create a file named `colors.xml` under `Resources/values-night`
        And, include your "Dark" theme colors:
        ```xml
        <?xml version="1.0" encoding="utf-8" ?>
        <resources>
	        <color name="MaterialPrimaryColor">#B6A8FB</color>
        </resources>
        ```
