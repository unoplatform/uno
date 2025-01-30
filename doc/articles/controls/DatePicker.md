---
uid: Uno.Controls.DatePicker
---

# DatePicker

## Summary

`DatePicker` is used to select a specific date.

## Managed vs. native implementation

On iOS and Android the `DatePicker` by default displays using the native time picker UI. If you prefer consistent UI across all targets, you can switch to the managed implementation by setting the `UseNativeStyle` property:

```xml
<DatePicker not_win:UseNativeStyle="False" ... />
```

To include the `not_win` XAML namespace on your page follow the instructions for [Platform-specific XAML](../platform-specific-xaml.md).
