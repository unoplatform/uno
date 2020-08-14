#if __ANDROID__ || __IOS__ || __MACOS__
using System;

namespace Windows.Media.Playback
{
	public  partial class MediaPlayerFailedEventArgs 
	{
		public MediaPlayerError Error { get; internal set; }

		public string ErrorMessage { get; internal set; }

		public Exception ExtendedErrorCode { get; internal set; }
	}
}
#endif
