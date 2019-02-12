# Building and Debugging Uno.UI

## Building Uno.UI

Prerequisites:
* Install Visual Studio 2017 (15.5 or later):
  * Workloads 
    * Universal Windows Platform development
    * Mobile Development with .NET (Xamarin)
    * .NET Desktop development
  * Individual components
    * .NET Framework 4.7.2 targeting pack
    * Windows 10 SDK version 10.0.14393.0
* Install Android Support Repository
  * Within Visual Studio go to **Tools** -> **Android** -> **Android SDK Manager**
  * Switch to **Tools** tab
  * Under **Extras** node check **Android Support Repository**


Using Visual Studio:
* Open the [Uno.UI.sln](/src/Uno.UI.sln)
* Select the Uno.UI project
* Build

In case build fails, make sure all NuGet packages have properly restored (right-click the solution in **Solution Explorer** and select **Restore NuGet Packages**. When finished, restart Visual Studio as it sometimes fails to pick up newly installed packages unless the solution is reloaded.

Inside Visual Studio, the number of platforms is restricted to limit the compilation time.

## Microsoft Source Link support
Uno.UI supports [SourceLink](https://github.com/dotnet/sourcelink/) and it now possible to
step into Uno.UI without downloading the repository.

Make sure **Enable source link support** check box is checked in **Tools** / **Options**
/ **Debugging** / **General** properties page.

## Debugging Uno.UI

To debug Uno.UI inside of an existing project, the simplest way (until Microsoft provides a better way to avoid overriding the global cache) is to :
* Install a published `Uno.UI` package in a project you want to debug, taking note of the version number.
* Rename [crosstargeting_override.props.sample](/src/crosstargeting_override.props.sample) to `crosstargeting_override.props`
* Uncomment the `UnoNugetOverrideVersion` node
* Change the version number to the package you installed at the first step
* Build your solution.

> Note: This overrides your local nuget cache, making the cache inconstent with the binaries you just built. 
To ensure that the file you have in your cache a correct, either clear the cache, or observe the properties of the `Uno.UI.dll` file, where the
product version should contain a git CommitID.

Once Uno.UI built, open the files you want to debug inside the solution running the application you need to debug, and set breakpoints there.

## Running the samples applications

The Uno solution provides a set of sample applications that provide a way to test features, as
well as provide a way to write UI Tests. See [this document](working-with-the-samples-apps.md) for more information.

## Building Uno.UI for macOS using Visual Studio for Mac

Building Uno.UI for the macOS platform using vs4mac requires Visual Studio for mac 7.7 preview or later.

A few steps to be able to build:
- The `xamarinmac20` Target Framework must be the first in the `TargetFrameworks` list of the `Uno.UI`, `Uno`, `Uno.Foundation` and `Uno.Xaml` projects. VS4Mac only builds the first Target Framework.
- In both `Uno` and `Uno.UI` the ItemGroups containing references to `Xamarin.Android.Support.v4` and `Xamarin.Android.Support.v7.AppCompat` must be commented out.
Failing to remove those groups will make the nuget restore fail, because VS4Mac does not support conditional package references.
- In `Uno.UI`, comment the project reference to the `Uno.UI.BindingHelper` project, because VS4Mac does not support conditional project references.
- Disable both nuget restore features in Visual Studio for Mac configuration

To build and run:
- In a shell in the `src/Uno.UI` folder, run `msbuild /r`. This will make the nuget restore work properly.
- Once done, in VS4Mac, run the `SampleApp.macOS` project, which will build the dependencies and the app itself.

## Troubleshooting Source Generation

When building, if you're having build error messages that looks like one of those:

- `the targets [Microsoft.Build.Execution.TargetResult] failed to execute.`
- `error : Project has no references.`

There may be issues with the analysis of the project's source or configuration.

**Security notice: That `binlog` files produced below should never be published in a public location, as they may contain private information, such as source files. Make sure to provide those in private channels after review.**

The Source Generation tooling diagnostics can be enabled as follows:

- In the project file that fails to build, in the first `PropertyGroup` node, add the following content:
```xml
<UnoSourceGeneratorUnsecureBinLogEnabled>true</UnoSourceGeneratorUnsecureBinLogEnabled>
```
- Make to update or add the `Uno.SourceGenerationTasks` to the latest version
- When building, in the inner `obj` folders, a set of `.binlog` files are generated that can be opened with the [msbuild log viewer](http://msbuildlog.com/) and help the troubleshooting of the generation errors.
- Once you've reviewed the files, you may provide those as a reference for troubleshooting to the Uno maintainers. 
- The best way to provide those file for troubleshooting is to make a zip archive of the whole solution folder without cleaning it, so it contains the proper diagnostics `.binlog` files.

**Make sure to remove the `UnoSourceGeneratorUnsecureBinLogEnabled` property once done.**

## Troubleshooting Memory Issues 

Uno provides a set of classes aimed at diagnosing memory issues related to leaking controls, wether it be from
an Uno.UI issue or from an invalid pattern in user code.

### Enable Memory intances counter
In your application, as early as possible in the initialization (generally in the App.xaml.cs
constructor), add and call the following method:

```
using Uno.UI.DataBinding;

// ....
private void EnableViewsMemoryStatistics()
{
	//
	// Call this method to enable Views memory tracking.
	// Make sure that you've added the following :
	//
	//  { "Uno.UI.DataBinding", LogLevel.Information }
	//
	// in the logger settings, so that the statistics are showing up.
	//


	var unused = Windows.UI.Xaml.Window.Current.Dispatcher.RunAsync(
		CoreDispatcherPriority.Normal,
		async () =>
		{
			BinderReferenceHolder.IsEnabled = true;

			while (true)
			{
				await Task.Delay(1500);

				try
				{
					BinderReferenceHolder.LogReport();

					var inactiveInstances = BinderReferenceHolder.GetInactiveViewBinders();

					// Force the variable to be kept by the linker so we can see it with the debugger.
					// Put a breakpoint on this line to dig into the inactive views.
					inactiveInstances.ToString();
				}
				catch (Exception ex)
				{
					this.Log().Error("Report generation failed", ex);
				}
			}
		}
	);
}
```
You'll also need to add the following logger filter:
```
	{ "Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Information },
```

### Interpeting the statistics output

The output provides two sets of DependencyObject in memory:
- Active, for which the instances have a parent dependency object (e.g. an item in a Grid)
- Inactive, for which the instances do not have a parent dependency object

When doing back and forth navigation between pages, instances of the controls in the dismissed pages should
generally be collected after a few seconds, once those have been cleared from the `Frame` control's forward
stack (done after every new page navigation).

Searching for inactive objects first is generally best, as those instances are most likely kept alive by
cross references.

This can happen when a Grid item has a strong field reference to its parent:

```
var myItem = new Grid(){ Tag = grid };
grid.Children.Add(myItem);
```

For Xamarin.iOS specifically, see [this article about performance tuning](https://docs.microsoft.com/en-us/xamarin/ios/deploy-test/performance).
