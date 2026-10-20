#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Shared.Windows_UI_Composition.GitHubContributionsWinUI;

[Sample(
	"Cool Graphics",
	Name = "GitHubContributionsWinUI",
	IsManualTest = true,
	Description =
		"The same contributions wave as the Skia sample, built only from public WinUI API: one Border " +
		"per day, with a Storyboard per cell driving a ScaleTransform and the background colour off a " +
		"staggered BeginTime. No custom drawing.")]
public sealed partial class GitHubContributionsWinUI : Page
{
	private const double Cell = 14;
	private const double Gap = 3;
	private const int Rows = 7;
	private const int Weeks = 53;

	private static readonly Color[] Ramp =
	{
		Color.FromArgb(255, 235, 237, 240),
		Color.FromArgb(255, 155, 233, 168),
		Color.FromArgb(255, 64, 196, 99),
		Color.FromArgb(255, 48, 161, 78),
		Color.FromArgb(255, 33, 110, 57),
	};

	private readonly List<Border> _cells = new();
	private readonly List<int> _levels = new();

	private bool _filled;

	public GitHubContributionsWinUI()
	{
		InitializeComponent();

		Build();
		runButton.Click += (_, _) => Toggle();
		Loaded += (_, _) => statsText.Text = $"{_cells.Count:N0} squares";
	}

	private void Build()
	{
		var rng = new Random(20261006);
		grid.Width = Weeks * (Cell + Gap) - Gap;
		grid.Height = Rows * (Cell + Gap) - Gap;

		for (var w = 0; w < Weeks; w++)
		{
			for (var d = 0; d < Rows; d++)
			{
				var r = rng.NextDouble();
				var level = r switch
				{
					< 0.42 => 0,
					< 0.66 => 1,
					< 0.84 => 2,
					< 0.95 => 3,
					_ => 4,
				};

				var cellBorder = new Border
				{
					Width = Cell,
					Height = Cell,
					CornerRadius = new CornerRadius(3),
					Background = new SolidColorBrush(Ramp[0]),
					RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
					RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 },
				};

				Canvas.SetLeft(cellBorder, w * (Cell + Gap));
				Canvas.SetTop(cellBorder, d * (Cell + Gap));
				grid.Children.Add(cellBorder);

				_cells.Add(cellBorder);
				_levels.Add(level);
			}
		}
	}

	private void Toggle()
	{
		_filled = !_filled;
		runButton.Content = _filled ? "Clear" : "Run";

		var storyboard = new Storyboard();

		for (var i = 0; i < _cells.Count; i++)
		{
			var w = i / Rows;
			var d = i % Rows;
			var delay = _filled
				? TimeSpan.FromMilliseconds(45 * (w + (Rows - 1 - d)))
				: TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500));

			var element = _cells[i];
			var transform = (ScaleTransform)element.RenderTransform;

			// Both axes of the pop ride the same Storyboard clock as the colour. The dip to 0.4 and
			// the overshoot back past 1 are what make a square land rather than fade in.
			foreach (var axis in new[] { "ScaleX", "ScaleY" })
			{
				var scale = new DoubleAnimationUsingKeyFrames { BeginTime = delay };
				scale.KeyFrames.Add(new LinearDoubleKeyFrame
				{
					KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
					Value = 1,
				});
				scale.KeyFrames.Add(new EasingDoubleKeyFrame
				{
					KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)),
					Value = 0.4,
					EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
				});
				scale.KeyFrames.Add(new EasingDoubleKeyFrame
				{
					KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(620)),
					Value = 1,
					EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 1.1 },
				});

				Storyboard.SetTarget(scale, transform);
				Storyboard.SetTargetProperty(scale, axis);
				storyboard.Children.Add(scale);
			}

			var target = _filled ? Ramp[_levels[i]] : Ramp[0];
			var colour = new ColorAnimation
			{
				To = target,
				Duration = new Duration(TimeSpan.FromMilliseconds(380)),
				BeginTime = delay,
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
			};

			Storyboard.SetTarget(colour, element.Background);
			Storyboard.SetTargetProperty(colour, "Color");
			storyboard.Children.Add(colour);
		}

		storyboard.Begin();
	}
}
