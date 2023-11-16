#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("921F03D6-641C-47DF-852D-B4BB6153AE11")]
internal class ColorMatrixEffect : ICanvasEffect
{
	private string _name = "ColorMatrixEffect";
	private Guid _id = new Guid("921F03D6-641C-47DF-852D-B4BB6153AE11");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public Matrix5x4 ColorMatrix { get; set; } = Matrix5x4.Identity;

	public CanvasAlphaMode AlphaMode { get; set; }

	public bool ClampOutput { get; set; }

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "ColorMatrix":
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.Direct;
					break;
				}
			case "AlphaMode":
				{
					index = 1;
					mapping = GraphicsEffectPropertyMapping.ColorMatrixAlphaMode;
					break;
				}
			case "ClampOutput":
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

	public object? GetProperty(uint index)
	{
		switch (index)
		{
			case 0:
				return ColorMatrix.ToArray();
			case 1:
				return AlphaMode;
			case 2:
				return ClampOutput;
			default:
				return null;
		}
	}

	public uint GetPropertyCount() => 3;
	public IGraphicsEffectSource? GetSource(uint index) => Source;
	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
