#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ConversationalAgent
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ConversationalAgentSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentState AgentState
		{
			get
			{
				throw new global::System.NotImplementedException("The member ConversationalAgentState ConversationalAgentSession.AgentState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsIndicatorLightAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsIndicatorLightAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInterrupted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsInterrupted is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInterruptible
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsInterruptible is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsScreenAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsScreenAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUserAuthenticated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsUserAuthenticated is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsVoiceActivationAvailable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.IsVoiceActivationAvailable is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSignal Signal
		{
			get
			{
				throw new global::System.NotImplementedException("The member ConversationalAgentSignal ConversationalAgentSession.Signal is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SessionInterrupted.add
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SessionInterrupted.remove
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SignalDetected.add
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SignalDetected.remove
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SystemStateChanged.add
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.SystemStateChanged.remove
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.AgentState.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.Signal.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsIndicatorLightAvailable.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsScreenAvailable.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsUserAuthenticated.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsVoiceActivationAvailable.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsInterruptible.get
		// Forced skipping of method Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession.IsInterrupted.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse> RequestInterruptibleAsync( bool interruptible)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConversationalAgentSessionUpdateResponse> ConversationalAgentSession.RequestInterruptibleAsync(bool interruptible) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse RequestInterruptible( bool interruptible)
		{
			throw new global::System.NotImplementedException("The member ConversationalAgentSessionUpdateResponse ConversationalAgentSession.RequestInterruptible(bool interruptible) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse> RequestAgentStateChangeAsync( global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentState state)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConversationalAgentSessionUpdateResponse> ConversationalAgentSession.RequestAgentStateChangeAsync(ConversationalAgentState state) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse RequestAgentStateChange( global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentState state)
		{
			throw new global::System.NotImplementedException("The member ConversationalAgentSessionUpdateResponse ConversationalAgentSession.RequestAgentStateChange(ConversationalAgentState state) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse> RequestForegroundActivationAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConversationalAgentSessionUpdateResponse> ConversationalAgentSession.RequestForegroundActivationAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionUpdateResponse RequestForegroundActivation()
		{
			throw new global::System.NotImplementedException("The member ConversationalAgentSessionUpdateResponse ConversationalAgentSession.RequestForegroundActivation() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<object> GetAudioClientAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<object> ConversationalAgentSession.GetAudioClientAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object GetAudioClient()
		{
			throw new global::System.NotImplementedException("The member object ConversationalAgentSession.GetAudioClient() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Audio.AudioDeviceInputNode> CreateAudioDeviceInputNodeAsync( global::Windows.Media.Audio.AudioGraph graph)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AudioDeviceInputNode> ConversationalAgentSession.CreateAudioDeviceInputNodeAsync(AudioGraph graph) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Audio.AudioDeviceInputNode CreateAudioDeviceInputNode( global::Windows.Media.Audio.AudioGraph graph)
		{
			throw new global::System.NotImplementedException("The member AudioDeviceInputNode ConversationalAgentSession.CreateAudioDeviceInputNode(AudioGraph graph) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetAudioCaptureDeviceIdAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> ConversationalAgentSession.GetAudioCaptureDeviceIdAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetAudioCaptureDeviceId()
		{
			throw new global::System.NotImplementedException("The member string ConversationalAgentSession.GetAudioCaptureDeviceId() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> GetAudioRenderDeviceIdAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> ConversationalAgentSession.GetAudioRenderDeviceIdAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetAudioRenderDeviceId()
		{
			throw new global::System.NotImplementedException("The member string ConversationalAgentSession.GetAudioRenderDeviceId() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<uint> GetSignalModelIdAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<uint> ConversationalAgentSession.GetSignalModelIdAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint GetSignalModelId()
		{
			throw new global::System.NotImplementedException("The member uint ConversationalAgentSession.GetSignalModelId() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SetSignalModelIdAsync( uint signalModelId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> ConversationalAgentSession.SetSignalModelIdAsync(uint signalModelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool SetSignalModelId( uint signalModelId)
		{
			throw new global::System.NotImplementedException("The member bool ConversationalAgentSession.SetSignalModelId(uint signalModelId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<uint>> GetSupportedSignalModelIdsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<uint>> ConversationalAgentSession.GetSupportedSignalModelIdsAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<uint> GetSupportedSignalModelIds()
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<uint> ConversationalAgentSession.GetSupportedSignalModelIds() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "void ConversationalAgentSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession> GetCurrentSessionAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ConversationalAgentSession> ConversationalAgentSession.GetCurrentSessionAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession GetCurrentSessionSync()
		{
			throw new global::System.NotImplementedException("The member ConversationalAgentSession ConversationalAgentSession.GetCurrentSessionSync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession, global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSessionInterruptedEventArgs> SessionInterrupted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSessionInterruptedEventArgs> ConversationalAgentSession.SessionInterrupted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSessionInterruptedEventArgs> ConversationalAgentSession.SessionInterrupted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession, global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSignalDetectedEventArgs> SignalDetected
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSignalDetectedEventArgs> ConversationalAgentSession.SignalDetected");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSignalDetectedEventArgs> ConversationalAgentSession.SignalDetected");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession, global::Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSystemStateChangedEventArgs> SystemStateChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSystemStateChangedEventArgs> ConversationalAgentSession.SystemStateChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ConversationalAgent.ConversationalAgentSession", "event TypedEventHandler<ConversationalAgentSession, ConversationalAgentSystemStateChangedEventArgs> ConversationalAgentSession.SystemStateChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
