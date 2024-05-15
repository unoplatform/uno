#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5")]
internal class GaussianBlurEffect : ICanvasEffect
{
	private string _name = "GaussianBlurEffect";
	private Guid _id = new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public IGraphicsEffectSource? Source { get; set; }

	public float BlurAmount { get; set; } = 3.0f;

	public EffectOptimization Optimization { get; set; } = EffectOptimization.Balanced;

	public EffectBorderMode BorderMode { get; set; } = EffectBorderMode.Soft;

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(BlurAmount):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(Optimization):
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(BorderMode):
				{
					index = 2;
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
		0 => BlurAmount,
		1 => (uint)Optimization,
		2 => (uint)BorderMode,
		_ => null,
	};

	public uint GetPropertyCount() => 3;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
