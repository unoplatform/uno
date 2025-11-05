using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	internal class AssemblyDeltaReload : IMessage
	{
		public const string Name = nameof(AssemblyDeltaReload);

		[JsonProperty]
		public ImmutableHashSet<string> FilePaths { get; set; } = [];

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
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;

		[MemberNotNullWhen(true, nameof(UpdatedTypes), nameof(MetadataDelta), nameof(ILDelta), nameof(PdbDelta), nameof(ModuleId))]
		public bool IsValid()
		{
			// FilePaths is { IsEmpty: false } ==> Used only for logging / diagnostics purposes
			return UpdatedTypes is not null
				&& MetadataDelta is not null
				&& ILDelta is not null
				&& PdbDelta is not null
				&& ModuleId is not null;
		}
	}
}
