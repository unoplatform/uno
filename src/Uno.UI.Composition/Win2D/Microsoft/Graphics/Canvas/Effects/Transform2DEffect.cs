#nullable enable

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("6AA97485-6354-4CFC-908C-E4A74F62C96C")]
	public class Transform2DEffect : ICanvasEffect
	{
		private string _name = "Transform2DEffect";
		private Guid _id = new Guid("6AA97485-6354-4CFC-908C-E4A74F62C96C");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public CanvasImageInterpolation InterpolationMode { get; set; } = CanvasImageInterpolation.Linear;

		public EffectBorderMode BorderMode { get; set; } = EffectBorderMode.Soft;

		public Matrix3x2 TransformMatrix { get; set; } = Matrix3x2.Identity;

		public float Sharpness { get; set; }

		public IGraphicsEffectSource? Source { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping)
		{
			switch (name)
			{
				case "InterpolationMode":
					{
						index = 0;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "BorderMode":
					{
						index = 1;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "TransformMatrix":
					{
						index = 2;
						mapping = GraphicsEffectPropertyMapping.Direct;
						break;
					}
				case "Sharpness":
					{
						index = 3;
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
					return InterpolationMode;
				case 1:
					return BorderMode;
				case 2:
					return TransformMatrix;
				case 3:
					return Sharpness;
				default:
					return null;
			}
		}

		public uint GetPropertyCount() => 4;
		public IGraphicsEffectSource? GetSource(uint index) => Source;
		public uint GetSourceCount() => 1;

		public void Dispose() { }
	}
}
