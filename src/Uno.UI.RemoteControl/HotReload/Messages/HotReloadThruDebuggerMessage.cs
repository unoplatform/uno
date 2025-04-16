using System;
using Newtonsoft.Json;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public record HotReloadThruDebuggerMessage : IMessage
{
	public const string Name = nameof(HotReloadThruDebuggerMessage);

	[JsonProperty]
	public string Scope => WellKnownScopes.HotReload;

	[JsonProperty]
	string IMessage.Name => Name;

	[JsonProperty]
	public string ModuleId { get; set; } = string.Empty;

	[JsonProperty]
	public string MetadataDelta { get; set; } = string.Empty;

	[JsonProperty]
	public string ILDelta { get; set; } = string.Empty;

	[JsonProperty]
	public string PdbBytes { get; set; } = string.Empty;
}
