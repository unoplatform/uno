#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("FC151437-049A-4784-A24A-F1C4DAF20987")]
internal class ArithmeticCompositeEffect : ICanvasEffect
{
	private string _name = "ArithmeticCompositeEffect";
	private Guid _id = new Guid("FC151437-049A-4784-A24A-F1C4DAF20987");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float MultiplyAmount { get; set; } = 1.0f;

	public float Source1Amount { get; set; }

	public float Source2Amount { get; set; }

	public float Offset { get; set; }

	public bool ClampOutput { get; set; }

	public IGraphicsEffectSource? Source1 { get; set; }

	public IGraphicsEffectSource? Source2 { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(MultiplyAmount):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(Source1Amount):
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(Source2Amount):
				{
					index = 2;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(Offset):
				{
					index = 3;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(ClampOutput):
				{
					index = 4;
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
		0 => MultiplyAmount,
		1 => Source1Amount,
		2 => Source2Amount,
		3 => Offset,
		4 => ClampOutput,
		_ => null,
	};

	public uint GetPropertyCount() => 5;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source1,
		1 => Source2,
		_ => null
	};

	public uint GetSourceCount() => 2;

	public void Dispose() { }
}
