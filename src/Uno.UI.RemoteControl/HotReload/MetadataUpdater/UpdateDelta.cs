#nullable enable

using System;

namespace Uno.UI.RemoteControl.HotReload.MetadataUpdater;

internal sealed class UpdateDelta
{
	public Guid ModuleId { get; set; }

	public byte[] MetadataDelta { get; set; } = default!;

	public byte[] ILDelta { get; set; } = default!;

	public byte[]? PdbBytes { get; set; }

	public int[]? UpdatedTypes { get; set; }
}
