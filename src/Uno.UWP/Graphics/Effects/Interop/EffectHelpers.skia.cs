using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace Windows.Graphics.Effects.Interop
{
	internal static partial class EffectHelpers
	{
		public static SKBlendMode ToSkia(this D2D1BlendEffectMode blendMode)
		{
			switch (blendMode)
			{
				case D2D1BlendEffectMode.Multiply:
					return SKBlendMode.Multiply;

				case D2D1BlendEffectMode.Screen:
					return SKBlendMode.Screen;

				case D2D1BlendEffectMode.Darken:
					return SKBlendMode.Darken;

				case D2D1BlendEffectMode.Lighten:
					return SKBlendMode.Lighten;

				case D2D1BlendEffectMode.ColorBurn:
					return SKBlendMode.ColorBurn;

				case D2D1BlendEffectMode.ColorDodge:
					return SKBlendMode.ColorDodge;

				case D2D1BlendEffectMode.Overlay:
					return SKBlendMode.Overlay;

				case D2D1BlendEffectMode.SoftLight:
					return SKBlendMode.SoftLight;

				case D2D1BlendEffectMode.HardLight:
					return SKBlendMode.HardLight;

				case D2D1BlendEffectMode.Difference:
					return SKBlendMode.Difference;

				case D2D1BlendEffectMode.Exclusion:
					return SKBlendMode.Exclusion;

				case D2D1BlendEffectMode.Hue:
					return SKBlendMode.Hue;

				case D2D1BlendEffectMode.Saturation:
					return SKBlendMode.Saturation;

				case D2D1BlendEffectMode.Color:
					return SKBlendMode.Color;

				case D2D1BlendEffectMode.Luminosity:
					return SKBlendMode.Luminosity;

				// Unsupported modes
				case D2D1BlendEffectMode.LinearBurn:
				case D2D1BlendEffectMode.DarkerColor:
				case D2D1BlendEffectMode.LighterColor:
				case D2D1BlendEffectMode.LinearDodge:
				case D2D1BlendEffectMode.VividLight:
				case D2D1BlendEffectMode.LinearLight:
				case D2D1BlendEffectMode.PinLight:
				case D2D1BlendEffectMode.HardMix:
				case D2D1BlendEffectMode.Subtract:
				case D2D1BlendEffectMode.Division:
				default:
					return (SKBlendMode)0xFF;
			}
		}

		public static SKBlendMode ToSkia(this D2D1CompositeMode compositeMode)
		{
			switch (compositeMode)
			{
				case D2D1CompositeMode.SourceOver:
					return SKBlendMode.SrcOver;

				case D2D1CompositeMode.DestinationOver:
					return SKBlendMode.DstOver;

				case D2D1CompositeMode.SourceIn:
					return SKBlendMode.SrcIn;

				case D2D1CompositeMode.DestinationIn:
					return SKBlendMode.DstIn;

				case D2D1CompositeMode.SourceOut:
					return SKBlendMode.SrcOut;

				case D2D1CompositeMode.DestinationOut:
					return SKBlendMode.DstOut;

				case D2D1CompositeMode.SourceAtop:
					return SKBlendMode.SrcATop;

				case D2D1CompositeMode.DestinationAtop:
					return SKBlendMode.DstATop;

				case D2D1CompositeMode.Xor:
					return SKBlendMode.Xor;

				case D2D1CompositeMode.Add:
					return SKBlendMode.Plus;

				// Unsupported modes

				case D2D1CompositeMode.Copy:
				case D2D1CompositeMode.MaskInvert:
				default:
					return (SKBlendMode)0xFF;
			}
		}

		public static SKShaderTileMode ToSkia(this D2D1BorderEdgeMode mode)
		{
			switch (mode)
			{
				case D2D1BorderEdgeMode.Clamp:
					return SKShaderTileMode.Clamp;
				case D2D1BorderEdgeMode.Wrap:
					return SKShaderTileMode.Repeat;
				case D2D1BorderEdgeMode.Mirror:
					return SKShaderTileMode.Mirror;
				default:
					return SKShaderTileMode.Clamp;
			}
		}
	}
}
