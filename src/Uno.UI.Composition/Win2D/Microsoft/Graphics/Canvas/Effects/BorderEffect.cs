using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27")]
	public class BorderEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
	{
		private string _name = "BorderEffect";
		private Guid _id = new Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasEdgeBehavior ExtendX { get; set; } = CanvasEdgeBehavior.Clamp;

		public CanvasEdgeBehavior ExtendY { get; set; } = CanvasEdgeBehavior.Clamp;

		public IGraphicsEffectSource Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "ExtendX":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "ExtendY":
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
					return ExtendX;
				case 1:
					return ExtendY;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 2;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;
	}
}
