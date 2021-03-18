#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Editing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MediaComposition 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Editing.BackgroundAudioTrack> BackgroundAudioTracks
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<BackgroundAudioTrack> MediaComposition.BackgroundAudioTracks is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Editing.MediaClip> Clips
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<MediaClip> MediaComposition.Clips is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MediaComposition.Duration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<string, string> UserData
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<string, string> MediaComposition.UserData is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Editing.MediaOverlayLayer> OverlayLayers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<MediaOverlayLayer> MediaComposition.OverlayLayers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public MediaComposition() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Editing.MediaComposition", "MediaComposition.MediaComposition()");
		}
		#endif
		// Forced skipping of method Windows.Media.Editing.MediaComposition.MediaComposition()
		// Forced skipping of method Windows.Media.Editing.MediaComposition.Duration.get
		// Forced skipping of method Windows.Media.Editing.MediaComposition.Clips.get
		// Forced skipping of method Windows.Media.Editing.MediaComposition.BackgroundAudioTracks.get
		// Forced skipping of method Windows.Media.Editing.MediaComposition.UserData.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Editing.MediaComposition Clone()
		{
			throw new global::System.NotImplementedException("The member MediaComposition MediaComposition.Clone() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveAsync( global::Windows.Storage.IStorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction MediaComposition.SaveAsync(IStorageFile file) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Graphics.Imaging.ImageStream> GetThumbnailAsync( global::System.TimeSpan timeFromStart,  int scaledWidth,  int scaledHeight,  global::Windows.Media.Editing.VideoFramePrecision framePrecision)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ImageStream> MediaComposition.GetThumbnailAsync(TimeSpan timeFromStart, int scaledWidth, int scaledHeight, VideoFramePrecision framePrecision) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Graphics.Imaging.ImageStream>> GetThumbnailsAsync( global::System.Collections.Generic.IEnumerable<global::System.TimeSpan> timesFromStart,  int scaledWidth,  int scaledHeight,  global::Windows.Media.Editing.VideoFramePrecision framePrecision)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<ImageStream>> MediaComposition.GetThumbnailsAsync(IEnumerable<TimeSpan> timesFromStart, int scaledWidth, int scaledHeight, VideoFramePrecision framePrecision) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Transcoding.TranscodeFailureReason, double> RenderToFileAsync( global::Windows.Storage.IStorageFile destination)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<TranscodeFailureReason, double> MediaComposition.RenderToFileAsync(IStorageFile destination) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Transcoding.TranscodeFailureReason, double> RenderToFileAsync( global::Windows.Storage.IStorageFile destination,  global::Windows.Media.Editing.MediaTrimmingPreference trimmingPreference)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<TranscodeFailureReason, double> MediaComposition.RenderToFileAsync(IStorageFile destination, MediaTrimmingPreference trimmingPreference) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.Media.Transcoding.TranscodeFailureReason, double> RenderToFileAsync( global::Windows.Storage.IStorageFile destination,  global::Windows.Media.Editing.MediaTrimmingPreference trimmingPreference,  global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<TranscodeFailureReason, double> MediaComposition.RenderToFileAsync(IStorageFile destination, MediaTrimmingPreference trimmingPreference, MediaEncodingProfile encodingProfile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.MediaProperties.MediaEncodingProfile CreateDefaultEncodingProfile()
		{
			throw new global::System.NotImplementedException("The member MediaEncodingProfile MediaComposition.CreateDefaultEncodingProfile() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSource GenerateMediaStreamSource()
		{
			throw new global::System.NotImplementedException("The member MediaStreamSource MediaComposition.GenerateMediaStreamSource() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSource GenerateMediaStreamSource( global::Windows.Media.MediaProperties.MediaEncodingProfile encodingProfile)
		{
			throw new global::System.NotImplementedException("The member MediaStreamSource MediaComposition.GenerateMediaStreamSource(MediaEncodingProfile encodingProfile) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MediaStreamSource GeneratePreviewMediaStreamSource( int scaledWidth,  int scaledHeight)
		{
			throw new global::System.NotImplementedException("The member MediaStreamSource MediaComposition.GeneratePreviewMediaStreamSource(int scaledWidth, int scaledHeight) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.Editing.MediaComposition.OverlayLayers.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Media.Editing.MediaComposition> LoadAsync( global::Windows.Storage.StorageFile file)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<MediaComposition> MediaComposition.LoadAsync(StorageFile file) is not implemented in Uno.");
		}
		#endif
	}
}
