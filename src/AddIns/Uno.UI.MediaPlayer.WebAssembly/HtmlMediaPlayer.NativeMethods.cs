#nullable enable

using System.Runtime.InteropServices.JavaScript;

namespace Uno.UI.Media;

partial class HtmlMediaPlayer
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.videoWidth")]
		internal static partial int VideoWidth(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.videoHeight")]
		internal static partial int VideoHeight(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.getCurrentPosition")]
		internal static partial double GetCurrentPosition(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setCurrentPosition")]
		internal static partial void SetCurrentPosition(nint htmlId, double currentPosition);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.reload")]
		internal static partial void Reload(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setVolume")]
		internal static partial void SetVolume(nint htmlId, float volume);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.getDuration")]
		internal static partial double GetDuration(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.setAutoPlay")]
		internal static partial void SetAutoPlay(nint htmlId, bool enabled);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.requestFullScreen")]
		internal static partial void RequestFullScreen(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.exitFullScreen")]
		internal static partial void ExitFullScreen();

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.pause")]
		internal static partial void Pause(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.play")]
		internal static partial void Play(nint htmlId);

		[JSImport("globalThis.Uno.UI.Media.HtmlMediaPlayer.stop")]
		internal static partial void Stop(nint htmlId);
	}
}
