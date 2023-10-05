#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("811D79A4-DE28-4454-8094-C64685F8BD4C")]
public class OpacityEffect : ICanvasEffect
{
	private string _name = "OpacityEffect";
	private Guid _id = new Guid("811D79A4-DE28-4454-8094-C64685F8BD4C");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public static bool IsSupported => true;

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float Opacity { get; set; } = 1.0f;

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "Opacity":
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
				return Opacity;
			default:
				return null;
		}
	}

	public uint GetPropertyCount() => 1;
	public IGraphicsEffectSource? GetSource(uint index) => Source;
	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
