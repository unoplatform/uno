# Minimum supported target platform versions

The platform requirements for the Uno Platform are as follows:

## Android

- The minimum supported version by the Uno Platform is Android "Lollipop" 5.0 (API 21) from 2014. Android v4.4 and lower is known to work but is unsupported.
- Android has [several API settings](https://docs.microsoft.com/en-us/xamarin/android/app-fundamentals/android-api-levels) that determine your application's compatibility. As a rule of thumb, always [use the latest Android SDK Tools and Android API platform](https://docs.microsoft.com/en-us/xamarin/android/get-started/installation/android-sdk?tabs=windows) during development as you can target older Android versions while using the latest SDK.

## iOS

The minimum supported version by the Uno Platform is iOS 8 from 2014.

## Universal Windows Platform (UWP)

When developing for the Universal Windows Platform (UWP) there are no special requirements. The Uno Platform is a Universal Windows Platform (UWP) Bridge that allows UWP based code to run on iOS, Android, and WebAssembly. Uno provides the full API definitions of the UWP Windows 10 October 2018 Update (17763), and the implementation of parts of the UWP API, such as Windows.UI.Xaml, to enable applications to run on these platforms.

## WebAssembly

WebAssembly is supported in Chrome, Edge, Edge Dev, Opera, Firefox and Safari. See the official WebAssembly site for [more details](https://webassembly.org/roadmap/).
