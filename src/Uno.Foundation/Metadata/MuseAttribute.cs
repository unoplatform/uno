namespace Windows.Foundation.Metadata;

/// <summary>
/// Indicates that a runtime class is compatible with UWP apps that are web browsers.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed partial class MuseAttribute : Attribute
{
	/// <summary>
	/// Creates and initializes a new instance of the attribute.
	/// </summary>
	public MuseAttribute() : base()
	{
	}

	/// <summary>
	/// Specifies the version.
	/// </summary>
	public uint Version;
}
