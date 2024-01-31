---
uid: Uno.Development.SplashScreen
---

# How to manually add a splash screen

Projects created using Uno Platform 4.8 or later have the Uno.Resizetizer [package](https://www.nuget.org/packages/Uno.Resizetizer) installed by default. Simply provide an SVG file, and the tool handles the task of generating various image sizes. That package updates the build process to automate configuring a splash screen for each platform.

While the new templates simplify adding a splash screen, this article covers how to add one to your application manually if using Uno.Resizetizer is not warranted.

> [!TIP]
> If your solution was generated using the older templates, it is possible to configure these projects to use Uno.Resizetizer instead. That process makes the steps below unnecessary.
>
> See the guide [How-To: Get Started with Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted#unosplashscreen) for more information.

## Step-by-steps

### 1. Shared splash screen image resources

* Review [Assets and image display](xref:Uno.Features.Assets) to understand the present support for various image asset types

* Prepare your images intended for the splash screen under different resolutions, eg:

    | File name | Width | Height |
    |----------------------------|:---:|:---:|
    | SplashScreen.scale-100.png | 216 | 148 |
    | SplashScreen.scale-125.png | 270 | 185 |
    | SplashScreen.scale-150.png | 324 | 222 |
    | SplashScreen.scale-200.png | 432 | 296 |
    | SplashScreen.scale-300.png | 648 | 444 |
    | SplashScreen.scale-400.png | 864 | 592 |

* Refer to this [table](xref:Uno.Features.Assets#table-of-scales) to see values for the different scales required.

  * You can instead provide only a single image named `SplashScreen.png` without the `scale-000` qualifier.

    > [!NOTE]
    > Regardless if you provide a single image or multiple images, you would always refer to this image as `SplashScreen.png`.

* Add these images under the `Assets\` folder of the `MyApp` Class Library project, right-click on each image, go to `Properties`, and set their build action as `Content`.

### 2. Windows

* In the `.Windows` project, open the file `Package.appxmanifest` and navigate to `Visual Assets > SplashScreen`.

* Make sure the value for `Preview Images > Splash Screen` is set to `Assets\SplashScreen.png`

    ![uwp-splash-screen](Assets/uwp-splash-screen.JPG)

### 3. Android

* In the `.Mobile` project, open the subfolder for `Android`

* Navigate further to the file at `Resources/values/Styles.xml`

* `Styles.xml` contains Android-specific customizations for the splash screen. Inside, look for the `AppTheme` style and add an `<item>` under it:

    ```xml
    <item name="android:windowBackground">@drawable/splash</item>
    ```

* Navigate upward to `Resources/drawable`, and create a new XML file named `splash.xml`:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
        <layer-list xmlns:android="http://schemas.android.com/apk/res/android">
        <item>
            <!-- background color -->
            <color android:color="#101010"/>
        </item>
        <item>
            <!-- splash image -->
            <bitmap android:src="@drawable/assets_splashscreen"
                    android:tileMode="disabled"
                    android:gravity="center" />
        </item>
    </layer-list>
    ```

    > [!IMPORTANT]
    > Before Uno.UI 4.5, the `@drawable/assets_splashscreen` source should be `@drawable/splashscreen`.
    > See the [breaking changes](https://github.com/unoplatform/uno/releases/tag/4.5.9) section of that release.

* Make sure `splash.xml` is added as an `AndroidResource` in the Droid project file: `[Project-name].Droid.csproj`.

  * This is not always done automatically and may occur if `splash.xml` was created and added outside the IDE.

    ```xml
    <ItemGroup>
      <AndroidResource Include="Resources\drawable\splash.xml" />
    </ItemGroup>
    ```

    > [!TIP]
    > After modifying `splash.xml`, you may run into errors like these while trying to debug:
    >
    > ```console
    > Resources\drawable-mdpi\SplashScreen.png : error APT2126: file not found.
    > Resources\drawable-hdpi\SplashScreen.png : error APT2126: file not found.
    > ```
    >
    > Simply rebuild the Android target to get rid of these errors.

### 4. iOS/MacCatalyst

* In the `.Mobile` project, open the subfolder for `iOS` or `MacCatalyst`.

  * Delete the old splash screen files:
    * `Resources\SplashScreen@2x.png`
    * `Resources\SplashScreen@3x.png`
    * `LaunchScreen.storyboard`

* Create a new **StoryBoard** named `LaunchScreen.storyboard`:
  * Right-click the `.Mobile` project subfolder you're working with (ex: `MyApp.Mobile\iOS`)
  * Select **Add** > **New Item...**
  * Create a **Visual C#** > **Apple** > **Empty Storyboard**

* In the **Toolbox** window, drag and drop a **View Controller** and then an **ImageView** inside the **View Controller**

  * Enable the **Is initial View Controller**-flag on the **View Controller**.

    ![`viewcontroller-imageview`](Assets/viewcontroller-imageview.png)

  * To have an image fill the screen, set your constraints as below

    ![ios-constraints](Assets/ios-constraints.png)

  * Set the **Content Mode** to **Aspect Fit**

    ![ios-content-fit](Assets/ios-content-fit.png)

  * In the **Properties** > **Storyboard Document** window, select the **Can be Launch Screen** checkbox.

    ![can-be-launch](Assets/can-be-launch.png)

* Close the designer and open the `.storyboard` file.

  * Add your image path to the `Image View`

    ``` xml
    <imageView ... image="Assets/SplashScreen">
    ```

* Open `info.plist` and update the `UILaunchStoryboardName` value to `LaunchScreen`.

    > [!TIP]
    > iOS caches the splash screen to improve the launch time, even across re-installs. In order to see the actual changes made, you need to restart the iPhone or simulator. Alternatively, you can rename the `CFBundleIdentifier` in `info.plist` incrementally (eg: MyApp1 -> MyApp2) before each build.

### 5. WebAssembly

* The default splash screen configuration for WebAssembly is to use the Uno Platform logo as a placeholder

* An `AppManifest.js` file contains settings for your WebAssembly application, including properties to customize its splash screen. This file is found in the `[AppName].Wasm` project, typically located at `WasmScripts/AppManifest.js`.

<<<<<<< HEAD
<<<<<<< HEAD
  * Customize the splash screen image and background color by setting the following properties related to splash screens

#### Standard properties for splash screens

| Property | Description | Notes |
|----------|-------------|-----|
| `splashScreenImage` | Location of the splash screen image. | You currently need to set an explicit scale for the image |
| `splashScreenColor` | A background color for the splash screen. | Any values assigned to the theme-aware properties are ignored unless this property is set to `transparent`. <br><br>If the theme-aware properties are unassigned, the default browser background color will be used instead. |

* Example:

    ```js
    var UnoAppManifest = {
        splashScreenImage: "Assets/SplashScreen.scale-200.png",
        splashScreenColor: "transparent",
        displayName: "SplashScreenSample"
    }
    ```

    > [!NOTE]
    > The section below contains optional properties. If nothing is assigned to them, the value of `splashScreenColor` will be used under both themes as the background color.
    >
    > [!TIP]
    > The `splashScreenColor` property allows you to set the background color for the splash screen. If you want to make the splash screen theme-aware, you must either omit this property or set it to `transparent`.

* Uno Platform supports theme-aware backgrounds as an optional customization for splash screens.

  * You can set the `darkThemeBackgroundColor` and `lightThemeBackgroundColor` properties to adjust the background color for each theme.

#### Optional: Properties for theme-aware splash screens

| Property | Description | Notes |
|---------------------------|-------------|-----|
|`lightThemeBackgroundColor`| Splash screen background to be used if a system light theme is enabled. | Default value is `#F3F3F3` |
|`darkThemeBackgroundColor` | Splash screen background to be used if a system dark theme is enabled.  | Default value is `#202020` |

## See also

* [Completed sample on GitHub](https://github.com/unoplatform/Uno.Samples/tree/master/UI/SplashScreenSample)
* [Ask for help on our Discord channel](https://www.platform.uno/discord)
* [Uno.Resizetizer repository](https://github.com/unoplatform/uno.resizetizer)
=======
    #### General properties
=======
#### General properties
>>>>>>> 98b7c36e94 (docs: Apply suggestions from code review)

You can customize the splash screen image and background color by adjusting several key properties:

  | Property | Description | Notes |
  |----------|-------------|-----|
  | `accentColor` | Color of the progress indicator's filled-in portion displayed during application launch | Default value is `#F85977` |
  | `displayName` | Default name visible in the browser window's title to represent the application | N/A |
  | `splashScreenColor` | Background color of the screen displayed during application launch | Any values assigned to the theme-aware properties are ignored unless this property is set to `transparent`. <br><br>If the theme-aware properties are unassigned, the default browser background color will be used instead. |
  | `splashScreenImage` | Path to an image that will be visible on the screen displayed during application launch | You currently need to set an explicit scale for the image |

  > [!TIP]
  > `splashScreenColor` allows you to maintain a background color regardless of the system theme. However, a simple method to make the splash screen theme-aware is to assign `transparent` as its value or by omitting that property altogether.

#### Theme-aware properties

  > [!NOTE]
  > The section below contains optional properties. If nothing is assigned to them, the value of `splashScreenColor` will be used under both themes as the background color.

  Uno Platform supports theme-aware backgrounds as an optional customization for splash screens. Set the following properties to adjust the splash screen based on a system theme:

  | Property | Description | Notes |
  | --- | --- | --- |
  | `lightThemeAccentColor` | Color of the progress indicator's filled-in portion displayed during application launch if a system light theme is enabled | Default value is `#F85977` |
  | `darkThemeAccentColor` | Color of the progress indicator's filled-in portion displayed during application launch if a system dark theme is enabled | Default value is `#F85977` |
  | `lightThemeBackgroundColor` | Background color of the screen displayed during application launch if a system light theme is enabled | Default value is `#F3F3F3` |
  | `darkThemeBackgroundColor` | Background color of the screen displayed during application launch if a system dark theme is enabled | Default value is `#202020` |

* Code example:

  ```javascript
  var UnoAppManifest = {
      splashScreenImage: "Assets/SplashScreen.scale-200.png",
      splashScreenColor: "transparent",
      displayName: "SplashScreenSample"
  }
  ```

## See also

<<<<<<< HEAD
- [Completed sample on GitHub](https://github.com/unoplatform/Uno.Samples/tree/master/UI/SplashScreenSample)
- [Ask for help on Discord](https://www.platform.uno/discord)
- [Uno.Resizetizer repository](https://github.com/unoplatform/uno.resizetizer)
>>>>>>> 20fb76b60b (docs: Align wasm manifest resources)
=======
* [Completed sample on GitHub](https://github.com/unoplatform/Uno.Samples/tree/master/UI/SplashScreenSample)
* [Ask for help on Discord](https://www.platform.uno/discord)
* [Uno.Resizetizer repository](https://github.com/unoplatform/uno.resizetizer)
>>>>>>> 021fab44b3 (chore: Fix to pass markdown linting check)
