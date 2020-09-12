#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MseStreamSource : global::Windows.Media.Core.IMediaSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MseStreamSource.Duration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "TimeSpan? MseStreamSource.Duration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseSourceBufferList ActiveSourceBuffers
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseSourceBufferList MseStreamSource.ActiveSourceBuffers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseReadyState ReadyState
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseReadyState MseStreamSource.ReadyState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseSourceBufferList SourceBuffers
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseSourceBufferList MseStreamSource.SourceBuffers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseTimeRange? LiveSeekableRange
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseTimeRange? MseStreamSource.LiveSeekableRange is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "MseTimeRange? MseStreamSource.LiveSeekableRange");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MseStreamSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "MseStreamSource.MseStreamSource()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MseStreamSource.MseStreamSource()
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Opened.add
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Opened.remove
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Ended.add
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Ended.remove
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Closed.add
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Closed.remove
		// Forced skipping of method Windows.Media.Core.MseStreamSource.SourceBuffers.get
		// Forced skipping of method Windows.Media.Core.MseStreamSource.ActiveSourceBuffers.get
		// Forced skipping of method Windows.Media.Core.MseStreamSource.ReadyState.get
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Duration.get
		// Forced skipping of method Windows.Media.Core.MseStreamSource.Duration.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseSourceBuffer AddSourceBuffer( string mimeType)
		{
			throw new global::System.NotImplementedException("The member MseSourceBuffer MseStreamSource.AddSourceBuffer(string mimeType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void RemoveSourceBuffer( global::Windows.Media.Core.MseSourceBuffer buffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "void MseStreamSource.RemoveSourceBuffer(MseSourceBuffer buffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void EndOfStream( global::Windows.Media.Core.MseEndOfStreamStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "void MseStreamSource.EndOfStream(MseEndOfStreamStatus status)");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MseStreamSource.LiveSeekableRange.get
		// Forced skipping of method Windows.Media.Core.MseStreamSource.LiveSeekableRange.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsContentTypeSupported( string contentType)
		{
			throw new global::System.NotImplementedException("The member bool MseStreamSource.IsContentTypeSupported(string contentType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseStreamSource, object> Closed
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Closed");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Closed");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseStreamSource, object> Ended
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Ended");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Ended");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseStreamSource, object> Opened
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Opened");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseStreamSource", "event TypedEventHandler<MseStreamSource, object> MseStreamSource.Opened");
			}
		}
		#endif
		// Processing: Windows.Media.Core.IMediaSource
	}
}
