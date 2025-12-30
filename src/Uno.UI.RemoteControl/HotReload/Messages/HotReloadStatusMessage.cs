using System;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;

#if !HAS_UNO // We don't want to add a dependency on Newtonsoft.Json in Uno.Toolkit
namespace Newtonsoft.Json
{
	internal class JsonPropertyAttribute : Attribute
	{
	}
}
#endif

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal record HotReloadStatusMessage(
		[property: JsonProperty] HotReloadState State,
		[property: JsonProperty] IImmutableList<HotReloadServerOperationData> Operations,
		[property: JsonProperty] string? ServerError = null)
		: IMessage
	{
		public const string Name = nameof(HotReloadStatusMessage);

		/// <inheritdoc />
		[JsonProperty]
		public string Scope => WellKnownScopes.HotReload;

		/// <inheritdoc />
		[JsonProperty]
		string IMessage.Name => Name;
	}

	public record HotReloadServerOperationData(
		long Id,
		DateTimeOffset StartTime,
		ImmutableHashSet<string> FilePaths,
		ImmutableHashSet<string>? IgnoredFilePaths,
		DateTimeOffset? EndTime,
		HotReloadServerResult? Result,
		IImmutableList<string>? Diagnostics);
}
