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
}
