using System;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaPlayerPresenter : Border
	{
		private HtmlMediaPlayer _player;

		private void SetVideoSurface(IHtmlMediaPlayer videoSurface)
		{
			_player = videoSurface as HtmlMediaPlayer;
			this.Child = _player;
		}

		private void OnStretchChanged(Stretch newValue, Stretch oldValue)
		{
			ApplyStretch();
		}

		internal void ApplyStretch()
		{
			if (MediaPlayer is null)
				return;

			var stretch = Stretch switch
			{
				Stretch.Uniform => Windows.Media.Playback.MediaPlayer.VideoStretch.Uniform,
				Stretch.Fill => Windows.Media.Playback.MediaPlayer.VideoStretch.Fill,
				Stretch.None => Windows.Media.Playback.MediaPlayer.VideoStretch.None,
				Stretch.UniformToFill => Windows.Media.Playback.MediaPlayer.VideoStretch.UniformToFill,
				_ => throw new NotSupportedException($"Stretch mode {Stretch} is not supported")
			};

			_player.UpdateVideoStretch(stretch);
		}
	}
}
