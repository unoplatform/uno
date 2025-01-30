#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06")]
internal class LinearTransferEffect : ICanvasEffect
{
	private string _name = "LinearTransferEffect";
	private Guid _id = new Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float RedOffset { get; set; }

	public float RedSlope { get; set; } = 1.0f;

	public bool RedDisable { get; set; }


	public float GreenOffset { get; set; }

	public float GreenSlope { get; set; } = 1.0f;

	public bool GreenDisable { get; set; }


	public float BlueOffset { get; set; }

	public float BlueSlope { get; set; } = 1.0f;

	public bool BlueDisable { get; set; }


	public float AlphaOffset { get; set; }

	public float AlphaSlope { get; set; } = 1.0f;

	public bool AlphaDisable { get; set; }


	public bool ClampOutput { get; set; }


	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(RedOffset):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(RedSlope):
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(RedDisable):
				{
					index = 2;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenOffset):
				{
					index = 3;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenSlope):
				{
					index = 4;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(GreenDisable):
				{
					index = 5;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueOffset):
				{
					index = 6;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueSlope):
				{
					index = 7;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BlueDisable):
				{
					index = 8;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaOffset):
				{
					index = 9;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaSlope):
				{
					index = 10;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(AlphaDisable):
				{
					index = 11;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(ClampOutput):
				{
					index = 12;
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
		0 => RedOffset,
		1 => RedSlope,
		2 => RedDisable,
		3 => GreenOffset,
		4 => GreenSlope,
		5 => GreenDisable,
		6 => BlueOffset,
		7 => BlueSlope,
		8 => BlueDisable,
		9 => AlphaOffset,
		10 => AlphaSlope,
		11 => AlphaDisable,
		12 => ClampOutput,
		_ => null,
	};

	public uint GetPropertyCount() => 13;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
