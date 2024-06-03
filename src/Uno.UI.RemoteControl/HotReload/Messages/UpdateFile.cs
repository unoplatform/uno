using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;
using Uno.UI.RemoteControl.Messaging.IdeChannel;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public class UpdateFile : IMessage
{
	public const string Name = nameof(UpdateFile);

	/// <summary>
	/// ID of this file update request.
	/// </summary>
	[JsonProperty]
	public string RequestId { get; } = Guid.NewGuid().ToString();

	[JsonProperty]
	public string FilePath { get; set; } = string.Empty;

	[JsonProperty]
	public string OldText { get; set; } = string.Empty;

	[JsonProperty]
	public string NewText { get; set; } = string.Empty;

	/// <summary>
	/// Disable the forced hot-reload requested on VS after the file has been modified.
	/// </summary>
	[JsonProperty]
	public bool IsForceHotReloadDisabled { get; set; }

	[JsonIgnore]
	public string Scope => WellKnownScopes.HotReload;

	[JsonIgnore]
	string IMessage.Name => Name;

	[MemberNotNullWhen(true, nameof(FilePath), nameof(OldText), nameof(NewText))]
	public bool IsValid()
		=> !FilePath.IsNullOrEmpty() &&
			OldText is not null &&
			NewText is not null;
}

public enum FileUpdateResult
{
	Success = 200,
	NoChanges = 204,
	BadRequest = 400,
	FileNotFound = 404,
	Failed = 500,
	FailedToRequestHotReload = 502,
	NotAvailable = 503
}
