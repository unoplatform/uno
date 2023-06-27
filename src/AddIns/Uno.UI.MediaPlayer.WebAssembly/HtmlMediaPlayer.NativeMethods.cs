#nullable enable

#if NET7_0_OR_GREATER
#define USE_JSIMPORT
#endif

using System;
#if USE_JSIMPORT
using System.Runtime.InteropServices.JavaScript;
#endif
using Uno.Foundation;

namespace Uno.UI.Media;

partial class HtmlMediaPlayer
{
	internal static partial class NativeMethods
	{
#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.videoWidth")]
#endif
		internal static partial int VideoWidth(nint htmlId);

#if !USE_JSIMPORT
		internal static partial int VideoWidth(nint htmlId)
			=> throw new NotSupportedException();
#endif


#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.videoHeight")]
#endif
		internal static partial int VideoHeight(nint htmlId);

#if !USE_JSIMPORT
		internal static partial int VideoHeight(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.getCurrentPosition")]
#endif
		internal static partial double GetCurrentPosition(nint htmlId);

#if !USE_JSIMPORT
		internal static partial double GetCurrentPosition(nint htmlId)
			=> throw new NotSupportedException();
#endif


#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.getPaused")]
#endif
		internal static partial bool GetPaused(nint htmlId);

#if !USE_JSIMPORT
		internal static partial bool GetPaused(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setCurrentPosition")]
#endif
		internal static partial void SetCurrentPosition(nint htmlId, double currentPosition);

#if !USE_JSIMPORT
		internal static partial void SetCurrentPosition(nint htmlId, double currentPosition)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setPlaybackRate")]
#endif
		internal static partial void SetPlaybackRate(nint htmlId, double playbackRate);

#if !USE_JSIMPORT
		internal static partial void SetPlaybackRate(nint htmlId, double playbackRate)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setAttribute")]
#endif
		internal static partial void SetAttribute(nint htmlId, string name, string value);

#if !USE_JSIMPORT
		internal static partial void SetAttribute(nint htmlId, string name, string value)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.reload")]
#endif
		internal static partial void Reload(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void Reload(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setVolume")]
#endif
		internal static partial void SetVolume(nint htmlId, float volume);

#if !USE_JSIMPORT
		internal static partial void SetVolume(nint htmlId, float volume)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.getDuration")]
#endif
		internal static partial double GetDuration(nint htmlId);

#if !USE_JSIMPORT
		internal static partial double GetDuration(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setAutoPlay")]
#endif
		internal static partial void SetAutoPlay(nint htmlId, bool enabled);

#if !USE_JSIMPORT
		internal static partial void SetAutoPlay(nint htmlId, bool enabled)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.requestFullScreen")]
#endif
		internal static partial void RequestFullScreen(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void RequestFullScreen(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.exitFullScreen")]
#endif
		internal static partial void ExitFullScreen();

#if !USE_JSIMPORT
		internal static partial void ExitFullScreen()
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.pause")]
#endif
		internal static partial void Pause(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void Pause(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.play")]
#endif
		internal static partial void Play(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void Play(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.stop")]
#endif
		internal static partial void Stop(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void Stop(nint htmlId)
			=> throw new NotSupportedException();
#endif



#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.requestPictureInPicture")]
#endif
		internal static partial void RequestPictureInPicture(nint htmlId);

#if !USE_JSIMPORT
		internal static partial void RequestPictureInPicture(nint htmlId)
			=> throw new NotSupportedException();
#endif

#if USE_JSIMPORT
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.exitPictureInPicture")]
#endif
		internal static partial void ExitPictureInPicture();

#if !USE_JSIMPORT
		internal static partial void ExitPictureInPicture()
			=> throw new NotSupportedException();
#endif
	}
}
