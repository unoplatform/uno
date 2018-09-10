#if __ANDROID__ || __IOS__

using System;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private const string PlayPauseButtonName = "PlayPauseButton";
		private const string PlayPauseButtonOnLeftName = "PlayPauseButtonOnLeft";
		private const string AudioMuteButtonName = "AudioMuteButton";
		private const string VolumeSliderName = "VolumeSlider";

		private Button _playPauseButton;
		private Button _playPauseButtonOnLeft;
		private Button _audioMuteButton;
		private Slider _volumeSlider;

		private Windows.Media.Playback.MediaPlayer _mediaPlayer;

		public MediaTransportControls() : base()
		{
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_playPauseButton = this.GetTemplateChild(PlayPauseButtonName) as Button;
			_playPauseButtonOnLeft = this.GetTemplateChild(PlayPauseButtonOnLeftName) as Button;
			_audioMuteButton = this.GetTemplateChild(AudioMuteButtonName) as Button;
			_volumeSlider = this.GetTemplateChild(VolumeSliderName) as Slider;
		}

		public  void Show()
		{
			VisualStateManager.GoToState(this, "ControlPanelFadeIn", false);
		}
		
		public  void Hide()
		{
			VisualStateManager.GoToState(this, "ControlPanelFadeOut", false);
		}
		
		internal void Unbind()
		{
			_playPauseButton.Click -= PlayPause;
			_playPauseButtonOnLeft.Click -= PlayPause;
			_audioMuteButton.Click -= ToggleMute;
			_volumeSlider.ValueChanged -= OnVolumeChanged;
		}

		internal void Bind(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			ApplyTemplate();
			Unbind();

			_mediaPlayer = mediaPlayer;

			if (_mediaPlayer == null)
			{
				return;
			}

			_mediaPlayer.CurrentStateChanged += OnMediaPlayerStateChanged;

			_playPauseButton.Click += PlayPause;
			_playPauseButtonOnLeft.Click += PlayPause;
			_audioMuteButton.Click += ToggleMute;
			_volumeSlider.ValueChanged += OnVolumeChanged;
		}

		private void OnMediaPlayerStateChanged(Windows.Media.Playback.MediaPlayer sender, object args)
		{
			var state = (MediaPlayerState)args;
			
			switch (state)
			{
				case MediaPlayerState.Paused:
					VisualStateManager.GoToState(this, "PlayState", false);
					break;
				case MediaPlayerState.Playing:
					VisualStateManager.GoToState(this, "PauseState", false);
					break;
			}
		}

		#region Interaction with MediaPlayer

		private void PlayPause(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer.CurrentState == MediaPlayerState.Buffering || _mediaPlayer.CurrentState == MediaPlayerState.Playing)
			{
				_mediaPlayer.Pause();
			}
			else
			{
				_mediaPlayer.Play();
			}
		}

		private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			_mediaPlayer.Volume = e.NewValue;
			VisualStateManager.GoToState(this, _mediaPlayer.Volume == 0 ? "MuteState" : "VolumeState", false);
		}

		private void ToggleMute(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
			VisualStateManager.GoToState(this, _mediaPlayer.IsMuted ? "MuteState" : "VolumeState", false);
		}

		#endregion
	}
}
#endif
