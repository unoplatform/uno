using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("921F03D6-641C-47DF-852D-B4BB6153AE11")]
	public class ColorMatrixEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
	{
		private string _name = "ColorMatrixEffect";
		private Guid _id = new Guid("921F03D6-641C-47DF-852D-B4BB6153AE11");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public Matrix5x4 ColorMatrix { get; set; } = Matrix5x4.Identity;

		public IGraphicsEffectSource Source { get; set; }

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
					return ColorMatrix.ToArray();
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 1;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;
	}
}
