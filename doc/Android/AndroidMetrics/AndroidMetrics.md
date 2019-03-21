# Android metrics

## Accessible measures

Android has some useful stuff to know how the screen is used by the app.

We can access the following values :
- realMetrics : full screen size
- displayRect : full screen size - navigation bar, no matter the navigation bar visibility
- usableRect : full screen size - (Status bar + Keyboard + Navigation bar), no matter the status bar and navigation bar visibilities.

These measures can be represented as the following :

| No Keyboard                                      | With Keyboard                                      |
|--------------------------------------------------|----------------------------------------------------|
| ![picture](Images/AndroidMetrics_NoKeyboard.png) | ![picture](Images/AndroidMetrics_WithKeyboard.png) |

## NavigationBarHelper

In order to define the Keyboard space in the different project, this helper will give you some information about the bottom navigation bar.

| Property                      | Type   | Usage                                                                                                |
|-------------------------------|--------|------------------------------------------------------------------------------------------------------|
| IsNavigationBarTranslucent    | bool   | Defines if the NavigationBar is currently translucent                                                |
| IsNavigationBarVisible        | bool   | Defines if the NavigationBar is currently visible                                                    |
| LogicalNavigationBarHeight    | double | Return the logical, in density pixels, NavigationBar Height, no matter it is translucent or visibile |
| PhysicalNavigationBarHeight   | double | Return the physical, in pixels,  NavigationBar Height, no matter it is translucent or visibile       |