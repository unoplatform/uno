using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Windows.UI;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("428C1EE5-77B8-4450-8AB5-72219C21ABDA")]
	public class DistantSpecularEffect : ICanvasEffect
	{
		private string _name = "DistantSpecularEffect";
		private Guid _id = new Guid("428C1EE5-77B8-4450-8AB5-72219C21ABDA");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float Azimuth { get; set; }

		public float Elevation { get; set; }

		public float SpecularExponent { get; set; } = 1.0f;

		public float SpecularAmount { get; set; } = 1.0f;

		public Color LightColor { get; set; } = Colors.White;

		public Vector4 LightColorHdr
		{
			get => new(LightColor.R * 255.0f, LightColor.G * 255.0f, LightColor.B * 255.0f, LightColor.A * 255.0f);
			set => LightColor = new((byte)(value.W / 255.0f), (byte)(value.X / 255.0f), (byte)(value.Y / 255.0f), (byte)(value.Z / 255.0f));
		}

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
				case "SpecularExponent":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "SpecularAmount":
					{
						index = 3;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "LightColor":
					{
						index = 4;
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
					return SpecularExponent;
				case 3:
					return SpecularAmount;
				case 4:
					return LightColor;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 5;
		public IGraphicsEffectSource GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
