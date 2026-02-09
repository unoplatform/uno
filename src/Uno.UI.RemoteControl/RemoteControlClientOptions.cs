#nullable enable

namespace Uno.UI.RemoteControl;

/// <summary>
/// Options used when creating a <see cref="RemoteControlClient"/>.
/// </summary>
public sealed record RemoteControlClientOptions
{
	/// <summary>
	/// Options for the main singleton client created through <see cref="RemoteControlClient.Initialize(System.Type)"/>.
	/// </summary>
	public static RemoteControlClientOptions DefaultClient { get; } = new()
	{
		SetAsDefaultInstance = true,
		EnableHotReloadProcessor = true,
		RegisterDiagnosticView = true,
		AutoRegisterAppIdentity = true
	};

	/// <summary>
	/// Options for additional independent clients created through <see cref="RemoteControlClient.CreateAdditional(System.Type, Uno.UI.RemoteControl.Messaging.IFrameTransport, RemoteControlClientOptions?)"/>.
	/// </summary>
	public static RemoteControlClientOptions AdditionalClient { get; } = new()
	{
		SetAsDefaultInstance = false,
		EnableHotReloadProcessor = false,
		RegisterDiagnosticView = false,
		AutoRegisterAppIdentity = false
	};

	/// <summary>
	/// Indicates whether the created client should be assigned to <see cref="RemoteControlClient.Instance"/>.
	/// </summary>
	public bool SetAsDefaultInstance { get; init; }

	/// <summary>
	/// Indicates whether the hot reload processor should be registered for the created client.
	/// </summary>
	public bool EnableHotReloadProcessor { get; init; }

	/// <summary>
	/// Indicates whether the diagnostic status view should be registered for the created client.
	/// </summary>
	public bool RegisterDiagnosticView { get; init; }

	/// <summary>
	/// Indicates whether app identity should be sent automatically when server processors are initialized.
	/// </summary>
	public bool AutoRegisterAppIdentity { get; init; }
}
