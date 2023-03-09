using System;
using Uno.Extensions;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

public partial class MediaPlayerElement
{
	private HtmlMediaPlayer _mediaPlayer;

	partial void Initialize()
	{
		_mediaPlayer = new HtmlMediaPlayer();
	}
}
