using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5")]
	public class GaussianBlurEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
	{
		private string _name = "GaussianBlurEffect";
		private Guid _id = new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public IGraphicsEffectSource Source { get; set; }

		public float BlurAmount { get; set; } = 3.0f;

		public EffectOptimization Optimization { get; set; } = EffectOptimization.Balanced;

		public EffectBorderMode BorderMode { get; set; } = EffectBorderMode.Soft;

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "BlurAmount":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "Optimization":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BorderMode":
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

		public object GetProperty(uint index)
		{
			switch (index)
			{
				case 0:
					return BlurAmount;
				case 1:
					return (uint)Optimization;
				case 2:
					return (uint)BorderMode;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 3;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;
	}
}
