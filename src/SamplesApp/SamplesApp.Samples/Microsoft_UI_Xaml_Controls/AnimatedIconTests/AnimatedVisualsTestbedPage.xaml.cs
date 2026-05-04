#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.AnimatedVisuals;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace UITests.Microsoft_UI_Xaml_Controls.AnimatedIconTests;

[Sample("Icons", IsManualTest = true)]
public sealed partial class AnimatedVisualsTestbedPage : Page
{
	// Each entry exposes the source instance and a setter for its Foreground color
	// (the public Foreground property is type-specific so it can't be reached through a base interface).
	private record SourceEntry(string Name, IAnimatedVisualSource2 Source, Action<Color> SetForeground);

	private readonly List<SourceEntry> _sources;
	private SourceEntry? _current;
	private bool _suppressSliderCallback;

	public AnimatedVisualsTestbedPage()
	{
		InitializeComponent();

		// Every ported source. New entries auto-pick up via reflection on the public Foreground property.
		_sources = new()
		{
			Make("Back", new AnimatedBackVisualSource()),
			Make("Accept", new AnimatedAcceptVisualSource()),
			Make("ChevronDownSmall", new AnimatedChevronDownSmallVisualSource()),
			Make("ChevronRightDownSmall", new AnimatedChevronRightDownSmallVisualSource()),
			Make("ChevronUpDownSmall", new AnimatedChevronUpDownSmallVisualSource()),
			Make("Find", new AnimatedFindVisualSource()),
			Make("GlobalNavigationButton", new AnimatedGlobalNavigationButtonVisualSource()),
			Make("Settings", new AnimatedSettingsVisualSource()),
		};

		foreach (var entry in _sources)
		{
			SourcePicker.Items.Add(entry.Name);
		}

		SourcePicker.SelectedIndex = 0;
	}

	private static SourceEntry Make<T>(string name, T source) where T : class, IAnimatedVisualSource2
	{
		// Resolve a setter for the source-specific Foreground property (every Lottie-generated
		// source exposes one). Other theme-color properties (e.g. AnimatedFindVisualSource may
		// have multiple) are still reachable via SetColorProperty in the marker handler below
		// when paired with a 'theme:' prefix.
		var foregroundSetter = typeof(T).GetProperty("Foreground", BindingFlags.Public | BindingFlags.Instance);
		Action<Color> setForeground = foregroundSetter is { CanWrite: true }
			? color => foregroundSetter.SetValue(source, color)
			: color => source.SetColorProperty("Foreground", color);
		return new SourceEntry(name, source, setForeground);
	}

	private void OnSourceChanged(object sender, SelectionChangedEventArgs e)
	{
		if (SourcePicker.SelectedIndex < 0)
		{
			return;
		}

		_current = _sources[SourcePicker.SelectedIndex];
		Player.Source = (IAnimatedVisualSource)_current.Source;

		BuildMarkerButtons();

		_suppressSliderCallback = true;
		ProgressSlider.Value = 0;
		_suppressSliderCallback = false;
		Player.SetProgress(0);

		StatusText.Text = $"Loaded: {_current.Name}";
	}

	private void BuildMarkerButtons()
	{
		MarkersHost.Children.Clear();
		if (_current is null)
		{
			return;
		}

		var markers = _current.Source.Markers;
		if (markers is null || markers.Count == 0)
		{
			MarkersHost.Children.Add(new TextBlock { Text = "(no markers exposed)" });
			return;
		}

		// Group marker pairs by their _Start / _End suffix so a single button can play the segment.
		// Markers without a matching pair fall back to "set progress" buttons.
		var ordered = markers.OrderBy(kvp => kvp.Value).ToList();
		var seen = new HashSet<string>();

		foreach (var (key, value) in ordered)
		{
			if (seen.Contains(key))
			{
				continue;
			}

			if (key.EndsWith("_Start", StringComparison.Ordinal))
			{
				var transition = key[..^"_Start".Length];
				var endKey = transition + "_End";
				if (markers.TryGetValue(endKey, out var endValue))
				{
					seen.Add(key);
					seen.Add(endKey);

					var fromProgress = value;
					var toProgress = endValue;
					var label = transition.Replace('_', ' ');

					var button = new Button
					{
						Content = $"▶ {label}",
						HorizontalAlignment = HorizontalAlignment.Stretch,
						HorizontalContentAlignment = HorizontalAlignment.Left,
					};
					button.Click += async (_, _) => await PlayAsync(label, fromProgress, toProgress);
					MarkersHost.Children.Add(button);
					continue;
				}
			}

			// Lone marker — exposes a "Set Progress" jump button.
			seen.Add(key);
			var jumpButton = new Button
			{
				Content = $"⇲ Jump to '{key}' ({value:F3})",
				HorizontalAlignment = HorizontalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Left,
			};
			var jumpProgress = value;
			var jumpKey = key;
			jumpButton.Click += (_, _) =>
			{
				Player.SetProgress(jumpProgress);
				_suppressSliderCallback = true;
				ProgressSlider.Value = jumpProgress;
				_suppressSliderCallback = false;
				StatusText.Text = $"Jumped to {jumpKey} ({jumpProgress:F3})";
			};
			MarkersHost.Children.Add(jumpButton);
		}

		// "Play all" plays the entire animation 0 → 1 in one go.
		var playAllButton = new Button
		{
			Content = "▶ Play 0 → 1 (entire timeline)",
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Left,
		};
		playAllButton.Click += async (_, _) => await PlayAsync("0 → 1", 0, 1);
		MarkersHost.Children.Add(playAllButton);
	}

	private async Task PlayAsync(string label, double fromProgress, double toProgress)
	{
		StatusText.Text = $"Playing: {label} ({fromProgress:F3} → {toProgress:F3})";
		await Player.PlayAsync(fromProgress, toProgress, false);
		_suppressSliderCallback = true;
		ProgressSlider.Value = toProgress;
		_suppressSliderCallback = false;
		StatusText.Text = $"Done: {label}";
	}

	private void OnProgressSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		if (_suppressSliderCallback)
		{
			return;
		}

		Player.SetProgress(e.NewValue);
		StatusText.Text = $"Progress: {e.NewValue:F2}";
	}

	private void OnBlackForeground(object sender, RoutedEventArgs e)
		=> _current?.SetForeground(Color.FromArgb(0xFF, 0x00, 0x00, 0x00));

	private void OnRedForeground(object sender, RoutedEventArgs e)
		=> _current?.SetForeground(Color.FromArgb(0xFF, 0xE8, 0x10, 0x23));

	private void OnAccentForeground(object sender, RoutedEventArgs e)
		=> _current?.SetForeground(Color.FromArgb(0xFF, 0x00, 0x78, 0xD4));
}
