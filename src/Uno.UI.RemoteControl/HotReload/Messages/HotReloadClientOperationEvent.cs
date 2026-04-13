using System;
using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.HotReload.Messages;

/// <summary>
/// Lifecycle state of a <see cref="HotReloadClientOperationEvent"/>,
/// derived from timestamps rather than explicit flags.
/// </summary>
public enum HotReloadClientOperationKind
{
	/// <summary>Operation created, UI update not yet attempted.</summary>
	Started,
	/// <summary>Operation was skipped (e.g., TypeMappings paused).</summary>
	Ignored,
	/// <summary>UI update completed successfully.</summary>
	Succeeded,
	/// <summary>UI update failed (partial or total).</summary>
	Failed,
}

/// <summary>
/// Sent by the client at each lifecycle transition of a <c>HotReloadClientOperation</c>
/// so the server can track the end-to-end hot-reload status. <see cref="Kind"/> is
/// derived from the timestamp fields — no redundant boolean flags.
/// </summary>
public class HotReloadClientOperationEvent : IMessage
{
	public const string Name = nameof(HotReloadClientOperationEvent);

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;

	/// <summary>
	/// The sequential <c>HotReloadClientOperation.Id</c> for correlation.
	/// </summary>
	public int OperationSequenceId { get; init; }

	/// <summary>When the operation was created.</summary>
	public DateTimeOffset StartTime { get; init; }

	/// <summary>When the operation was ignored (skipped). Null if not ignored.</summary>
	public DateTimeOffset? IgnoreTime { get; init; }

	/// <summary>When the UI update completed (success or failure). Null while in progress.</summary>
	public DateTimeOffset? EndTime { get; init; }

	/// <summary>Error details. Null on success or when still in progress.</summary>
	public string? ErrorMessage { get; init; }

	/// <summary>Number of elements that failed individually (per-element isolation).</summary>
	public int FailedElementCount { get; init; }

	/// <summary>Total number of elements that were candidates for update.</summary>
	public int TotalElementCount { get; init; }

	/// <summary>
	/// Derived lifecycle state based on which timestamps are set.
	/// </summary>
	[JsonIgnore]
	public HotReloadClientOperationKind Kind => (IgnoreTime, EndTime, ErrorMessage) switch
	{
		({ }, _, _) => HotReloadClientOperationKind.Ignored,
		(_, { }, null or "") => HotReloadClientOperationKind.Succeeded,
		(_, { }, _) => HotReloadClientOperationKind.Failed,
		_ => HotReloadClientOperationKind.Started,
	};
}
