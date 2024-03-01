#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("89176087-8AF9-4A08-AEB1-895F38DB1766")]
internal class TemperatureAndTintEffect : ICanvasEffect
{
	private string _name = "TemperatureAndTintEffect";
	private Guid _id = new Guid("89176087-8AF9-4A08-AEB1-895F38DB1766");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float Temperature { get; set; }

	public float Tint { get; set; }

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(Temperature):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case nameof(Tint):
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
		0 => (object)Temperature,
		1 => (object)Tint,
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
