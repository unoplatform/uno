#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VariablePhotoSequenceCapture 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StartAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VariablePhotoSequenceCapture.StartAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction StopAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VariablePhotoSequenceCapture.StopAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction FinishAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VariablePhotoSequenceCapture.FinishAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoSequenceCapture.PhotoCaptured.add
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoSequenceCapture.PhotoCaptured.remove
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoSequenceCapture.Stopped.add
		// Forced skipping of method Windows.Media.Capture.Core.VariablePhotoSequenceCapture.Stopped.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction UpdateSettingsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VariablePhotoSequenceCapture.UpdateSettingsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.Core.VariablePhotoSequenceCapture, global::Windows.Media.Capture.Core.VariablePhotoCapturedEventArgs> PhotoCaptured
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Core.VariablePhotoSequenceCapture", "event TypedEventHandler<VariablePhotoSequenceCapture, VariablePhotoCapturedEventArgs> VariablePhotoSequenceCapture.PhotoCaptured");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Core.VariablePhotoSequenceCapture", "event TypedEventHandler<VariablePhotoSequenceCapture, VariablePhotoCapturedEventArgs> VariablePhotoSequenceCapture.PhotoCaptured");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Capture.Core.VariablePhotoSequenceCapture, object> Stopped
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Core.VariablePhotoSequenceCapture", "event TypedEventHandler<VariablePhotoSequenceCapture, object> VariablePhotoSequenceCapture.Stopped");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Capture.Core.VariablePhotoSequenceCapture", "event TypedEventHandler<VariablePhotoSequenceCapture, object> VariablePhotoSequenceCapture.Stopped");
			}
		}
		#endif
	}
}
