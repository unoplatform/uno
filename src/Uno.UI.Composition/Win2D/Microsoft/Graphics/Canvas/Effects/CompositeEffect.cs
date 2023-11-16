#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("48FC9F51-F6AC-48F1-8B58-3B28AC46F76D")]
internal class CompositeEffect : ICanvasEffect
{
	private string _name = "CompositeEffect";
	private Guid _id = new Guid("48FC9F51-F6AC-48F1-8B58-3B28AC46F76D");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public CanvasComposite Mode { get; set; } = CanvasComposite.SourceOver;

	public List<IGraphicsEffectSource> Sources { get; set; } = new();

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

	public IGraphicsEffectSource? GetSource(uint index) => index < Sources.Count ? Sources[(int)index] : null;

	public uint GetSourceCount() => (uint)Sources.Count;

	public void Dispose() { }
}
