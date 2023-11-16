#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("81C5B77B-13F8-4CDD-AD20-C890547AC65D")]
internal class BlendEffect : ICanvasEffect
{
	private string _name = "BlendEffect";
	private Guid _id = new Guid("81C5B77B-13F8-4CDD-AD20-C890547AC65D");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public BlendEffectMode Mode { get; set; } = BlendEffectMode.Multiply;

	public IGraphicsEffectSource? Background { get; set; }

	public IGraphicsEffectSource? Foreground { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "Mode":
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

	public object? GetProperty(uint index)
	{
		switch (index)
		{
			case 0:
				return Mode;
			default:
				return null;
		}
	}

	public uint GetPropertyCount() => 1;

	public IGraphicsEffectSource? GetSource(uint index) => index is 0 ? Background : Foreground;

	public uint GetSourceCount() => 2;

	public void Dispose() { }
}
