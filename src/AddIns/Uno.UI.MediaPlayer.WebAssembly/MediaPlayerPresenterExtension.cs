#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

[assembly: ApiExtension(typeof(IMediaPlayerPresenterExtension), typeof(Uno.UI.Media.MediaPlayerPresenterExtension))]

namespace Uno.UI.Media;

public class MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private MediaPlayerPresenter? _owner;
	private HtmlMediaPlayer _htmlPlayer;

	public MediaPlayerPresenterExtension(object owner)
	{
		if (owner is not MediaPlayerPresenter presenter)
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}

		_owner = presenter;
		_owner.Child = _htmlPlayer = new HtmlMediaPlayer();
	}

	public void ExitFullScreen() => throw new NotImplementedException();

	public void MediaPlayerChanged()
	{
		Console.WriteLine($"MediaPlayerPresenterExtension.MediaPlayerChanged()");

		if (_owner is not null
			&& MediaPlayerExtension.GetByMediaPlayer(_owner.MediaPlayer) is { } extension)
		{
			extension.HtmlPlayer = _htmlPlayer;
		}
		else
		{
			Console.WriteLine($"MediaPlayerPresenter.OnMediaPlayerChanged: Unable to find associated MediaPlayerExtension");
		}
	}

	public void RequestFullScreen() => throw new NotImplementedException();
	public void StretchChanged()
	{
		if (_owner is not null)
		{
			var stretch = _owner.Stretch switch
			{
				Stretch.Uniform => VideoStretch.Uniform,
				Stretch.Fill => VideoStretch.Fill,
				Stretch.None => VideoStretch.None,
				Stretch.UniformToFill => VideoStretch.UniformToFill,
				_ => throw new NotSupportedException($"Stretch mode {_owner.Stretch} is not supported")
			};

			_htmlPlayer.UpdateVideoStretch(stretch);
		}
	}
}
