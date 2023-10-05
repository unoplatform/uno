namespace Windows.Graphics.Effects;

/// <summary>
/// This enum specifies Direct2D border edge modes/behaviors supported by Composition APIs<br/><br/>
/// <remarks>
/// References:<br/>
///		1- <see href="https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_CanvasEdgeBehavior.htm"/><br/>
///		2- wuceffects.dll!Windows::UI::Composition::g_rgModes<br/>
///		3- wuceffects.dll!Windows::UI::Composition::SampleEdgeMode
///	</remarks>
/// </summary>
internal enum D2D1BorderEdgeMode
{
	Clamp = 0,
	Wrap = 1,
	Mirror = 2
}
