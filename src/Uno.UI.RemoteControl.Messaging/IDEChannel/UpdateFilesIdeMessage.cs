#nullable enable

using System;
using System.Collections.Immutable;
using Uno.UI.RemoteControl.Messaging.IdeChannel.HotReload;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

/// <summary>
/// Batched file-update request forwarded to the IDE: every edit of an UpdateFile request
/// plus the hot-reload intent travel as a single message, so the IDE can apply all edits
/// before any hot-reload is triggered (spec 052 — prevents EnC from evaluating an
/// intermediate state, e.g. a code-behind added before its markup and generated code exist).
/// </summary>
/// <remarks>
/// Deletes (<see cref="FileEdit.NewText"/> is null) are not forwarded to the IDE — the
/// dev-server applies them on disk — so every edit of this message carries content.
/// </remarks>
public record UpdateFilesIdeMessage(
	long CorrelationId,
	string RequestId,
	ImmutableArray<FileEdit> Edits,
	bool? ForceSaveOnDisk,
	bool IsForceHotReloadDisabled,
	TimeSpan? ForceHotReloadDelay,
	int? ForceHotReloadAttempts) : IdeMessageWithCorrelationId(CorrelationId, WellKnownScopes.HotReload), IUpdateFileRequest;
