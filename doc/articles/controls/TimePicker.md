---
uid: Uno.Controls.TimePicker
---

# TimePicker

## Summary

TimePicker is use to select a specific time of the day in hour and minute (AM/PM).

Button showing time open the time picker popup.
Bind to the Time property of the control to set initial time.
days, seconds and milliseconds of input timespan are ignored
By default minute increment is set to 1
if you assign a negative value or 0, it will use 1 minute increment
if you assign a value over 30, it will use 30 minute increment
While all increments under 30 minutes are valid, we recommend using 1,2,5,10,15,20,25,30 minute increment.
If bound time is 2h03 and time increment is 5 than time picker initial pickable time will be 2h05
If bound time is 2h08 and time increment is 15 than time picker initial pickable time will be 2h15
Cancel button cancel the new time selection. You can also click outside the time picker to do the same.
Done/OK button save the new selected time.

### Styles

Time button style: TimePickerFlyoutButtonStyle
Time picker flyout popup style: default generic TimePickerFlyoutPresenterStyle
TimePickerSelector is a platform specific wrapper for IOS/Android time pickers

### Device-specific implementation quirks

There might be differences in the time picker on different platform since it wraps platform specific ios/android time picker

#### Android

- Native time picker is wrapped in the flyout.
- `Timepicker` flyout appear centered to screen.
- You can change the flyout button by copying and modifying TimePickerFlyoutButtonStyle.
- You can change the flyout button by copying and modifying TimePickerFlyoutPresenterStyle.
- If 'MinuteIncrement` is more than 1, `TimePicker` will show in "spinner mode"
- In case clock mode still appear for some reason picked value will be rounded to minute increment intervals..

#### iOS

- Native time picker is wrapped in the flyout.
- Set 'ios:FlyoutPlacement' property to change flyout docking placement
- Default 'ios:FlyoutPlacement' is 'Full' and will dock of the flyout at the bottom of the screen
- You can change the flyout button by copying and modifying TimePickerFlyoutButtonStyle.
- You can change the flyout button by copying and modifying TimePickerFlyoutPresenterStyle.

Some `ColorBrushes` are specific to **iOS** and could be changed by copying and redoing the new style so they use your own color brushes:

```resources
IOSTimePickerAcceptButtonForegroundBrush  Color="#055bb7"
IOSTimePickerDismissButtonForegroundBrush  Color="#055bb7"
IOSTimePickerHeaderBackgroundBrush  Color="{ThemeResource SystemListLowColor}"
```

If you want to show a dimmed overlay underneath the picker, set the `TimePicker.LightDismissOverlayMode` property to `On`.

If you wish to customize the overlay color, add the following to your top-level `App.Resources`:

```xml
<SolidColorBrush x:Key="TimePickerLightDismissOverlayBackground"
                 Color="Pink" />
```

Since **iOS14** the native `TimePicker` changed the way it's presented. By default iOS14 devices will display this new style.  You can still force the previous style (the one found in iOS13 or earlier) by adding the following at your `App.cs` or `App.xaml.cs` class:

```csharp
Uno.UI.FeatureConfiguration.TimePicker.UseLegacyStyle = true;
```

> [!IMPORTANT]
> This feature flag is required and will only affect iOS 14 devices. As of **iOS 15**, the preferred style for the DatePicker is again the one found in iOS13 and earlier.
