#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.VoiceCommands
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VoiceCommandServiceConnection 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Globalization.Language Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member Language VoiceCommandServiceConnection.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.VoiceCommands.VoiceCommand> GetVoiceCommandAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VoiceCommand> VoiceCommandServiceConnection.GetVoiceCommandAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandConfirmationResult> RequestConfirmationAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VoiceCommandConfirmationResult> VoiceCommandServiceConnection.RequestConfirmationAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandDisambiguationResult> RequestDisambiguationAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<VoiceCommandDisambiguationResult> VoiceCommandServiceConnection.RequestDisambiguationAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportProgressAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandServiceConnection.ReportProgressAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportSuccessAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandServiceConnection.ReportSuccessAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailureAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandServiceConnection.ReportFailureAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RequestAppLaunchAsync( global::Windows.ApplicationModel.VoiceCommands.VoiceCommandResponse response)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandServiceConnection.RequestAppLaunchAsync(VoiceCommandResponse response) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection.Language.get
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection.VoiceCommandCompleted.add
		// Forced skipping of method Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection.VoiceCommandCompleted.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection FromAppServiceTriggerDetails( global::Windows.ApplicationModel.AppService.AppServiceTriggerDetails triggerDetails)
		{
			throw new global::System.NotImplementedException("The member VoiceCommandServiceConnection VoiceCommandServiceConnection.FromAppServiceTriggerDetails(AppServiceTriggerDetails triggerDetails) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection, global::Windows.ApplicationModel.VoiceCommands.VoiceCommandCompletedEventArgs> VoiceCommandCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection", "event TypedEventHandler<VoiceCommandServiceConnection, VoiceCommandCompletedEventArgs> VoiceCommandServiceConnection.VoiceCommandCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.VoiceCommands.VoiceCommandServiceConnection", "event TypedEventHandler<VoiceCommandServiceConnection, VoiceCommandCompletedEventArgs> VoiceCommandServiceConnection.VoiceCommandCompleted");
			}
		}
		#endif
	}
}
