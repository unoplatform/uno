using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
	public class AssemblyDeltaReload : IMessage
	{
		public const string Name = nameof(AssemblyDeltaReload);

		public ImmutableHashSet<string> FilePaths { get; set; } = [];

		public string? ModuleId { get; set; }

		public string? ILDelta { get; set; }

		public string? MetadataDelta { get; set; }

		public string? PdbDelta { get; set; }

		public string? UpdatedTypes { get; set; }

		[JsonIgnore]
		public string Scope => WellKnownScopes.HotReload;

		[JsonIgnore]
		string IMessage.Name => Name;

		[MemberNotNullWhen(true, nameof(UpdatedTypes), nameof(MetadataDelta), nameof(ILDelta), nameof(PdbDelta), nameof(ModuleId))]
		public bool IsValid()
		{
			return FilePaths is { IsEmpty: false }
				&& UpdatedTypes is not null
				&& MetadataDelta is not null
				&& ILDelta is not null
				&& PdbDelta is not null
				&& ModuleId is not null;
		}
	}
}
