#nullable enable

using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Uno.UI.Runtime.AppleUIKit;

/// <summary>
/// Hosts the single window of an app in <see cref="AppleUIKitLifecycleMode.AdoptedScene"/> mode in its application
/// scene, and drives the window from that scene's lifecycle.
/// </summary>
/// <remarks>
/// The window is created up front, as under the app lifecycle, and moved into the application scene when it
/// connects, or when the window is created if the scene connected first. A disconnect keeps the window for the next
/// scene. This is the single-window case of the session-keyed binding proposed for scene-manifest apps in #24814.
/// UIKit callbacks all run on the main thread.
/// </remarks>
internal static class AdoptedSceneBinder
{
	private static NativeWindowWrapper? _window;
	private static UIWindowScene? _scene;
	private static bool _isInBackground;

	/// <summary>
	/// Gets whether <paramref name="scene"/> is the app's application scene, rather than one a newer scene replaced.
	/// </summary>
	internal static bool IsAppScene(UIScene scene) => _scene?.Handle == scene.Handle;

	/// <summary>
	/// Gets whether the app is in the background, which, unlike the app lifecycle, the scene lifecycle also reports
	/// the app leaving as it launches.
	/// </summary>
	internal static bool IsInBackground => _isInBackground;

	internal static void SetWindow(NativeWindowWrapper wrapper)
	{
		if (_window is not null)
		{
			return;
		}

		_window = wrapper;

		if (_scene is { } scene)
		{
			Host(wrapper, scene);

			// The scene entered the foreground before the window existed. This counts the window as visible, the XAML
			// events being deferred until it is shown, and may run the app's Resuming handlers, which must not fail the
			// window's creation.
			if (scene.ActivationState is UISceneActivationState.ForegroundActive or UISceneActivationState.ForegroundInactive)
			{
				NativeCallbackGuard.Run(wrapper.OnSceneEnteredForeground);
			}
		}
	}

	/// <summary>
	/// Forgets a window the app closed, so that the app-level events no longer go through it.
	/// </summary>
	internal static void OnWindowClosed(NativeWindowWrapper wrapper)
	{
		if (_window == wrapper)
		{
			_window = null;
		}
	}

	internal static void Connect(UIWindowScene scene)
	{
		if (_scene is { } previousScene
			&& previousScene.Handle != scene.Handle
			&& previousScene.Delegate is AdoptedSceneDelegate previousDelegate)
		{
			// A scene taking over the window from one that has not disconnected yet.
			previousDelegate.Window = null;
		}

		_scene = scene;

		if (_window is { } window)
		{
			Host(window, scene);
			return;
		}

		var log = typeof(AdoptedSceneBinder).Log();

		if (log.IsEnabled(LogLevel.Debug))
		{
			log.Debug($"Scene {scene.Session?.PersistentIdentifier} connected before the app window, which it waits for.");
		}
	}

	internal static void Disconnect(UIScene scene)
	{
		if (!IsAppScene(scene))
		{
			return;
		}

		// UIKit also disconnects a background scene to reclaim memory, and the same session may reconnect: the window
		// is kept for the next application scene rather than closed. The app is in the background until then, so that
		// scene entering the foreground is forwarded to the application delegate.
		_scene = null;
		_isInBackground = true;
		_window?.OnSceneEnteredBackground();

		var log = typeof(AdoptedSceneBinder).Log();

		if (log.IsEnabled(LogLevel.Debug))
		{
			log.Debug($"Scene {scene.Session?.PersistentIdentifier} disconnected, the app window is kept for the next scene.");
		}
	}

	internal static void OnEnteredForeground()
	{
		var wasInBackground = _isInBackground;
		_isInBackground = false;

		if (_window is { } window)
		{
			window.OnSceneEnteredForeground();
		}
		else if (wasInBackground)
		{
			// Without a window, the app-level events are raised as under the app lifecycle.
			AppleUIKitLifecycle.RaiseAppLeavingBackground(static () => { });
		}
	}

	internal static void OnEnteredBackground()
	{
		_isInBackground = true;

		if (_window is { } window)
		{
			window.OnSceneEnteredBackground();
		}
		else
		{
			AppleUIKitLifecycle.RaiseAppEnteredBackground();
		}
	}

	internal static void OnActivationChanged(CoreWindowActivationState state) => _window?.OnSceneActivationChanged(state);

	/// <summary>
	/// Keeps the window filling its scene when the scene is resized, for instance by iPad multitasking.
	/// </summary>
	internal static void OnCoordinateSpaceChanged(UIWindowScene scene)
	{
		if (IsAppScene(scene) && _window?.NativeWindow is { } window)
		{
			window.Frame = scene.CoordinateSpace.Bounds;
		}
	}

	private static void Host(NativeWindowWrapper wrapper, UIWindowScene scene)
	{
		var log = typeof(AdoptedSceneBinder).Log();

		if (wrapper.NativeWindow is not { } window)
		{
			if (log.IsEnabled(LogLevel.Warning))
			{
				log.Warn("The app window has no native window to host in its scene.");
			}

			return;
		}

		wrapper.AttachToScene(scene);

		if (scene.Delegate is AdoptedSceneDelegate sceneDelegate)
		{
			sceneDelegate.Window = window;
		}

		if (log.IsEnabled(LogLevel.Debug))
		{
			log.Debug($"App window hosted in scene {scene.Session?.PersistentIdentifier}.");
		}
	}
}
