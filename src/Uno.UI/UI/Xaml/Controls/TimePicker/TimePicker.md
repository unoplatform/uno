# TimePicker

## Summary

TimePicker is use to select a specific time of the day in hour and minute (AM/PM).

Time button open the time picker flyout popup. 
Cancel button cancel the new time selection. You can also click outside the time picker to do the same.
Done/OK button save the new selected time. 

### Styles
Time button style: TimePickerFlyoutButtonStyle
Time picker flyout popup style: default generic TimePickerFlyoutPresenterStyle
TimePickerSelector is a platform specific wrapper for IOS/Android time pickers

### Device-specific implementation quirks

There might be differences in the time picker on different platform since it wraps platform specific ios/android time picker

#### Android

Native time picker is wrapped in the flyout.
Timepicker flyout appear centered to screen.
You can change the flyout button by copying and modifying TimePickerFlyoutButtonStyle.
You can change the flyout button by copying and modifying TimePickerFlyoutPresenterStyle.

#### iOS
Native time picker is wrapped in the flyout.
Timepicker flyout appear at bottom of the screen.
You can change the flyout button by copying and modifying TimePickerFlyoutButtonStyle.
You can change the flyout button by copying and modifying TimePickerFlyoutPresenterStyle.

Some ColorBrushes are specific to IOS and could be changed by copying and redoing the new style so they use your own color brushes  
 IOSTimePickerAcceptButtonForegroundBrush  Color="#324f85"
 IOSTimePickerDismissButtonForegroundBrush  Color="#324f85"
 IOSTimePickerHeaderBackgroundBrush  Color="#f8f8f8" 