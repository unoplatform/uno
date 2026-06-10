using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	public partial record HotReloadStatusMessage(
		HotReloadState State,
		IImmutableList<HotReloadServerOperationData> Operations,
		string? ServerError = null)
		: IMessage
	{
		public const string Name = nameof(HotReloadStatusMessage);

		/// <inheritdoc />
		public string Scope => WellKnownScopes.HotReload;

		/// <inheritdoc />
		string IMessage.Name => Name;
	}

	public partial record HotReloadServerOperationData(
		long Id,
		DateTimeOffset StartTime,
		ImmutableHashSet<string> FilePaths,
		ImmutableHashSet<string>? IgnoredFilePaths,
		DateTimeOffset? EndTime,
		HotReloadServerResult? Result,
		IImmutableList<string>? Diagnostics)
	{ }
}
