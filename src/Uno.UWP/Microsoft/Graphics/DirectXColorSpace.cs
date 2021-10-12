#if HAS_UNO_WINUI
using Microsoft.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Graphics.DirectX
{
	[ContractVersion(typeof(LiftedContract), 65536u)]
	public enum DirectXColorSpace
	{
		RgbFullG22NoneP709,
		RgbFullG10NoneP709,
		RgbStudioG22NoneP709,
		RgbStudioG22NoneP2020,
		Reserved,
		YccFullG22NoneP709X601,
		YccStudioG22LeftP601,
		YccFullG22LeftP601,
		YccStudioG22LeftP709,
		YccFullG22LeftP709,
		YccStudioG22LeftP2020,
		YccFullG22LeftP2020,
		RgbFullG2084NoneP2020,
		YccStudioG2084LeftP2020,
		RgbStudioG2084NoneP2020,
		YccStudioG22TopLeftP2020,
		YccStudioG2084TopLeftP2020,
		RgbFullG22NoneP2020,
		YccStudioGHlgTopLeftP2020,
		YccFullGHlgTopLeftP2020,
		RgbStudioG24NoneP709,
		RgbStudioG24NoneP2020,
		YccStudioG24LeftP709,
		YccStudioG24LeftP2020,
		YccStudioG24TopLeftP2020
	}

}
#endif
