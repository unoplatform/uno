#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if false || false || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaSource : global::System.IDisposable,global::Windows.Media.Playback.IMediaPlaybackSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.ValueSet CustomProperties
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet MediaSource.CustomProperties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan? Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MediaSource.Duration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.Media.Core.TimedMetadataTrack> ExternalTimedMetadataTracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableVector<TimedMetadataTrack> MediaSource.ExternalTimedMetadataTracks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.Collections.IObservableVector<global::Windows.Media.Core.TimedTextSource> ExternalTimedTextSources
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableVector<TimedTextSource> MediaSource.ExternalTimedTextSources is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsOpen
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MediaSource.IsOpen is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Core.MediaSourceState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaSourceState MediaSource.State is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Streaming.Adaptive.AdaptiveMediaSource AdaptiveMediaSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member AdaptiveMediaSource MediaSource.AdaptiveMediaSource is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Core.MediaStreamSource MediaStreamSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaStreamSource MediaSource.MediaStreamSource is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.Core.MseStreamSource MseStreamSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseStreamSource MediaSource.MseStreamSource is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri MediaSource.Uri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Networking.BackgroundTransfer.DownloadOperation DownloadOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member DownloadOperation MediaSource.DownloadOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSource.OpenOperationCompleted.add
		// Forced skipping of method Windows.Media.Core.MediaSource.OpenOperationCompleted.remove
		// Forced skipping of method Windows.Media.Core.MediaSource.CustomProperties.get
		// Forced skipping of method Windows.Media.Core.MediaSource.Duration.get
		// Forced skipping of method Windows.Media.Core.MediaSource.IsOpen.get
		// Forced skipping of method Windows.Media.Core.MediaSource.ExternalTimedTextSources.get
		// Forced skipping of method Windows.Media.Core.MediaSource.ExternalTimedMetadataTracks.get
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "void MediaSource.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSource.StateChanged.add
		// Forced skipping of method Windows.Media.Core.MediaSource.StateChanged.remove
		// Forced skipping of method Windows.Media.Core.MediaSource.State.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "void MediaSource.Reset()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSource.AdaptiveMediaSource.get
		// Forced skipping of method Windows.Media.Core.MediaSource.MediaStreamSource.get
		// Forced skipping of method Windows.Media.Core.MediaSource.MseStreamSource.get
		// Forced skipping of method Windows.Media.Core.MediaSource.Uri.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction OpenAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaSource.OpenAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MediaSource.DownloadOperation.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromDownloadOperation( global::Windows.Networking.BackgroundTransfer.DownloadOperation downloadOperation)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromDownloadOperation(DownloadOperation downloadOperation) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromMediaFrameSource( global::Windows.Media.Capture.Frames.MediaFrameSource frameSource)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromMediaFrameSource(MediaFrameSource frameSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromMediaBinder( global::Windows.Media.Core.MediaBinder binder)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromMediaBinder(MediaBinder binder) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromAdaptiveMediaSource( global::Windows.Media.Streaming.Adaptive.AdaptiveMediaSource mediaSource)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromAdaptiveMediaSource(AdaptiveMediaSource mediaSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromMediaStreamSource( global::Windows.Media.Core.MediaStreamSource mediaSource)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromMediaStreamSource(MediaStreamSource mediaSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromMseStreamSource( global::Windows.Media.Core.MseStreamSource mediaSource)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromMseStreamSource(MseStreamSource mediaSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromIMediaSource( global::Windows.Media.Core.IMediaSource mediaSource)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromIMediaSource(IMediaSource mediaSource) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromStorageFile( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromStorageFile(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromStream( global::Windows.Storage.Streams.IRandomAccessStream stream,  string contentType)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromStream(IRandomAccessStream stream, string contentType) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromStreamReference( global::Windows.Storage.Streams.IRandomAccessStreamReference stream,  string contentType)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromStreamReference(IRandomAccessStreamReference stream, string contentType) is not implemented in Uno.");
		}
		#endif
		#if false || false || NET461 || __WASM__ || false
		[global::Uno.NotImplemented]
		public static global::Windows.Media.Core.MediaSource CreateFromUri( global::System.Uri uri)
		{
			throw new global::System.NotImplementedException("The member MediaSource MediaSource.CreateFromUri(Uri uri) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MediaSource, global::Windows.Media.Core.MediaSourceOpenOperationCompletedEventArgs> OpenOperationCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "event TypedEventHandler<MediaSource, MediaSourceOpenOperationCompletedEventArgs> MediaSource.OpenOperationCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "event TypedEventHandler<MediaSource, MediaSourceOpenOperationCompletedEventArgs> MediaSource.OpenOperationCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MediaSource, global::Windows.Media.Core.MediaSourceStateChangedEventArgs> StateChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "event TypedEventHandler<MediaSource, MediaSourceStateChangedEventArgs> MediaSource.StateChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MediaSource", "event TypedEventHandler<MediaSource, MediaSourceStateChangedEventArgs> MediaSource.StateChanged");
			}
		}
		#endif
		// Processing: System.IDisposable
		// Processing: Windows.Media.Playback.IMediaPlaybackSource
	}
}
