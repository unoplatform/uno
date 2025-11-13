using System;

namespace Uno.UI.RemoteControl.Messages;

/// <summary>
/// Sent by the runtime client shortly after establishing the WebSocket connection to identify
/// itself so the dev-server can correlate with prior launch registrations.
/// </summary>
public record AppLaunchMessage : IMessage
{
	public const string Name = nameof(AppLaunchMessage);

	public Guid Mvid { get; init; }

	public string? Platform { get; init; }

	public bool IsDebug { get; init; }

	public string? Ide { get; init; }

	public string? Plugin { get; init; }

	public required AppLaunchStep Step { get; init; }

	public string Scope => WellKnownScopes.DevServerChannel;

	string IMessage.Name => Name;
}

public enum AppLaunchStep
{
	Launched,
	Connected,
}
