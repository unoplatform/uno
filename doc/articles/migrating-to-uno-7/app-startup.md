---
uid: Uno.Development.MigratingToUno7.AppStartup
---

# App startup, activation and platform state

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** you customize an Android `Application` class, override `Application.OnActivated`, subscribe to `Application.Suspending` and friends, handle activations on iOS or tvOS, read `ApplicationData` settings on iOS or tvOS, or use `ContextHelper` on Android.

**Skip it if** your app targets none of these and uses the unmodified template `App` class.

## Android head uses the host builder

The Android head now builds its host through `UnoPlatformHostBuilder`, like every other target.
`NativeApplication` (now `Uno.UI.Runtime.Android.NativeApplication`, see [Runtime host renames](xref:Uno.Development.MigratingToUno7.Renames#runtime-host-renames)) is now `abstract`; its `AppBuilder` delegate type and the
constructor taking one have been removed, replaced by an abstract `CreateHost()` method.

```csharp
// Before
public class Application : Microsoft.UI.Xaml.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(() => new App(), javaReference, transfer)
    {
    }
}

// After
using Uno.UI.Hosting;

public class Application : Uno.UI.Runtime.Android.NativeApplication
{
    public Application(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override UnoPlatformHost CreateHost() =>
        UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseAndroid()
            .Build();
}
```

Two related behavior changes:

- **Startup failures now propagate.** Previously an exception during Android host initialization
  was written to the console and swallowed, leaving a blank screen. It is now logged and rethrown.
  Apps with latent initialization failures that used to start degraded will now crash on launch.
- **`Uno.UI.Runtime.Skia.Android.AndroidHost` is now `internal`.** Use `UseAndroid()` instead of
  constructing it.

See [Customizing the `Application` class on Android](xref:Uno.Features.CustomizingAndroidApplication).

## Activation is read from `AppInstance`

**`Application.OnActivated` is removed.** It is not part of the shipping WinUI 3 surface —
`microsoft.ui.xaml.coretypes2.idl` declares it only behind the UWP-support feature gate — and a Windows
App SDK app discovers how it was activated through `Microsoft.Windows.AppLifecycle.AppInstance` instead.
An existing override now fails to compile, which is deliberate: a method that silently stopped being
invoked would be far harder to notice than a build error.

`OnLaunched` always runs, and always receives plain `ActivationKind.Launch` arguments, matching WinUI's
`FrameworkApplication`, which synthesizes them regardless of how the process was activated. The real
payload comes from `AppInstance`:

```csharp
// Before
protected override void OnActivated(IActivatedEventArgs args)
{
    if (args.Kind == ActivationKind.Protocol)
    {
        var uri = ((ProtocolActivatedEventArgs)args).Uri;
        // …
    }
}

// After
using Microsoft.Windows.AppLifecycle;

protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    // …create and activate the window as usual…

    var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
    if (activation.Kind == ExtendedActivationKind.Protocol &&
        activation.Data is Windows.ApplicationModel.Activation.ProtocolActivatedEventArgs protocol)
    {
        var uri = protocol.Uri;
        // …
    }
}
```

`GetActivatedEventArgs()` never returns `null` and never throws; with no activation payload it reports
`ExtendedActivationKind.Launch` over the process command line. It always describes the activation the
process *started* with, so an activation that arrives while the app is already running is raised on
`AppInstance.GetCurrent().Activated` — subscribe to it in the `App` constructor. See
[Custom protocol activation](xref:Uno.Features.ProtocolActivation) for a complete `App.xaml.cs`.

**On iOS and tvOS under the [UIKit scene lifecycle](#uikit-scene-lifecycle-on-ios-and-tvos), a cold-start
activation arrives after `OnLaunched`.** UIKit only hands the app the URL, universal link or home-screen
shortcut it was started with when the first scene connects, which happens after `OnLaunched` has run. At
that point `GetActivatedEventArgs()` still reports `Launch`; the real activation is then raised on
`AppInstance.GetCurrent().Activated`, and `GetActivatedEventArgs()` returns it from then on. Handle the
`Activated` event as well as reading `GetActivatedEventArgs()` in `OnLaunched`, and the app gets the
activation on every target.

**`Application.OnLaunched` is `protected virtual`**, not Uno's `protected internal virtual`, matching
WinUI. Declare the override as `protected override void OnLaunched(LaunchActivatedEventArgs args)` —
which is what the templates generate, and what C# already required of an override in another assembly.

**On WebAssembly, `LaunchActivatedEventArgs.Arguments` no longer carries the internal
`unoprotocolactivation` query key** that transports a protocol activation across the browser
navigation. The key is stripped before the app sees the arguments, so code that parsed it out of
`Arguments` should read the URI from `GetActivatedEventArgs()` instead.

## App lifecycle events move to `CoreApplication`

**`Application.Suspending`, `Application.Resuming`, `Application.EnteredBackground` and
`Application.LeavingBackground` are removed.** Subscribe to the events of the same names on
`Windows.ApplicationModel.Core.CoreApplication`, which Uno Platform already raised in lockstep with
these — including the `SuspendingOperation` deferral, so handler bodies need no change:

```csharp
// Before
this.Suspending += OnSuspending;
this.Resuming += OnResuming;

// After
using Windows.ApplicationModel.Core;

CoreApplication.Suspending += OnSuspending;
CoreApplication.Resuming += OnResuming;
```

The handler signature changes from the `SuspendingEventHandler` family to
`EventHandler<SuspendingEventArgs>`, so a nullable-enabled file needs `object? sender`. The
`SuspendingEventHandler`, `EnteredBackgroundEventHandler` and `LeavingBackgroundEventHandler`
delegates themselves remain — they are still Windows App SDK types.

Like `OnActivated`, all four sat behind the UWP-support feature gate in WinUI: the shipping WinUI 3
`Application` exposes only `ResourceManagerRequested` and `UnhandledException`. `CoreApplication` is
also the more portable choice, being Windows Runtime API that compiles against the Windows App SDK,
which `Application.Suspending` does not.

**On Skia Desktop, suspension is no longer reported at all.** Windows App SDK desktop apps have no
suspended state, and Uno Platform previously reported one on Windows only, when a window closed —
which also skipped the `EnteredBackground` that precedes `Suspending`. Save state from
`Window.Closed` instead. Android, iOS and tvOS report the real platform lifecycle as before.

**`Application.RequiresPointerMode` is now `internal`.** It is not part of the public Windows App SDK
`Application` surface either, and nothing in Uno Platform's rendering depends on the value an app sets.
Delete the assignment; there is no replacement to move to.

## UIKit scene lifecycle on iOS and tvOS

Apps built with Xcode 27 and its version 27 SDKs must use the UIKit scene lifecycle, whatever
their .NET version, or iOS and tvOS 27 refuse to launch them. An existing app needs no scene manifest
for this: when its `Info.plist` has none, Uno Platform adopts the scene lifecycle on its behalf
once the app is built with a version 27 SDK, and hosts the app's window in its scene. The switch
therefore comes with the move to Xcode 27, not with the upgrade to 7.0: an app built with an
older SDK keeps the app lifecycle unless it sets `UnoUseSceneLifecycle`. An app that declares a
manifest keeps using it.

Under the scene lifecycle UIKit no longer calls the application delegate's URL, user activity,
home-screen shortcut, and foreground and background methods. For an app without a manifest, Uno
Platform forwards them to the application delegate, so a custom `UnoUIApplicationDelegate` keeps
receiving them. The activation the app was launched with then arrives that way after `OnLaunched`,
rather than in `FinishedLaunching`'s launch options. See
[Scene lifecycle](xref:Uno.Features.CustomizingUIApplicationDelegate#scene-lifecycle), and for how
the app reads that activation, [Activation is read from `AppInstance`](#activation-is-read-from-appinstance).

## Application settings on iOS and tvOS

Values stored through `ApplicationData.Current.LocalSettings` / `.RoamingSettings` used to be
written directly into the shared `NSUserDefaults.StandardUserDefaults` domain. In 7.0 they
are stored in a dedicated `NSUserDefaults` suite named `UnoApplicationData`, persisted as
`Library/Preferences/UnoApplicationData.plist` inside the app sandbox.

This isolates Uno-managed settings from the keys the OS, Apple frameworks, and native
libraries keep in the standard domain: enumerating (`Values.Keys`, `Values.Count`) or
clearing (`Values.Clear()`) application settings no longer sees — or deletes — unrelated
native keys.

**Nothing is migrated for you.** A new app is unaffected. An app updating from an earlier
Uno Platform version keeps its old values in the standard defaults, where the
`ApplicationData` API no longer looks — until you ask for them:

```csharp
#if __IOS__ || __TVOS__
// Call once during startup, before the settings are first read.
var migrated = Uno.Storage.ApplicationDataMigrator.MigrateSettings();
#endif
```

`MigrateSettings()` moves the entries an earlier Uno Platform version wrote (recognized by
Uno's serialized `TypeName:value` format) out of the standard defaults and into the
`UnoApplicationData` suite, and returns how many it took. It is safe to call on every
launch: a key that already exists in the new suite keeps its current value, and an install
with nothing to migrate is a no-op. If the new suite cannot be saved, it throws an
`IOException` and leaves the old values where they are, so a later launch can retry.

Also update native/interop code that reads these values directly from the standard defaults:

```csharp
// In 7.0 and later, read the values from the dedicated UnoApplicationData suite
// (before 7.0 they were in NSUserDefaults.StandardUserDefaults)
var unoDefaults = new NSUserDefaults("UnoApplicationData", NSUserDefaultsType.SuiteName);
```

```swift
// Swift companion code
let unoDefaults = UserDefaults(suiteName: "UnoApplicationData")
```

Values your app writes to the standard defaults itself through native APIs are not
affected — they stay where they are and remain invisible to `ApplicationData`, as before.

> [!IMPORTANT]
> The migration is one-way. Once you call `MigrateSettings()`, the moved values are gone from
> `NSUserDefaults.StandardUserDefaults`, so downgrading to a pre-7.0 build of your app will
> not find them there anymore.

See [Application Data and Settings](xref:Uno.Features.ApplicationData) for details on where
each platform stores its data.

## Android context APIs

Skia-on-Android no longer keeps the window, its render stack, or its input sources in
process-wide statics, so the ambient Android context APIs changed shape:

- **`Uno.UI.ContextHelper.Current` is now typed `Android.Content.Context?`.** It has always been
  able to be `null` — before any activity is created — but the property was annotated non-null,
  so the compiler never surfaced it. Code that dereferences it directly will now warn under
  nullable reference types. Guard it, or use the new `ContextHelper.ApplicationContext` when a
  process-wide context is all that is needed (system services, resources, package info).
- **`Uno.UI.ContextHelper.Current` now tracks the most recently active live activity.** It stays
  on an activity while it is paused or stopped, so background work still resolves one, and moves
  to another live activity (or `null`) when that activity is destroyed. It previously kept the
  last activity ever assigned, including a destroyed one. It is not necessarily the foreground
  activity, so avoid it for work that needs a resumed activity.
- **The `Uno.UI.ContextHelper.Current` setter is now `internal`.** The value is owned by the
  activity lifecycle (`BaseActivity`), which overwrote any value app code assigned.
- **`Uno.UI.ViewManagement.ApplicationViewHelper.GetBaseActivityEvents()` and
  `IBaseActivityEvents` were removed.** No Skia activity implements the interface, so the method
  always returned `null`. Observe activity lifecycle events with Android's
  `Application.RegisterActivityLifecycleCallbacks` instead. The `Uno.UI.ViewManagement.Activity*Handler`
  delegates it used were removed with it.
- **`Uno.UI.OnSystemUiVisibilityChangeListener` is now `internal`.** It is constructed by the host
  with the activity that owns the window; app code had no way to supply one.
