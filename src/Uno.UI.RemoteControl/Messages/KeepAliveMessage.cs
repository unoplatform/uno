using System;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Messages;

public record KeepAliveMessage : IMessage
{
	private static readonly string _localVersion = VersionHelper.GetVersion(typeof(KeepAliveMessage));

	public const string Name = nameof(KeepAliveMessage);

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
