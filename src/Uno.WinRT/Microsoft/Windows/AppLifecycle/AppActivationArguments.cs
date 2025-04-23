using Windows.ApplicationModel.Activation;

namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Contains information about the type and data payload for an app activation that was registered by using one of the static methods of the ActivationRegistrationManager class.
/// </summary>
public partial class AppActivationArguments
{
	private AppActivationArguments(ExtendedActivationKind kind, object data)
	{
		Kind = kind;
		Data = data;
	}

	/// <summary>
	/// Gets the data payload for a registered activation.
	/// </summary>
	public object Data { get; }

	/// <summary>
	/// Gets the type of a registered activation.
	/// </summary>
	public ExtendedActivationKind Kind { get; }

	internal static AppActivationArguments CreateLaunch(LaunchActivatedEventArgs launchArgs) => new AppActivationArguments(ExtendedActivationKind.Launch, launchArgs);

	internal static AppActivationArguments CreateProtocol(ProtocolActivatedEventArgs protocolArgs) => new AppActivationArguments(ExtendedActivationKind.Protocol, protocolArgs);
}
