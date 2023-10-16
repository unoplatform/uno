using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload.Messages;

public class UpdateFile : IMessage
{
	public const string Name = nameof(UpdateFile);

	[JsonProperty]
	public string FilePath { get; set; } = string.Empty;

	[JsonProperty]
	public string OldText { get; set; } = string.Empty;

	[JsonProperty]
	public string NewText { get; set; } = string.Empty;

	[JsonIgnore]
	public string Scope => HotReloadConstants.TestingScopeName;

	[JsonIgnore]
	string IMessage.Name => Name;

	[MemberNotNullWhen(true, nameof(FilePath), nameof(OldText), nameof(NewText))]
	public bool IsValid()
		=> !FilePath.IsNullOrEmpty() &&
			OldText is not null &&
			NewText is not null;
}
