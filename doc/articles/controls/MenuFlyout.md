# MenuFlyout

MenuFlyout is implemented to provide support for `ContextMenu` and `MenuBar` features.

## Platform specific behavior

On iOS and Android, the flyout can either be displayed using native popup, or Uno managed popups.

The default behavior of the MenuFlyout is to follow the value `FeatureConfiguration.Style.UseUWPDefaultStyles`, but this can be changed per MenuFlyout using the `FlyoutBase.UseNativePopup` property.
