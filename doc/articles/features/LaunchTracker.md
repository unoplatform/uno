# LaunchTracker

To track launches of your application, use the `Uno.UI.Toolkit.Helpers.LaunchTracker` API. The API initialized automatically when the `Current` property is first retrieved, but accurate data we recommend calling the `Track()` method early in the application lifecycle, for example in the `App.xaml.cs` constructor:

```
public App()
{
    LaunchTracker.Current.Track();
}
```

Now we can use the various properties and methods of the API:

```
if (LaunchTracker.Current.IsFirstLaunch)
{
    // Display pop-up alert for first launch
}

if (LaunchTracker.Current.IsFirstLaunchForCurrentVersion)
{
    // Display update notification for current version (1.0.0)
}

if (LaunchTracker.Current.IsFirstLaunchForCurrentBuild)
{
    // Display update notification for current build number (2)
}

// Get number of times the app has been launched
LaunchTracker.Current.LaunchCount

// Get the time when the current instance of app has been launched
LaunchTracker.Current.CurrentLaunchDate

// Get the time the app has been launched last time
LaunchTracker.Current.PreviousLaunchDate
```