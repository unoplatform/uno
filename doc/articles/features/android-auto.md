---
uid: Uno.Features.AndroidAuto
---

# Support for Android Auto

Uno Platform supports Android Auto and Android Automotive OS, allowing your application to extend into the in-car experience using the AndroidX Car App Library.

## Enabling Android Auto support via `UnoFeatures` (recommended)

For projects using the `Uno.Sdk`, add the `AndroidAuto` feature to your project's `<UnoFeatures>` property:

```xml
<UnoFeatures>$(UnoFeatures);AndroidAuto</UnoFeatures>
```

This automatically adds a reference to the `Xamarin.AndroidX.Car.App.App` package. New projects created from the Uno Platform templates with the **Android Auto** option enabled will also have the required manifest entries and the `automotive_app_desc.xml` resource stamped automatically.

## What gets configured

When the `AndroidAuto` feature is enabled, the template stamps the following declarations into your `AndroidManifest.xml`:

```xml
<meta-data android:name="com.google.android.gms.car.application"
           android:resource="@xml/automotive_app_desc" />
```

It also adds an `automotive_app_desc.xml` resource file under `Resources/xml/`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<automotiveApp>
    <uses name="media" />
</automotiveApp>
```

The `<uses>` element declares which categories of Auto experiences your app provides (`media`, `template`, etc.). For a navigation, point-of-interest, or messaging app, change `media` to `template` and add a `CarAppService` declaration in your manifest.

## Implementing the Car App Service

To host content in Android Auto / Automotive OS, you must provide a `CarAppService` derived class that exposes a `Session` and root `Screen`. Refer to the [Android Auto developer guide](https://developer.android.com/training/cars/apps) and the [AndroidX Car App Library documentation](https://developer.android.com/reference/androidx/car/app/CarAppService) for the full API surface.

## Testing your application

You can test Android Auto integration locally using the Desktop Head Unit (DHU) tool that ships with Android Studio. Pair a phone (real or emulator) running your application to DHU and confirm the app appears in the Auto launcher.

## Shipping an Auto-only or Automotive-only app alongside a phone app

The opt-in described above produces a single APK that exposes both a phone experience and an Auto / Automotive experience. If you need to publish them separately (for example, an Automotive OS-only build for in-car infotainment), the recommended approach is to ship **two separate Uno Platform apps**:

1. **A "phone" Uno Platform app** — your existing Uno project, without the `AndroidAuto` opt-in.
2. **A "car" Uno Platform app** — a second Uno project dedicated to the in-car experience with the `AndroidAuto` opt-in enabled.

To set up the car-only app:

- Create a new Uno Platform app (`dotnet new unoapp -o MyApp.Car`) targeting Android, and enable the `AndroidAuto` opt-in.
- For an Automotive OS-only build, add `<uses-feature android:name="android.hardware.type.automotive" android:required="true" />` to the manifest so the APK only installs on Automotive OS head units.
- Use a distinct application id (for example `com.companyname.myapp.car`) so the car app and phone app can coexist on the Play Store as separate listings.
- Move shared code (view models, services, resources) into a class library and reference it from both apps.
