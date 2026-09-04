#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;

namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Represents an instance of an app.
/// </summary>
public partial class AppInstance
{
	private static readonly Lazy<AppInstance> _current = new(() => new AppInstance());

	private AppActivationArguments? _activationArguments;
	private bool _hasLaunched;
	private string _key = string.Empty;

	internal AppInstance()
	{
	}

	/// <summary>
	/// Raised for activations that arrive after the app has launched.
	/// </summary>
	/// <remarks>
	/// On Windows this only ever fires for activations redirected through
	/// <see cref="RedirectActivationToAsync"/>, because a second launch is a second process.
	/// Uno also raises it where the OS delivers a further activation straight into the living
	/// process instead of starting a new one — an Android <c>onNewIntent</c>, an iOS
	/// <c>openURL</c> or shortcut tap, or a browser navigation on WebAssembly.
	/// </remarks>
	public event EventHandler<AppActivationArguments>? Activated;

	/// <summary>
	/// Gets a value that indicates whether this AppInstance object represents the current instance of the app or a different instance.
	/// </summary>
	public bool IsCurrent => true;

	/// <summary>
	/// Gets an app-defined string value that identifies the current app instance for redirection purposes.
	/// </summary>
	public string Key => _key;

	/// <summary>
	/// Gets the process identifier of the app instance.
	/// </summary>
	public uint ProcessId => (uint)Environment.ProcessId;

	/// <summary>
	/// Retrieves the event arguments for the activation that started this app instance.
	/// </summary>
	/// <returns>
	/// The activation that started this instance. Never <c>null</c>: an app started without any
	/// activation payload gets <see cref="ExtendedActivationKind.Launch"/> over the process command line.
	/// </returns>
	/// <remarks>
	/// This always describes the activation the instance *started* with. Later activations arrive
	/// through <see cref="Activated"/> and deliberately do not change what this returns.
	/// </remarks>
	public AppActivationArguments GetActivatedEventArgs()
		=> _activationArguments ?? AppActivationArguments.CreateLaunch(
			new LaunchActivatedEventArgs(ActivationKind.Launch, Environment.CommandLine));

	/// <summary>
	/// Retrieves the current running instance of the app.
	/// </summary>
	/// <returns>The current running instance of the app.</returns>
	public static AppInstance GetCurrent() => _current.Value;

	/// <summary>
	/// Retrieves a collection of all running instances of the app.
	/// </summary>
	/// <returns>The collection of all running instances of the app.</returns>
	/// <remarks>
	/// Uno does not track sibling instances, so this always reports just the current one.
	/// </remarks>
	public static IList<AppInstance> GetInstances() => [_current.Value];

	/// <summary>
	/// Registers <paramref name="key"/> against an app instance, or finds the instance that already owns it.
	/// </summary>
	/// <param name="key">The key to register.</param>
	/// <returns>The instance that owns <paramref name="key"/>.</returns>
	/// <remarks>
	/// Uno cannot yet see sibling processes, so the key is always claimed by — and this always
	/// returns — the current instance. The canonical single-instancing pattern therefore behaves
	/// as it would on a platform that can only ever run one instance: correct on Android, iOS and
	/// WebAssembly, and on desktop it means a second launch runs as a second instance rather than
	/// redirecting into the first.
	/// </remarks>
	public static AppInstance FindOrRegisterForKey(string key)
	{
		var instance = _current.Value;
		instance._key = key ?? string.Empty;
		return instance;
	}

	/// <summary>
	/// Releases the key previously registered by <see cref="FindOrRegisterForKey"/>.
	/// </summary>
	public void UnregisterKey() => _key = string.Empty;

	/// <summary>
	/// Redirects the given activation to this instance.
	/// </summary>
	/// <param name="args">The activation to redirect.</param>
	/// <remarks>
	/// Matches Windows in no-op'ing when the target is the current instance. Since Uno only ever
	/// resolves the current instance today, this never transfers an activation across processes.
	/// </remarks>
	public IAsyncAction RedirectActivationToAsync(AppActivationArguments args)
		=> Task.CompletedTask.AsAsyncAction();

	/// <summary>
	/// Stores the activation that started this instance, or raises <see cref="Activated"/> when the
	/// app is already running. This is the single entry point every platform host funnels activations through.
	/// </summary>
	internal void SetOrRaiseActivation(AppActivationArguments args)
	{
		ArgumentNullException.ThrowIfNull(args);

		if (_hasLaunched)
		{
			Activated?.Invoke(this, args);
		}
		else
		{
			_activationArguments = args;
		}
	}

	/// <summary>
	/// Marks the app as launched, so that any further activation is raised rather than stored.
	/// </summary>
	internal void NotifyLaunched() => _hasLaunched = true;

	/// <summary>
	/// The activation a platform host actually reported, as opposed to the Launch activation
	/// <see cref="GetActivatedEventArgs"/> synthesizes when there was none.
	/// </summary>
	internal AppActivationArguments? ReportedActivation => _activationArguments;
}
