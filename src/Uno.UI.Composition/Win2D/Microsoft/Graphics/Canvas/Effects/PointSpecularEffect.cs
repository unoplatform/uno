#nullable enable

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("09C3CA26-3AE2-4F09-9EBC-ED3865D53F22")]
internal class PointSpecularEffect : ICanvasEffect
{
	private string _name = "PointSpecularEffect";
	private Guid _id = new Guid("09C3CA26-3AE2-4F09-9EBC-ED3865D53F22");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public Vector3 LightPosition { get; set; }

	public float SpecularExponent { get; set; } = 1.0f;

	public float SpecularAmount { get; set; } = 1.0f;

	public Color LightColor { get; set; } = Colors.White;

	public Vector4 LightColorHdr
	{
		get => new(LightColor.R * 255.0f, LightColor.G * 255.0f, LightColor.B * 255.0f, LightColor.A * 255.0f);
		set => LightColor = new((byte)(value.W / 255.0f), (byte)(value.X / 255.0f), (byte)(value.Y / 255.0f), (byte)(value.Z / 255.0f));
	}

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(LightPosition):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(SpecularExponent):
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(SpecularAmount):
				{
					index = 2;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(LightColor):
				{
					index = 3;
					mapping = GraphicsEffectPropertyMapping.ColorToVector3;
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

	public object? GetProperty(uint index) => index switch
	{
		0 => LightPosition,
		1 => SpecularExponent,
		2 => SpecularAmount,
		3 => LightColor,
		_ => null,
	};

	public uint GetPropertyCount() => 4;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
