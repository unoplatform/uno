# Uno Support for Windows.UI.Notifications.Toast*

## Limitations

**Android**
Currently, from XML, only toast.launch attribute and text elements are supported.
First text element is converted to Android notification title, and subsequent text elements - to notification text. But on Android < 14, new lines within texts are ignored; on newer Android versions, new lines are ignored in first text (title).
