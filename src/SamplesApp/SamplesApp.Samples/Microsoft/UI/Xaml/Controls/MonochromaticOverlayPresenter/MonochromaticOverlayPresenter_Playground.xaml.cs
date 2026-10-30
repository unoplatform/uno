using System;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI;

namespace UITests.Windows_UI_Xaml_Controls.MonochromaticOverlayPresenterTests;

[Sample(
	"Pickers",
	Name = "MonochromaticOverlayPresenter_Playground",
	Description = "Interactive MonochromaticOverlayPresenter: pick the source content, ReplacementColor, presenter background and size, "
		+ "and move, scroll or animate the source to see the overlay follow it live.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class MonochromaticOverlayPresenter_Playground : Page
{
	private readonly DispatcherTimer _animationTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
	private DateTimeOffset _animationStart;

	public MonochromaticOverlayPresenter_Playground()
	{
		this.InitializeComponent();

		ListItems.ItemsSource = Enumerable.Range(1, 50).Select(i => $"List item {i}").ToArray();

		_animationTimer.Tick += OnAnimationTick;
		Unloaded += (s, e) => _animationTimer.Stop();
		Loaded += (s, e) =>
		{
			if (AnimateToggle.IsOn)
			{
				StartAnimation();
			}
		};

		// The SelectionChanged handlers already ran inside InitializeComponent, before the named elements were assigned.
		ApplySourceKind();
		ApplyOverlayBackground();
		UpdateStatus();
	}

	private void OnSourceKindChanged(object sender, SelectionChangedEventArgs e) => ApplySourceKind();

	private void ApplySourceKind()
	{
		if (TextContent is null)
		{
			return;
		}

		var index = SourceKindCombo.SelectedIndex;
		TextContent.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
		IconsContent.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
		ShapesContent.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
		ListContent.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
	}

	private void OnReplacementColorPresetChanged(object sender, SelectionChangedEventArgs e)
		=> SetReplacementColor(ReplacementColorCombo.SelectedIndex switch
		{
			1 => Colors.Black,
			2 => Colors.Red,
			3 => Colors.Gold,
			4 => Colors.Transparent,
			_ => Colors.White,
		});

	private void OnCustomColorChanged(ColorPicker sender, ColorChangedEventArgs args) => SetReplacementColor(args.NewColor);

	private void SetReplacementColor(Color color)
	{
		if (Overlay is null)
		{
			return;
		}

		Overlay.ReplacementColor = color;
		UpdateStatus();
	}

	private void OnOverlayBackgroundChanged(object sender, SelectionChangedEventArgs e) => ApplyOverlayBackground();

	private void ApplyOverlayBackground()
	{
		if (Overlay is null)
		{
			return;
		}

		Overlay.Background = OverlayBackgroundCombo.SelectedIndex switch
		{
			1 => (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"],
			2 => new SolidColorBrush(Color.FromArgb(0xFF, 0x20, 0x20, 0x20)),
			3 => new SolidColorBrush(Colors.LightBlue),
			_ => null,
		};
	}

	private void OnOverlaySizeChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (Overlay is null || OverlayWidthSlider is null || OverlayHeightSlider is null)
		{
			return;
		}

		Overlay.Width = OverlayWidthSlider.Value;
		Overlay.Height = OverlayHeightSlider.Value;
		UpdateStatus();
	}

	private void OnSourceOffsetChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		if (SourceTranslate is null || AnimateToggle?.IsOn == true)
		{
			return;
		}

		ApplySourceOffset(SourceOffsetXSlider?.Value ?? 0, SourceOffsetYSlider?.Value ?? 0);
	}

	private void ApplySourceOffset(double x, double y)
	{
		SourceTranslate.X = x;
		SourceTranslate.Y = y;
	}

	private void OnAnimateToggled(object sender, RoutedEventArgs e)
	{
		if (AnimateToggle.IsOn)
		{
			StartAnimation();
		}
		else
		{
			_animationTimer.Stop();
			ApplySourceOffset(SourceOffsetXSlider.Value, SourceOffsetYSlider.Value);
		}
	}

	private void StartAnimation()
	{
		_animationStart = DateTimeOffset.Now;
		_animationTimer.Start();
	}

	private void OnAnimationTick(object sender, object e)
	{
		var t = (DateTimeOffset.Now - _animationStart).TotalSeconds;
		ApplySourceOffset(Math.Sin(t * 1.3) * 110, Math.Sin(t * 0.9) * 50);
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		AnimateToggle.IsOn = false;
		SourceKindCombo.SelectedIndex = 0;
		ReplacementColorCombo.SelectedIndex = 0;
		OverlayBackgroundCombo.SelectedIndex = 1;
		OverlayWidthSlider.Value = 260;
		OverlayHeightSlider.Value = 36;
		SourceOffsetXSlider.Value = 0;
		SourceOffsetYSlider.Value = 0;
		ListContent.ChangeView(null, 0, null, disableAnimation: true);
	}

	private void UpdateStatus()
	{
		if (StatusText is null || Overlay is null)
		{
			return;
		}

		var c = Overlay.ReplacementColor;
		StatusText.Text = $"ReplacementColor = #{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}   Size = {Overlay.Width:0} x {Overlay.Height:0}";
	}
}
