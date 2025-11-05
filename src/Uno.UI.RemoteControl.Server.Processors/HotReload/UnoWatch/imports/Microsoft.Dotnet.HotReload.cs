using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.DotNet.HotReload;

internal readonly struct HotReloadManagedCodeUpdate(
	Guid moduleId,
	ImmutableArray<byte> metadataDelta,
	ImmutableArray<byte> ilDelta,
	ImmutableArray<byte> pdbDelta,
	ImmutableArray<int> updatedTypes,
	ImmutableArray<string> requiredCapabilities)
{
	public Guid ModuleId { get; } = moduleId;
	public ImmutableArray<byte> MetadataDelta { get; } = metadataDelta;
	public ImmutableArray<byte> ILDelta { get; } = ilDelta;
	public ImmutableArray<byte> PdbDelta { get; } = pdbDelta;
	public ImmutableArray<int> UpdatedTypes { get; } = updatedTypes;
	public ImmutableArray<string> RequiredCapabilities { get; } = requiredCapabilities;
}

internal readonly struct HotReloadStaticAssetUpdate(string assemblyName, string relativePath, ImmutableArray<byte> content, bool isApplicationProject)
{
	public string RelativePath { get; } = relativePath;
	public string AssemblyName { get; } = assemblyName;
	public ImmutableArray<byte> Content { get; } = content;
	public bool IsApplicationProject { get; } = isApplicationProject;
}

internal enum ApplyStatus
{
	/// <summary>
	/// Failed to apply updates.
	/// </summary>
	Failed = 0,

	/// <summary>
	/// All requested updates have been applied successfully.
	/// </summary>
	AllChangesApplied = 1,

	/// <summary>
	/// Succeeded aplying changes, but some updates were not applicable to the target process because of required capabilities.
	/// </summary>
	SomeChangesApplied = 2,

	/// <summary>
	/// No updates were applicable to the target process because of required capabilities.
	/// </summary>
	NoChangesApplied = 3,
}
