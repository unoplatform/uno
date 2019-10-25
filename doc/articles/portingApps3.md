# How to contribute to Uno? My experience, part 3.

Previous parts tells:
1. How to run first build of app ported from 'plain UWP' to Uno, targetting UWP.
2. What you can do with missing features.

Now it is time for compiling and test for Android, and how to deal with error found.

## Connect Android device
Similar to connecting Windows device - you have to enable debugging.
So, go to Settings > System > About , then tap the Build number seven times. This unhides Settings > System > Developer options. In this screen, you can enable USB debugging.

## Steps before compiling
Before you build/run app as Android, you should open .Droid project properties (right click on project in Solution Explorer), and:
1. in Application tab, check target framework (should be 9.0 - requirement, if you want to put app in Google Store)
2. in Android Manifest tab, check/enter
* app name,
* package name (in format <yourid>.<app> - app will be available in Store at link: https://play.google.com/apps?id=<yourid>.<app>)
* version number - (integer) number of your build
* version name - (string) set it to same value as in Manifest for UWP, in format 1.2.3.4
* minimum version
* permissions - see [permission dictionary](https://developer.android.com/reference/android/Manifest.permission.html)
* you can also convert .UWP\Assets\SmallTile.scale-100.png to .Droid\Resources\drawable\Icon.png by resizing from 71x71 to 72x72

## compile and test
If all is ok (as it should be, with great probability), then rest of this article is not for you, and you can send app to e.g. Google Store. You can use [this tutorial](https://riptutorial.com/xamarin-android/example/29653/preparing-your-apk-in-the-visual-studio).
But sometimes you will find some bug.

## what to do if bug is found
This would be your first real contribution to Uno.
And your options are:

### create an Issue (bug report)
Before submitting new Issue, check if someone else doesn't submit such issue before: [Issues](https://github.com/unoplatform/uno/issues).
If not, use this: [new bug report](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fbug%2C+triage%2Funtriaged&template=bug-report.md).
Now, be patient and wait for someone to propose solution.

### try to find/catch error in Uno source
This step is described [here](https://platform.uno/docs/articles/uno-development/debugging-uno-ui.html).
Do not miss "Faster dev loop with single target-framework builds" and "Debugging Uno.UI" sections.

Try to change Uno code, compile it. Then switch to your app solution, clean, build (beware: use Uno.UI nuget package in same version as in UnoNugetOverrideVersion node in crosstargeting_override.props file).
Test. If it works, you have solution, and you can treat this solution as your hidden secret. But you can also share this solution, by creating Pull Request, to be included in future versions of Uno.

Every PR should contain not only code change, but also test code (automatic test), and doc change. See also [Guide for creating test](https://platform.uno/docs/articles/uno-development/working-with-the-samples-apps.html).
But, for now, assume that someone else would create test for your PR, and you can/want only to share your bug solution.

### for each bug, 
#### creating PR from changes in one file
If your correction is contained in one file only (and tested!), go to [Uno source code](https://github.com/unoplatform/uno/tree/master/src), find file you changed in your Uno clone.
Change it, accept changes, create PR (it happens almost automatically).
#### creating PR from changes in many files
If your change is spread in more than one file, you have to create Git clone of Uno, change first file - but do not commit to your master branch - choose create new branch option.
Make all changes, all in same branch. Then create PR from this branch (it happens almost automatically).


In next part, we will deal with adding features to Uno, and with creating tests.
