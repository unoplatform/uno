#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Audio
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CreateAudioFileInputNodeResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFileInputNode FileInputNode
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioFileInputNode CreateAudioFileInputNodeResult.FileInputNode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AudioFileInputNode%20CreateAudioFileInputNodeResult.FileInputNode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioFileNodeCreationStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member AudioFileNodeCreationStatus CreateAudioFileInputNodeResult.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=AudioFileNodeCreationStatus%20CreateAudioFileInputNodeResult.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Exception ExtendedError
		{
			get
			{
				throw new global::System.NotImplementedException("The member Exception CreateAudioFileInputNodeResult.ExtendedError is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Exception%20CreateAudioFileInputNodeResult.ExtendedError");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Audio.CreateAudioFileInputNodeResult.Status.get
		// Forced skipping of method Windows.Media.Audio.CreateAudioFileInputNodeResult.FileInputNode.get
		// Forced skipping of method Windows.Media.Audio.CreateAudioFileInputNodeResult.ExtendedError.get
	}
}
