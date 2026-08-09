#nullable enable

using System;
using Foundation;
using Microsoft.UI.Xaml;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Default <see cref="UISceneDelegate"/> implementation used by Uno Platform on Apple UIKit targets.
/// </summary>
/// <remarks>
/// <para>
/// Creates the native <see cref="UIWindow"/> for a connecting <see cref="UIWindowScene"/>, binds it to
/// the XAML window that requested it, and forwards the scene lifecycle to that window.
/// </para>
/// <para>
/// Apps opting into the scene lifecycle subclass this type and register the subclass as
/// <c>UISceneDelegateClassName</c> in the <c>UIApplicationSceneManifest</c> entry of their Info.plist.
/// Override the <c>OnScene*</c> hooks rather than the <see cref="UISceneDelegate"/> members, which are
/// sealed so that window binding cannot be bypassed by forgetting a base call.
/// </para>
/// </remarks>
public class UnoUISceneDelegate : UISceneDelegate
{
	internal const string GetConfigurationSelectorName = "application:configurationForConnectingSceneSession:options:";
	internal const string UnoSceneConfigurationKey = "__UNO_DEFAULT_SCENE_CONFIGURATION__";
	internal const string UIApplicationSceneManifestKey = "UIApplicationSceneManifest";

	private static readonly bool _hasSceneManifest =
		NSBundle.MainBundle.InfoDictionary?.ContainsKey(new NSString(UIApplicationSceneManifestKey)) == true;

	private NativeWindowWrapper? _wrapper;

	/// <summary>
	/// Gets or sets the native window backing the scene, as required by <see cref="UISceneDelegate"/>.
	/// </summary>
	[Export("window")]
	public UIWindow? Window { get; set; }

	/// <summary>
	/// Gets a value indicating whether the app declares a scene manifest, and therefore runs the
	/// scene-based lifecycle rather than the app-level one.
	/// </summary>
	internal static bool HasSceneManifest => _hasSceneManifest;

	[Export("scene:willConnectToSession:options:")]
	public sealed override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
	{
		try
		{
			if (scene is not UIWindowScene windowScene)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Ignoring a non-window scene (role: {session.Role}).");
				}

				return;
			}

			if (!SceneWindowRegistry.TryTake(GetRequestToken(connectionOptions), out var wrapper))
			{
				// UIKit connects scenes the app never asked for: session restoration after a
				// relaunch, external displays, or the user opening a window from the OS. There is
				// no XAML window to bind those to, so the session is discarded rather than left
				// on screen as a blank window - and never by throwing, which would kill the app.
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn(
						$"No window is awaiting a scene (session: {session.PersistentIdentifier}, role: {session.Role}); " +
						$"discarding the session. This happens when the OS restores more scenes than the app created windows for.");
				}

				UIApplication.SharedApplication.RequestSceneSessionDestruction(session, null, null);
				return;
			}

			var window = new AppleUIKitWindow(windowScene);

			Window = window;
			_wrapper = wrapper;
			wrapper.SetNativeWindow(window);

			OnSceneConnected(scene);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Window attached to scene {session.PersistentIdentifier}.");
			}
		}
		catch (Exception ex)
		{
			// A managed exception must never escape into a UIKit callback.
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}
	}

	public sealed override void DidDisconnect(UIScene scene) =>
		Forward(() =>
		{
			_wrapper?.OnSceneDisconnected();
			_wrapper = null;
			Window = null;

			OnSceneDisconnected(scene);
		});

	public sealed override void WillEnterForeground(UIScene scene) =>
		Forward(() => _wrapper?.OnSceneEnteredForeground());

	public sealed override void DidEnterBackground(UIScene scene) =>
		Forward(() => _wrapper?.OnSceneEnteredBackground());

	public sealed override void DidBecomeActive(UIScene scene) =>
		Forward(() => _wrapper?.OnSceneActivationChanged(CoreWindowActivationState.CodeActivated));

	public sealed override void WillResignActive(UIScene scene) =>
		Forward(() => _wrapper?.OnSceneActivationChanged(CoreWindowActivationState.Deactivated));

	/// <summary>
	/// Called once the scene has been bound to its window.
	/// </summary>
	protected virtual void OnSceneConnected(UIScene scene)
	{
	}

	/// <summary>
	/// Called once the scene has been unbound from its window.
	/// </summary>
	protected virtual void OnSceneDisconnected(UIScene scene)
	{
	}

	private static string? GetRequestToken(UISceneConnectionOptions connectionOptions)
	{
		foreach (var activity in connectionOptions.UserActivities)
		{
			if (activity.ActivityType == SceneWindowRegistry.ActivityType &&
				activity.UserInfo?[SceneWindowRegistry.TokenKey] is NSString token)
			{
				return token.ToString();
			}
		}

		return null;
	}

	private void Forward(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Application.Current?.RaiseRecoverableUnhandledException(ex);
		}
	}
}
