#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("B56C8CFA-F634-41EE-BEE0-FFA617106004")]
	public class ExposureEffect : ICanvasEffect
	{
		private string _name = "ExposureEffect";
		private Guid _id = new Guid("B56C8CFA-F634-41EE-BEE0-FFA617106004");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float Exposure { get; set; }

		public IGraphicsEffectSource? Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "Exposure":
				case "ExposureValue":
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
					return Exposure;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 1;
		public IGraphicsEffectSource? GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
