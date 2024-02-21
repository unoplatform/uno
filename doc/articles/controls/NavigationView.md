---
uid: Uno.Controls.NavigationView
---

# NavigationView

The `NavigationView` control provides top-level navigation for your app. It adapts to a variety of screen sizes and supports both top and left navigation styles. For more information about its usage, see [Combo box and list box Microsoft documentation](https://learn.microsoft.com/windows/apps/design/controls/navigationview).

## Back button visibility

The `IsBackButtonVisible` property is by default set to `Auto`. This means the system chooses whether or not to display the back button. As per the design guidelines on Android, this means it is not shown by default and the [**hardware back button** should be used instead](../features/hardware-back-button.md). If you prefer to always display the back button, set the `IsBackButtonVisible` property to `Visible`.