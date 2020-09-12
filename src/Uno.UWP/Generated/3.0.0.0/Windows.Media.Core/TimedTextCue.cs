#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TimedTextCue : global::Windows.Media.Core.IMediaCue
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan StartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan TimedTextCue.StartTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "TimeSpan TimedTextCue.StartTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TimedTextCue.Id is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "string TimedTextCue.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan TimedTextCue.Duration is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "TimeSpan TimedTextCue.Duration");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.TimedTextStyle CueStyle
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedTextStyle TimedTextCue.CueStyle is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "TimedTextStyle TimedTextCue.CueStyle");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.TimedTextRegion CueRegion
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimedTextRegion TimedTextCue.CueRegion is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "TimedTextRegion TimedTextCue.CueRegion");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Media.Core.TimedTextLine> Lines
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<TimedTextLine> TimedTextCue.Lines is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public TimedTextCue() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.TimedTextCue", "TimedTextCue.TimedTextCue()");
		}
		#endif
		// Forced skipping of method Windows.Media.Core.TimedTextCue.TimedTextCue()
		// Forced skipping of method Windows.Media.Core.TimedTextCue.CueRegion.get
		// Forced skipping of method Windows.Media.Core.TimedTextCue.CueRegion.set
		// Forced skipping of method Windows.Media.Core.TimedTextCue.CueStyle.get
		// Forced skipping of method Windows.Media.Core.TimedTextCue.CueStyle.set
		// Forced skipping of method Windows.Media.Core.TimedTextCue.Lines.get
		// Forced skipping of method Windows.Media.Core.TimedTextCue.StartTime.set
		// Forced skipping of method Windows.Media.Core.TimedTextCue.StartTime.get
		// Forced skipping of method Windows.Media.Core.TimedTextCue.Duration.set
		// Forced skipping of method Windows.Media.Core.TimedTextCue.Duration.get
		// Forced skipping of method Windows.Media.Core.TimedTextCue.Id.set
		// Forced skipping of method Windows.Media.Core.TimedTextCue.Id.get
		// Processing: Windows.Media.Core.IMediaCue
	}
}
