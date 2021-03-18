# DatePicker

## Summary

DatePicker is use to select a specific date.

*Button showing date open the date picker popup. 
*Bind to the Date property of the control to set initial time.
*Done/OK button save the new selected date. 

### Device-specific implementation quirks

There might be differences in the date picker on different platform since it wraps platform specific ios/android date picker

#### Android

- Native date picker is wrapped in the flyout.
- DatePicker flyout appear centered to screen.

#### iOS
- Native date picker is wrapped in the flyout.
- Set '**FlyoutPlacement**' property to change flyout docking placement
- Default '**FlyoutPlacement**' is '**Full**' and will dock of the flyout at the bottom of the screen

If you want to show a dimmed overlay underneath the picker, set the `DatePicker.LightDismissOverlayMode` property to `On`.

If you wish to customise the overlay color, add the following to your top-level `App.Resources`:
```xml
<SolidColorBrush x:Key="DatePickerLightDismissOverlayBackground"
		Color="Pink" />
```

Since **iOS14** the native `DatePicker` changed the way it's presented. By default iOS14 devices will display this new style.  You can still force the previous style (the one found in iOS13 or earlier) by adding the following at your `App.xaml.cs` class:

```csharp
Uno.UI.FeatureConfiguration.DatePicker.UseLegacyStyle = true;
```