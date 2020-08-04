#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Buffer : global::Windows.Storage.Streams.IBuffer
	{
		// Skipping already declared property Length
		// Skipping already declared property Capacity
		// Skipping already declared method Windows.Storage.Streams.Buffer.Buffer(uint)
		// Forced skipping of method Windows.Storage.Streams.Buffer.Buffer(uint)
		// Forced skipping of method Windows.Storage.Streams.Buffer.Capacity.get
		// Forced skipping of method Windows.Storage.Streams.Buffer.Length.get
		// Forced skipping of method Windows.Storage.Streams.Buffer.Length.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Storage.Streams.Buffer CreateCopyFromMemoryBuffer( global::Windows.Foundation.IMemoryBuffer input)
		{
			throw new global::System.NotImplementedException("The member Buffer Buffer.CreateCopyFromMemoryBuffer(IMemoryBuffer input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.MemoryBuffer CreateMemoryBufferOverIBuffer( global::Windows.Storage.Streams.IBuffer input)
		{
			throw new global::System.NotImplementedException("The member MemoryBuffer Buffer.CreateMemoryBufferOverIBuffer(IBuffer input) is not implemented in Uno.");
		}
		#endif
		// Processing: Windows.Storage.Streams.IBuffer
	}
}
