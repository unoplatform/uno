#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IMediaFrame : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan? Duration
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Collections.IPropertySet ExtendedProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDiscontinuous
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsReadOnly
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan? RelativeTime
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.TimeSpan? SystemRelativeTime
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.IMediaFrame.Type.get
		// Forced skipping of method Windows.Media.IMediaFrame.IsReadOnly.get
		// Forced skipping of method Windows.Media.IMediaFrame.RelativeTime.set
		// Forced skipping of method Windows.Media.IMediaFrame.RelativeTime.get
		// Forced skipping of method Windows.Media.IMediaFrame.SystemRelativeTime.set
		// Forced skipping of method Windows.Media.IMediaFrame.SystemRelativeTime.get
		// Forced skipping of method Windows.Media.IMediaFrame.Duration.set
		// Forced skipping of method Windows.Media.IMediaFrame.Duration.get
		// Forced skipping of method Windows.Media.IMediaFrame.IsDiscontinuous.set
		// Forced skipping of method Windows.Media.IMediaFrame.IsDiscontinuous.get
		// Forced skipping of method Windows.Media.IMediaFrame.ExtendedProperties.get
	}
}
