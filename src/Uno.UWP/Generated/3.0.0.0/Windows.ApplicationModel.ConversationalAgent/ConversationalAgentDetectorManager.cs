#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ConversationalAgent
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConversationalAgentDetectorManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentDetectorManager Default
		{
			get
			{
				throw new global::System.NotImplementedException("The member ConversationalAgentDetectorManager ConversationalAgentDetectorManager.Default is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector> GetAllActivationSignalDetectors()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ActivationSignalDetector> ConversationalAgentDetectorManager.GetAllActivationSignalDetectors() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector>> GetAllActivationSignalDetectorsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ActivationSignalDetector>> ConversationalAgentDetectorManager.GetAllActivationSignalDetectorsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector> GetActivationSignalDetectors( global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectorKind kind)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ActivationSignalDetector> ConversationalAgentDetectorManager.GetActivationSignalDetectors(ActivationSignalDetectorKind kind) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector>> GetActivationSignalDetectorsAsync( global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectorKind kind)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ActivationSignalDetector>> ConversationalAgentDetectorManager.GetActivationSignalDetectorsAsync(ActivationSignalDetectorKind kind) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentDetectorManager.Default.get
	}
}
