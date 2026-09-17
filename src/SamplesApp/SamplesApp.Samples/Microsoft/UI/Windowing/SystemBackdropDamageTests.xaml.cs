using System;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using SamplesApp;
using Uno.UI.Samples.Controls;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "SystemBackdrop ghosting (damage)", IsManualTest = true,
	Description =
		"Repro for the ghosting seen on Skia once Window.SystemBackdrop is set. " +
		"The backdrop transparentizes the visual tree, so the retained damage layer carries per-pixel " +
		"alpha and only the damaged region is redrawn into it; blitting that layer over a recycled " +
		"swapchain drawable with source-over lets the frame the drawable still holds show through. " +
		"The moving square must stay crisp: ghost copies trailing it, or the window flickering, is the bug. " +
		"'Opaque plate' puts an opaque Rectangle fill behind the square — ghosting cannot happen where " +
		"the layer has no alpha, so the artifact should disappear over the plate and nowhere else.")]
public sealed partial class SystemBackdropDamageTests : Page
{
	private const double BoxSize = 64;

	private Storyboard _storyboard;
	private double _travel;
#if HAS_UNO
	private bool _overlayWasEnabled;
#endif

	public SystemBackdropDamageTests()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
#if HAS_UNO
		_overlayWasEnabled = global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay;
		OverlayToggle.IsOn = _overlayWasEnabled;
#else
		OverlayToggle.IsEnabled = false;
#endif

		// The layer only carries alpha while a backdrop is set, so apply one up front: without it
		// source-over and replace are indistinguishable and there is nothing to observe.
		SetBackdrop(new MicaBackdrop { Kind = MicaKind.Base });
		RestartAnimation();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		StopAnimation();

		// Both the backdrop and the overlay outlive the page, so leave the window as it was found.
		App.MainWindow.SystemBackdrop = null;
#if HAS_UNO
		global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay = _overlayWasEnabled;
#endif
	}

	private void OnSetMica(object sender, RoutedEventArgs e) => SetBackdrop(new MicaBackdrop { Kind = MicaKind.Base });

	private void OnSetAcrylic(object sender, RoutedEventArgs e) => SetBackdrop(new DesktopAcrylicBackdrop());

	private void OnClearBackdrop(object sender, RoutedEventArgs e) => SetBackdrop(null);

	private void OnAnimateToggled(object sender, RoutedEventArgs e) => RestartAnimation();

	private void OnPlateToggled(object sender, RoutedEventArgs e)
		=> OpaquePlate.Visibility = PlateToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;

	private void OnOverlayToggled(object sender, RoutedEventArgs e)
	{
#if HAS_UNO
		global::Uno.UI.FeatureConfiguration.Rendering.DamageRegionOverlay = OverlayToggle.IsOn;
#endif
	}

	private void OnAnimationHostSizeChanged(object sender, SizeChangedEventArgs e)
	{
		// Restarting snaps the square back to the left edge, so only do it when the travel really changed.
		if (Math.Abs(GetTravel() - _travel) > 1)
		{
			RestartAnimation();
		}
	}

	private void SetBackdrop(SystemBackdrop backdrop)
	{
		App.MainWindow.SystemBackdrop = backdrop;
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		var name = App.MainWindow.SystemBackdrop switch
		{
			MicaBackdrop mica => $"Mica ({mica.Kind})",
			null => "None",
			var other => other.GetType().Name,
		};

		var isSupported = Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported();

		StatusText.Text = $"Backdrop: {name} — MicaController.IsSupported(): {isSupported}";
	}

	private double GetTravel() => Math.Max(0, AnimationHost.ActualWidth - BoxSize);

	private void RestartAnimation()
	{
		StopAnimation();

		_travel = GetTravel();
		if (!AnimateToggle.IsOn || _travel <= 0)
		{
			return;
		}

		// A small square crossing a large window keeps the per-frame damage far smaller than the
		// window, which is what leaves most of the stale drawable visible when the blit blends.
		var animation = new DoubleAnimation
		{
			From = 0,
			To = _travel,
			Duration = new Duration(TimeSpan.FromSeconds(1.2)),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
		};

		Storyboard.SetTargetProperty(animation, nameof(TranslateTransform.X));
		Storyboard.SetTarget(animation, MovingBoxTransform);

		_storyboard = new Storyboard
		{
			Children = { animation }
		};

		_storyboard.Begin();
	}

	private void StopAnimation()
	{
		_storyboard?.Stop();
		_storyboard = null;
		MovingBoxTransform.X = 0;
	}
}
