#nullable enable

using System.Collections.Generic;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Represents a node published through the AT-SPI accessibility tree.
/// </summary>
internal sealed class AtspiNode
{
	public required string Path { get; init; }
	public nint Handle { get; set; }
	public uint Role { get; set; }
	public string RoleName { get; set; } = "";
	public string Name { get; set; } = "";
	public AtspiNode? Parent { get; set; }
	public List<AtspiNode> Children { get; } = new();
	public double X { get; set; }
	public double Y { get; set; }
	public double W { get; set; }
	public double H { get; set; }
	public bool Enabled { get; set; } = true;
	public bool Focusable { get; set; }
	public bool Checked { get; set; }
	public bool HasToggle { get; set; }
	public bool Editable { get; set; }
	public bool HasText { get; set; }
	public string Text { get; set; } = "";
	public bool ReadOnly { get; set; }
	public bool Expandable { get; set; }
	public bool Expanded { get; set; }
	public bool Selectable { get; set; }
	public bool Selected { get; set; }
	public bool HasRange { get; set; }
	public double Min { get; set; }
	public double Max { get; set; }
	public double Val { get; set; }
	public int ItemIndex { get; set; } = -1;
	public string? Description { get; set; }
	public int HeadingLevel { get; set; }
	public string? Landmark { get; set; }
	public bool Required { get; set; }
	public bool Offscreen { get; set; }
	public int PositionInSet { get; set; }
	public int SizeOfSet { get; set; }
}
