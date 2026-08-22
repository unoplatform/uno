using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "Interactive Storyboard lifecycle controls — Begin / Pause / Resume / Stop / Seek / SkipToFill — with live GetCurrentTime readout.")]
public sealed partial class StoryboardControlShowcase : Page
{
	private readonly DispatcherTimer _timer;

	public StoryboardControlShowcase()
	{
		this.InitializeComponent();
		_timer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(100),
		};
		_timer.Tick += (_, _) => UpdateCurrentTime();

		Loaded += (_, _) => _timer.Start();
		Unloaded += (_, _) =>
		{
			_timer.Stop();
			ControllableStoryboard.Stop();
		};

		ControllableStoryboard.Completed += (_, _) => Status("Completed");
	}

	private void UpdateCurrentTime()
	{
		var t = ControllableStoryboard.GetCurrentTime();
		CurrentTimeText.Text = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
	}

	private void Status(string message) => StatusText.Text = message;

	private void DoBegin(object sender, RoutedEventArgs e)
	{
		ControllableStoryboard.Begin();
		Status("Begin()");
	}

	private void DoPause(object sender, RoutedEventArgs e)
	{
		ControllableStoryboard.Pause();
		Status("Pause()");
	}

	private void DoResume(object sender, RoutedEventArgs e)
	{
		ControllableStoryboard.Resume();
		Status("Resume()");
	}

	private void DoStop(object sender, RoutedEventArgs e)
	{
		ControllableStoryboard.Stop();
		Status("Stop()");
	}

	private void DoSkipToFill(object sender, RoutedEventArgs e)
	{
		ControllableStoryboard.SkipToFill();
		Status("SkipToFill()");
	}

	private void SeekTo(double seconds)
	{
		ControllableStoryboard.Seek(TimeSpan.FromSeconds(seconds));
		Status($"Seek({seconds:0.0}s)");
	}

	private void SeekTo0(object sender, RoutedEventArgs e) => SeekTo(0);
	private void SeekTo1(object sender, RoutedEventArgs e) => SeekTo(1);
	private void SeekTo3(object sender, RoutedEventArgs e) => SeekTo(3);
	private void SeekTo5(object sender, RoutedEventArgs e) => SeekTo(5);
	private void SeekToEnd(object sender, RoutedEventArgs e) => SeekTo(6);
}
