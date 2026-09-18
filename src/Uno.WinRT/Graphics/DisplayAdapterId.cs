namespace Windows.Graphics;

/// <summary>
/// Corresponds to the LUID (Locally Unique Identifier) associated with a graphics adapter.
/// </summary>
public partial struct DisplayAdapterId
{
	/// <summary>
	/// The low part of the LUID.
	/// </summary>
	public uint LowPart;

	/// <summary>
	/// The high part of the LUID.
	/// </summary>
	public int HighPart;
}
