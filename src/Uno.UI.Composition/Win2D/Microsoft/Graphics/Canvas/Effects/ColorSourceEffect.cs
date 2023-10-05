#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.UI;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using System.Numerics;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("61C23C20-AE69-4D8E-94CF-50078DF638F2")]
public class ColorSourceEffect : ICanvasEffect
{
	private string _name = "ColorSourceEffect";
	private Guid _id = new Guid("61C23C20-AE69-4D8E-94CF-50078DF638F2");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);

	public Vector4 ColorHdr
	{
		get => new(Color.R * 255.0f, Color.G * 255.0f, Color.B * 255.0f, Color.A * 255.0f);
		set => Color = new((byte)(value.W / 255.0f), (byte)(value.X / 255.0f), (byte)(value.Y / 255.0f), (byte)(value.Z / 255.0f));
	}

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "Color":
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.ColorToVector4;
					break;
				}
			default:
				{
					index = 0xFF;
					mapping = (GraphicsEffectPropertyMapping)0xFF;
					break;
				}
		}
	}

	public object? GetProperty(uint index)
	{
		switch (index)
		{
			case 0:
				return Color;
			default:
				return null;
		}
	}

	public uint GetPropertyCount() => 1;
	public IGraphicsEffectSource GetSource(uint index) => throw new InvalidOperationException();
	public uint GetSourceCount() => 0;

	public void Dispose() { }
}
