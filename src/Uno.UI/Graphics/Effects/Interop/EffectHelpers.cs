using System;
using Windows.Foundation;
using Windows.Graphics.Effects;
using System.Runtime.InteropServices;

namespace Windows.UI.Graphics.Effects.Interop
{
	internal static class EffectHelpers
	{
		public static EffectType GetEffectType(Guid effectId)
		{
			// D2D1

			if (effectId.Equals(new Guid("C80ECFF0-3FD5-4F05-8328-C5D1724B4F0A")))
				return EffectType.AlphaMaskEffect;

			if (effectId.Equals(new Guid("FC151437-049A-4784-A24A-F1C4DAF20987")))
				return EffectType.ArithmeticCompositeEffect;

			if (effectId.Equals(new Guid("81C5B77B-13F8-4CDD-AD20-C890547AC65D")))
				return EffectType.BlendEffect;

			if (effectId.Equals(new Guid("2A2D49C0-4ACF-43C7-8C6A-7C4A27874D27")))
				return EffectType.BorderEffect;

			if (effectId.Equals(new Guid("921F03D6-641C-47DF-852D-B4BB6153AE11")))
				return EffectType.ColorMatrixEffect;

			if (effectId.Equals(new Guid("61C23C20-AE69-4D8E-94CF-50078DF638F2")))
				return EffectType.ColorSourceEffect;

			if (effectId.Equals(new Guid("48FC9F51-F6AC-48F1-8B58-3B28AC46F76D")))
				return EffectType.CompositeEffect;

			if (effectId.Equals(new Guid("B648A78A-0ED5-4F80-A94A-8E825ACA6B77")))
				return EffectType.ContrastEffect;

			if (effectId.Equals(new Guid("12F575E8-4DB1-485F-9A84-03A07DD3829F")))
				return EffectType.CrossFadeEffect;

			if (effectId.Equals(new Guid("3E7EFD62-A32D-46D4-A83C-5278889AC954")))
				return EffectType.DistantDiffuseEffect;

			if (effectId.Equals(new Guid("428C1EE5-77B8-4450-8AB5-72219C21ABDA")))
				return EffectType.DistantSpecularEffect;

			if (effectId.Equals(new Guid("B56C8CFA-F634-41EE-BEE0-FFA617106004")))
				return EffectType.ExposureEffect;

			if (effectId.Equals(new Guid("409444C4-C419-41A0-B0C1-8CD0C0A18E42")))
				return EffectType.GammaTransferEffect;

			if (effectId.Equals(new Guid("1FEB6D69-2FE6-4AC9-8C58-1D7F93E7A6A5")))
				return EffectType.GaussianBlurEffect;

			if (effectId.Equals(new Guid("36DDE0EB-3725-42E0-836D-52FB20AEE644")))
				return EffectType.GrayscaleEffect;

			if (effectId.Equals(new Guid("0F4458EC-4B32-491B-9E85-BD73F44D3EB6")))
				return EffectType.HueRotationEffect;

			if (effectId.Equals(new Guid("E0C3784D-CB39-4E84-B6FD-6B72F0810263")))
				return EffectType.InvertEffect;

			if (effectId.Equals(new Guid("41251AB7-0BEB-46F8-9DA7-59E93FCCE5DE")))
				return EffectType.LuminanceToAlphaEffect;

			if (effectId.Equals(new Guid("AD47C8FD-63EF-4ACC-9B51-67979C036C06")))
				return EffectType.LinearTransferEffect;

			if (effectId.Equals(new Guid("811D79A4-DE28-4454-8094-C64685F8BD4C")))
				return EffectType.OpacityEffect;

			if (effectId.Equals(new Guid("B9E303C3-C08C-4F91-8B7B-38656BC48C20")))
				return EffectType.PointDiffuseEffect;

			if (effectId.Equals(new Guid("09C3CA26-3AE2-4F09-9EBC-ED3865D53F22")))
				return EffectType.PointSpecularEffect;

			if (effectId.Equals(new Guid("5CB2D9CF-327D-459F-A0CE-40C0B2086BF7")))
				return EffectType.SaturationEffect;

			if (effectId.Equals(new Guid("3A1AF410-5F1D-4DBE-84DF-915DA79B7153")))
				return EffectType.SepiaEffect;

			if (effectId.Equals(new Guid("818A1105-7932-44F4-AA86-08AE7B2F2C93")))
				return EffectType.SpotDiffuseEffect;

			if (effectId.Equals(new Guid("EDAE421E-7654-4A37-9DB8-71ACC1BEB3C1")))
				return EffectType.SpotSpecularEffect;

			if (effectId.Equals(new Guid("89176087-8AF9-4A08-AEB1-895F38DB1766")))
				return EffectType.TemperatureAndTintEffect;

			if (effectId.Equals(new Guid("36312B17-F7DD-4014-915D-FFCA768CF211")))
				return EffectType.TintEffect;

			if (effectId.Equals(new Guid("6AA97485-6354-4CFC-908C-E4A74F62C96C")))
				return EffectType.Transform2DEffect;

			// Composition

			if (effectId.Equals(new Guid("91BB5E52-95D1-4F8B-9A5A-6408B24B8C6A"))) // wuceffects.dll!Windows::UI::Composition::CLSID_SceneLightingEffect
				return EffectType.SceneLightingEffect;

			if (effectId.Equals(new Guid("6152DFC6-9FBA-4810-8CBA-B280AA27BFF6"))) // wuceffects.dll!Windows::UI::Composition::CLSID_WhiteNoiseEffect
				return EffectType.WhiteNoiseEffect;

			// Unsupported

			else
				return EffectType.Unsupported;
		}
	}
}
