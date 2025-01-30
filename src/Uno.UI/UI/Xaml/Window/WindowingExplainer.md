# `ApplicationView`

The `ApplicationView` class is needed to retrieve the "visible bounds" of the screen - especially on mobile devices like Android and iOS where we have a status bar and navigation bar. This concept has been mostly dropped in WinAppSDK (at least for now), so calling `ApplicationView.GetForCurrentView` throws an exception. To keep this functionality internally we are using `ApplicationView.GetForWindowId` method, which returns a Window-specific instance.

For easier access to the `VisibleBounds` and `TrueVisibleBounds` within our codebase, you can utilize the `XamlRoot.VisualTree.VisibleBounds` and `XamlRoot.VisualTree.TrueVisibleBounds` properties. This property also works in windowless scenarios like Uno Islands, where it just returns the plain island bounds.

# Window initialization

When `Window` is constructed before application is initialized (e.g. before `Application.Current` is set), we do not actually construct its content, as that would cause issues on mobile targets (where UI can be created only after a certain app
initialization is done. Hence we delay this initialization and run it manually when the application is initialized. If the window is created later in the lifetime of the application, we initialize it immediately. 
The initialization is done by calling `Window.Initialize` method.
