using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("B9E303C3-C08C-4F91-8B7B-38656BC48C20")]
	public class PointDiffuseEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
	{
		private string _name = "PointDiffuseEffect";
		private Guid _id = new Guid("B9E303C3-C08C-4F91-8B7B-38656BC48C20");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public Vector3 LightPosition { get; set; }

		public float DiffuseAmount { get; set; } = 1.0f;

		public Color LightColor { get; set; } = Colors.White;

		public IGraphicsEffectSource Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "LightPosition":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "DiffuseAmount":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "LightColor":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.ColorToVector3;
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
					return LightPosition;
				case 1:
					return DiffuseAmount;
				case 2:
					return LightColor;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 3;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;
	}
}
