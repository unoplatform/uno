using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("5CB2D9CF-327D-459F-A0CE-40C0B2086BF7")]
	public class SaturationEffect : ICanvasEffect
	{
		private string _name = "SaturationEffect";
		private Guid _id = new Guid("5CB2D9CF-327D-459F-A0CE-40C0B2086BF7");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float Saturation { get; set; } = 0.5f;

		public IGraphicsEffectSource Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "Saturation":
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

		public object GetProperty(uint index)
		{
			switch (index)
			{
				case 0:
					return Saturation;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 1;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
