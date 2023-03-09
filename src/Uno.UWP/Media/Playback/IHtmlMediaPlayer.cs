using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
namespace Windows.Media.Playback;

public interface IHtmlMediaPlayer : IDisposable
{
	bool AreTransportControlsEnabled { get; set; }
	bool AutoPlay { get; set; }
	bool IsVideo { get; }
	bool IsAudio { get; }
	int VideoWidth { get; }
	int VideoHeight { get; }
	double CurrentPosition { get; set; }

	string Source { get; set; }
	void Pause();
	void Play();
	void Stop();
	void RequestFullScreen();
	void ExitFullScreen();
	double Duration { get; }
	void SetVolume(float volume);
	void SetAnonymousCORS(bool enable);
	void UpdateVideoStretch(MediaPlayer.VideoStretch stretch);
	event EventHandler<object> OnSourceLoaded;
	event EventHandler<object> OnSourceFailed;
	event EventHandler<object> OnSourceEnded;
	event EventHandler<object> OnMetadataLoaded;
	event EventHandler<object> OnTimeUpdate;
}
