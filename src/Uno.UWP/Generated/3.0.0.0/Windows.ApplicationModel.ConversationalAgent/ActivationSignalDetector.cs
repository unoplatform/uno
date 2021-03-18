#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ConversationalAgent
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ActivationSignalDetector 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanCreateConfigurations
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ActivationSignalDetector.CanCreateConfigurations is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectorKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ActivationSignalDetectorKind ActivationSignalDetector.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ProviderId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ActivationSignalDetector.ProviderId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> SupportedModelDataTypes
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> ActivationSignalDetector.SupportedModelDataTypes is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectorPowerState> SupportedPowerStates
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ActivationSignalDetectorPowerState> ActivationSignalDetector.SupportedPowerStates is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectionTrainingDataFormat> SupportedTrainingDataFormats
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<ActivationSignalDetectionTrainingDataFormat> ActivationSignalDetector.SupportedTrainingDataFormats is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.ProviderId.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.Kind.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.CanCreateConfigurations.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.SupportedModelDataTypes.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.SupportedTrainingDataFormats.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector.SupportedPowerStates.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> GetSupportedModelIdsForSignalId( string signalId)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<string> ActivationSignalDetector.GetSupportedModelIdsForSignalId(string signalId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> GetSupportedModelIdsForSignalIdAsync( string signalId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> ActivationSignalDetector.GetSupportedModelIdsForSignalIdAsync(string signalId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void CreateConfiguration( string signalId,  string modelId,  string displayName)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector", "void ActivationSignalDetector.CreateConfiguration(string signalId, string modelId, string displayName)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction CreateConfigurationAsync( string signalId,  string modelId,  string displayName)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ActivationSignalDetector.CreateConfigurationAsync(string signalId, string modelId, string displayName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectionConfiguration> GetConfigurations()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ActivationSignalDetectionConfiguration> ActivationSignalDetector.GetConfigurations() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectionConfiguration>> GetConfigurationsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ActivationSignalDetectionConfiguration>> ActivationSignalDetector.GetConfigurationsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectionConfiguration GetConfiguration( string signalId,  string modelId)
		{
			throw new global::System.NotImplementedException("The member ActivationSignalDetectionConfiguration ActivationSignalDetector.GetConfiguration(string signalId, string modelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetectionConfiguration> GetConfigurationAsync( string signalId,  string modelId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ActivationSignalDetectionConfiguration> ActivationSignalDetector.GetConfigurationAsync(string signalId, string modelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveConfiguration( string signalId,  string modelId)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ActivationSignalDetector", "void ActivationSignalDetector.RemoveConfiguration(string signalId, string modelId)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RemoveConfigurationAsync( string signalId,  string modelId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ActivationSignalDetector.RemoveConfigurationAsync(string signalId, string modelId) is not implemented in Uno.");
		}
		#endif
	}
}
