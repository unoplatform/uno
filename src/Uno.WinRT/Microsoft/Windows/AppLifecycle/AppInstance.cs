using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.ApplicationModel.Core;

namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Represents an instance of an app.
/// </summary>
public partial class AppInstance
{
	private static Lazy<AppInstance> _current = new(() => new AppInstance());
	
	private AppActivationArguments _defaultActivationArguments;
	private AppActivationArguments _appActivationArguments;

	internal AppInstance()
	{
	}

	/// <summary>
	/// Raised for activations that have been redirected via Microsoft.Windows.AppLifecycle.AppInstance.RedirectActivationToAsync.
	/// </summary>
	public event EventHandler<AppActivationArguments> Activated;

	/// <summary>
	/// Gets a value that indicates whether this AppInstance object represents the current instance of the app or a different instance.
	/// </summary>
	public bool IsCurrent => true;

	/// <summary>
	/// Gets an app-defined string value that identifies the current app instance for redirection purposes.
	/// </summary>
	public string Key => "";

	/// <summary>
	/// Retrieves the event arguments for an app activation that was registered by using one of the static methods of the ActivationRegistrationManager class.
	/// </summary>
	/// <returns>An object that contains the activation type and the data payload, or null.</returns>
	public AppActivationArguments GetActivatedEventArgs() => _appActivationArguments ?? _defaultActivationArguments ?? throw new InvalidOperationException("No activation arguments were set.");

	/// <summary>
	/// Retrieves the current running instance of the app.
	/// </summary>
	/// <returns>The current running instance of the app.</returns>
	public static AppInstance GetCurrent() => _current.Value;

	/// <summary>
	/// Retrieves a collection of all running instances of the app.
	/// </summary>
	/// <returns>The collection of all running instances of the app.</returns>
	public static IList<AppInstance> GetInstances() => [_current.Value];

	/// <summary>
	/// This method either sets the activation arguments if the app is not yet launched,
	/// or raises the Activated event if the app is already launched.
	/// </summary>
	/// <param name="args">App activation arguments.</param>
	internal void SetOrRaiseActivation(AppActivationArguments args)
	{
		if (!CoreApplication.WasLaunched)
		{
			// If these are the initial activation arguments for launch, store them for later.
			_appActivationArguments = args ?? throw new ArgumentNullException(nameof(args));
		}
		else
		{
			Activated?.Invoke(this, args ?? throw new ArgumentNullException(nameof(args)));
		}
	}

	internal void SetDefaultLaunchActivatedArgs(AppActivationArguments appActivationArguments) =>
		_defaultActivationArguments = appActivationArguments ?? throw new ArgumentNullException(nameof(appActivationArguments));
}
