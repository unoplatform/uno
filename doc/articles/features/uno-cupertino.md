# Uno.Cupertino

Uno Cupertino is an add-on package that lets you apply [Human Interface Guideline styling](https://developer.apple.com/design/human-interface-guidelines) to your application with a few lines of code. It features:

- Color system for both Light and Dark themes
- Styles for existing WinUI controls like Buttons, TextBox, etc.

## Getting Started
1. Add NuGet package `Uno.Cupertino` to each of project heads
1. Add the following code inside `App.xaml`:
    ```xml
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Load WinUI resources -->
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />

                <CupertinoColors xmlns="using:Uno.Cupertino"  />
				 <CupertinoResources xmlns="using:Uno.Cupertino" />

                <!-- Application's custom styles -->
                <!-- other ResourceDictionaries -->
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
    ```

For complete instructions on using Uno Cupertino in your projects, check out this walkthrough: [How to use Uno.Cupertino](../guides/uno-cupertino-walkthrough.md).

## Features

### Styles for basic controls
Control|Resource Key
-|-
`Button`|CupertinoButtonStyle<br>CupertinoContainedButtonStyle
`CalendarView`|CupertinoCalendarViewStyle
`CalendarDatePicker`|CupertinoCalendarDatePickerStyle
`CheckBox`|CupertinoCheckBoxStyle
`ComboBox`|CupertinoComboBoxStyle
`ComboBoxItem`|CupertinoComboBoxItemStyle
`DatePicker`|CupertinoDatePickerStyle
`DatePickerFlyoutPresenter`|CupertinoDatePickerFlyoutPresenterStyle
`HyperlinkButton`|CupertinoHyperlinkButtonStyle
`NumberBox`| CupertinoNumberBoxStyle
`PasswordBox`|CupertinoPasswordBoxStyle
`RadioButton`|CupertinoRadioButtonStyle
`Slider`|CupertinoSliderStyle
`TextBlock`|CupertinoBaseTextBlockStyle<br>CupertinoLargeTitle<br>CupertinoPrimaryTitle<br>CupertinoSecondaryTitle<br>CupertinoTertiaryTitle<br>CupertinoHeadline<br>CupertinoBody<br>CupertinoCallout<br>CupertinoSubhead<br>CupertinoFootnote<br>CupertinoPrimaryCaption<br>CupertinoSecondaryCaption
`TextBox`|CupertinoTextBoxStyle
`ToggleSwitch`|CupertinoToggleSwitchStyle
`winui:ProgressBar`|CupertinoProgressBarStyle
`winui:ProgressRing`|CupertinoProgressRingStyle

### Customizable Color Theme
The colors used in the Cupertino styles are part of the color palette system, which can be customized to suit the theme of your application. Since this is decoupled from the styles, the application theme can be changed, without having to make a copy of every style and edit each of them.

## Additional Resources
- Official Human Interface Guidelines site: https://developer.apple.com/design/human-interface-guidelines/
- Uno.Cupertino library repository: https://github.com/unoplatform/Uno.Themes
