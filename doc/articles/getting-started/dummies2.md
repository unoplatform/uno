# How to contribute to Uno? My experience, part 2.

[Previous part](dummies1.md) tells how to run first build of app ported from 'plain UWP' to Uno, targetting UWP.
Now it is time for some guidelines about dealing with Uno0001 warnings, in other words: how to enable app to be build for other targets.

You have several options, from simplest to hardest:

## update Uno.UI/Uno.Core to last stable version
As Uno gets new features constantly, maybe UWP feature is already added.
Use Tools / NuGet Package Manager / Manage NuGet Packages for Solution, Updates tab, to check if Uno.Core/Uno.UI can be updated.
Or, check this file: [ReleaseNotes](https://github.com/unoplatform/uno/blob/master/doc/ReleaseNotes/_ReleaseNotes.md) (skip "next version" section)

## update Uno.UI/Uno.Core to last dev version
Or maybe feature is added, and you can use it - with small risk that something is broken.
Check "next version" section in previously mentioned file: [ReleaseNotes](https://github.com/unoplatform/uno/blob/master/doc/ReleaseNotes/_ReleaseNotes.md).
If it seems that your problem is resolved, install latest dev version (in Manage NuGet Packages for Solution, check 'include prerelease' checkbox).

## check PR (Pull Requests)
Your next hope is that someone already make necessary changes to Uno, but these changes are not included even in dev version (yet). It means that some discussion about implementation is under way, or maybe some tests are failing. But you can expect this feature in some near time appear in at least dev version.

List of open PR can be found here: [Pull Requests](https://github.com/unoplatform/uno/pulls).

## consider workaround
Maybe feature is not implemented, but some "very close" feature is. For example, maybe your app is using CalendarDatePicker (not implemented in Uno 1.45). But Uno implement DatePicker, and this allows to select date, and besides that, this implementation in many ways resemble CalendarDatePicker from UWP.
So, you can use CalendarDatePicker in UWP, and DatePicker in all other. It can be done using XAML prefixes, in this case:

`<win:CalendarDatePicker Name="yourName" DateChanged="myDateChanged" ... />`

`<non_win:DatePicker Name="youName" DateChanged="myDateChangedDP" ... />`

and add second event handler, myDateChangedDP - it has to be new handler, as CalendarDatePicker.DateChanged and DatePicker.DateChanged handlers have different argument types.

All prefixes list: [platform-specific-xaml](https://github.com/unoplatform/uno/blob/master/doc/articles/platform-specific-xaml.md).

## search for NuGet package for Xamarin
Maybe your app sends Toasts. They are not implemented in Uno, but you can use this: https://github.com/edsnider/LocalNotificationsPlugin .
Similar solution exist for using Clipboard, and probably for many more problems.

## create an Issue (feature request)
If all this fails, you can request adding new feature to Uno.

Before submitting new Issue, check if someone else doesn't submit such issue before: [Issues](https://github.com/unoplatform/uno/issues).

If not, use this: [new feature request](https://github.com/unoplatform/uno/issues/new?labels=kind%2Fenhancement%2C+triage%2Funtriaged&template=enhancement.md).

## contribute to Uno
This is best (or: preferable) option, if using newer Uno versions / checking existing PR doesn't solve your problem. Best, but also it requires much more time from your side.
It will be covered in another article.

## make strip-down version
As last resort, you can make some app functions work only under Windows, and WASM/iOS/Android versions somehow limited.

But for now, assume that you found some workaround, and now you are ready to build (or: try to build...) app for target other than UWP. This would be our next topic.
