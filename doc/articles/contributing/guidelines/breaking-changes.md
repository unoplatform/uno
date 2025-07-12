---
uid: Uno.Contributing.BreakingChanges
---

# Guidelines for breaking changes

## Overview

Uno uses a [Package Diff tool](https://github.com/unoplatform/uno.PackageDiff) to ensure that [binary breaking changes](https://learn.microsoft.com/dotnet/standard/library-guidance/breaking-changes#binary-breaking-change) do not
go unnoticed. As [part of our continuous integration process](https://github.com/unoplatform/uno/blob/b1a6eddcad3bcca6d9756b0a57ff6cf458321048/build/Uno.UI.Build.csproj#L529), the PackageDiffTool consumes the last published non-experimental package available on nuget.org, and compares it with the current PR.

This process only diffs against previous versions of Uno, not against the WinUI assemblies, so it doesn't pick up all forms of mismatches. There are [some inconsistencies](https://github.com/unoplatform/uno/pull/1300) dating from before SyncGenerator was added. At some point it might be a good idea to extend SyncGenerator tool to try to report them all (or even automatically fix them)

Breaking changes must be marked as such when committed using the [Conventional Commits formatting](xref:Uno.Contributing.ConventionalCommits).

## When are binary breaking changes acceptable?

### Breaking cross-platform compatibility - not ok

Changes that break compatibility with the public API surface of WinUI are generally never acceptable, because they not only break existing code but break cross-platform compatibility as well.

### Restoring cross-platform compatibility - ok, but discuss with core team

In some cases, Uno's existing API is close to WinUI, but not identical. (Hypothetical example: a property with type `DependencyObject[]` on Uno, but type `IList<DependencyObject>` on WinUI.) This is mostly the case for older code that was written before the use of [generated `NotImplemented` stubs](xref:Uno.Contributing.Overview#generated-notimplemented-stubs) and the `PackageDiff` tool, which act in combination to prevent these kinds of errors when implementing new features.

In these cases, we do want to align Uno with WinUI, even at the expense of a breaking change. However, we tend to be more careful with when we merge these changes, compared to other bugfixes. We prefer to 'batch' many such breaking changes into a single stable release cycle, rather than wear out consumers' patience with a steady trickle of breaking changes each release.

The best way to proceed is to create an issue if one doesn't exist already, and open a discussion with the core team about the change in question, so we can jointly work out how best to manage it.

Note that some cases may be sufficiently benign that the breaking change is acceptable in a normal release cycle. (For example, removing a public constructor for an obscure `EventArgs` subclass that would presumably never be created from user code anyway.)

### Breaking changes to Uno-only APIs - it depends

The diff tool guards against all changes to Uno's public API surface, including functionality that has no equivalent in WinUI's API surface.

In the cases where these Uno-only APIs are exposed intentionally (example: the [`VisibleBoundsPadding` behavior](xref:Uno.Features.VisibleBoundsPadding)), we would usually reject breaking changes, unless there were a very compelling reason for them.

In other cases, this might be functionality that's inadvertently exposed - in other words, functionality that was made public when it should really have been internal. Here the validity of the breaking change should be considered on a case-by-case basis, taking into account the risk of breaking existing Uno app code (and conversely, the possibility for the Uno-only APIs to collide with 'brownfield' WinUI code). Again, the principle of 'batching' breaking changes applies.

## Adding breaking changes to the allow list

Where a breaking change is acceptable according to the above criteria, and after discussion with the core team where appropriate, it can be marked as such using the [build/PackageDiffIgnore.xml](../../../../build/PackageDiffIgnore.xml) file.

Please refer to the documentation of the [Uno.PackageDiff tool](https://github.com/unoplatform/uno.PackageDiff) for more information.

## Example report

Below is a comparison report for Uno.UI **1.45.0** with Uno.UI **1.46.0-PullRequest1300.2330** as part of [this pull-request](https://github.com/unoplatform/uno/pull/1300).

You can find the report within the `NuGetPackages.zip` archive on the build server and attached to the pull-request job. The report uses this convention: `ApiDiff.1.46.0-PullRequest1300.2330.md`

Key things to pay attention to:

- the report is for each TFM and the assemblies for that TFM.
- breaking changes which have been blessed have been ~~struck out~~.
- `Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()` is the breaking change which has not been blessed and thus the process failed the build.

```md
## MonoAndroid80 Platform
#### Uno.dll
##### 0 missing types:
##### 8 missing or changed method in existing types:
- `Windows.Phone.Devices.Notification.VibrationDevice`
    * ~~``System.Void Windows.Phone.Devices.Notification.VibrationDevice..ctor()``~~
- `Windows.Devices.Sensors.Accelerometer`
    * ~~``System.Void Windows.Devices.Sensors.Accelerometer..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReading`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReading..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerShakenEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerShakenEventArgs..ctor()``~~
- `Windows.Devices.Sensors.Barometer`
    * ~~``System.Void Windows.Devices.Sensors.Barometer..ctor()``~~
- `Windows.Devices.Sensors.BarometerReading`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReading..ctor()``~~
- `Windows.Devices.Sensors.BarometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReadingChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Foundation.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.BindingHelper.Android.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.dll
##### 0 missing types:
##### 15 missing or changed method in existing types:
- `Windows.UI.Xaml.Controls.ComboBoxItem`
    * ~~``Windows.UI.Xaml.Controls.Popup Windows.UI.Xaml.Controls.ComboBoxItem.GetPopupControl()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextChangedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextChangedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.Popup`
    * ~~``Android.Views.View Windows.UI.Xaml.Controls.Popup.get_Anchor()``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.Popup.set_Anchor(Android.Views.View value)``~~
- `Windows.UI.Xaml.Controls.TextBoxView`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView..ctor()``~~
    * ~~``System.String Windows.UI.Xaml.Controls.TextBoxView.get_BindableText()``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView.set_BindableText(System.String value)``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView.NotifyTextChanged()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 3 missing or changed properties in existing types:
- `Windows.UI.Xaml.ResourceDictionary`
    * ``Windows.UI.Xaml.ResourceDictionary[] Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()``
- `Windows.UI.Xaml.Controls.Popup`
    * ~~``Android.Views.View Windows.UI.Xaml.Controls.Popup::Anchor()``~~
- `Windows.UI.Xaml.Controls.TextBoxView`
    * ~~``System.String Windows.UI.Xaml.Controls.TextBoxView::BindableText()``~~
#### Uno.UI.Toolkit.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Xaml.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
## MonoAndroid90 Platform
#### Uno.dll
##### 0 missing types:
##### 8 missing or changed method in existing types:
- `Windows.Phone.Devices.Notification.VibrationDevice`
    * ~~``System.Void Windows.Phone.Devices.Notification.VibrationDevice..ctor()``~~
- `Windows.Devices.Sensors.Accelerometer`
    * ~~``System.Void Windows.Devices.Sensors.Accelerometer..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReading`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReading..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerShakenEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerShakenEventArgs..ctor()``~~
- `Windows.Devices.Sensors.Barometer`
    * ~~``System.Void Windows.Devices.Sensors.Barometer..ctor()``~~
- `Windows.Devices.Sensors.BarometerReading`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReading..ctor()``~~
- `Windows.Devices.Sensors.BarometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReadingChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Foundation.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.BindingHelper.Android.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.dll
##### 0 missing types:
##### 15 missing or changed method in existing types:
- `Windows.UI.Xaml.Controls.ComboBoxItem`
    * ~~``Windows.UI.Xaml.Controls.Popup Windows.UI.Xaml.Controls.ComboBoxItem.GetPopupControl()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextChangedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextChangedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.Popup`
    * ~~``Android.Views.View Windows.UI.Xaml.Controls.Popup.get_Anchor()``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.Popup.set_Anchor(Android.Views.View value)``~~
- `Windows.UI.Xaml.Controls.TextBoxView`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView..ctor()``~~
    * ~~``System.String Windows.UI.Xaml.Controls.TextBoxView.get_BindableText()``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView.set_BindableText(System.String value)``~~
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxView.NotifyTextChanged()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 3 missing or changed properties in existing types:
- `Windows.UI.Xaml.ResourceDictionary`
    * ``Windows.UI.Xaml.ResourceDictionary[] Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()``
- `Windows.UI.Xaml.Controls.Popup`
    * ~~``Android.Views.View Windows.UI.Xaml.Controls.Popup::Anchor()``~~
- `Windows.UI.Xaml.Controls.TextBoxView`
    * ~~``System.String Windows.UI.Xaml.Controls.TextBoxView::BindableText()``~~
#### Uno.UI.Toolkit.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Xaml.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
## netstandard2.0 Platform
#### Uno.dll
##### 0 missing types:
##### 7 missing or changed method in existing types:
- `Windows.Phone.Devices.Notification.VibrationDevice`
    * ~~``System.Void Windows.Phone.Devices.Notification.VibrationDevice..ctor()``~~
- `Windows.Devices.Sensors.Accelerometer`
    * ~~``System.Void Windows.Devices.Sensors.Accelerometer..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReading`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReading..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerShakenEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerShakenEventArgs..ctor()``~~
- `Windows.Devices.Sensors.BarometerReading`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReading..ctor()``~~
- `Windows.Devices.Sensors.BarometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReadingChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Foundation.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.dll
##### 0 missing types:
##### 9 missing or changed method in existing types:
- `Windows.UI.Xaml.Controls.ComboBoxItem`
    * ~~``Windows.UI.Xaml.Controls.Popup Windows.UI.Xaml.Controls.ComboBoxItem.GetPopupControl()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextChangedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 1 missing or changed properties in existing types:
- `Windows.UI.Xaml.ResourceDictionary`
    * ``Windows.UI.Xaml.ResourceDictionary[] Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()``
#### Uno.UI.Toolkit.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.Wasm.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Xaml.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
## UAP Platform
#### Uno.UI.Toolkit.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
## xamarinios10 Platform
#### Uno.dll
##### 0 missing types:
##### 8 missing or changed method in existing types:
- `Windows.Phone.Devices.Notification.VibrationDevice`
    * ~~``System.Void Windows.Phone.Devices.Notification.VibrationDevice..ctor()``~~
- `Windows.Devices.Sensors.Accelerometer`
    * ~~``System.Void Windows.Devices.Sensors.Accelerometer..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReading`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReading..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerReadingChangedEventArgs..ctor()``~~
- `Windows.Devices.Sensors.AccelerometerShakenEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.AccelerometerShakenEventArgs..ctor()``~~
- `Windows.Devices.Sensors.Barometer`
    * ~~``System.Void Windows.Devices.Sensors.Barometer..ctor()``~~
- `Windows.Devices.Sensors.BarometerReading`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReading..ctor()``~~
- `Windows.Devices.Sensors.BarometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReadingChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Foundation.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.dll
##### 0 missing types:
##### 10 missing or changed method in existing types:
- `Windows.UI.Xaml.Controls.ComboBoxItem`
    * ~~``Windows.UI.Xaml.Controls.Popup Windows.UI.Xaml.Controls.ComboBoxItem.GetPopupControl()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextChangedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextChangedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.VirtualizingPanelLayout`
    * ~~``System.Void Windows.UI.Xaml.Controls.VirtualizingPanelLayout.UpdateLayoutAttributesForItem(UIKit.UICollectionViewLayoutAttributes layoutAttributes)``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 1 missing or changed properties in existing types:
- `Windows.UI.Xaml.ResourceDictionary`
    * ``Windows.UI.Xaml.ResourceDictionary[] Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()``
#### Uno.UI.Toolkit.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Xaml.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
## xamarinmac20 Platform
#### Uno.dll
##### 0 missing types:
##### 2 missing or changed method in existing types:
- `Windows.Devices.Sensors.BarometerReading`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReading..ctor()``~~
- `Windows.Devices.Sensors.BarometerReadingChangedEventArgs`
    * ~~``System.Void Windows.Devices.Sensors.BarometerReadingChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.Foundation.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
#### Uno.UI.dll
##### 0 missing types:
##### 9 missing or changed method in existing types:
- `Windows.UI.Xaml.Controls.ComboBoxItem`
    * ~~``Windows.UI.Xaml.Controls.Popup Windows.UI.Xaml.Controls.ComboBoxItem.GetPopupControl()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogButtonClickEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosedEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingDeferral`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingDeferral..ctor()``~~
- `Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.ContentDialogClosingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxBeforeTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextBoxTextChangingEventArgs..ctor()``~~
- `Windows.UI.Xaml.Controls.TextChangedEventArgs`
    * ~~``System.Void Windows.UI.Xaml.Controls.TextChangedEventArgs..ctor()``~~
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 1 missing or changed properties in existing types:
- `Windows.UI.Xaml.ResourceDictionary`
    * ``Windows.UI.Xaml.ResourceDictionary[] Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()``
#### Uno.Xaml.dll
##### 0 missing types:
##### 0 missing or changed method in existing types:
##### 0 missing or changed events in existing types:
##### 0 missing or changed fields in existing types:
##### 0 missing or changed properties in existing types:
```
