using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class UpdateFile : IMessage
	{
		public const string Name = nameof(UpdateFile);

		[JsonProperty]
		public string FilePath { get; set; } = string.Empty;

		[JsonProperty]
		public string OriginalXaml { get; set; } = string.Empty;

		[JsonProperty]
		public string ReplacementXaml { get; set; } = string.Empty;

		[JsonIgnore]
		public string Scope => HotReloadConstants.TestingScopeName;

		[JsonIgnore]
		string IMessage.Name => Name;

		[MemberNotNullWhen(true, nameof(FilePath), nameof(OriginalXaml), nameof(ReplacementXaml))]
		public bool IsValid() =>
			!FilePath.IsNullOrEmpty() &&
			OriginalXaml is not null &&
			ReplacementXaml is not null;
	}
}
