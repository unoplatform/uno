#nullable enable

using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06")]
	public class LinearTransferEffect : ICanvasEffect
	{
		private string _name = "LinearTransferEffect";
		private Guid _id = new Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public float RedOffset { get; set; }

		public float RedSlope { get; set; } = 1.0f;

		public bool RedDisable { get; set; }


		public float GreenOffset { get; set; }

		public float GreenSlope { get; set; } = 1.0f;

		public bool GreenDisable { get; set; }


		public float BlueOffset { get; set; }

		public float BlueSlope { get; set; } = 1.0f;

		public bool BlueDisable { get; set; }


		public float AlphaOffset { get; set; }

		public float AlphaSlope { get; set; } = 1.0f;

		public bool AlphaDisable { get; set; }


		public bool ClampOutput { get; set; }


		public IGraphicsEffectSource? Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "RedOffset":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "RedSlope":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "RedDisable":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenOffset":
					{
						index = 3;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenSlope":
					{
						index = 4;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "GreenDisable":
					{
						index = 5;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueOffset":
					{
						index = 6;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueSlope":
					{
						index = 7;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BlueDisable":
					{
						index = 8;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaOffset":
					{
						index = 9;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaSlope":
					{
						index = 10;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "AlphaDisable":
					{
						index = 11;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "ClampOutput":
					{
						index = 12;
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
					return RedOffset;
				case 1:
					return RedSlope;
				case 2:
					return RedDisable;
				case 3:
					return GreenOffset;
				case 4:
					return GreenSlope;
				case 5:
					return GreenDisable;
				case 6:
					return BlueOffset;
				case 7:
					return BlueSlope;
				case 8:
					return BlueDisable;
				case 9:
					return AlphaOffset;
				case 10:
					return AlphaSlope;
				case 11:
					return AlphaDisable;
				case 12:
					return ClampOutput;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 13;
		public IGraphicsEffectSource? GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
