using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace AnimatedVisualPlayerPlayground;

[Sample("Lottie", Name = "Generated animations playground", IgnoreInSnapshotTests = true)]
public sealed partial class GeneratedAnimationsPlayground : Page
{
	public GeneratedAnimationsPlayground()
	{
		this.InitializeComponent();

		stretchPicker.ItemsSource = Enum.GetNames<Stretch>();
		stretchPicker.SelectionChanged += (_, _) =>
		{
			if (stretchPicker.SelectedItem is string name && Enum.TryParse<Stretch>(name, out var stretch))
			{
				player.Stretch = stretch;
			}
		};
		stretchPicker.SelectedItem = nameof(Stretch.Uniform);

		autoPlayToggle.Toggled += (_, _) => player.AutoPlay = autoPlayToggle.IsOn;

		playbackRateSlider.ValueChanged += (_, e) =>
		{
			player.PlaybackRate = e.NewValue;
			playbackRateLabel.Text = $"Playback rate: {e.NewValue:0.00}";
		};

		fromSlider.ValueChanged += (_, e) => fromLabel.Text = $"From: {e.NewValue:0.00}";
		toSlider.ValueChanged += (_, e) => toLabel.Text = $"To: {e.NewValue:0.00}";

		progressSlider.ValueChanged += (_, e) =>
		{
			progressLabel.Text = $"Progress: {e.NewValue:0.00}";
			player.SetProgress(e.NewValue);
		};

		playButton.Click += (_, _) => _ = player.PlayAsync(fromSlider.Value, toSlider.Value, false);
		loopButton.Click += (_, _) => _ = player.PlayAsync(fromSlider.Value, toSlider.Value, true);
		pauseButton.Click += (_, _) => player.Pause();
		resumeButton.Click += (_, _) => player.Resume();
		stopButton.Click += (_, _) => player.Stop();

		player.RegisterPropertyChangedCallback(AnimatedVisualPlayer.IsPlayingProperty, (_, _) => UpdateStatus());
		player.RegisterPropertyChangedCallback(AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty, (_, _) => UpdateStatus());
		player.RegisterPropertyChangedCallback(AnimatedVisualPlayer.DurationProperty, (_, _) => UpdateStatus());

		PopulateSources();
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		loadedText.Text = $"IsAnimatedVisualLoaded: {player.IsAnimatedVisualLoaded}";
		playingText.Text = $"IsPlaying: {player.IsPlaying}";
		durationText.Text = $"Duration: {player.Duration.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)} s";
	}

	private void PopulateSources()
	{
		var sources = new List<KeyValuePair<string, Func<IAnimatedVisualSource>>>();

#if __SKIA__
		sources.Add(new("Watermelon", () => new global::AnimatedVisuals.Watermelon()));
		sources.Add(new("Gradient shapes", () => new global::AnimatedVisuals.Gradient_shapes()));
		sources.Add(new("Lottie logo", () => new global::AnimatedVisuals.LottieLogo1()));
		sources.Add(new("Pin jump", () => new global::AnimatedVisuals.PinJump()));
		sources.Add(new("Hamburger arrow", () => new global::AnimatedVisuals.HamburgerArrow()));
#endif

		sourcePicker.DisplayMemberPath = nameof(KeyValuePair<string, Func<IAnimatedVisualSource>>.Key);
		sourcePicker.ItemsSource = sources;
		sourcePicker.SelectionChanged += (_, _) =>
		{
			if (sourcePicker.SelectedItem is KeyValuePair<string, Func<IAnimatedVisualSource>> selected)
			{
				player.Source = selected.Value();
			}
		};

		if (sources.Count > 0)
		{
			sourcePicker.SelectedIndex = 0;
		}
		else
		{
			durationText.Text = "Composition Lottie rendering requires the Skia backend.";
		}
	}
}
