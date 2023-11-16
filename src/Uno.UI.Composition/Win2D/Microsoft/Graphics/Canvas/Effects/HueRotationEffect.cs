#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects;

[Guid("0F4458EC-4B32-491B-9E85-BD73F44D3EB6")]
internal class HueRotationEffect : ICanvasEffect
{
	private string _name = "HueRotationEffect";
	private Guid _id = new Guid("0F4458EC-4B32-491B-9E85-BD73F44D3EB6");

	public string Name
	{
		get => _name;
		set => _name = value;
	}

	public CanvasBufferPrecision? BufferPrecision { get; set; }

	public bool CacheOutput { get; set; }

	public float Angle { get; set; } // Radians

	public IGraphicsEffectSource? Source { get; set; }

	public Guid GetEffectId() => _id;

	public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
	{
		switch (name)
		{
			case "Angle":
				{
					index = 0;
					mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
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
				return Angle;
			default:
				return null;
		}
	}

	public uint GetPropertyCount() => 1;
	public IGraphicsEffectSource? GetSource(uint index) => Source;
	public uint GetSourceCount() => 1;

	public void Dispose() { }
}
