---
uid: Uno.Features.AndroidWear
---

# Support for Wear OS (Android Watch)

Uno Platform supports Wear OS as an opt-in for the existing Android head, enabling a single APK that can target both phones and watches.

## Enabling Wear OS support via `UnoFeatures` (recommended)

For projects using the `Uno.Sdk`, add the `AndroidWear` feature to your project's `<UnoFeatures>` property:

```xml
<UnoFeatures>$(UnoFeatures);AndroidWear</UnoFeatures>
```

This automatically adds references to the `Xamarin.AndroidX.Wear` and `Xamarin.AndroidX.Wear.Tiles` packages. New projects created from the Uno Platform templates with the **Wear OS** option enabled will also have the required `<uses-feature>` manifest declaration stamped automatically.

## What gets configured

When the `AndroidWear` feature is enabled, the template stamps the following declaration into your `AndroidManifest.xml`:

```xml
<uses-feature android:name="android.hardware.type.watch" android:required="false" />
```

`required="false"` means the same APK can also install on phones — Wear-specific code paths simply remain dormant on non-watch devices.

## Designing for round and small displays

Watches typically have very small displays — round, square, or chin-cut variants. When designing UI for Wear OS, prefer:

- Vertical scrolling with `ScrollViewer` and large touch targets.
- Avoid horizontal page navigation that conflicts with the system swipe-to-dismiss gesture.
- Use the `WearableNavigationDrawer` and `WearableActionDrawer` from `Xamarin.AndroidX.Wear` for navigation that adapts to round bezels.
- Build glanceable Tiles using the `Xamarin.AndroidX.Wear.Tiles` package.

## Standalone Wear-only APKs

Production-grade Wear apps are typically distributed as **standalone Wear-only APKs** rather than as a dual-purpose phone+watch APK. The `AndroidWear` opt-in described above is intended for the dual-form-factor case where the same project also targets phones. For the standalone case, the recommended approach is to ship **two separate Uno Platform apps**:

1. **A "phone" Uno Platform app** — your existing Uno project, targeting Android (and any other platforms you need). Do not enable the `AndroidWear` `<UnoFeatures>` opt-in here.
2. **A "watch" Uno Platform app** — a second Uno project dedicated to Wear OS. Both apps can share UI and business logic via class libraries that they both reference, but each ships its own APK with its own application id and manifest tailored to its form factor.

To set up the standalone watch app:

- Create a new Uno Platform app (`dotnet new unoapp -o MyApp.Watch`) targeting only Android, and enable the `AndroidWear` opt-in.
- In `MyApp.Watch/Platforms/Android/AndroidManifest.xml`, change `android:required` on the `android.hardware.type.watch` `<uses-feature>` element from `false` to `true`. This filters the APK out of the phone Play Store listing so it is only installable on watches.
- Use a distinct application id (for example `com.companyname.myapp.watch`) so the watch app and phone app can coexist on the same paired device.
- Move shared code (view models, services, resources) into a class library and reference it from both apps.

This split mirrors Google's recommendation for Wear OS distribution and gives each form factor an independent release cycle.

## Testing your application

You can test Wear OS support using the Wear OS emulator images available in Android Studio's device manager. Create a Round or Square Wear emulator (API 33 or higher recommended) and deploy your app directly. For the dual-form-factor pattern, also pair the Wear emulator to a phone emulator and verify that the app installs and launches on both devices.
