#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("36312B17-F7DD-4014-915D-FFCA768CF211")]
internal class TintEffect : ICanvasEffect
{
	private string _name = "TintEffect";
	private Guid _id = new Guid("36312B17-F7DD-4014-915D-FFCA768CF211");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public static bool IsSupported => true;

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public Color Color { get; set; } = Color.FromArgb(255, 255, 255, 255);

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case nameof(Color):
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.ColorToVector4;
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
		0 => Color,
		_ => null,
	};

	public uint GetPropertyCount() => 1;

	public IGraphicsEffectSource? GetSource(uint index) => index switch
	{
		0 => Source,
		_ => null
	};

	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
