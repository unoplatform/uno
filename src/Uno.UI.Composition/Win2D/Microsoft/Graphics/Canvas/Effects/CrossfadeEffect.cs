#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("12F575E8-4DB1-485F-9A84-03A07DD3829F")]
internal class CrossFadeEffect : ICanvasEffect
{
	private string _name = "CrossFadeEffect";
	private Guid _id = new Guid("12F575E8-4DB1-485F-9A84-03A07DD3829F");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public static bool IsSupported => true;

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float CrossFade { get; set; } = 0.5f;

	public IGraphicsEffectSource? Source1 { get; set; }

	public IGraphicsEffectSource? Source2 { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "Weight":
			case "Crossfade":
			case nameof(CrossFade):
				{
					index = 0;
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
		0 => CrossFade,
		_ => null,
	};

	public uint GetPropertyCount() => 1;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source1,
		1 => Source2,
		_ => null
	};

	public uint GetSourceCount() => 2;

	public void Dispose() { }
}
