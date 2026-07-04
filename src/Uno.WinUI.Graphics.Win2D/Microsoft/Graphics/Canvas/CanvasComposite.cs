namespace Microsoft.Graphics.Canvas;

internal enum CanvasComposite
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
	Add = 9,
	Copy = 10,
	BoundedCopy = 11,
	MaskInvert = 12
}
