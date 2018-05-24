#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.DirectX
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DirectXPixelFormat 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32A32Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32A32Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32A32UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32A32Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32B32Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16B16A16Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G32Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32G8X24Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		D32FloatS8X24UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32FloatX8X24Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X32TypelessG8X24UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R10G10B10A2Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R10G10B10A2UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R10G10B10A2UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R11G11B10Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8A8Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16G16Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		D32Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R32Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R24G8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		D24UIntNormalizedS8UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R24UIntNormalizedX8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		X24TypelessG8UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		D16UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R16Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8UInt,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8Int,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		A8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R1UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R9G9B9E5SharedExponent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R8G8B8G8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		G8R8G8B8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC1Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC1UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC1UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC2Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC2UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC2UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC3Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC3UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC3UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC4Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC4UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC4IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC5Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC5UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC5IntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B5G6R5UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B5G5R5A1UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8A8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8X8UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		R10G10B10XRBiasA2UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8A8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8A8UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8X8Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B8G8R8X8UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC6HTypeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC6H16UnsignedFloat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC6H16Float,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC7Typeless,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC7UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BC7UIntNormalizedSrgb,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ayuv,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y410,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y416,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NV12,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P010,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P016,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Opaque420,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Yuy2,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y210,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Y216,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NV11,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AI44,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IA44,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		A8P8,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		B4G4R4A4UIntNormalized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		P208,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		V208,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		V408,
		#endif
	}
	#endif
}
