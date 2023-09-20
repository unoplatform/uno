using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("B648A78A-0ED5-4F80-A94A-8E825ACA6B77")]
	public class ContrastEffect : ICanvasEffect
	{
		private string _name = "ContrastEffect";
		private Guid _id = new Guid("B648A78A-0ED5-4F80-A94A-8E825ACA6B77");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float Contrast { get; set; }

		public bool ClampSource { get; set; }

		public IGraphicsEffectSource Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "Contrast":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "ClampSource":
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

		public object GetProperty(uint index)
		{
			switch (index)
			{
				case 0:
					return Contrast;
				case 1:
					return ClampSource;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 2;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
