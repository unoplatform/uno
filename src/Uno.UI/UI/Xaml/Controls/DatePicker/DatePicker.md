# DatePicker

## Summary

DatePicker is use to select a specific date.

*Button showing time open the time picker popup. 
Bind to the Date property of the control to set initial time.
*By default minute increment is set to 1
*Cancel button cancel the new time selection. You can also click outside the time picker to do the same.
*Done/OK button save the new selected time. 

### Device-specific implementation quirks

There might be differences in the date picker on different platform since it wraps platform specific ios/android date picker

#### Android

Native date picker is wrapped in the flyout.
DatePicker flyout appear centered to screen.

#### iOS
Native date picker is wrapped in the flyout.
Set 'FlyoutPlacement' property to change flyout docking placement
Default 'FlyoutPlacement' is 'Full' and will dock of the flyout at the bottom of the screen
