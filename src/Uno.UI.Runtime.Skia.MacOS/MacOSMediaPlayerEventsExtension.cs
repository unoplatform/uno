#nullable enable

using System.Runtime.CompilerServices;

using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSMediaPlayerEventsExtension : IMediaPlayerEventsExtension
{
	public void NaturalDurationChanged() => NotImplemented();
	public void RaiseBufferingEnded() => NotImplemented();
	public void RaiseBufferingStarted() => NotImplemented();
	public void RaiseCurrentStateChanged() => NotImplemented();
	public void RaiseIsMutedChanged() => NotImplemented();
	public void RaiseMediaEnded() => NotImplemented();
	public void RaiseMediaFailed(MediaPlayerError error, string? errorMessage, Exception? extendedErrorCode) => NotImplemented();
	public void RaiseMediaOpened() => NotImplemented();
	public void RaiseMediaPlayerRateChanged(double newRate) => NotImplemented();
	public void RaiseNaturalVideoDimensionChanged() => NotImplemented();
	public void RaisePlaybackMediaMarkerReached(PlaybackMediaMarker playbackMediaMarker) => NotImplemented();
	public void RaisePositionChanged() => NotImplemented();
	public void RaiseSeekCompleted() => NotImplemented();
	public void RaiseSourceChanged() => NotImplemented();
	public void RaiseSubtitleFrameChanged() => NotImplemented();
	public void RaiseVideoFrameAvailable() => NotImplemented();
	public void RaiseVolumeChanged() => NotImplemented();

	public void NotImplemented([CallerMemberName] string name = "unknown")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Member {name} is not implemented");
		}
	}
}
