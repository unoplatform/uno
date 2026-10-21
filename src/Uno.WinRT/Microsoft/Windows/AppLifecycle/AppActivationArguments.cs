#nullable enable

using Windows.ApplicationModel.Activation;

namespace Microsoft.Windows.AppLifecycle;

/// <summary>
/// Contains information about the type and data payload for an app activation.
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
	/// <remarks>
	/// The concrete type follows <see cref="Kind"/>: a
	/// <see cref="Windows.ApplicationModel.Activation.LaunchActivatedEventArgs"/> for
	/// <see cref="ExtendedActivationKind.Launch"/>, a
	/// <see cref="Windows.ApplicationModel.Activation.ProtocolActivatedEventArgs"/> for
	/// <see cref="ExtendedActivationKind.Protocol"/>, and so on.
	/// </remarks>
	public object Data { get; }

	/// <summary>
	/// Gets the type of a registered activation.
	/// </summary>
	public ExtendedActivationKind Kind { get; }

	internal static AppActivationArguments CreateLaunch(LaunchActivatedEventArgs launchArgs)
		=> new(ExtendedActivationKind.Launch, launchArgs);

	internal static AppActivationArguments CreateProtocol(ProtocolActivatedEventArgs protocolArgs)
		=> new(ExtendedActivationKind.Protocol, protocolArgs);

	/// <summary>
	/// Wraps a platform activation payload, mapping its
	/// <see cref="Windows.ApplicationModel.Activation.ActivationKind"/> onto the matching
	/// <see cref="ExtendedActivationKind"/>. The two enums share their numeric values below 5000.
	/// </summary>
	internal static AppActivationArguments FromActivatedEventArgs(IActivatedEventArgs args)
		=> new((ExtendedActivationKind)(int)args.Kind, args);
}
