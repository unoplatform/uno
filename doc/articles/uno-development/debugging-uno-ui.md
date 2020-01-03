# Building and Debugging Uno.UI

## Building Uno.UI

Prerequisites:
- Visual Studio 2017 (15.8 or later) or 2019 (Preview 2 or later)
    - `Mobile Development with .NET` (Xamarin) development
    - `Visual Studio extensions development` (for the VSIX projects)
    - `ASP.NET and Web Development`
    - `.NET Core cross-platform development`
    - `UWP Development`, install all recent UWP SDKs, starting from 10.0.14393 (or above or equal to `TargetPlatformVersion` line [in this file](/src/Uno.CrossTargetting.props))
- Install (**Tools** / **Android** / **Android SDK manager**) all Android SDKs starting from 7.1 (or the Android versions `TargetFrameworks` [list used here](https://github.com/unoplatform/uno/blob/master/src/Uno.UI.BindingHelper.Android/Uno.UI.BindingHelper.Android.csproj))

### Building Uno.UI for all available targets
* Open the [Uno.UI.sln](/src/Uno.UI.sln)
* Select the `Uno.UI` project
* Build

Inside Visual Studio, the number of platforms is restricted to limit the compilation time.

### Faster dev loop with single target-framework builds
To enable faster development, it's possible to use the [Visual Studio Solution Filters](https://docs.microsoft.com/en-us/visualstudio/ide/filtered-solutions?view=vs-2019), and only load the projects relevant for the task at hand.

For instance, if you want to debug an iOS feature:
- Make sure the `Uno.UI.sln` solution is not opened in Visual Studio.
- Make a copy of the [src/crosstargeting_override.props.sample](src/crosstargeting_override.props.sample) file to `src/crosstargeting_override.props`
- In this new file, uncomment the `UnoTargetFrameworkOverride` line and set its value to `xamarinios10`
- Open the `Uno.UI-iOS-only.slnf` solution filter (either via the VS folder view, or the Windows explorer)
- Build

This technique works for `xamarinios10`, `monoandroid90`, `netstandard2.0` (wasm), and `net461` (Unit Tests).

> Note that it's very important to close Visual Studio when editing the `src/crosstargeting_override.props` file, otherwise VS may crash or behave inconsistently.

## Microsoft Source Link support
Uno.UI supports [SourceLink](https://github.com/dotnet/sourcelink/) and it now possible to
step into Uno.UI without downloading the repository.

Make sure **Enable source link support** check box is checked in **Tools** / **Options**
/ **Debugging** / **General** properties page.

## Updating the Nuget packages used by the Uno.UI solution
The versions used are centralized in the [Directory.Build.targets](src/Directory.Build.targets) file, and all the
locations where `<PackageReference />` are used.

When updating the versions of nuget packages, make sure to update the [Uno.UI.nuspec](build/Uno.UI.nuspec) file.

## Debugging Uno.UI

To debug Uno.UI inside of an existing project, the simplest way (until Microsoft provides a better way to avoid overriding the global cache) is to :
* Install a published `Uno.UI` package in a project you want to debug, taking note of the version number.
* Rename [crosstargeting_override.props.sample](/src/crosstargeting_override.props.sample) to `crosstargeting_override.props`
* Uncomment the `UnoNugetOverrideVersion` node
* Change the version number to the package you installed at the first step
* Build your solution.

> Note: This overrides your local nuget cache, making the cache inconsistent with the binaries you just built. 
To ensure that the file you have in your cache a correct, either clear the cache, or observe the properties of the `Uno.UI.dll` file, where the
product version should contain a git CommitID.

Once Uno.UI built, open the files you want to debug inside the solution running the application you need to debug, and set breakpoints there.

You may improve your built time by selecting an active target framework, see the **Faster dev loop** section above.

## Running the samples applications

The Uno solution provides a set of sample applications that provide a way to test features, as
well as provide a way to write UI Tests. See [this document](working-with-the-samples-apps.md) for more information.

## Using the Package Diff tool

Refer to the [guidelines for breaking changes](../contributing/guidelines/breaking-changes.md) document.

## Building Uno.UI for macOS using Visual Studio for Mac

Building Uno.UI for the macOS platform using vs4mac requires Visual Studio for mac 8.1 Preview or later.

To build and run:
- In a shell in the `src/Uno.UI` folder, run `msbuild /r Uno.UI-vs4mac.sln`. This will make the nuget restore work properly.
- Once done, in VS4Mac, run the `SampleApp.macOS` project, which will build the dependencies and the app itself.

**Beware: VS for Mac currently makes many unwanted "changes" to the `.csproj` files (like adding package version numbers explicitly, switching attributes to elements and vice-versa). Please do not commit these unwanted changes in your Pull Requests.**

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

If ever the need arises to view the generated source code of a *failing* CI build, you can perform the following steps:

1. In your local branch, locate the one of build yaml files (located in the root Uno folder):
     - .azure-devops-android-tests.yml
     - .azure-devops-macos.yml
     - .azure-devops-wasm-uitests.yml
    
2. At the bottom of the yaml files, you'll find *Publish...* tasks, right above these tasks, copy/paste the following code to create a task which will copy all generated source files and put them in an artifact for you to download:

       - bash: cp -r $(build.sourcesdirectory)<YourProjectDirectory>/obj/Release/g/XamlCodeGenerator/ $(build.artifactstagingdirectory)
            condition: failed()
            displayName: "Copy generated XAML code"

Therefore, in the case of Uno, an example of <YourProjectDirectory> would be */src/SamplesApp/SamplesApp.Droid*.

3. Once the build fails and completes, you can download the corresponding build artifact from the Artifacts menu to view your generated source files.

**Remember that you should never submit this change, this is temporary and only for viewing generated code.**

## Troubleshooting Memory Issues 

Uno provides a set of classes aimed at diagnosing memory issues related to leaking controls, whether it be from
an Uno.UI issue or from an invalid pattern in user code.

### Enable Memory instances counter
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

### Interpreting the statistics output

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
