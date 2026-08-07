namespace Windows.Graphics.Effects;

/// <summary>
/// This enum specifies Direct2D border edge modes/behaviors supported by Composition APIs<br/><br/>
/// <remarks>
/// References:<br/>
///		- <see href="https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_CanvasEdgeBehavior.htm"/><br/>
///	</remarks>
/// </summary>
internal enum D2D1BorderEdgeMode
{
	Clamp = 0,
	Wrap = 1,
	Mirror = 2
}
