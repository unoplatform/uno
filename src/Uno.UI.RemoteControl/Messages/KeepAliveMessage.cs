using System;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Messages;

public record KeepAliveMessage : IMessage
{
	private static readonly string _localVersion = VersionHelper.GetVersion(typeof(KeepAliveMessage));

	public const string Name = nameof(KeepAliveMessage);

	/// <summary>
	/// Interval at which a connected client sends keep-alive pings. This file is linked into
	/// Uno.UI.RemoteControl.ServerCore, so client and server share the exact same cadence: the
	/// server tears down a connection that stays silent for more than 2.1x this interval
	/// (a presumed-dead/half-open socket). See https://github.com/unoplatform/uno/issues/24206.
	/// </summary>
	public static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

	public string Scope => WellKnownScopes.DevServerChannel;

	string IMessage.Name => Name;

	/// <summary>
	/// The version of the dev-server version of the sender.
	/// </summary>
	public string? AssemblyVersion { get; init; } = _localVersion;

	/// <summary>
	/// Sequence ID of the ping.
	/// </summary>
	public ulong SequenceId { get; init; }

	public KeepAliveMessage Next()
		=> this with { SequenceId = SequenceId + 1 };
}
