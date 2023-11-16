#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("41251AB7-0BEB-46F8-9DA7-59E93FCCE5DE")]
internal class LuminanceToAlphaEffect : ICanvasEffect
{
	private string _name = "LuminanceToAlphaEffect";
	private Guid _id = new Guid("41251AB7-0BEB-46F8-9DA7-59E93FCCE5DE");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping) => throw new NotSupportedException();

	public object GetProperty(uint index) => throw new NotSupportedException();

	public uint GetPropertyCount() => 0;
	public IGraphicsEffectSource? GetSource(uint index) => Source;
	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
