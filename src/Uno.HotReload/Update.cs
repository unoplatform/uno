#pragma warning disable CA1051

using System;
using System.Collections.Immutable;

namespace Uno.HotReload;

/// <summary>
/// Represents a set of changes applied to a module, including intermediate language (IL), metadata, and debugging
/// information deltas.
/// </summary>
/// <remarks>This struct is typically used to describe updates to a .NET assembly during scenarios such as Edit
/// and Continue or dynamic code updates. Each delta contains only the changes since the previous version, not the full
/// content. The struct is immutable and can be safely shared between threads.</remarks>
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
