# MapControl

The `MapControl` is a control which allows you to display maps in your app.

## Architecture

To be able to support multiple map providers, the map control renders its content in a `MapPresenter` control in the `Uno.UI.Maps` NuGet package. This separation is also required to avoid pulling dependencies in an application that does not need Maps.

The current implementation uses the native UIKit map for iOS and the Google Play Services map control for Android.

## Sample Xaml

See a more complete sample here: 

```xml
<Page x:Class="MapControlSample.MainPage"
	  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
	  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
	  xmlns:local="using:MapControlSample"
	  xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
	  xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
	  xmlns:maps="using:Windows.UI.Xaml.Controls.Maps"
	  mc:Ignorable="d">

	<Grid Background="{ThemeResource ApplicationPageBackgroundThemeBrush}">
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

### 1. Configure your application.

- For **Android**,
    1.  Add the following to AndroidManifest.xml:
    ```xml
    <uses-library android:name="com.google.android.maps" />
    ```
    2.  Generate the API key and/or inform your client how to generate it.
		1.  Go to https://console.developers.google.com
		2.  Login with a Google account
		3.  Go to the Credentials section to the top left
		4.  Click on "Create credentials", then "API key"
		5.  Go to the Dashboard section in the left-hand side menu
		6.  Click on the relevant service - for instance, "Google Maps Android API" and click on Enable
	
    3.  Add the API key to `AssemblyInfo.cs`. This should vary depending on the platform and environment; therefore, it should be retrieved from ClientConstants:
    ```csharp
	[assembly: MetaData("com.google.android.maps.v2.API_KEY", Value = ClientConstants.Maps.GoogleMapApiKey)]
    ```
	
    4.  Add the relevant permissions, if you wish to access the location of the user (either coarse or fine location):
	 ```csharp
	//[assembly: UsesPermission(Android.Manifest.Permission.AccessCoarseLocation)]
	[assembly: UsesPermission(Android.Manifest.Permission.AccessFineLocation)]

	[assembly: UsesPermission("com.myapp.permission.MAPS_RECEIVE")]
	[assembly: Permission(Name = "com.myapp.permission.MAPS_RECEIVE", ProtectionLevel = Android.Content.PM.Protection.Signature)]
	```


# Get API key for map component

In order to use the map component, you will need an API key for Windows and Android. Here are the steps to retrieve it.

## Windows

_For the detailed procedure for Windows, please follow this link: https://msdn.microsoft.com/en-us/library/windows/apps/xaml/mt219694.aspx _

+ Go to https://www.bingmapsportal.com
+ Login to your account or register if you don't have one
+ Go to MyAccount -> Keys
+ Enter the following information:
	- Application name
	- Application URL (optional)
	- Key type
	- Application type
+ Enter the characters you see in the box
+ Hit *Create* and get the key

The key should be added as the value for the parameter _MapServiceToken_ for the MapControl object.

## Android

_For the detailed procedure on Android, please follow this link: https://developers.google.com/maps/documentation/android-api/signup#release-cert _

+ Retrieve the application's SHA-1 fingerprint
+ Create a project in the Google Developers Console
+ Go to Credentials -> Add credentials -> API key -> Android key
+ In the dialog box, enter the SHA-1 fingerprint and the app package name
+ Hit *Create* and get the key

The API key should be the value for the property _com.google.android.maps.v2.API_KEY_ in the AndroidManifest.xml file.
