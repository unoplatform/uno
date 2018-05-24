#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.DirectX.Direct3D11
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum Direct3DBindings 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VertexBuffer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IndexBuffer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConstantBuffer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ShaderResource,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StreamOutput,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RenderTarget,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DepthStencil,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnorderedAccess,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Decoder,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoEncoder,
		#endif
	}
	#endif
}
