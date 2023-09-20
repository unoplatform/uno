using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("3E7EFD62-A32D-46D4-A83C-5278889AC954")]
	public class DistantDiffuseEffect : IGraphicsEffect, IGraphicsEffectSource, IGraphicsEffectD2D1Interop
	{
		private string _name = "DistantDiffuseEffect";
		private Guid _id = new Guid("3E7EFD62-A32D-46D4-A83C-5278889AC954");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public float Azimuth { get; set; }

		public float Elevation { get; set; }

		public float DiffuseAmount { get; set; } = 1.0f;

		public Color LightColor { get; set; } = Colors.White;

		public IGraphicsEffectSource Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "Azimuth":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
						break;
					}
				case "Elevation":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.RadiansToDegrees;
						break;
					}
				case "DiffuseAmount":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "LightColor":
					{
						index = 3;
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
					return Azimuth;
				case 1:
					return Elevation;
				case 2:
					return DiffuseAmount;
				case 3:
					return LightColor;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 4;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;
	}
}
