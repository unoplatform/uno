#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AudioBuffer : global::Windows.Foundation.IMemoryBuffer,global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Length
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint AudioBuffer.Length is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20AudioBuffer.Length");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.AudioBuffer", "uint AudioBuffer.Length");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Capacity
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint AudioBuffer.Capacity is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20AudioBuffer.Capacity");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.AudioBuffer.Capacity.get
		// Forced skipping of method Windows.Media.AudioBuffer.Length.get
		// Forced skipping of method Windows.Media.AudioBuffer.Length.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IMemoryBufferReference CreateReference()
		{
			throw new global::System.NotImplementedException("The member IMemoryBufferReference AudioBuffer.CreateReference() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IMemoryBufferReference%20AudioBuffer.CreateReference%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.AudioBuffer", "void AudioBuffer.Dispose()");
		}
		#endif
		// Processing: Windows.Foundation.IMemoryBuffer
		// Processing: System.IDisposable
	}
}
