#nullable enable

using System;
using Atk;
using Cairo;
using Pango;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Media.Playback;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

[assembly: ApiExtension(typeof(IMediaPlayerPresenterExtension), typeof(Uno.UI.Media.MediaPlayerPresenterExtension))]

namespace Uno.UI.Media;

public class MediaPlayerPresenterExtension : IMediaPlayerPresenterExtension
{
	private MediaPlayerPresenter? _owner;
	private GTKMediaPlayer _player;

	public MediaPlayerPresenterExtension(object owner)
	{
		if (owner is not MediaPlayerPresenter presenter)
		{
			throw new InvalidOperationException($"MediaPlayerPresenterExtension must be initialized with a MediaPlayer instance");
		}
		_owner = presenter;


		_player = new GTKMediaPlayer();

		var ContentView = new ContentControl();
		ContentView.Content = _player;

		ContentView.VerticalContentAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		ContentView.HorizontalContentAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		//ContentView.Opacity = 1;
		ContentView.Background = new SolidColorBrush(Colors.Yellow);
		_owner.Child = ContentView;

		//var width = _owner.Width = _owner.ActualWidth;
		////_player.Width = 500;
		////_player.Height = 240;
		//_player.Background = new SolidColorBrush(Colors.Green);

		//StackPanel stackPanel = new StackPanel();
		//stackPanel.Children.Add(_player);
		//stackPanel.Orientation = Orientation.Vertical;
		//stackPanel.HorizontalAlignment = HorizontalAlignment.Center;
		//stackPanel.VerticalAlignment = VerticalAlignment.Center;
		//_owner.Child = stackPanel;






		//Grid grid = new Grid();

		//grid.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
		//grid.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
		//grid.BorderBrush = new SolidColorBrush(Colors.Blue);
		//grid.BorderThickness = new Thickness(2);

		//RowDefinition rowDef = new RowDefinition();
		//rowDef.Height = new GridLength(10, GridUnitType.Star);
		//grid.RowDefinitions.Add(rowDef);

		//RowDefinition rowDef1 = new RowDefinition();
		//rowDef1.Height = new GridLength(1, GridUnitType.Auto);
		//grid.RowDefinitions.Add(rowDef1);

		//RowDefinition rowDef2 = new RowDefinition();
		//rowDef2.Height = new GridLength(10, GridUnitType.Star);
		//grid.RowDefinitions.Add(rowDef2);



		//ColumnDefinition colDef = new ColumnDefinition();
		//colDef.Width = new GridLength(10, GridUnitType.Star);
		//grid.ColumnDefinitions.Add(colDef);

		//ColumnDefinition colDef1 = new ColumnDefinition();
		//colDef1.Width = new GridLength(1, GridUnitType.Auto);
		//grid.ColumnDefinitions.Add(colDef1);

		//ColumnDefinition colDef2 = new ColumnDefinition();
		//colDef2.Width = new GridLength(10, GridUnitType.Star);
		//grid.ColumnDefinitions.Add(colDef2);


		//Button button = new Button();
		//button.Content = ContentView;
		//Grid.SetRow(button, 1);
		//Grid.SetColumn(button, 1);

		//Button button00 = new Button();
		//button00.Content = "1";
		//button00.Background = new SolidColorBrush(Colors.Red);
		//Grid.SetRow(button00, 0);
		//Grid.SetColumn(button00, 0);

		//Button button01 = new Button();
		//button01.Content = "2";
		//button01.Background = new SolidColorBrush(Colors.Yellow);
		//Grid.SetRow(button01, 2);
		//Grid.SetColumn(button01, 2);

		//grid.Children.Add(button);
		//_owner.Child = grid;



		//_player.UpdateVideoStretch();
	}


	public void MediaPlayerChanged()
	{
		//if (this.Log().IsEnabled(LogLevel.Debug))
		//{
		//	this.Log().LogDebug("Enter MediaPlayerPresenterExtension.MediaPlayerChanged().");
		//}
		if (_owner is not null
			&& MediaPlayerExtension.GetByMediaPlayer(_owner.MediaPlayer) is { } extension)
		{
			extension.GTKMediaPlayer = _player;
			//var ContentView = new Button();
			//ContentView.Content = _player.VideoView;
			//ContentView.
			//
			//= new SolidColorBrush(Colors.Red);
			////ContentView.Width = 800;
			////ContentView.Height = 640;
			//_player.UpdateVideoStretch();
			//_owner.Child = ContentView;
		}
		else
		{
			//if (this.Log().IsEnabled(LogLevel.Debug))
			//{
			//	this.Log().LogDebug($"MediaPlayerPresenter.OnMediaPlayerChanged: Unable to find associated MediaPlayerExtension");
			//}
		}
	}

	public void ExitFullScreen() => throw new NotImplementedException();
	public void RequestFullScreen() => throw new NotImplementedException();
	public void StretchChanged()
	{
		if (_owner is not null)
		{
			_player.UpdateVideoStretch(_owner.Stretch);
		}
	}
}
