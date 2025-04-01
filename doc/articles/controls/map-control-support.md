---
uid: Uno.Controls.MapControl
---

# MapControl

The `MapControl` is a control that allows you to display maps in your app. Currently, Uno supports `MapControl` on iOS and Android.

## Architecture

Although `MapControl` is defined in `Uno.UI`, to function, it requires an additional package to be installed, `Uno.UI.Maps`, which supplies the actual implementation. This is done to avoid imposing an unnecessary dependency on applications that don't use maps, and also to allow alternative map providers (eg, Google Maps on iOS) to be easily supported in the future.

The current implementation uses the native UIKit Map for iOS and the Google Play Services Map control for Android.

## How to use MapControl in an Uno Platform app

1. Install the [Uno.WinUI.Maps NuGet package](https://www.nuget.org/packages/Uno.WinUI.Maps/) in the Android and/or iOS head projects of your app.
1. Add the `MapResources` resource dictionary to `Application.Resources` in your `App.xaml` file:

    ```xml
    <Application.Resources>
        <MapResources xmlns="using:Uno.UI.Maps"/>
    </Application.Resources>
    ```

1. (Windows and Android) Obtain an API key for your app, following the instructions below.
1. (Android) Configure permissions in the manifest, following the instructions below.
1. Add `MapControl` to your app (`<MapControl xmlns="using:Microsoft.UI.Xaml.Controls.Maps" />`).

## Sample XAML

Here's a complete sample:

```xml
<Page x:Class="MapControlSample.MainPage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:local="using:MapControlSample"
      xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
      xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
      xmlns:maps="using:Microsoft.UI.Xaml.Controls.Maps"
      mc:Ignorable="d"
      Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <StackPanel>
            <Slider Minimum="1"
                    Maximum="20"
                    StepFrequency=".5"
                    Header="ZoomLevel"
                    x:Name="zoomSlider"
                    Value="12" />
        </StackPanel>
        <maps:MapControl Grid.Row="1"
                         ZoomLevel="{Binding ElementName=zoomSlider, Path=Value, Mode=TwoWay}" />
    </Grid>
</Page>
```

The above code will display Page with a Map Control and a slider that will be used to change the ZoomLevel.

## Platform support

| Feature                                   | Android | iOS | Wasm |
| ------------------------------------------|:-------:|:---:|:----:|
| Display a map                             |    X    |  X  |      |
| Display a path                            |         |  X  |      |
| Customize pushpins with icon              |         |     |      |
| Fully template pushpins                   |         |     |      |
| Show user's location on map               |         |     |      |
| Toggle native Locate-Me button visibility |         |     |      |
| Toggle native Compass button visibility   |         |     |      |

## Usage

## Get API key for the map component

To use the map component, you will need an API key for Windows and Android. Here are the steps to retrieve it.

### Windows

For the detailed procedure for Windows, see [Request a maps authentication key](https://learn.microsoft.com/windows/uwp/maps-and-location/authentication-key) documentation.

+ Go to <https://www.bingmapsportal.com>
+ Login to your account or register if you don't have one
+ Go to MyAccount -> Keys
+ Click on create a new key
+ Enter the following information:

  + Application name
  + Application URL (optional)
  + Key type
  + Application type
+ Hit *Create* and get the key

The key will be set as the value for the parameter *MapServiceToken* for the MapControl object.

### Android

+ Create a project in the Google Developers Console.

+ Enable Map Service. In case this is your first time using the Google Maps API you will need to enable it before using it.

    1. Go to <https://console.developers.google.com>
    2. Login with a Google account
    3. Click on "Enable APIs and Services"
    4. Select "Maps SDK for Android" and click on Enable
+ Generate an API key.

    1. If you are coming from step-2 just navigate back until you are again in the dashboard, otherwise, go to <https://console.developers.google.com> and login with a Google account.
    2. Go to the Credentials section on the left-hand side menu
    3. Click on "Create Credentials", then "API key"
    4. Copy the key generated as this will be the one we will use later in the application

**Note:** For apps in production we suggest restricting the keys to be used only by your Android app. This is possible by using the SHA-1 fingerprint of your app.

*For a detailed procedure on how to retrieve the SHA-1 fingerprint for your Android application, please follow this link: <https://developers.google.com/maps/documentation/android-api/signup#release-cert>*

## Configure your application

+ For **Android**
    1. Add the following to AndroidManifest.xml:

        ```xml
        <uses-library android:name="com.google.android.maps" />
        ```

    2. Add the API key to `AssemblyInfo.cs`.

        ```csharp
        [assembly: MetaData("com.google.android.maps.v2.API_KEY", Value = "YOUR_API_KEY")]
        ```

        Replace the text YOUR_API_KEY with the key generated in previous step.

        Note: Since this key might vary depending on the platform and environment we suggest using a constant class where the key could be retrieved from.

    3. Add the relevant permissions to `AssemblyInfo.cs`. For example, if you wish to access the user location

        ```csharp
        [assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)]
        [assembly: UsesPermission("com.myapp.permission.MAPS_RECEIVE")]
        [assembly: Permission(Name = "com.myapp.permission.MAPS_RECEIVE", ProtectionLevel = Android.Content.PM.Protection.Signature)]
        ```
