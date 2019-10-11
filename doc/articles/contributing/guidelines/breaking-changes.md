# Guidelines for breaking changes

## overview

Uno uses a [Package Diff tool](https://github.com/unoplatform/uno.PackageDiff) to ensure that [binary breaking changes](https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/breaking-changes#binary-breaking-change) do not
go unnoticed. As [part of our continuous integration process](https://github.com/unoplatform/uno/blob/1a786a652394f5a3d674fadfdd7b459f8f476a1b/build/Uno.UI.Build.csproj#L201) the PackageDiffTool consumes the last published non-experimental package available on nuget.org, and compares it with the current PR.

This process only diffs against previous versions of Uno, not against the UWP assemblies, so it doesn't pick up all forms of mismatches. There are [some inconsistencies](https://github.com/unoplatform/uno/pull/1300) dating from before SyncGenerator was added. At some point it might be a good idea to extend SyncGenerator tool to try to report them all (or even automatically fix them)

## rule of thumb

* Binary breaking changes, by default, are considered unacceptable.
* Uno is an implementation of the `Windows.UI.Xaml` APIs and as such the public API surface of UWP dictates the direction of Uno.
* Thus, the rule of thumb is to correct Uno to match UWP but proceed with extra caution.

## blessing breaking changes

In most cases, breaking changes are not acceptable, but in cases where there is no easy work around the [build/PackageDiffIgnore.xml](https://github.com/unoplatform/uno/blob/master/build/PackageDiffIgnore.xml) file can be adjusted to bless the changes. Please refer
to the documentation of the [Uno.PackageDiff tool](https://github.com/unoplatform/uno.PackageDiff) for more information.

## example report

Below is a comparison report for Uno.UI **1.45.0** with Uno.UI **1.46.0-PullRequest1300.2330** as part of [this pull-request](https://github.com/unoplatform/uno/pull/1300).

You can find the report within the `NuGetPackages.zip` archive on the build server and attached to the pull-request job. The report uses this convention: `ApiDiff.1.46.0-PullRequest1300.2330.md`

Key things to pay attention to:
- the report is for each TFM and the assemblies for that TFM.
- breaking changes which have been blessed have been ~~striked out~~.
- `Windows.UI.Xaml.ResourceDictionary::MergedDictionaries()` is the breaking change which has not been blessed and thus the process failed the build.

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
