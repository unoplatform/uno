# How to migrate existing UWP code to Uno

There are two separate paths for using an existing UWP codebase on top of Uno:
- An existing UWP application
- An existing UWP library

For migrating a UWP library, see [this article](howto-migrate-existing-code.md).

# How to migrate UWP app to Uno?
# Step by step guide.

This guide assumes that you have some UWP app, and want to convert this app to be multiplatform.



In short:
* convert project into Uno,
* find if something you use is not implemented in Uno,
* if yes, you have two options:
	* try to find workaround for this, or
	* extend Uno
* try to run converted app, and test it



## adding new project

First, open your app solution in VStudio. Then using Solution Explorer window:
* add new project (right click on Solution, Add, New Project, Visual C#, Uno Plaform, Cross-Plaform App (Uno Platform), using name as e.g. your previous project (APPNAME) with "\_Uno" suffix. In effect, APPNAME_Uno folder would be created, and inside it - folders as APPNAME_Uno.Shared, APPNAME_Uno.Droid, etc. - one project per one target platform.
* for now, you can Unload your APPNAME_Uno.Droid, .iOS and .WASM projects (right click in Solution Explorer). Why? To make VStudio using less memory and start faster.

## converting your code
In simple words, you have to copy all your content from APPNAME to APPNAME_Uno.Shared; all your XAML pages, all code behind it (.cs), and replace folder Strings (delete just generated Strings folders). Copy also all other files and folders you created in APPNAME.
Use Solution Explorer for this.

If your code is in VB, you can use some simple translators, e.g. https://codeconverter.icsharpcode.net/ . It is not perfect translation, some issues you would have to correct manually, but it is a good start. So, in Solution Explorer, for each XAML page:
* open .xaml file from APPNAME,
* open .vb file from APPNAME,
* right click on APPNAME_Uno.Shared, Add, New, C#, XAML, Blank Page - use same name as in APPNAME project,
* open both .xaml and .cs file you just created,
* copy contens of XAML page
* convert .vb code to .cs code, and insert it to .cs page - but do not remove constructor, with `this.InitializeComponent();`. From App.xaml.vb, convert only code you added. Take care of "namespace" - should be same as in .xaml (and as in manifest), without "\_Uno" and "Shared" sufixes.

You have to copy also Package.appxmanifest (especially, app capabilities etc.) and Assets folder - but to APPNAME_Uno.UWP, not to APPNAME_Uno.Shared. 

Now, you can Unload your (old) APPNAME project (right click in Solution Explorer), not only to make VStudio using less memory and start faster, but also to be sure you don't mistakenly change something in your old code.

## check conversion to Uno (UWP)
Try to build your app - not Solution, but only UWP project (choose Debug, and right click APPNAME.UWP, Build). If nothing unexpected happens (no errors), your first step of porting app is done.
Build UWP project, check if it is working as expected.
You can upload new version of your app Microsoft Store. 

## check if everything is implemented in Uno
Now, reload APPNAME_Uno.Droid project (right click on it in Solution Explorer). Give VStudio some time (in minutes). It will rebuild Intellisense database.

Look into ErrorList window for warnings "is not implemented in Uno", e.g.
`"Warning Uno0001 Windows.UI.Xaml.Application.Exit() is not implemented in Uno".`

 If you don't have such warnings, you are lucky - and your app is already 'multiplatformed'.
You can build for another platform (e.g. Android), to check if compilation is ok. Uploading app to Google Store requires much more work, and is outside scope of this text.

 But most probably, you would find some Uno0001 warnings...

## dealing with Uno0001 warnings

Now, you have several options, from simplest to hardest:

### update Uno.UI/Uno.Core to last stable version
As Uno gets new features constantly, maybe UWP feature is already added.
Use Tools / NuGet Package Manager / Manage NuGet Packages for Solution, Updates tab, to check if Uno.Core/Uno.UI can be updated.
Or, check this file: [ReleaseNotes](https://github.com/unoplatform/uno/blob/master/doc/ReleaseNotes/_ReleaseNotes.md) (skip "next version" section)

### update Uno.UI/Uno.Core to last dev version
Or maybe feature is added, and you can use it - with small risk that something is broken.
Check "next version" section in previously mentioned file: [ReleaseNotes](https://github.com/unoplatform/uno/blob/master/doc/ReleaseNotes/_ReleaseNotes.md).
If it seems that your problem is resolved, install latest dev version (in Manage NuGet Packages for Solution, check 'include prerelease' checkbox).

### check PR (Pull Requests)
Your next hope is that someone already make necessary changes to Uno, but these changes are not included even in dev version (yet). It means that some discussion about implementation is under way, or maybe some tests are failing. But you can expect this feature in some near time appear in at least dev version.

List of open PR can be found here: [Pull Requests](https://github.com/unoplatform/uno/pulls).

### consider workaround
Maybe feature is not implemented, but some "very close" feature is. For example, maybe your app is using CalendarDatePicker (not implemented in Uno 1.45). But Uno implement DatePicker, and this allows to select date, and besides that, this implementation in many ways resemble CalendarDatePicker from UWP.
So, you can use CalendarDatePicker in UWP, and DatePicker in all other. It can be done using XAML prefixes, in this case:

`<win:CalendarDatePicker Name="yourName" DateChanged="myDateChanged" ... />`

`<non_win:DatePicker Name="youName" DateChanged="myDateChangedDP" ... />`

and add second event handler, myDateChangedDP - it has to be new handler, as CalendarDatePicker.DateChanged and DatePicker.DateChanged handlers have different argument types.

All prefixes list: [platform-specific-xaml](https://github.com/unoplatform/uno/blob/master/doc/articles/platform-specific-xaml.md).

### search for NuGet package for Xamarin
Maybe your app sends Toasts. They are not implemented in Uno, but you can use this: https://github.com/edsnider/LocalNotificationsPlugin .
Similar solution exist for using Clipboard, and probably for many more problems.

### create an Issue (feature request)
If all this fails, you can request adding new feature to Uno.

Before submitting new Issue, check if someone else doesn't submit such issue before: [Issues](https://github.com/unoplatform/uno/issues).

If not, use this: [new feature request](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fenhancement%2C+triage%2Funtriaged&template=enhancement.md).

### contribute to Uno
This is best (or: preferable) option, if using newer Uno versions / checking existing PR doesn't solve your problem. Best, but also it requires much more time from your side.
But this is outside scope of this article.

### make strip-down version
As last resort, you can make some app functions work only under Windows, and WASM/iOS/Android versions somehow limited.


Now it is time for compiling and test for platforms other than UWP.

## special case: migrating app to Android

### Connect Android device
Similar to connecting Windows device - you have to enable debugging.
So, go to Settings > System > About , then tap the Build number seven times. This unhides Settings > System > Developer options. In this screen, you can enable USB debugging.

### Steps before compiling
Before you build/run app as Android, you should open .Droid project properties (right click on project in Solution Explorer), and:
1. in Application tab, check target framework (should be 9.0 - requirement, if you want to put app in Google Store)
2. in Android Manifest tab, check/enter
* app name,
* package name (in format `<yourid>.<app>` - app will be available in Store at link: `https://play.google.com/apps?id=<yourid>.<app>)`
* version number - (integer) number of your build
* version name - (string) set it to same value as in Manifest for UWP, in format 1.2.3.4
* minimum API version
* permissions - see [permission dictionary](https://developer.android.com/reference/android/Manifest.permission.html)
* you can also convert .UWP\Assets\SmallTile.scale-100.png to .Droid\Resources\drawable\Icon.png by resizing from 71x71 to 72x72

### compile and test
Build `Debug`, `Any CPU` configuration.
Prepare to long delay when app starts on Android device.
If all is ok (as it should be, with great probability), you can send app to e.g. Google Store. You can use [this tutorial](https://riptutorial.com/xamarin-android/example/29653/preparing-your-apk-in-the-visual-studio).
