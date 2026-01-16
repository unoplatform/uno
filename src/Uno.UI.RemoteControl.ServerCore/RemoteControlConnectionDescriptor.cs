using System.Collections.Generic;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Describes metadata about an incoming transport connection handled by the devserver core.
/// Hosts pass this to provide transport/end point context so connection-scoped logging and telemetry stay consistent
/// without leaking host-specific primitives (e.g., HttpContext).
/// </summary>
public readonly record struct RemoteControlConnectionDescriptor(
	string? TransportName,
	string? RemoteEndpoint,
	IReadOnlyDictionary<string, string>? Properties)
{
	/// <summary>
	/// Gets an empty descriptor with no metadata.
	/// </summary>
	public static RemoteControlConnectionDescriptor Empty { get; } = new(null, null, null);

	/// <summary>
	/// Descriptor used for in-process transport pairs so logs show the loopback nature of the connection.
	/// </summary>
	public static RemoteControlConnectionDescriptor InProcess { get; } = new("in-process", "loopback", null);
}
