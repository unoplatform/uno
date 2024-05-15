# MessageDialog

## Summary

For details on MessageDialog implementation and usage, see the [MSDN documentation](https://learn.microsoft.com/uwp/api/windows.ui.popups.messagedialog).

### Device-specific implementation quirks

The implementation of MessageDialog for iOS and Android are based on their respective native analog. This means that depending on the platform, there might be differences.

#### Android

- There is a limit of 3 items displayed in an AlertDialog
- It is possible to tap outside of the AlertDialog. If the user performs this action, the result of the MessageDialog will be equal to the CancelIndex provided. Unlike UWP, if no CancelIndex is provided, the result will be null

#### iOS

- There is no limit to the amount of items displayed. If >2 items are added to the Commands list, it will be displayed as a vertical scrolling list of options
- It is impossible to cancel the dialog. The user is forced to select one of the options displayed
