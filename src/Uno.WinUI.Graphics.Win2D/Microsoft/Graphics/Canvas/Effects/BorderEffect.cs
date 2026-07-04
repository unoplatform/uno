#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27")]
internal class BorderEffect : ICanvasEffect
{
	private string _name = "BorderEffect";
	private Guid _id = new Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public CanvasEdgeBehavior ExtendX { get; set; } = CanvasEdgeBehavior.Clamp;

	public CanvasEdgeBehavior ExtendY { get; set; } = CanvasEdgeBehavior.Clamp;

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(ExtendX):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(ExtendY):
				{
					index = 1;
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
		0 => (uint)ExtendX,
		1 => (uint)ExtendY,
		_ => null,
	};

	public uint GetPropertyCount() => 2;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
