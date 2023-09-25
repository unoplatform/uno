#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42")]
	public class GammaTransferEffect : ICanvasEffect
	{
		private string _name = "GammaTransferEffect";
		private Guid _id = new Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float RedAmplitude { get; set; } = 1.0f;

		public float RedExponent { get; set; } = 1.0f;

		public float RedOffset { get; set; }

		public bool RedDisable { get; set; }


		public float GreenAmplitude { get; set; } = 1.0f;

		public float GreenExponent { get; set; } = 1.0f;

		public float GreenOffset { get; set; }

		public bool GreenDisable { get; set; }


		public float BlueAmplitude { get; set; } = 1.0f;

		public float BlueExponent { get; set; } = 1.0f;

		public float BlueOffset { get; set; }

		public bool BlueDisable { get; set; }


		public float AlphaAmplitude { get; set; } = 1.0f;

		public float AlphaExponent { get; set; } = 1.0f;

		public float AlphaOffset { get; set; }

		public bool AlphaDisable { get; set; }


		public bool ClampOutput { get; set; }


		public IGraphicsEffectSource? Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "RedAmplitude":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "RedExponent":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "RedOffset":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "RedDisable":
					{
						index = 3;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenAmplitude":
					{
						index = 4;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenExponent":
					{
						index = 5;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenOffset":
					{
						index = 6;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenDisable":
					{
						index = 7;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueAmplitude":
					{
						index = 8;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueExponent":
					{
						index = 9;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueOffset":
					{
						index = 10;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueDisable":
					{
						index = 11;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaAmplitude":
					{
						index = 12;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaExponent":
					{
						index = 13;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaOffset":
					{
						index = 14;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaDisable":
					{
						index = 15;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "ClampOutput":
					{
						index = 16;
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
					return RedAmplitude;
				case 1:
					return RedExponent;
				case 2:
					return RedOffset;
				case 3:
					return RedDisable;
				case 4:
					return GreenAmplitude;
				case 5:
					return GreenExponent;
				case 6:
					return GreenOffset;
				case 7:
					return GreenDisable;
				case 8:
					return BlueAmplitude;
				case 9:
					return BlueExponent;
				case 10:
					return BlueOffset;
				case 11:
					return BlueDisable;
				case 12:
					return AlphaAmplitude;
				case 13:
					return AlphaExponent;
				case 14:
					return AlphaOffset;
				case 15:
					return AlphaDisable;
				case 16:
					return ClampOutput;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 17;
		public IGraphicsEffectSource? GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
