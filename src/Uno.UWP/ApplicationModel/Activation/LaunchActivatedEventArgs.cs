using System;
using Uno;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.ApplicationModel.Activation;

/// <summary>
/// Provides event information when an app is launched.
/// </summary>
public sealed partial class LaunchActivatedEventArgs : IActivatedEventArgs
{
	internal LaunchActivatedEventArgs()
	{
	}

	internal LaunchActivatedEventArgs(ActivationKind kind, string arguments)
	{
		Arguments = arguments;
		Kind = kind;
	}

	/// <summary>
	/// Gets the reason that this app is being activated.
	/// </summary>
	public ActivationKind Kind { get; } = ActivationKind.Launch;

	/// <summary>
	/// Defaults to NotRunning, may not be accurate in all cases for all platforms.
	/// </summary>
	public ApplicationExecutionState PreviousExecutionState { get; } = ApplicationExecutionState.NotRunning;

	/// <summary>
	/// Gets the splash screen object that provides information about the transition from the splash screen to the activated app.
	/// </summary>
	/// <remarks>
	/// SplashScreen is not directly supported, exists for interoperability with UWP APIs.
	/// </remarks>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public SplashScreen SplashScreen { get; } = new SplashScreen();

	/// <summary>
	/// Gets the identifier for the currently shown app view.
	/// </summary>
	/// <remarks>
	/// The ID defaults to 0 on non-UWP targets.
	/// </remarks>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public int CurrentlyShownApplicationViewId { get; }

	/// <summary>
	/// Gets the arguments that are passed to the app during its launch activation.
	/// </summary>
	public string Arguments { get; } = "";

	/// <summary>
	/// Gets the ID of the tile that was invoked to launch the app.
	/// </summary>
	[NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
	public string TileId { get; } = "App";

	/// <summary>
	/// Indicates whether the app was pre-launched.
	/// </summary>
	public bool PrelaunchActivated => false; // No platform other than UWP supports prelaunch yet.
}
