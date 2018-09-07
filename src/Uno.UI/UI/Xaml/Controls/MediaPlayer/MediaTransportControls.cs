#if __ANDROID__ || __IOS__

using System;
using Windows.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private const string PlayPauseButtonName = "PlayPauseButton";

		private Button _playPauseButton;

		private Windows.Media.Playback.MediaPlayer _mediaPlayer;
		private bool _isTemplateApplied = false;

		public MediaTransportControls() : base()
		{
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_playPauseButton = this.GetTemplateChild(PlayPauseButtonName) as Button;

			_isTemplateApplied = true;
		}

		public  void Show()
		{
		}
		
		public  void Hide()
		{
		}
		
		internal void Unbind()
		{
			_playPauseButton.Click -= PlayPause;
		}

		internal void Bind(Windows.Media.Playback.MediaPlayer mediaPlayer)
		{
			if (!_isTemplateApplied)
			{
				ApplyTemplate();
			}

			Unbind();

			_mediaPlayer = mediaPlayer;

			if (_mediaPlayer == null)
			{
				return;
			}

			_mediaPlayer.CurrentStateChanged += OnMediaPlayerStateChanged;

			_playPauseButton.Click += PlayPause;
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

		internal void StartAutoPlay()
		{
			PlayPause(this, null);
		}

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
	}
}
#endif
