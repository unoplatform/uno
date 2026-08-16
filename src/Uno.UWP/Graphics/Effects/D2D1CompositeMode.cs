namespace Windows.Graphics.Effects;

/// <summary>
/// This enum specifies Direct2D composite modes supported by Composition APIs<br/><br/>
/// <remarks>
/// References:<br/>
///		- <see href="https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_CanvasComposite.htm"/><br/>
///	</remarks>
/// </summary>
internal enum D2D1CompositeMode
{
	SourceOver = 0,
	DestinationOver = 1,
	SourceIn = 2,
	DestinationIn = 3,
	SourceOut = 4,
	DestinationOut = 5,
	SourceAtop = 6,
	DestinationAtop = 7,
	Xor = 8,
	Add = 9, // Plus
	Copy = 10,
	//BoundedCopy = 11, // Note: Composition doesn't support BoundedCopy yet (as of 10.0.25941.1000)
	MaskInvert = 12
}
