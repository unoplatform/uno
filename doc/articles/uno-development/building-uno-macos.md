---
uid: Uno.Contributing.BuildingUnomacOS
---

# Building Uno.UI for macOS using Visual Studio for Mac

Building Uno.UI for the macOS platform using vs4mac requires Visual Studio for Mac 8.1 or later.

Steps:

* Make sure to [create the `crosstargeting_override.props` file](debugging-uno-ui.md) and set `UnoTargetFrameworkOverride` to `xamarinmac20`.

* Open `Uno.UI-vs4mac.sln` to for iOS/Android/macOS heads or `Uno.UI-vs4mac-macOS-only.sln` for macOS only.

* Set the `SamplesApp.macOS` project as the Startup Project.

* Launch the application.

Support for building the `Uno.UI` solution is still somewhat unstable, this is a list of known issues and troubleshooting steps:

* You may get a message like `Error while trying to load the project '/Users/user/src/uno/build/Uno.UI.Build.csproj': Index has to be between upper and lower bound of the array.` when you open the solution. You can safely ignore this error.

* If NuGet restore fails when building from the IDE, or if it gets stuck for some other reason, try building from the command line. Open a terminal session in the `uno/src` folder and use the following command:

   ``` shell
   msbuild /m /r SamplesApp/SamplesApp.macOS/SamplesApp.macOS.csproj
   ```

   Then reopen Visual Studio and try to launch the sample again.

* If you get an error when building the `Uno.UI.Lottie` project complaining about typescript errors, you may need to install `Node.js` on your Mac. The easiest way to do this is to install the [Homebrew package manager](https://brew.sh/) and then use Homebrew to [install Node](https://changelog.com/posts/install-node-js-with-homebrew-on-os-x).

 **Beware: VS for Mac currently makes many unwanted "changes" to the `.csproj` files (like adding package version numbers explicitly, switching attributes to elements and vice-versa). Please do not commit these changes in your Pull Requests.**

* Make sure to apply the workarounds specified in https://github.com/unoplatform/uno/issues/3609, otherwise VS4Mac will fail to load the solution.
* In order to successfully debug an external application, use [cross-targeting overrides](building-uno-ui.md#building-unoui-for-a-single-target-platform) and make sure to enable "Step into external code" in the "Projects / Debugger" options in VS4mac.
