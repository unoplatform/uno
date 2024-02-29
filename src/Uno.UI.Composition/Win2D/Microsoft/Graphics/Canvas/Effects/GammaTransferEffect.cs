#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42")]
internal class GammaTransferEffect : ICanvasEffect
{
	private string _name = "GammaTransferEffect";
	private Guid _id = new Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float RedAmplitude { get; set; } = 1.0f;

	public float RedExponent { get; set; } = 1.0f;

	public float RedOffset { get; set; }

	public bool RedDisable { get; set; }


	public float GreenAmplitude { get; set; } = 1.0f;

	public float GreenExponent { get; set; } = 1.0f;

	public float GreenOffset { get; set; }

	public bool GreenDisable { get; set; }


	public float BlueAmplitude { get; set; } = 1.0f;

	public float BlueExponent { get; set; } = 1.0f;

	public float BlueOffset { get; set; }

	public bool BlueDisable { get; set; }


	public float AlphaAmplitude { get; set; } = 1.0f;

	public float AlphaExponent { get; set; } = 1.0f;

	public float AlphaOffset { get; set; }

	public bool AlphaDisable { get; set; }


	public bool ClampOutput { get; set; }


	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(RedAmplitude):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(RedExponent):
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(RedOffset):
				{
					index = 2;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(RedDisable):
				{
					index = 3;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenAmplitude):
				{
					index = 4;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenExponent):
				{
					index = 5;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenOffset):
				{
					index = 6;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenDisable):
				{
					index = 7;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueAmplitude):
				{
					index = 8;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueExponent):
				{
					index = 9;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueOffset):
				{
					index = 10;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueDisable):
				{
					index = 11;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaAmplitude):
				{
					index = 12;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaExponent):
				{
					index = 13;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaOffset):
				{
					index = 14;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaDisable):
				{
					index = 15;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(ClampOutput):
				{
					index = 16;
					mapping = GraphicsEffectPropertyMapping.Direct;
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
		0 => RedAmplitude,
		1 => RedExponent,
		2 => RedOffset,
		3 => RedDisable,
		4 => GreenAmplitude,
		5 => GreenExponent,
		6 => GreenOffset,
		7 => GreenDisable,
		8 => BlueAmplitude,
		9 => BlueExponent,
		10 => BlueOffset,
		11 => BlueDisable,
		12 => AlphaAmplitude,
		13 => AlphaExponent,
		14 => AlphaOffset,
		15 => AlphaDisable,
		16 => ClampOutput,
		_ => null,
	};

	public uint GetPropertyCount() => 17;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
