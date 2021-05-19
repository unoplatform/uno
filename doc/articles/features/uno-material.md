# Uno.Material - cross-platform Material Design

Uno Material is an add-on package which lets you apply [Material Design styling](https://material.io/design) to your application with a few lines of code. It features:

- Color system for both Light and Dark themes
- Styles for existing WinUI controls like Buttons, TextBox, etc.
- Custom Controls for Material Components not offered out of the box by WinUI, such as `Card` and `BottomNavigationBar`.

For complete instructions on using Uno Material in your projects, including a set of Sketch files for designers, [consult the readme](https://github.com/unoplatform/Uno.Themes/blob/master/README.md).

## Getting Started
1. Add NuGet package `Uno.Material` to each of project heads
1. Add the following code inside `App.xaml`:
    ```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Load WinUI resources -->
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

                <!-- Application's custom styles -->
                <!-- other ResourceDictionaries -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
    ```
1. Initialize the material library in `App.xaml.cs`:
    ```cs
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
         Uno.Material.Resources.Init(this, null);

        // [existing code...]
    }
    ```

For complete instructions on using Uno Material in your projects, check out this walkthrough: [How to use Uno.Material](../guides/uno-material-walkthrough.md).

> [!NOTE]
> Certain controls require [additional setup steps](uno-material-controls-extra-setup.md).

## Features

### Styles for basic controls
Control|Resource Key
-|-
`AppBarButton`|MaterialAppBarButton
`Button`|MaterialContainedButtonStyle<br>MaterialContainedSecondaryButtonStyle<br>MaterialFabStyle<br>MaterialOutlinedButtonStyle<br>MaterialOutlinedSecondaryButtonStyle<br>MaterialPaneBackArrowToggleButtonStyle<br>MaterialPaneToggleButtonStyle<br>MaterialPrimaryInvertedFabStyle<br>MaterialSecondaryFabStyle<br>MaterialSecondaryInvertedFabStyle<br>MaterialSmallFabStyle<br>MaterialTextButtonStyle<br>MaterialTextSecondaryButtonStyle
`CheckBox`|MaterialCheckBoxStyle<br>MaterialSecondaryCheckBoxStyle
`ComboBox`|MaterialComboBoxStyle
`ComboBoxItem`|MaterialComboBoxItemStyle
`CommandBar`|MaterialCommandBarStyle
`DatePicker`|MaterialDatePickerStyle<br>MaterialSecondaryDatePickerStyle
`FlyoutPresenter`|MaterialFlyoutPresenterStyle
`HyperlinkButton`|MaterialHyperlinkButtonStyle<br>MaterialSecondaryHyperlinkButtonStyle
`NavigationView`|MaterialNavigationViewStyle<br>MaterialNoCompactMenuNavigationViewStyle
`PasswordBox`|MaterialFilledPasswordBoxStyle<br>MaterialOutlinedPasswordBoxStyle
`RadioButton`|MaterialRadioButtonStyle<br>MaterialSecondaryRadioButtonStyle
`Slider`|MaterialSecondarySliderStyle<br>MaterialSliderStyle
`TextBlock`|MaterialBaseTextBlockStyle<br>MaterialBody1<br>MaterialBody2<br>MaterialButtonText<br>MaterialCaption<br>MaterialHeadline1<br>MaterialHeadline2<br>MaterialHeadline3<br>MaterialHeadline4<br>MaterialHeadline5<br>MaterialHeadline6<br>MaterialOverline<br>MaterialSubtitle1<br>MaterialSubtitle2
`TextBox`|MaterialFilledTextBoxStyle<br>MaterialOutlinedTextBoxStyle
`TimePicker`|MaterialTimePickerStyle
`ToggleButton`|MaterialExpandingBottomSheetToggleButtonStyle<br>MaterialTextToggleButtonStyle
`ToggleSwitch`|MaterialSecondaryToggleSwitchStyle<br>MaterialToggleSwitchStyle
`winui:InfoBar`|MaterialInfoBarStyle
`winui:ProgressBar`|MaterialProgressBarStyle<br>MaterialSecondaryProgressBarStyle
`winui:ProgressRing`|MaterialProgressRingStyle<br>MaterialSecondaryProgressRingStyle

### Styles for custom material controls
Control|Resource Key
-|-
`BottomNavigationBar`|MaterialBottomNavigationBarStyle
`BottomNavigationBarItem`|MaterialBottomNavigationBarItemStyle
`Card`|MaterialAvatarElevatedCardStyle<br>MaterialAvatarOutlinedCardStyle<br>MaterialBaseCardStyle<br>MaterialElevatedCardStyle<br>MaterialOutlinedCardStyle<br>MaterialSmallMediaElevatedCardStyle<br>MaterialSmallMediaOutlinedCardStyle
`Chip`|MaterialChipStyle<br>MaterialOutlinedChipStyle<br>MaterialPrimaryChipStyle<br>MaterialPrimaryOutlinedChipStyle
`ChipGroup`|MaterialChipGroupStyle
`Divider`|MaterialDividerStyle
`ExpandingBottomSheet`|MaterialExpandingBottomSheetStyle
`ModalStandardBottomSheet`|MaterialModalStandardBottomSheetStyle
`SnackBar`|MaterialSnackBarStyle
`StandardBottomSheet`|MaterialStandardBottomSheetStyle

### Customizable Color Theme
The colors used in the material styles are part of the color palette system, which can be customized to suit the theme of your application. Since this is decoupled from the styles, the application theme can be changed, without having to make a copy of every style and edit each of them.

## Additional Resources
- Official Material Design site: https://material.io/design
- Uno.Material library repository: https://github.com/unoplatform/Uno.Themes
- [Additional control-specific setup](uno-material-controls-extra-setup.md)
- Tools for picking colors: https://material.io/design/color/the-color-system.html#tools-for-picking-colors
