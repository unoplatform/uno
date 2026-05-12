#nullable enable

using System;
using Microsoft.UI.Xaml;

namespace Uno.UI.Hosting;

/// <summary>
/// Internal contract for trusted hosts that need to load and launch a secondary
/// <see cref="Microsoft.UI.Xaml.Application"/> in a child
/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/> on platforms where the
/// native application object (UIKit's <c>UIApplication</c>, Android's
/// <c>android.app.Application</c>) is a process singleton and a second
/// <see cref="UnoPlatformHostBuilder"/>-backed host is therefore unusable.
/// </summary>
/// <remarks>
/// <para>
/// Why <c>internal</c>: this helper relies on otherwise non-public Uno internals
/// (<see cref="LaunchActivatedEventArgs"/>'s parameterless constructor and the
/// <c>protected internal</c> <see cref="Application.OnLaunched"/> override hook).
/// Exposing it as <c>public</c> would invite app-developer code to call it directly,
/// where it would silently bypass the platform's normal activation pipeline and
/// drop the activation arguments the OS actually delivered.
/// </para>
/// <para>
/// Stability contract — this is the load-bearing part of the design.
/// </para>
/// <para>
/// Downstream hosts that need to launch a secondary ALC application on native mobile
/// reach this helper via <c>System.Reflection</c> against the type's full name and
/// the two member signatures below. The members are <c>internal</c> so the host
/// cannot directly link against them, but the names, parameter types, and
/// behavior are frozen — Uno commits not to rename, retype, or remove them without
/// a deprecation cycle, exactly as it would for a public API. The internal
/// visibility constrains accidental in-process use; the frozen-signature commitment
/// is what makes the reflective access reliable. See
/// <c>specs/000-alc-secondary-app-support.md</c> § "Native Mobile Secondary App
/// Launch" for the full rationale.
/// </para>
/// <para>
/// The helper deliberately does NOT marshal calls onto a UI thread. Callers must
/// invoke <see cref="LaunchSecondary"/> on the platform UI thread themselves
/// (the iOS main dispatch queue or Android's main looper) because UI threading
/// requirements are platform-host-specific and cannot be encoded once at this layer.
/// </para>
/// <para>
/// On platforms where <see cref="UnoPlatformHostBuilder"/> already supports a second
/// host instance (Skia, WebAssembly), prefer that API; this helper exists for native
/// mobile targets where the platform's application singleton prevents that approach.
/// </para>
/// </remarks>
internal static class SecondaryApplicationLauncher
{
	/// <summary>
	/// Creates a default <see cref="LaunchActivatedEventArgs"/> equivalent to the
	/// arguments Uno produces when reporting a normal launch on each platform.
	/// </summary>
	/// <remarks>
	/// Equivalent to <c>new LaunchActivatedEventArgs(ActivationKind.Launch, arguments: null)</c>
	/// inside Uno.UI. Exposed here so external hosts do not need to reach for the
	/// internal constructor via reflection.
	/// </remarks>
	internal static LaunchActivatedEventArgs CreateDefaultLaunchActivatedEventArgs()
		=> new LaunchActivatedEventArgs();

	/// <summary>
	/// Invokes <see cref="Microsoft.UI.Xaml.Application.OnLaunched"/> on the supplied
	/// secondary <paramref name="app"/> instance with a default
	/// <see cref="LaunchActivatedEventArgs"/>.
	/// </summary>
	/// <param name="app">
	/// The secondary application instance to launch. Typically constructed by a factory
	/// delegate from an assembly loaded into a child
	/// <see cref="System.Runtime.Loader.AssemblyLoadContext"/>.
	/// </param>
	/// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/>.</exception>
	/// <remarks>
	/// Must be called on the platform UI thread. The caller is responsible for
	/// dispatching onto that thread (e.g.
	/// <c>CoreFoundation.DispatchQueue.MainQueue.DispatchAsync</c> on iOS, or
	/// <c>new Android.OS.Handler(Android.OS.Looper.MainLooper).Post</c> on Android).
	/// </remarks>
	internal static void LaunchSecondary(Application app)
	{
		if (app is null)
		{
			throw new ArgumentNullException(nameof(app));
		}

		app.OnLaunched(CreateDefaultLaunchActivatedEventArgs());
	}
}
