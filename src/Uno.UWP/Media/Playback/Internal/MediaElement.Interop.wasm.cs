#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.Devices.Media
{
	internal static partial class MediaElement
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.Media.MediaElement.videoWidth")]
			internal static partial int VideoWidth(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.videoHeight")]
			internal static partial int VideoHeight(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.getCurrentPosition")]
			internal static partial double GetCurrentPosition(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.setCurrentPosition")]
			internal static partial void SetCurrentPosition(nint htmlId, double currentPosition);

			[JSImport("globalThis.Windows.Media.MediaElement.reload")]
			internal static partial void Reload(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.setVolume")]
			internal static partial void SetVolume(nint htmlId, float volume);

			[JSImport("globalThis.Windows.Media.MediaElement.getDuration")]
			internal static partial double GetDuration(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.setAutoPlay")]
			internal static partial void SetAutoPlay(nint htmlId, bool enabled);

			[JSImport("globalThis.Windows.Media.MediaElement.requestFullScreen")]
			internal static partial void RequestFullScreen(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.exitFullScreen")]
			internal static partial void ExitFullScreen();

			[JSImport("globalThis.Windows.Media.MediaElement.pause")]
			internal static partial void Pause(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.play")]
			internal static partial void Play(nint htmlId);

			[JSImport("globalThis.Windows.Media.MediaElement.stop")]
			internal static partial void Stop(nint htmlId);
		}
	}
}
#endif
