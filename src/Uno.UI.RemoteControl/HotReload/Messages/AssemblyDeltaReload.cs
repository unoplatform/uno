using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class AssemblyDeltaReload : IMessage
	{
		public const string Name = nameof(AssemblyDeltaReload);

		[JsonProperty]
		public string? FilePath { get; set; }

		[JsonProperty]
		public string? ModuleId { get; set; }

		[JsonProperty]
		public string? ILDelta { get; set; }

		[JsonProperty]
		public string? MetadataDelta { get; set; }

		[JsonProperty]
		public string? PdbDelta { get; set; }

		[JsonProperty]
		public string? UpdatedTypes { get; set; }

		[JsonIgnore]
		public string Scope => HotReloadConstants.ScopeName;

		[JsonIgnore]
		string IMessage.Name => Name;

		[MemberNotNullWhen(true, nameof(UpdatedTypes), nameof(MetadataDelta), nameof(ILDelta), nameof(PdbDelta), nameof(ModuleId))]
		public bool IsValid()
		{
			return FilePath is not null
				&& UpdatedTypes is not null
				&& MetadataDelta is not null
				&& ILDelta is not null
				&& PdbDelta is not null
				&& ModuleId is not null;
		}
	}
}
