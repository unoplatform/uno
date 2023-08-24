namespace Windows.UI.Composition;

/// <summary>
/// Specifies how content is scaled when mapped from its source to a destination space.
/// </summary>
public enum CompositionStretch
{
	/// <summary>
	/// No Scaling. If the size of the content is greater than size of destination, 
	/// the content is clipped to the bounds of the destination space.
	/// </summary>
	None = 0,

	/// <summary>
	/// Scale content such that its size is equal to the size of the destination. 
	/// The aspect ratio of the content is not preserved.
	/// </summary>
	Fill = 1,

	/// <summary>
	/// Scale content such that its aspect ratio is preserved and it fits entirely 
	/// within the bounds of the destination space. If the content’s aspect ratio 
	/// does not match that of the destination, the content will not cover some 
	/// of the area bound by the destination space. This is the default value 
	/// for CompositionSurfaceBrush.Stretch.
	/// </summary>
	Uniform = 2,

	/// <summary>
	/// Scale content such that its aspect ratio is preserved and it fills the entirety 
	/// of the destination’s bounds. If the content’s aspect ratio does not match that 
	/// of the destination, the content will be clipped to the bounds of the destination.
	/// </summary>
	UniformToFill = 3,
}
