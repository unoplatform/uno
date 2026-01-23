using System;
using System.Collections.Immutable;

namespace Uno.HotReload;

public readonly record struct Update
{
	public readonly Guid ModuleId;
	public readonly ImmutableArray<byte> ILDelta;
	public readonly ImmutableArray<byte> MetadataDelta;
	public readonly ImmutableArray<byte> PdbDelta;
	public readonly ImmutableArray<int> UpdatedTypes;

	public Update(Guid moduleId, ImmutableArray<byte> ilDelta, ImmutableArray<byte> metadataDelta, ImmutableArray<byte> pdbDelta, ImmutableArray<int> updatedTypes)
	{
		ModuleId = moduleId;
		ILDelta = ilDelta;
		MetadataDelta = metadataDelta;
		PdbDelta = pdbDelta;
		UpdatedTypes = updatedTypes;
	}
}
