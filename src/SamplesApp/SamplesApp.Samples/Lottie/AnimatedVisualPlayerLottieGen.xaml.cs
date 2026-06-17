using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Lottie;

[Sample("Lottie",
	Name = "AnimatedVisualPlayer (LottieGen sources)",
	Description = "Exercises the AnimatedVisualPlayer API (play/pause/resume/stop, from/to range, loop, progress scrub, autoplay, stretch; PlaybackRate is surfaced but not yet wired on the Skia flow) with LottieGen-generated IAnimatedVisualSource2 sources.",
	IgnoreInSnapshotTests = true)]
public sealed partial class AnimatedVisualPlayerLottieGen : Page
{
	// LottieGen-generated sources live in the same assembly (compiled from Lottie/*.cs into each
	// SamplesApp head), so they can be instantiated directly despite being internal.
	private static readonly (string Name, Func<IAnimatedVisualSource> Factory)[] _sources =
	{
		("Cooking", static () => new AnimatedVisuals.Cooking()),
		("Cute cat", static () => new AnimatedVisuals.Cute_cat()),
		("Steaming bowl", static () => new AnimatedVisuals.Steaming_bowl()),
	};

	public AnimatedVisualPlayerLottieGen()
	{
		this.InitializeComponent();

		StretchSelector.ItemsSource = Enum.GetValues<Stretch>();
		StretchSelector.SelectedItem = Stretch.Uniform;

		var names = new string[_sources.Length];
		for (var i = 0; i < _sources.Length; i++)
		{
			names[i] = _sources[i].Name;
		}

		SourceSelector.ItemsSource = names;
		SourceSelector.SelectedIndex = 0;

		Player.RegisterPropertyChangedCallback(AnimatedVisualPlayer.DurationProperty, OnDurationChanged);
	}

	private bool Looped => LoopToggle.IsChecked == true;

	private void OnDurationChanged(DependencyObject sender, DependencyProperty dp)
		=> DurationText.Text = $"Duration: {Player.Duration.TotalSeconds:0.###} s";

	private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var index = SourceSelector.SelectedIndex;
		if (index >= 0 && index < _sources.Length)
		{
			Player.Source = _sources[index].Factory();
		}
	}

	private void PlayButton_Click(object sender, RoutedEventArgs e)
		=> _ = Player.PlayAsync(FromSlider.Value, ToSlider.Value, Looped);

	private void PlayFullButton_Click(object sender, RoutedEventArgs e)
		=> _ = Player.PlayAsync(0, 1, Looped);

	private void PauseButton_Click(object sender, RoutedEventArgs e)
		=> Player.Pause();

	private void ResumeButton_Click(object sender, RoutedEventArgs e)
		=> Player.Resume();

	private void StopButton_Click(object sender, RoutedEventArgs e)
		=> Player.Stop();

	private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		=> Player?.SetProgress(e.NewValue);
}
