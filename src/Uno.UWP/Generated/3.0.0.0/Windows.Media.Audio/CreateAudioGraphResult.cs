#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CreateAudioGraphResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioGraph Graph
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioGraph CreateAudioGraphResult.Graph is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AudioGraph%20CreateAudioGraphResult.Graph");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioGraphCreationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioGraphCreationStatus CreateAudioGraphResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AudioGraphCreationStatus%20CreateAudioGraphResult.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception CreateAudioGraphResult.ExtendedError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20CreateAudioGraphResult.ExtendedError");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.CreateAudioGraphResult.Status.get
		// Forced skipping of method Windows.Media.Audio.CreateAudioGraphResult.Graph.get
		// Forced skipping of method Windows.Media.Audio.CreateAudioGraphResult.ExtendedError.get
	}
}
